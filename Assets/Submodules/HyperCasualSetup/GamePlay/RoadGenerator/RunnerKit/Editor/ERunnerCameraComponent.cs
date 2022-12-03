using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {
    [CustomEditor(typeof(RunnerCameraComponent))]
    public class ERunnerCameraComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as RunnerCameraComponent.Data, this);
        }
        public class Editor : EditableElement<RunnerCameraComponent.Data> {
            private bool _apply = false;
            public Editor(RunnerCameraComponent.Data component, EComponentDefinition editor) : base(component, editor) { }
            public override void DrawGUILayout() {
                base.DrawGUILayout();

                _apply = EditorGUILayout.Toggle("apply scene camer", _apply);
            }
            private void SetSceneCameraPosition() {
                var cMain = Camera.main;
                var mat = Handles.matrix;
                var pose = ElementEditor.GetPose(this);
                if (cMain is { }) {
                    Transform tr;
                    (tr = cMain.transform).position = mat.MultiplyPoint3x4(pose.position);
                    tr.rotation = mat.rotation * pose.rotation;
                    //cMain.fieldOfView = DataObjectT.fov;
                }
            }
            public override bool SceneGUI(in SceneParams @params) {
                bool enabled = @params.solelySelected;
                bool edited = false;

                var temp = ElementEditor.BeginMatrix(@params.matrix);

                var pose = new Pose(Handles.matrix.GetPosition(), Handles.matrix.rotation);
                DrawCameraPreview(DataObjectT, pose, enabled, Camera.main);

                ElementEditor.EndMatrix(temp);
                if (_apply) {
                    SetSceneCameraPosition();
                }
                // Handles.matrix = oldMatrix;
                return enabled && edited;
            }
            private static RenderTexture _texture;
            private static void DrawCameraPreview(BaseComponent component, Pose pose, bool editing, Camera cameraRes) {
                if (cameraRes) {
                    var teo = TemporaryEditorObjects.Shared;
                    var iCam = teo.EnsureObject(component, cameraRes.transform).GetComponent<Camera>();

                    if (iCam && editing) {
                        iCam.gameObject.SetActive(true);
                        iCam.transform.position = pose.position;
                        iCam.transform.rotation = pose.rotation;

                        Handles.BeginGUI();
                        if (_texture == null) {
                            _texture = new RenderTexture(200, 200, 24);
                        }
                        iCam.targetTexture = _texture;
                        iCam.Render();
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
        }
    }
}
             