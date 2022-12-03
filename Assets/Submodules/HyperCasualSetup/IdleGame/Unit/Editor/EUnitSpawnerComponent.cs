using Mobge.Core;
using Mobge.Core.Components;
using System;
using UnityEditor;
using UnityEngine;

namespace Mobge.IdleGame {
    [CustomEditor(typeof(UnitSpawnerComponent))]
    public class EUnitSpawnerComponent : EComponentDefinition {
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as UnitSpawnerComponent.Data, this);
        }
        public class Editor : EPrefabSpawnerComponent.PSEditor {
            //private bool _editMode = false;
            public Editor(UnitSpawnerComponent.Data component, EComponentDefinition editor) : base(component, editor) { }

            public new UnitSpawnerComponent.Data DataObjectT {
                get => (UnitSpawnerComponent.Data)base.DataObjectT;
            }

            public override void DrawGUILayout() {
                base.DrawGUILayout();
                //_editMode = ExclusiveEditField("Edit On Scene");
                var data = DataObjectT;
                if (data.res != null) {
                    var unit = data.res.GetComponent<IUnit>();
                    if(unit == null) {
                        EditorGUILayout.HelpBox("This component has extra features when used with " + typeof(IUnit) + " component.", MessageType.Warning);
                    }
                    else {
                        int rankCount = unit.RankCount;
                        
                        if(data.ranks == null) {
                            data.ranks = new UnitSpawnerComponent.Data.RankData[rankCount];
                        }
                        else if(data.ranks.Length != rankCount){
                            Array.Resize(ref data.ranks, rankCount);
                            GUI.changed = true;
                        }
                    }
                }
            }
            // public override bool SceneGUI(in SceneParams @params) {
            //     bool enabled = @params.selected;
            //     bool edited = false;
            //     var matrix = ElementEditor.BeginMatrix(@params.matrix);
            //     // always on scene gui code goes here
            //     if (_editMode) {
            //         //edit mode scene gui code goes here
            //     }
            //     ElementEditor.EndMatrix(matrix);
            //     return enabled && edited;
            // }
        }
    }
}
             