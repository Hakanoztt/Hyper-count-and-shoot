using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Mobge.Animation;

namespace Mobge.Platformer.Character {
    [CustomEditor(typeof(CharacterMappings))]
    public class ECharacterMappings : Editor
    {
        private CharacterMappings _go;
        private bool _modulesOpen;
        private int _modules = -1, _splits = -1, _attachs = -1;
        private AnimationSpliterDrawer _splitDrawer;
        private AnimationSplitterAttribute _splitterAttribute;
        protected void OnEnable() {
            _go = target as CharacterMappings;
            _splitDrawer = new AnimationSpliterDrawer();
            _splitterAttribute = new AnimationSplitterAttribute();
        }
        public override void OnInspectorGUI() {
            if(!_go) {
                return;
            }
            _go.scheme = EditorLayoutDrawer.ObjectField("scheme", _go.scheme, false);
            var scheme = _go.scheme;
            if(!scheme) {
                EditorGUILayout.LabelField("Please set a scheme to see settings.");
                EditorGUILayout.LabelField("You can create a scheme from create asset menu of unity.");                
            }
            else {
                ArrayField("Modules", scheme.modules, serializedObject.FindProperty(nameof(CharacterMappings.modules)), (r, p, i, sch) => {
                    
                }, ref _modules);
                ArrayField("Animation splits", scheme.animations, serializedObject.FindProperty(nameof(CharacterMappings.animations)), (r, property, i, sch) => {
                    if(sch!=null){
                        _splitterAttribute.constantIndexNames = sch.sectionNames;
                        _splitDrawer.Attribute = _splitterAttribute;
                    }
                    var trackP = property.FindPropertyRelative(nameof(CharacterMappings.AimationSplit.track));
                    EditorGUI.PropertyField(r.NextRect(), trackP);
                    var p = property.FindPropertyRelative(nameof(CharacterMappings.AimationSplit.spliter));
                    var label =  new GUIContent(p.name);
                    var height = _splitDrawer.GetPropertyHeight(p, label);
                    _splitDrawer.OnGUI(r.NextRect(height) ,p, label);
                }, ref _splits);
                ArrayField("Animation attachments", scheme.animationAttachments, serializedObject.FindProperty(nameof(CharacterMappings.animationAttachments)), (r, p, i, sch) => {
                    
                }, ref _attachs);
            }
            serializedObject.ApplyModifiedProperties();
            if(GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
        private int Count(SerializedProperty elements, int hash) {
            int c = 0;
            for(int i  = 0; i < elements.arraySize; i++) {
                if(elements.GetArrayElementAtIndex(i).FindPropertyRelative("hash").intValue == hash) c++;
            }
            return c;
        }
        private void ArrayField<Mapping>(string label, Mapping[] schemes, SerializedProperty property, Action<LayoutRectSource,SerializedProperty,int, Mapping> field, ref int selectedIndex) where Mapping : CharacterMappingScheme.Mapping {
            EditorLayoutDrawer.CustomCollectionField(property, (r, p, index) => {
                var hash = p.FindPropertyRelative("hash");
                Mapping sch;
                hash.intValue = EditorDrawer.Popup(r.NextRect(), "name", schemes, hash.intValue, (scheme) => {
                   return scheme.Hash; 
                }, out sch);
                var c = Count(property, hash.intValue);
                if(c > 1) {
                    var errorRect = r.NextRect();
                    EditorGUI.DrawRect(errorRect, Color.yellow);
                    EditorGUI.LabelField(errorRect, "Multiple elements cannot share the same name.");
                }
                field(r, p, index, sch);
            });
        }
    }
    [CustomPropertyDrawer(typeof(CharacterMappingAttribute))]
    [CustomPropertyDrawer(typeof(AnimationSplitDurations))]
    public class CharacterMappingAttributeDrawer : BasePropertyDrawer {
        static LayoutRectSource _layout = new LayoutRectSource();
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            _layout.Reset(position);
            var obj = property.serializedObject.targetObject as StateModuleData;
            if(obj == null) {
                EditorGUI.LabelField(_layout.NextRect(), typeof(CharacterMappingAttribute) + " can only be used in subclasses of " + typeof(StateModuleData));
            }
            else {
                if(obj.mappingScheme == null) {
                    EditorGUI.LabelField(_layout.NextRect(), label.text + ": "  + nameof(StateModuleData.mappingScheme) + " must not be null.");
                }
                else {
                    CharacterMappingAttribute att = attribute as CharacterMappingAttribute;
                    IList<CharacterMappingScheme.Mapping> content;
                    if(att != null) {
                        switch(att.map) {
                            default:
                            case CharacterMappingAttribute.Mapping.Animation:
                            content = obj.mappingScheme.animations;
                            break;
                            case CharacterMappingAttribute.Mapping.AnimationAttachment:
                            content = obj.mappingScheme.animationAttachments;
                            break;
                            case CharacterMappingAttribute.Mapping.Module:
                            content = obj.mappingScheme.modules;
                            break;
                        }
                        property.intValue = EditorDrawer.Popup(_layout.NextRect(), label.text, content, property.intValue, (mapping)=> mapping.Hash);
                    }
                    else if(property.type == nameof(AnimationSplitDurations)) {
                        property.FindPropertyRelative(nameof(AnimationSplitDurations.durations));
                        var id = property.FindPropertyRelative(nameof(AnimationSplitDurations.splitterId));
                        CharacterMappingScheme.AnimationMapping splitMapping;
                        id.intValue = EditorDrawer.Popup(_layout.NextRect(), label.text, obj.mappingScheme.animations, id.intValue, (split) => split.Hash, out splitMapping);
                        if(splitMapping != null) {
                            var durations = property.FindPropertyRelative(nameof(AnimationSplitDurations.durations));
                            durations.arraySize = splitMapping.sectionNames.Length;
                            for(int i = 0; i < splitMapping.sectionNames.Length; i++) {
                                var duration = durations.GetArrayElementAtIndex(i);
                                duration.floatValue = EditorGUI.FloatField(_layout.NextRect(), splitMapping.sectionNames[i], duration.floatValue);
                            }
                        }
                    }
                }
            }
            SetHeight(property, _layout.Height);
        }
    }
}
