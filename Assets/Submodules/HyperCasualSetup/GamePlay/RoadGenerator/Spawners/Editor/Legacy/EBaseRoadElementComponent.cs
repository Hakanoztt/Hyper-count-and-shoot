using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator {

    [CustomEditor(typeof(BaseRoadElementComponent))]
    public class EBaseRoadElementComponent : EComponentDefinition {

        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as BaseRoadElementComponent.Data, this);
        }

        public class Editor : EditableElement<BaseRoadElementComponent.Data>, EIRoadElement {

            public Editor(BaseRoadElementComponent.Data component, EComponentDefinition editor) : base(component, editor) {}

            BaseRoadElementComponent.Data EIRoadElement.DataObjectT => DataObjectT;

            public override Vector3 Position {
                get {
                    return RoadElementEditorHelper.ToWorld(ParentPose, base.Position);
                }
                set {
                    base.Position = RoadElementEditorHelper.ToLocal(ParentPose, value);
                }
            }

            public override Quaternion Rotation {
                get {
                    return RoadElementEditorHelper.ToWorld(ParentPose, base.Rotation);
                }
                set {
                    base.Rotation = RoadElementEditorHelper.ToLocal(ParentPose, value);
                }
            }

            private Pose ParentPose {
                get {
                    return RoadElementEditorHelper.GetParentPose(ElementEditor, DataObjectT.parentIndex, DataObjectT.roadGenerator);
                }
            }

            public override void DrawGUILayout() {
                base.DrawGUILayout();
                ParentField();
                if (GUILayout.Button("Snap to closest piece")) {
                    RoadElementEditorHelper.SnapToClosestPiece(ElementEditor, DataObjectT, 0);
                }
            }

            void ParentField() {
                Vector3 pos = Position;
                Quaternion rot = Rotation;
                RoadElementEditorHelper.ParentField(ElementEditor, DataObjectT, ref pos, ref rot);
                Position = pos;
                Rotation = rot;
            }

            public override bool SceneGUI(in SceneParams @params) {
                bool enabled = @params.selected;
                bool edited = false;
                var matrix = ElementEditor.BeginMatrix(@params.matrix);

                var pp = ParentPose.position;
                var relativeParent = Matrix4x4.Inverse(@params.matrix).MultiplyPoint3x4(pp);
                Handles.DrawLine(Vector3.zero, relativeParent);

                ElementEditor.EndMatrix(matrix);
                return enabled && edited;
            }
        }
    }
}
