using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.IdleGame.CustomerShopSystem
{
    [CustomEditor(typeof(ShopSpawnerComponent))]
    public class EShopSpawnerComponent : EComponentDefinition
    {
        public override EditableElement CreateEditorElement(BaseComponent dataObject)
        {
            return new Editor(dataObject as ShopSpawnerComponent.Data, this);
        }
        public class Editor : EUnitSpawnerComponent.Editor
        {
            public Editor(ShopSpawnerComponent.Data component, EComponentDefinition editor) : base(component, editor) { }
            public new ShopSpawnerComponent.Data DataObjectT
            {
                get => (ShopSpawnerComponent.Data)base.DataObjectT;
            }
            //private bool _editMode = false;

            public override void DrawGUILayout()
            {
                base.DrawGUILayout();

                var data = DataObjectT;
                if (data.res != null)
                {
                    var unit = data.res.GetComponent<IUnit>();
                    if (unit != null)
                    {
                        int rankCount = unit.RankCount;

                        if (data.ranks != null)
                        {
                            if (data.customerLimit == null)
                            {
                                data.customerLimit = new int[rankCount];
                            }

                            if (data.customerRate == null)
                            {
                                data.customerRate = new float[rankCount];
                            }

                            if (data.ranks.Length != data.customerLimit.Length)
                            {
                                System.Array.Resize(ref data.customerLimit, rankCount);
                                System.Array.Resize(ref data.customerRate, rankCount);
                                GUI.changed = true;
                            }
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
