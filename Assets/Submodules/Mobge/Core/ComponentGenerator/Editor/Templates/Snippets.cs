using System.IO;
using System.Text;
using UnityEngine;

namespace Mobge.Core.ComponentGenerator {
    public static class Snippets {
        private const string EditorScriptPrefix = "E";

        public static void Generate(string nameSpace, string componentName) {
            GenerateComponent(nameSpace, componentName);
            GenerateComponentEditor(nameSpace, componentName);
        }

        private static void GenerateComponent(string nameSpace, string componentName) {
            string template =
$@"using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace {nameSpace} {{
    public class {componentName} : ComponentDefinition<{componentName}.Data> {{
        [Serializable]
        public class Data : BaseComponent {{
            //public override LogicConnections Connections {{ get => connections; set => connections = value; }}
            //[SerializeField] [HideInInspector] private LogicConnections connections;
            //private Dictionary<int, BaseComponent> _components;

            //private LevelPlayer _player;

            public override void Start(in InitArgs initData) {{
                //_player = initData.player;
                //_components = initData.components;
                
                //string value = ""example argument value"";
                //Connections.InvokeSimple(this, 0, value, _components);
            }}
            //public override object HandleInput(ILogicComponent sender, int index, object input) {{
            //    switch (index) {{
            //        case 0:
            //            return ""example output"";
            //    }}
            //    return null;
            //}}
        #if UNITY_EDITOR
            //public override void EditorInputs(List<LogicSlot> slots) {{
            //    slots.Add(new LogicSlot(""example input"", 0));
            //}}
            //public override void EditorOutputs(List<LogicSlot> slots) {{
            //    slots.Add(new LogicSlot(""example output"", 0));
            //}}
        #endif
        }}
    }}
}}
            ";

            var path = ProjectWindow.GetSelectedPathOrFallback();
            var filename = componentName + ".cs";
            var filePath = Path.Combine(path, filename);
            TryWriteScript(filePath, template, $"GenerateComponent: {componentName} Component exists.");
        }
        
        private static void GenerateComponentEditor(string nameSpace, string componentName) {
            string template = 
$@"using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace {nameSpace} {{
    [CustomEditor(typeof({componentName}))]
    public class E{componentName} : EComponentDefinition {{
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {{
            return new Editor(dataObject as {componentName}.Data, this);
        }}
        public class Editor : EditableElement<{componentName}.Data> {{
            //private bool _editMode = false;
            public Editor({componentName}.Data component, EComponentDefinition editor) : base(component, editor) {{ }}
            // public override void DrawGUILayout() {{
            //     base.DrawGUILayout();
            //     _editMode = ExclusiveEditField(""Edit On Scene"");
            //     if (GUILayout.Button(""Example Button"")) {{
            //         Debug.Log(""Button Does Stuff"");
            //     }}
            //     GUILayout.Label(""Example Label"");
            // }}
            // public override bool SceneGUI(in SceneParams @params) {{
            //     bool enabled = @params.selected;
            //     bool edited = false;
            //     var matrix = ElementEditor.BeginMatrix(@params.matrix);
            //     // always on scene gui code goes here
            //     if (_editMode) {{
            //         //edit mode scene gui code goes here
            //     }}
            //     ElementEditor.EndMatrix(matrix);
            //     return enabled && edited;
            // }}
        }}
    }}
}}
             ";


            var path = Path.Combine(ProjectWindow.GetSelectedPathOrFallback(), "Editor");
            var filename = EditorScriptPrefix + componentName + ".cs";
            var filePath = Path.Combine(path, filename);
            Directory.CreateDirectory(path);
            TryWriteScript(filePath, template,
                $"GenerateComponentEditor: {componentName} Component editor exists.");
        }

        private static void TryWriteScript(string filePath, string template, string failMessage) {
            if (!File.Exists(filePath))
                File.WriteAllText(filePath, template);
            else {
                Debug.LogError(failMessage);
            }
        }
    }
}