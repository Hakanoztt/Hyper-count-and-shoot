#!/bin/bash
# In Cloud Build, edit the Advanced Settings for the build target for which you want to run the script, and add the relative path to the "Post Build Script Path" entry.
function json_extract() {
  local key=$1
  local json=$2

  local string_regex='"([^"\]|\\.)*"'
  local number_regex='-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?'
  local value_regex="${string_regex}|${number_regex}|true|false|null"
  local pair_regex="\"${key}\"[[:space:]]*:[[:space:]]*(${value_regex})"

  if [[ ${json} =~ ${pair_regex} ]]; then
    echo $(sed 's/^"\|"$//g' <<< "${BASH_REMATCH[1]}")
  else
    return 1
  fi
}

API_TOKEN="sslFa0d8PIfV7S304K7GcuSvMDE7IGyz8TyHGvRpo9"
EMAIL="mobgebuild@gmail.com"
DISCORD_WEBHOOK='https://discordapp.com/api/webhooks/748532450581413949/cn4_DK3jdbhlH5GYjH9-m_L6Mgy5FohTsHu3-Mar5pN7_h5Q3TpmPXQ8Wuz4YDloG7RF'

echo "starting cloud build post build script"

# upload artifact to diawi
artifact_path=$(find "$2" -name *.ipa -o -name *.aab)
upload_response=$(curl --http1.1 https://upload.diawi.com/ \
-F token=${API_TOKEN} \
-F file=@"${artifact_path}" \
-F comment="${commit_id}" \
-F callback_emails=${EMAIL})
echo "upload response: ${upload_response}"

# get download link
job_id=$(json_extract job $upload_response)
echo "job id: ${job_id}"
job_id=$(echo $job_id | sed 's/"//g') 
echo "job id unquoted: ${job_id}"
start_time="$(date -u +%s)"
echo "start time: ${start_time}"
while :
do
	sleep 10
	response=$(curl --http1.1 https://upload.diawi.com/status?token="$API_TOKEN"\&job="$job_id")
	echo "upload link fetch response: ${response}"
	status_code=$(json_extract status $response)
	echo "status code: ${status_code}"
	link_address=$(json_extract link $response)
	echo "link address: ${link_address}"
	if test "$status_code" -eq 2000
	then
		break
	fi
	end_time="$(date -u +%s)"
	echo "end time: ${end_time}"
	elapsed="$(($end_time-$start_time))"
	echo "elapsed: ${elapsed}"
	if test $elapsed -gt 360
	then 
	    break
	fi
done

#remove quotes from link address
link_address=$(echo "$link_address" | sed 's/"//g')
echo "link address: ${link_address}"

# get project name
git_path=$(dirname "$1")
echo "git path: ${git_path}"
game_name=$(git -C "$git_path" config --local remote.origin.url|sed -n 's#.*/\([^.]*\)\.git#\1#p')
echo "game name: ${game_name}"

# get commit id
commit_id=$(git -C "$git_path" rev-parse HEAD)
echo "commit id: ${commit_id}"

# get version info
basic_version=$(cat "$git_path"/Assets/Temp/MobgeVersion/mobge_basic_version.txt)
echo "basic version: ${basic_version}"
full_version=$(cat "$git_path"/Assets/Temp/MobgeVersion/mobge_full_version.txt)
echo "full version: ${full_version}"

# send discord message
message="\n$game_name "
if [[ $artifact_path == *.aab ]]
then
	message+="Android "
else
	message+="IOS "
fi
message+="Build"
message+="\nBasic Version: $basic_version"
message+="\nFull Version: $full_version"
message+="\nCommit ID: $commit_id"
message+="\nLink: $link_address"
echo "message: ${message}"

response=$(curl --http1.1 -H "Content-Type: application/json" -X POST -d "{\"content\": \"$message\"}" $DISCORD_WEBHOOK)
echo "response: ${response}"

echo "end cloud build post build script"
