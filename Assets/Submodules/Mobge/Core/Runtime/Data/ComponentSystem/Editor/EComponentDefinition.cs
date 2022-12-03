using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Serialization;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core {
    [CustomEditor(typeof(ComponentDefinition), true)]
    public class EComponentDefinition : Editor {

        private static Dictionary<ComponentDefinition, EComponentDefinition> _editors = new Dictionary<ComponentDefinition, EComponentDefinition>();
        private static Dictionary<ComponentDefinition, ComponentDefinition> _temporaryInspectorDrawHolders = new Dictionary<ComponentDefinition, ComponentDefinition>();
        
        public virtual EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new EditableElement(dataObject, this);
        }
        public virtual ElementEditor.GlobalComponentSettingsField GetOptionsGUI() {
            return null;
        }
        public override void OnInspectorGUI() {
            var _labelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            EditorGUILayout.LabelField("Icon for the component should be 32x32 pixel.", _labelStyle);
            EditorGUILayout.LabelField("Import setting: Texture Type should be \"Sprite (2D and UI)\"", _labelStyle);
            base.OnInspectorGUI();
        }
        public class EditableElement<T> : EditableElement where T : BaseComponent {
            public EditableElement(T component, EComponentDefinition editor) : base(component, editor) {
            }
            public new T DataObjectT {
                get {
                    return (T)base.DataObjectT;
                }
                set => base.DataObjectT = value;
            }
        }
        public class EditableElement : AEditableElement<BaseComponent> {
            protected internal Piece piece;
            protected internal LevelComponentData componentData;
            protected internal Level level;
            private EComponentDefinition _editor;

            public EComponentDefinition Editor => _editor;


            public override Texture2D IconTexture => ((ComponentDefinition)(_editor.target)).icon;
            public EditableElement(BaseComponent component, EComponentDefinition editor) : base(editor.target) {
                DataObjectT = component;
                _editor = editor;
            }

            public override Vector3 Position {
                get => ObjectPosition;
                set {
                    if (!ObjectPosition.Equals(value)) {
                        ObjectPosition = value;
                        UpdateData();
                    }
                }
            }
            public Vector3 ObjectPosition { get => DataObjectT.position; set => DataObjectT.position = value; }

            public override string Name => componentData.Definition.ToString();

            public override Transform PrefabReference {
                get {
                    var r = DataObjectT.PrefabReference;
                    return r;
                }
            }
            public sealed override bool Delete() {
                return piece.Components.RemoveByValue(componentData);
            }
            public sealed override void InspectorGUILayout() {
                EditorGUI.BeginChangeCheck();
                DrawGUILayout();
                if (EditorGUI.EndChangeCheck()) {
                    UpdateData();
                }    
            }
            public virtual void DrawGUILayout() {
                var def = componentData.Definition;
                var tmpHolder = GetTempHolder(def);
                var ef = tmpHolder.EditorValueField;
                ef.SetValue(tmpHolder, DataObjectT);
                var so = new SerializedObject(tmpHolder);
                var sp = so.FindProperty(ef.Name);
                EditorGUI.BeginChangeCheck();
                sp.isExpanded = true;
                EditorGUILayout.PropertyField(sp, new GUIContent("value"), true);
                if (EditorGUI.EndChangeCheck()) {
                    so.ApplyModifiedProperties();
                    DataObjectT = (BaseComponent)ef.GetValue(tmpHolder);
                }
                so.Dispose();
            }
            private static ComponentDefinition GetTempHolder(ComponentDefinition def) {
                if (_temporaryInspectorDrawHolders.TryGetValue(def, out var val)) {
                    if (val != null) {
                        return val;
                    }
                    _temporaryInspectorDrawHolders.Remove(def);
                }
                var holder = def.Clone();
                if (holder == null) {
                    Debug.LogError("Component Definition Clone Error!!!");
                    return null;
                }
                _temporaryInspectorDrawHolders.Add(def, holder);
                return holder;
            }
            public override void UpdateData() {
                base.UpdateData();
                RecordObject(piece, "edit");
                componentData.SetObject(DataObject);
                EditorExtensions.SetDirty(piece);
            }
        }
        internal static void AddButtons(Mobge.ElementEditor elementEditor, Level level, Piece piece) {
            var componentDescriptions = AssetDatabase.FindAssets("t:" + typeof(ComponentDefinition).ToString());
            //Debug.Log( typeof(ComponentDefinition).ToString());
            //Debug.Log(componentDescriptions.Length);
            for (int i = 0; i < componentDescriptions.Length; i++) {
                var def = AssetDatabase.LoadAssetAtPath<ComponentDefinition>(AssetDatabase.GUIDToAssetPath(componentDescriptions[i]));
                if (def != null) {
                    if (!typeof(BaseComponent).IsAssignableFrom(def.DataType)) {
                        Debug.LogError(nameof(ComponentDefinition.DataType) + " field has to be a type that extends " + typeof(BaseComponent) + "(" + def + ").", def);
                    }
                    else {
                        var editor = CreateEditor(def) as EComponentDefinition;
                        if (editor == null) {
                            Debug.LogError("Editors of classes that extends " + typeof(ComponentDefinition) + " has to extend " + typeof(EComponentDefinition) + "(" + def + ").", def);
                        }
                        else {
                            elementEditor.AddButtonData(new Mobge.ElementEditor.NewButtonData(def.name, def.menuPriority, () => {
                                var obj = NewDefaultBaseComponent(def);
                                var element = CreateComponent(obj, def);
                                var id = piece.Components.AddElement(element);
                                var e = CreateEditor(editor, level, piece, element, obj, id);
                                return e;
                            }, def, editor.GetOptionsGUI()));
                        }
                    }
                }
            }
        }
        private static BaseComponent NewDefaultBaseComponent(ComponentDefinition def) {
            var ef = def.EditorValueField;
            var obj = (BaseComponent)ef.GetValue(def);
            // return obj; //Uncomment this to edit component definitions on creation
            var da = BinarySerializer.Instance.Serialize(ef.FieldType, obj);
            var ds = (BaseComponent)BinaryDeserializer.Instance.Deserialize(da, ef.FieldType);
            return ds;
            
            // BaseComponent dataObject = Activator.CreateInstance(def.DataType) as BaseComponent;
            // var ef = def.EditorValueField;
            // BaseComponent defaultValue = (BaseComponent)ef.GetValue(def);
            // var destProperties = dataObject.GetType().GetFields();
            // foreach (var field in destProperties) {
            //     if (!field.IsNotSerialized) {
            //         field.SetValue(dataObject, field.GetValue(defaultValue));
            //     }
            // }     
            // return dataObject;
        }
        private static LevelComponentData CreateComponent(object obj, ComponentDefinition definition) {
            LevelComponentData data = new LevelComponentData(definition, obj);
            return data;
        }
        private static EditableElement CreateEditor(EComponentDefinition componentEditor, Level level, Piece piece, LevelComponentData data, BaseComponent obj, int id) {
            var editor = (EditableElement)componentEditor.CreateEditorElement(obj);
            editor.piece = piece;
            editor.level = level;
            editor.componentData = data;
            editor.Id = id;
            return editor;
        }
        internal static void UpdateElements(Mobge.ElementEditor elementEditor, Level level, Piece.PieceRef selectedPiece) {
            if (selectedPiece.piece.Components == null) {
                selectedPiece.piece.Components = new Piece.LevelComponentMap();
            }
            var e = selectedPiece.piece.Components.GetPairEnumerator();
            while (e.MoveNext()) {
                var c = e.Current;
                var comp = c.Value;
                int key = c.Key;
                if(!_editors.TryGetValue(comp.Definition, out EComponentDefinition editor)) {
                    editor = CreateEditor(comp.Definition) as EComponentDefinition;
                    _editors.Add(comp.Definition, editor);
                }
                BaseComponent dataObject;
                try {
                    dataObject = comp.GetObject<BaseComponent>();
                }
                catch (Exception ex){
                    dataObject = null;
                    Debug.LogError(ex);
                    BinaryDeserializer.Instance.Reset();
                }
                if (dataObject == null) {
                    Debug.Log(comp.Definition,comp.Definition);
                    dataObject = NewDefaultBaseComponent(comp.Definition);
                }
                elementEditor.AddElement(CreateEditor(editor, level, selectedPiece.piece, comp, dataObject, key));
            }
        }
    }
}
