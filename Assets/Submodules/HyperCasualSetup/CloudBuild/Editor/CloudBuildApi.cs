using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mobge.Build {
    public class CloudBuildApi {
        public Dictionary<string, BuildTarget> buildTargets = new Dictionary<string, BuildTarget>();
        public class BuildTarget {
            public string targetId;
            public string targetName;
            public string buildNumber;
            public string commitId;
            public BuildStatus buildStatus;
            public enum BuildStatus {
                Unknown,
                Queued,
                Building,
                Canceled,
                Success,
                Failed,
            }
            public string downloadLink;
            public long buildStartTime;
            public Color Color {
                get {
                    switch (buildStatus) {
                        case BuildStatus.Queued:   return Color.yellow;
                        case BuildStatus.Building: return Color.yellow;
                        case BuildStatus.Canceled: return Color.white;
                        case BuildStatus.Success:  return Color.green;
                        case BuildStatus.Failed:   return Color.red;
                        case BuildStatus.Unknown:  return Color.white;
                        default:                   return Color.white;
                    }
                }
            }
            public void SetBuildStatus(string status) {
                switch (status) {
                    case "queued":        buildStatus = BuildStatus.Queued;   break;
                    case "sentToBuilder": buildStatus = BuildStatus.Building; break;
                    case "started":       buildStatus = BuildStatus.Building; break;
                    case "restarted":     buildStatus = BuildStatus.Building; break;
                    case "success":       buildStatus = BuildStatus.Success;  break;
                    case "failure":       buildStatus = BuildStatus.Failed;   break;
                    case "canceled":      buildStatus = BuildStatus.Canceled; break;
                    case "unknown":       buildStatus = BuildStatus.Unknown;  break;
                    default:              buildStatus = BuildStatus.Unknown;  break;
                }
            }
            public bool QueuedOrBuilding => buildStatus == BuildStatus.Queued | buildStatus == BuildStatus.Building;
        }
        private CloudBuildQueueStatus _cloudBuildQueueStatus = new CloudBuildQueueStatus();
        private CloudBuildSettings _settings = new CloudBuildSettings();
        private bool _readyToDrawGui = false;
        private string _projectId;
        private HttpClient _client;
        private HttpClient _serviceHttpClient;
        private object _threadLock = new object();
        public class CloudBuildSettings {
            #pragma warning disable 649
                public string apiRoot;
                public string apiKey;
                public string organizationId;
                public string gitUrl;
                public string projectName;
                public string bundleId;
                public string iosCredentialId;
                public string androidCredentialId;
            #pragma warning restore 649
        }
        public CloudBuildApi() {
            if (!File.Exists(".unitycloudbuildsettings")) {
                Debug.LogError(".unitycloudbuildsettings file could not be found.");
                return;
            }
            var json = File.ReadAllText(".unitycloudbuildsettings");
            EditorJsonUtility.FromJsonOverwrite(json, _settings);
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", $"Basic {_settings.apiKey}");
            _serviceHttpClient = new HttpClient();
            _serviceHttpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {_settings.apiKey}");
            RefreshData((() => {
                _readyToDrawGui = true;
            }));
        }
        public bool IsReadyToDrawGUI => _readyToDrawGui;
        public bool IsCloudBuildProjectCreated => _projectId != null;
        public void CreateCloudBuildProject() {
            _readyToDrawGui = false;
            var requestJson = @" 
                {
                    'name': 'project_name', 
                    'settings': {
                        'scm': {
                            'type': 'git', 
                            'url': 'git_url'
                        }
                    }
                }
                ".Replace("\'", "\"")
                .Replace("project_name", _settings.projectName)
                .Replace("git_url", _settings.gitUrl);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects";
            var result = _client.PostAsync(url, content).Result;
            if (result.StatusCode != HttpStatusCode.Created) {
                Debug.LogError("Cloud build could not create project!");
                return;
            }
            var resultBody= result.Content.ReadAsStringAsync().Result;
            var json = JSON.Parse(resultBody);
            _projectId = json["guid"].Value;
            requestJson = @"
                {
                    'name': 'iOS',
                    'platform': 'ios',
                    'enabled': true,
                    'settings': {
                        'autoBuild': false,
                        'unityVersion': 'latest2020_3',
                        'platform': {
                            'bundleId': 'bundle_id',
                            'xcodeVersion': 'latest'
                        },
                        'scm': {
                            'branch': 'main'
                        },
                        'autoBuildCancellation': false,
                        'advanced': {
                            'unity': {
                                'preExportMethod': 'Mobge.Build.CloudBuild.OnBeforeBuild',
                                'postBuildScript': 'Assets/Submodules/HyperCasualSetup/CloudBuild/Editor/CloudBuildPostBuildScript.sh'
                            }
                        }
                    },
                    'credentials': {
                        'signing': {
                            'credentialid': 'ios_credential_id'
                        }
                    }
                }
                ".Replace("\'", "\"")
                .Replace("bundle_id", _settings.bundleId)
                .Replace("ios_credential_id", _settings.iosCredentialId);
            content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects/{_projectId}/buildtargets";
            result = _client.PostAsync(url, content).Result;
            if (result.StatusCode != HttpStatusCode.Created) {
                Debug.LogError("Cloud build could not create ios build target!");
                Debug.LogError(result.Content.ReadAsStringAsync().Result);
            }
            requestJson = @"
                {
                    'name': 'Android',
                    'platform': 'android',
                    'enabled': true,
                    'settings': {
                        'autoBuild': false,
                        'unityVersion': 'latest2020_3',
                        'platform': {
                            'bundleId': 'bundle_id'
                        },
                        'scm': {
                            'branch': 'main'
                        },
                        'autoBuildCancellation': false,
                        'advanced': {
                            'unity': {
                                'preExportMethod': 'Mobge.Build.CloudBuild.OnBeforeBuild',
                                'postBuildScript': 'Assets/Submodules/HyperCasualSetup/CloudBuild/Editor/CloudBuildPostBuildScript.sh'
                            }
                        }
                    },
                    'credentials': {
                        'signing': {
                            'credentialid': 'android_credential_id'
                        }
                    }
                }
                ".Replace("\'", "\"")
                .Replace("bundle_id", _settings.bundleId)
                .Replace("android_credential_id", _settings.androidCredentialId);
            content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects/{_projectId}/buildtargets";
            result = _client.PostAsync(url, content).Result;
            if (result.StatusCode != HttpStatusCode.Created) {
                Debug.LogError("Cloud build could not create android build target!");
                Debug.LogError(result.Content.ReadAsStringAsync().Result);
            }
            RefreshData((() => {
                _readyToDrawGui = true;
            }));
        }
        public void RefreshData(Action onCompleted) {
            new Thread(RefreshDataThreadFunction).Start(onCompleted);
        }
        private void RefreshDataThreadFunction(object onCompleted) {
            lock (_threadLock) {
                try {
                    FetchDataSynchronously();
                }
                catch (Exception) { /* ignored */ }
                finally {
                    EditorApplication.delayCall += () => {
                        ((Action) onCompleted)?.Invoke();
                    };
                }
            }
        }
        private void FetchDataSynchronously() {
            if (_projectId == null) {
                _projectId = GetProjectId();
                if (_projectId == null) {
                    return;
                }
            }
            FetchQueueStatus();
            var buildTargetsIds = GetBuildTargets();
            foreach (var buildTargetId in buildTargetsIds) {
                if (!buildTargets.ContainsKey(buildTargetId)) {
                    buildTargets.Add(buildTargetId, new BuildTarget());
                }
                var buildData = GetBuildTargetLastBuildData(buildTargetId);
                var buildTargetStatus = buildTargets[buildTargetId];
                var statusString = buildData["buildStatus"].Value;
                buildTargetStatus.targetId = buildTargetId;
                buildTargetStatus.SetBuildStatus(statusString);
                buildTargetStatus.targetName = buildData["buildTargetName"].Value;
                buildTargetStatus.buildNumber = buildData["build"].Value;
                buildTargetStatus.downloadLink = buildData["links"]["download_primary"]["href"].Value;
                var commitIdString = buildData["lastBuiltRevision"].Value;
                if (!string.IsNullOrEmpty(commitIdString)) commitIdString = commitIdString.Substring(0, 10);
                buildTargetStatus.commitId = commitIdString;
                var buildStartString = buildData["buildStartTime"].Value;
                if (!string.IsNullOrEmpty(buildStartString)) {
                    var buildStartTime = DateTime.ParseExact(buildStartString.Substring(0, 19),
                        "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                    buildTargetStatus.buildStartTime = buildStartTime.Ticks;
                }
            }
        }
        public CloudBuildQueueStatus GetCachedCloudBuildQueueStatus() {
            return _cloudBuildQueueStatus;
        }
        public void PrintBuildLogToConsole(BuildTarget buildTarget) {
            var log = GetBuildLogString(buildTarget);
            using (var reader = new StringReader(log)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    if (line.IndexOf("error", 0, StringComparison.CurrentCultureIgnoreCase) != -1) {
                        Debug.LogError(line);
                    }
                    else {
                        Debug.Log(line);
                    }
                }
            }
        }
        public void DownloadBuildLog(BuildTarget buildTarget) {
            var path = EditorUtility.SaveFilePanel(nameof(CloudBuildWindow), "Logs", "CloudBuildLog", "txt");
            if (string.IsNullOrWhiteSpace(path)) return;
            Debug.Log($"saving build log to {path}");
            var log = GetBuildLogString(buildTarget);
            File.WriteAllText(path, log, Encoding.Unicode);
            Process.Start(path);
        }
        private string GetBuildLogString(BuildTarget buildTarget) {
            var url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects/{_projectId}/buildtargets/{buildTarget.targetId}/builds/{buildTarget.buildNumber}/log";
            var result = _client.GetAsync(url).Result;
            var log = result.Content.ReadAsStringAsync().Result;
            return log;
        }
        public void DownloadBuildArtifact(BuildTarget buildTarget) {
            if (string.IsNullOrEmpty(buildTarget.downloadLink)) return;
            Process.Start(buildTarget.downloadLink);
        }
        public void StartNewBuild(BuildTarget buildTarget, bool clean = false) {
            if (!EditorUtility.DisplayDialog("Cloud Build",$"You are going to start an {buildTarget.targetId} build. Are you sure?", "Yes", "Cancel")) {
                return;
            }
            var url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects/{_projectId}/buildtargets/{buildTarget.targetId}/builds";
            var boolString = clean ? "true" : "false";
            var jsonPostString = $"{{\"clean\": {boolString}}}";
            var content = new StringContent(jsonPostString, Encoding.UTF8, "application/json");
            var result = _client.PostAsync(url, content).Result;
            var resultString = result.Content.ReadAsStringAsync().Result;
            var json = JSON.Parse(resultString);
            if (result.StatusCode != HttpStatusCode.Accepted) {
                Debug.LogError("Build Start Failed!");
                return;
            }
            _cloudBuildQueueStatus.jobs.Add(new CloudBuildJob(){projectName = _settings.projectName});
            buildTarget.buildStatus = BuildTarget.BuildStatus.Queued;
            buildTarget.buildNumber = json[0]["build"].Value;
            buildTarget.commitId = "";
            buildTarget.downloadLink = "";
            buildTarget.buildStartTime = 0;
        }
        public void CancelBuild(BuildTarget buildTarget) {
            if (string.IsNullOrEmpty(buildTarget.targetId)) return;
            if (string.IsNullOrEmpty(buildTarget.buildNumber)) return;
            if (!EditorUtility.DisplayDialog("Cloud Build",$"You are going to cancel the {buildTarget.targetId} build. Are you sure?", "Yes", "Cancel")) {
                return;
            }
            var url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects/{_projectId}/buildtargets/{buildTarget.targetId}/builds/{buildTarget.buildNumber}";
            var result = _client.DeleteAsync(url).Result;
            if (result.StatusCode != HttpStatusCode.NoContent) {
                Debug.LogError("Cancel Cloud Build Failed!");
                return;
            }
            _cloudBuildQueueStatus.jobs.RemoveAt(_cloudBuildQueueStatus.jobs.FindIndex((job => job.projectName == _settings.projectName)));
            buildTarget.buildStatus = BuildTarget.BuildStatus.Canceled;
        }
        private string GetProjectId() {
            var url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects";
            var result = _serviceHttpClient.GetAsync(url).Result;
            if (result.StatusCode != HttpStatusCode.OK) {
                return null;
            }
            var resultString = result.Content.ReadAsStringAsync().Result;
            var json = JSON.Parse(resultString);
            var projectCount = json.Count;
            for (int i = projectCount - 1; i >= 0; i--) {
                var projectJson = json[i];
                if (projectJson["name"].Value == _settings.projectName) {
                    return projectJson["guid"].Value;
                }
            }
            return null;
        }
        private JSONNode GetBuildTargetLastBuildData(string buildTarget) {
            var url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects/{_projectId}/buildtargets/{buildTarget}/builds";
            var parameters = $"?per_page=1&page=1";
            var result = _serviceHttpClient.GetAsync(url+parameters).Result;
            var resultString = result.Content.ReadAsStringAsync().Result;
            return JSON.Parse(resultString)[0];
        }
        private void FetchQueueStatus() {
            var url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/builds";
            var parameters = $"?orgid={_settings.organizationId}&per_page=10&page=1&buildStatus=queued,sentToBuilder,started,restarted,unknown";
            var result = _serviceHttpClient.GetAsync(url+parameters).Result;
            var resultString = result.Content.ReadAsStringAsync().Result;
            var json = JSON.Parse(resultString);
            _cloudBuildQueueStatus.Parse(json);
        }
        private List<string> GetBuildTargets(){
            var url = $"{_settings.apiRoot}/orgs/{_settings.organizationId}/projects/{_projectId}/buildtargets";
            var result = _serviceHttpClient.GetAsync(url).Result;
            var resultString = result.Content.ReadAsStringAsync().Result;
            var json = JSON.Parse(resultString);
            var targetCount = json.Count;
            var list = new List<string>();
            for (int i = 0; i < targetCount; i++) {
                var targetId = json[i]["buildtargetid"].Value;
                list.Add(targetId);
            }
            return list;
        }
        public class CloudBuildQueueStatus {
            public List<CloudBuildJob> jobs = new List<CloudBuildJob>();
            public void Parse(JSONNode builds){
                jobs.Clear();
                for (int i = 0; i < builds.Count; i++) {
                    var buildData = builds[i];
                    jobs.Add(new CloudBuildJob() {
                        projectName = buildData["projectName"],
                        buildTargetName = buildData["buildTargetName"],
                        buildNumber = buildData["build"],
                        buildStatus = buildData["buildStatus"],
                    });
                }
            }
        }
        public class CloudBuildJob {
            public string projectName;
            public string buildTargetName;
            public string buildNumber;
            public string buildStatus;
        }
    }
}

