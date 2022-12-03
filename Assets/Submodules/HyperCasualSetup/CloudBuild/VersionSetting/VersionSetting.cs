using System;
using System.Globalization;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class VersionSetting : ScriptableObject
{
	private static string baseURI = "https://versionidentifier.mobge.net/";

	private static string CombineURI(string[] @params) {
		return @params.Aggregate(baseURI, (current, s) => current + s);
	}
	public static VersionSetting GetNewSetting(Action<bool, VersionSetting> onSettingsFetched) {
		Debug.Log($"...Requesting new patch version...");
		var setting = Resources.Load<VersionSetting>("MobgeVersionSetting");
		if (setting == null) {
			setting = CreateInstance<VersionSetting>();
		}

		var bundleId = Application.identifier;
		var version = setting.FullVersionString;

		var uri = CombineURI(new [] {
			"request-version?",
			$"bundleId={bundleId}",
			"&",
			$"currentVersion={version}"
		});
		var request = UnityWebRequest.Get(uri);
		var op = request.SendWebRequest();
		op.completed += r => {
			if (r.isDone) {
				var resSuccess = op.webRequest.result == UnityWebRequest.Result.Success;
				if (resSuccess) {
					var jsonNode = JSON.Parse(op.webRequest.downloadHandler.text);
					var majorVersion = jsonNode[1][0];
					var minorVersion = jsonNode[1][1];
					var patchVersion = jsonNode[1][2];
					setting.majorVersion = int.Parse(majorVersion);
					setting.minorVersion = int.Parse(minorVersion);
					setting.patchVersion = int.Parse(patchVersion);
					onSettingsFetched(true, setting);
				} else {
					setting.patchVersion += 1;
					onSettingsFetched(false, setting);
				}
			}
		};

		return setting == null ? CreateInstance<VersionSetting>() : setting;
	}

	public static VersionSetting GetCurrentSetting(Action<bool, VersionSetting> result) {
		var setting = Resources.Load<VersionSetting>("MobgeVersionSetting");
		if (setting == null) {
			setting = CreateInstance<VersionSetting>();
		}

		var bundleId = Application.identifier;

		var uri = CombineURI(new [] {
			"/request-current-version?",
			$"bundleId={bundleId}"
		});
		var request = UnityWebRequest.Get(uri);
		var op = request.SendWebRequest();
		op.completed += r => {
			if (r.isDone) {
				var resSuccess = op.webRequest.result == UnityWebRequest.Result.Success;
				if (resSuccess) {
					var jsonNode = JSON.Parse(op.webRequest.downloadHandler.text);
					var majorVersion = jsonNode[0];
					var minorVersion = jsonNode[1];
					var patchVersion = jsonNode[2];
					setting.majorVersion = int.Parse(majorVersion);
					setting.minorVersion = int.Parse(minorVersion);
					setting.patchVersion = int.Parse(patchVersion);
					result(true, setting);
				} else {
					setting.patchVersion += 1;
					result(false, setting);
				}
			}
		};

		return setting == null ? CreateInstance<VersionSetting>() : setting;
	}

	[SerializeField] private int majorVersion = 1;
	[SerializeField] private int minorVersion;
	[SerializeField] private int patchVersion;
	[SerializeField] [Obsolete] private int minorVersionOffset;
	[Obsolete]
	private string DateNumber {
		get {
			var startDate = new DateTime(2020, 11, 05);
			var endDate = DateTime.UtcNow;
			return (((int)(endDate - startDate).TotalDays) + minorVersionOffset).ToString(CultureInfo.InvariantCulture);
		}
	}
	[Obsolete]
	private string HourMinuteNumber => DateTime.UtcNow.ToString("HHmm");
	public string MajorVersion => majorVersion.ToString();
	public string MinorVersion => minorVersion.ToString();
	public string PatchVersion => patchVersion.ToString();
	public string FullVersionString => $"{MajorVersion}.{MinorVersion}.{PatchVersion}";
	public string BasicVersionString => $"{MajorVersion}.{MinorVersion}";
}
