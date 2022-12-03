using System;
using UnityEditor;
using UnityEngine;

namespace Mobge.Build {
    public class CloudBuildWindow : EditorWindow {
        [MenuItem("Mobge/Cloud Build", false)]
        [MenuItem("File/Cloud Build", false, 220)]
        public static void OpenWindow() {
            var window = (CloudBuildWindow)GetWindow(typeof(CloudBuildWindow), false, "Cloud Build");
            window.Show();
        }
        private Vector2 _scrollStatus = Vector2.zero;
        private long _lastRefreshTime = 0;
        private CloudBuildApi _cloudBuildApi;
        private void OnEnable() {
            if (_cloudBuildApi == null) _cloudBuildApi = new CloudBuildApi();
        }
        private void OnGUI() {
            if (!_cloudBuildApi.IsReadyToDrawGUI) {
                GUILayout.Label("Loading...");
                return;
            }
            if (!_cloudBuildApi.IsCloudBuildProjectCreated) {
                GUILayout.Label("Unity cloud build project has not been created.");
                GUILayout.Label("Please create project using this button.");
                if (GUILayout.Button("Create Cloud Build Project")) {
                    _cloudBuildApi.CreateCloudBuildProject();
                }
                return;
            }
            using (Scopes.ScrollView(ref _scrollStatus)) {
                var queueStatus = _cloudBuildApi.GetCachedCloudBuildQueueStatus();
                GUILayout.Label("Build Queue");
                foreach (var job in queueStatus.jobs) {
                    GUILayout.Label($"{job.projectName} {job.buildTargetName} {job.buildNumber} {job.buildStatus}");
                }
                foreach (var buildTarget in _cloudBuildApi.buildTargets.Values) {
                    if (string.IsNullOrEmpty(buildTarget.targetId)) continue;
                    using (Scopes.GUIColor(buildTarget.Color)) {
                        using (Scopes.Vertical(EditorStyles.helpBox)) {
                            GUILayout.Label($"{(buildTarget.targetId+" "+buildTarget.buildNumber).PadRight(20)} {buildTarget.buildStatus}     {buildTarget.commitId}");
                            if (buildTarget.buildStartTime > 10 && buildTarget.QueuedOrBuilding) {
                                var currentTicks = DateTime.UtcNow.Ticks;
                                var tickDifference = currentTicks - buildTarget.buildStartTime;
                                var elapsedMinutes = (int)(tickDifference / 600000000);
                                GUILayout.Label($"Building For {elapsedMinutes} Minutes");
                            }
                            using (Scopes.GUIColor(Color.white)) {
                                if (buildTarget.QueuedOrBuilding) {
                                    if (GUILayout.Button("Cancel Build")) {_cloudBuildApi.CancelBuild(buildTarget); RefreshData();}
                                } else {
                                    if (GUILayout.Button("Start New Fast Build")) { _cloudBuildApi.StartNewBuild(buildTarget, false); RefreshData(); }
                                    if (GUILayout.Button("Start New Clean Build")) { _cloudBuildApi.StartNewBuild(buildTarget, true); RefreshData(); }
                                }
                                if (GUILayout.Button("Download Build Artifact")) {_cloudBuildApi.DownloadBuildArtifact(buildTarget);}
                                if (GUILayout.Button("Print Build Log To Console")) {_cloudBuildApi.PrintBuildLogToConsole(buildTarget);}
                                if (GUILayout.Button("Download Build Log")) {_cloudBuildApi.DownloadBuildLog(buildTarget);}
                            }
                        }
                    }
                }
            }
        }
        private void Update() {
            var timeDifference = DateTime.UtcNow.Ticks - _lastRefreshTime;
            var secondsPassed = timeDifference / 10000000;
            if (secondsPassed > 30) {
                RefreshData();
            }
        }
        private void RefreshData() {
            Repaint();
            _lastRefreshTime = DateTime.UtcNow.Ticks;
            _cloudBuildApi?.RefreshData((() => {
                Repaint();
            }));
        }
    }
}
