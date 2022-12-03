using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(SliderJointComponent))]
    public class ESliderJointComponent : EComponentDefinition
    {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor((SliderJointComponent.Data)dataObject, this);
        }
        public class Editor : EditableElement<SliderJointComponent.Data>
        {
            public Editor(SliderJointComponent.Data component, ESliderJointComponent editor) : base(component, editor)
            {
            }
            
            public override void DrawGUILayout()
            {
                base.DrawGUILayout();
            }

            public override bool SceneGUI(in SceneParams @params)
            {
                var cs = GetConnections(0);
                Matrix4x4 mat;
                if(cs.MoveNext()) {
                    
                    var pose = cs.Current.elemenet.GetMatrix();
                    mat = pose;
                }
                else {
                    mat = @params.matrix;
                }
                mat = ElementEditor.BeginMatrix(mat);
                var comp = DataObjectT;
                Handles.color = Color.green;
                var rad = comp.angle * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                if(comp.UseLimits) {
                    Handles.DrawLine(-dir * comp.minLimit, -dir * comp.maxLimit);
                }
                else {
                    var s = HandleUtility.GetHandleSize(Vector3.zero) * 10;
                    Handles.DrawLine(-dir*s, dir*s);
                }

                ElementEditor.EndMatrix(mat);
                return false;
            }
        }
    }
}