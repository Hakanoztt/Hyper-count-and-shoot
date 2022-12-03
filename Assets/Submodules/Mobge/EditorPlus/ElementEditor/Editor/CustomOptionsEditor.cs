using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public partial class ElementEditor {
        private class CustomOptionsEditor
        {
            EditorFoldGroups _groups;
            ElementEditor _editor;
            LayoutRectSource _rects;
            public CustomOptionsEditor(ElementEditor editor){
                _editor = editor;
                _groups = new EditorFoldGroups(EditorFoldGroups.FilterMode.ShowOnlyIfFilter, "Box");
                _rects = new LayoutRectSource();
            }
            public void OnGUI() {
                UnityEditor.EditorGUILayout.BeginVertical("Box");
                _groups.GuilayoutField(CreateGroups, "options");
                UnityEditor.EditorGUILayout.EndVertical();
            }

            private void CreateGroups(EditorFoldGroups.Group obj)
            {
                for(int i = 0; i < _editor._newButtons.Count; i++){
                    var d = _editor._newButtons[i];
                    if(d.optionsGUI != null) {
                        obj.AddChild(d.name, () => {
                            _rects.ResetInLayout();
                            d.optionsGUI(_rects, _editor);
                            _rects.ConvertToLayout();
                        });
                    }
                }
            }
        }
    }
}