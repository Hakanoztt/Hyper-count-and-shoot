using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Mobge.AdvancedScreenshot
{
    public class AdvancedScreenshotWindow : EditorWindow
    {
        Schemes data;
        bool paused = false;
        public const string ASSET_FOLDER = "Assets/Editor Default Resources/AssetBundles/";
        public const string ASSET_NAME = "AdvancedScreenShot";
        public const string OUTPUT_FOLDER_NAME = "Screenshots";
        private CanvasScaleBackups _backups;

        [MenuItem("Mobge/Advanced Screenshot")]
        static void init() {
            GetWindow<AdvancedScreenshotWindow>();
        }

        static string fullAssetPath {
            get { return ASSET_FOLDER + ASSET_NAME + ".asset"; }
        }

        Schemes.ResolutionOption selectedResolution;
        Schemes.Scheme selected;


        //  AnimBool resolutions;


        OriginalCameraLimits cameraLimits = new OriginalCameraLimits();
        private Vector2 scrollPosition;
        GameObject updater;
        Queue<Action> doOnUpdate;

        void OnEnable() {
            //var fullAssetPath = ASSET_FOLDER + ASSET_NAME + ".asset";
            data = AssetDatabase.LoadAssetAtPath<Schemes>(fullAssetPath);
            if (!data) {

                if (!AssetDatabase.IsValidFolder(ASSET_FOLDER)) {
                    Directory.CreateDirectory(ASSET_FOLDER);
                }
                data = CreateInstance<Schemes>();
                AssetDatabase.CreateAsset(data, fullAssetPath);
                AssetDatabase.SaveAssets();
                data = AssetDatabase.LoadAssetAtPath<Schemes>(fullAssetPath);
            }

            doOnUpdate = new Queue<Action>();
            // resolutions = new AnimBool();
        }


        private void OnDisable() {
            cameraLimits.Cam = null;
            if (updater != null) {
                updater.DestroySelf();
            }
        }

        Schemes.CameraLimits newCameraLimit() {
            return new Schemes.CameraLimits() {
                far = cameraLimits.Far,
                near = cameraLimits.Near,
                limitType = Schemes.CameraLimitType.RelativeToCamera,
                name = "{0}",
                dontUseAlpha = true,
            };
        }

        void vertical(Action guiLayout, GUIStyle style = null) {
            if (style == null) {
                style = GUIStyle.none;
            }
            EditorGUILayout.BeginVertical(style);
            guiLayout();
            EditorGUILayout.EndVertical();
        }
        void horizontal(Action guiLayout, GUIStyle style = null) {
            if (style == null) {
                style = GUIStyle.none;
            }
            EditorGUILayout.BeginHorizontal(style);
            guiLayout();
            EditorGUILayout.EndHorizontal();
        }
        private void OnGUI() {
            if (!Application.isPlaying) {
                EditorGUILayout.LabelField("press play to use the tool");
                return;
            }
            cameraLimits.Cam = (Camera.allCameras[0]);
            //Debug.Log("cam: " + cameraLimits.Cam);
            selected = null;



            if (GUILayout.Button("reset camera")) {
                var cam = cameraLimits.Cam;
                cameraLimits.Cam = null;
                cameraLimits.Cam = cam;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            vertical(() => {
                EditorGUILayout.LabelField("options");
                data.pauseKey = (KeyCode)EditorGUILayout.EnumPopup("pause key", data.pauseKey);
                data.screenshotKey = (KeyCode)EditorGUILayout.EnumPopup("screenshot key", data.screenshotKey);
            }, "Box");

            vertical(() => {
                EditorGUILayout.LabelField("schemes");
                //data.selectedSchemeIndex = EditorExtension.listField("slected scheme", data.schemes, data.selectedSchemeIndex);
                if (data.schemes == null || data.schemes.Length == 0) {
                    data.schemes = new Schemes.Scheme[1];
                    data.schemes[0] = NewScheme();
                    data.schemes[0].name = "all";
                    data.selectedSchemeIndex = 0;
                    //};
                }
                data.selectedSchemeIndex = EditorLayoutDrawer.Popup("selected scheme", data.schemes, data.selectedSchemeIndex);

                selected = data.selectedScheme;

                if (selected != null) {
                    selected.name = EditorGUILayout.TextField("name", selected.name);

                    selected.hasCameraPosition = EditorGUILayout.Toggle("has camera position", selected.hasCameraPosition);
                    if (selected.hasCameraPosition) {
                        selected.cameraPos = EditorGUILayout.Vector3Field("camera pos", selected.cameraPos);
                        if (GUILayout.Button("take current")) {
                            selected.cameraPos = cameraLimits.Cam.transform.root.position;
                        }
                    }
                    selected.disableCanvasses = EditorGUILayout.Toggle("disable canvasses", selected.disableCanvasses);

                    EditorLayoutDrawer.CustomArrayField("limits", ref selected.limits, (layout, l) => {
                        vertical(() => {
                            EditorGUI.BeginChangeCheck();
                            l.name = EditorGUI.TextField(layout.NextRect(),"name", l.name);
                            horizontal(() => {
                                l.near = EditorGUI.FloatField(layout.NextRect(), "near", l.near);
                                l.far = EditorGUI.FloatField(layout.NextRect(), "far", l.far);
                            });
                            l.dontUseAlpha = EditorGUI.Toggle(layout.NextRect(), "no alpha", l.dontUseAlpha);
                            l.limitType = (Schemes.CameraLimitType)EditorGUI.EnumPopup(layout.NextRect(), "type", l.limitType);
                            if (l.limitType == Schemes.CameraLimitType.RelativeToWorldPlane) {
                                //EditorGUI.BeginVertical(layout.NextRect(), "Box");
                                EditorGUI.LabelField(layout.NextRect(), "plane field");
                                l.worldPlane.normal = EditorGUI.Vector3Field(layout.NextRect(), "normal", l.worldPlane.normal);
                                l.worldPlane.distance = EditorGUI.FloatField(layout.NextRect(), "distance", l.worldPlane.distance);
                                //EditorGUILayout.EndVertical();
                            }
                            if (GUILayout.Button("show") || EditorGUI.EndChangeCheck()) {
                                applyCameraPos(selected);
                                l.apply(cameraLimits.Cam);
                            }
                        });
                        return l;
                    });



                    if (GUILayout.Button("X", GUILayout.Width(30))) {
                        if (EditorUtility.DisplayDialog("Deleting " + selected + "...", "Are you sure?", "YES", "NO")) {
                            ArrayUtility.RemoveAt(ref data.schemes, data.selectedSchemeIndex);
                        }
                    }
                }
                if (GUILayout.Button("new scheme")) {
                    data.selectedSchemeIndex = data.schemes.Length;
                    ArrayUtility.Add(ref data.schemes, NewScheme());
                }
            }, "Box");


            //EditorExtension.guiLayoutFadingfield(this.Repaint, resolutions, "resolutions", () => {

            //});

            selectedResolution = null;

            vertical(() => {
                EditorGUILayout.LabelField("resolution");
                if (data.resolutionOptions == null) {
                    data.resolutionOptions = new Schemes.ResolutionOption[0];
                }
                data.selectedResolutionIndex = EditorLayoutDrawer.Popup("selected resolution", data.resolutionOptions, data.selectedResolutionIndex);
                selectedResolution = data.selectedResolution;
                if (selectedResolution != null) {
                    selectedResolution.name = EditorGUILayout.TextField("name", selectedResolution.name);

                    selectedResolution.height = EditorGUILayout.IntField("height", selectedResolution.height);

                    EditorGUILayout.BeginHorizontal();

                    selectedResolution.width = EditorGUILayout.IntField("width", selectedResolution.width, GUILayout.Width(EditorGUIUtility.labelWidth + 60));


                    var calcWidth = width(selectedResolution);
                    EditorGUILayout.LabelField(calcWidth.ToString());

                    EditorGUILayout.EndHorizontal();

                    if (GUILayout.Button("X", GUILayout.Width(30))) {
                        if (EditorUtility.DisplayDialog("Deleting " + selectedResolution + "...", "Are you sure?", "YES", "NO")) {
                            ArrayUtility.RemoveAt(ref data.resolutionOptions, data.selectedResolutionIndex);
                        }
                    }




                }
                if (GUILayout.Button("new resolution")) {
                    ArrayUtility.Add(ref data.resolutionOptions, new Schemes.ResolutionOption() {
                        height = Screen.height,
                        name = "new resolution",
                    });
                }
            }, "Box");


            if (selectedResolution != null && selected != null) {
                if (GUILayout.Button("take screen shots")) {
                    takeScreenShots(selected, selectedResolution);
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUI.changed) {
                EditorUtility.SetDirty(data);
            }


            if (Application.isPlaying && updater == null) {
                updater = new GameObject("updater");
                updater.AddComponent<LateUpdateCallback>().onLateUpdate = update;
            }
        }

        private Schemes.Scheme NewScheme() {
            return new Schemes.Scheme() {
                name = "new scheme",
                limits = new Schemes.CameraLimits[] {
                        newCameraLimit(),
                    }

            };
        }
        int width(Schemes.ResolutionOption res) {

            int sw = cameraLimits.Cam.pixelWidth;
            int sh = cameraLimits.Cam.pixelHeight;

            return Mathf.RoundToInt(res.height * sw / (float)sh);
        }

        private void update(LateUpdateCallback obj) {
            if (doOnUpdate.Count > 0) {
                doOnUpdate.Dequeue()();
            }
            if (Input.GetKeyDown(data.pauseKey)) {
                //EditorApplication.isPaused = !EditorApplication.isPaused;
                paused = !paused;
                //TimeScaleManager.SetTimeScale(11, paused ? 0 : 1, 20);
                Time.timeScale = paused ? 0 : 1;

            }
            if (Input.GetKeyDown(data.screenshotKey)) {

                if (selectedResolution != null && selected != null) {
                    takeScreenShots(selected, selectedResolution);
                }
            }

        }
        void takeScreenShots(Schemes.Scheme scheme, Schemes.ResolutionOption res) {

            var cam = cameraLimits.Cam;
            int height = res.height;
            int width = res.width;

            float scaleFactor = height / (float)Screen.height;

            var projectPath = Directory.GetParent(Application.dataPath);
            var directoryPath = projectPath + "/" + OUTPUT_FOLDER_NAME + "/" + scheme.name;
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            RenderTexture t = null;
            Texture2D helperT = null;
            bool paused = EditorApplication.isPaused;
            EditorApplication.isPaused = false;
            doOnUpdate.Enqueue(() => {

                _backups.BackupScales();
                _backups.ApplyScales(scaleFactor);

                t = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);

                helperT = new Texture2D(width, height);
                //cam.clearFlags = CameraClearFlags.SolidColor;

                cam.targetTexture = t;

                Screen.SetResolution(width, height, false);



                applyCameraPos(scheme);

                if (scheme.disableCanvasses) {
                    var canvasses = FindObjectsOfType<Canvas>();
                    for (int i = 0; i < canvasses.Length; i++) {
                        canvasses[i].enabled = false;
                    }
                }


                //Debug.Log("directoryPath: " + directoryPath);

            });
            for (int i = 0; i < scheme.limits.Length; i++) {
                int index = i;
                doOnUpdate.Enqueue(() => {

                    var limit = scheme.limits[index];
                    limit.apply(cam);
                    var color = cam.backgroundColor;
                    if (limit.dontUseAlpha) {

                        color.a = 1;
                    }
                    else {
                        color.a =0;
                    }
                    cam.backgroundColor = color;
                    cam.Render();
                    var screenshotPath = directoryPath + "/" + limit.name + ".png";
                    screenshotPath = string.Format(screenshotPath, DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH'.'mm'.'ss"));
                    RenderTexture.active = t;

                    helperT.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    if (limit.dontUseAlpha) {
                        var colors = helperT.GetPixels();
                        for (int j = 0; j < colors.Length; j++) {
                            var c = colors[j];
                            c.a = 1.0f;
                            colors[j] = c;
                        }
                        helperT.SetPixels(colors);
                    }
                    helperT.Apply();

                    var bytes = helperT.EncodeToPNG();

                    File.WriteAllBytes(screenshotPath, bytes);

                    _backups.ApplyScales(1);
                    Debug.Log("screenshot saved to: " + screenshotPath);

                });
            }
            doOnUpdate.Enqueue(() => {

                cam.targetTexture = null;
                helperT.DestroySelf();
                t.DestroySelf();
                RenderTexture.active = null;
                EditorApplication.isPaused = paused;
            });

        }
        void applyCameraPos(Schemes.Scheme scheme) {
            if (scheme.hasCameraPosition) {
                cameraLimits.Cam.transform.root.position = scheme.cameraPos;
            }
        }
        private struct CanvasScaleBackups {
            private Dictionary<Canvas, float> _scales;
            public void BackupScales() {
                var canvases = FindObjectsOfType<Canvas>();
                if (_scales == null) {
                    _scales = new Dictionary<Canvas, float>();
                }
                else {
                    _scales.Clear();
                }
                for (int i = 0; i < canvases.Length; i++) {
                    _scales[canvases[i]] = canvases[i].scaleFactor;
                }
            }
            public void ApplyScales(float multiplayer) {
                if (_scales == null) {
                    return;
                }
                foreach (var item in _scales) {
                    if (item.Key != null) {
                        item.Key.scaleFactor = item.Value * multiplayer;
                    }
                }
            }
        }
        struct OriginalCameraLimits
        {
            private float far;
            private float near;
            private CameraClearFlags clearFlags;
            private Camera cam;

            public float Far { get { return far; } }
            public float Near { get { return near; } }
            public Camera Cam {
                get {
                    return cam;
                }
                set {
                    if (cam != value) {
                        if (cam != null) {
                            restoreCameraValues();
                        }
                        cam = value;
                        if (cam != null) {
                            this.far = cam.farClipPlane;
                            this.near = cam.nearClipPlane;
                            this.clearFlags = cam.clearFlags;
                        }
                    }
                }
            }
            public void restoreCameraValues() {
                cam.nearClipPlane = near;
                cam.farClipPlane = far;
                cam.clearFlags = clearFlags;
            }
        }

    }
}