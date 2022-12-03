using Mobge.Core.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    public class ECameraComponentData {

        private static GUIStyle s_textStyle;


        private static RenderTexture _texture;
        public static void DrawCameraPreview(BaseComponent component, Pose worldPose, bool editing, Camera cameraRes) {
            if (cameraRes) {
                var teo = TemporaryEditorObjects.Shared;
                var iCam = teo.EnsureObject(component, cameraRes.transform).GetComponent<Camera>();

                if (iCam && editing) {
                    iCam.gameObject.SetActive(true);
                    iCam.transform.position = worldPose.position;
                    iCam.transform.rotation = worldPose.rotation;

                    iCam.targetTexture = _texture;
                    iCam.Render();
                    Handles.BeginGUI();
                    if (_texture == null) {
                        _texture = new RenderTexture(200, 200, 24);
                    }
                    GUILayout.Window(2, new Rect(10, 50, 200, 200), id => {
                        var r = GUILayoutUtility.GetRect(200, 200);
                        EditorGUI.DrawPreviewTexture(r, _texture);
                        //GUI.DrawTexture();
                    }, "Preview");
                    Handles.EndGUI();
                }
                else {
                    if (iCam) {
                        iCam.gameObject.SetActive(false);
                    }
                }
            }
        }

        public static GUIStyle TextStyle {
            get {
                if (s_textStyle == null) {
                    s_textStyle = new GUIStyle("Box");
                    s_textStyle.normal.textColor = Color.yellow;
                    Texture2D t = new Texture2D(3, 3);
                    Color[] cs = new Color[t.width * t.height];
                    for (int i = 0; i < cs.Length; i++) {
                        cs[i] = Color.red;
                    }
                    t.SetPixels(0, 0, 3, 3, cs);
                    t.Apply();
                    s_textStyle.normal.scaledBackgrounds = new Texture2D[] { t };
                    //GUI.backgroundColor = Color.yellow;
                }
                return s_textStyle;
            }
        }
        public static bool SceneGUI(ElementEditor elementEditor, in Matrix4x4 currentSpace, CameraManager.ICamera component, bool updateSceneCamera, bool drawPreview) {
            // currentSpace = @params.matrix
            // drawPreview = @params.solelySelected
            var tm = elementEditor.BeginMatrix(currentSpace);
            var cameraPos = component.Data.offset;


            Handles.color = Color.blue;
            Handles.Label(Vector3.zero, "Camera\nTarget", TextStyle);
            Handles.color = Color.white;


            DrawLine(cameraPos.position - new Vector3(0, 0.3f, 0), cameraPos.position + new Vector3(0, 0.3f, 0));
            DrawLine(cameraPos.position, cameraPos.position + cameraPos.rotation * new Vector3(0, 0, 0.15f));


            if (updateSceneCamera) {
                SetSceneCameraPosition(cameraPos);
            }


            Pose worldCameraPose;
            worldCameraPose.position = Handles.matrix.MultiplyPoint3x4(cameraPos.position);
            worldCameraPose.rotation = Handles.matrix.rotation * cameraPos.rotation;
            DrawCameraPreview((BaseComponent)component, worldCameraPose, drawPreview, Camera.main);

            elementEditor.EndMatrix(tm);
            return false;
        }

        private static void SetSceneCameraPosition(Pose pose) {
            var cMain = Camera.main;
            var mat = Handles.matrix;
            if (cMain is { }) {
                Transform tr;
                (tr = cMain.transform).position = mat.MultiplyPoint3x4(pose.position);
                tr.rotation = mat.rotation * pose.rotation;
                //cMain.fieldOfView = DataObjectT.fov;
            }
        }

        private static void DrawLine(Vector3 p1, Vector3 p2) {
            Handles.color = Color.black;
            Handles.DrawLine(p1, p2, 5f);
            Handles.color = Color.white;
            Handles.DrawLine(p1, p2, 1.5f);
            Handles.color = Color.white;
        }
    }
}