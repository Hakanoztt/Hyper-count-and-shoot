using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mobge.Core.ComponentGenerator {
    public class MobgeComponentGeneratorWindow : EditorWindow {
        private const string ComponentNameKey = "MobgeComponentGenerator::ComponentNameKey";
        private const string ShouldRunKey = "MobgeComponentGenerator::ShouldRunKey";
        
        private string _nameSpace = "Mobge.Game";
        private string _componentName;
        private bool _focusAlreadyGained = false;
        private EditorTools _editorTools;

        [MenuItem("Assets/Create/Mobge/Mobge Component", priority = 0)]
        private static void Init() {
            var window = GetWindow<MobgeComponentGeneratorWindow>("Component Generator");
            window.maxSize = new Vector2(480f, 110f);
            window.minSize = window.maxSize;
            window.ShowPopup();
        }
        private void OnEnable() {
            _editorTools = new EditorTools();
            _editorTools.AddTool(new EditorTools.Tool("Add Element Menu Enter to Add") {
                activation = new EditorTools.ActivationRule() {
                    key = KeyCode.Return,
                },
                onPress = () => {
                    Create();
                    return true;
                },
            });
            
        }
        private void OnGUI() {
            _editorTools.OnSceneGUI();
            _nameSpace = EditorGUILayout.TextField("Namespace:", _nameSpace);
            GUI.SetNextControlName("component name input");
            _componentName = EditorGUILayout.TextField("Component Name:", _componentName);
            if (!_focusAlreadyGained) {
                _focusAlreadyGained = true;
                GUI.FocusControl("component name input");
            }
            EditorGUILayout.HelpBox("Component Asset will be generated after compilation finishes." + 
                                    "\nAfter pressing [Create] please be patient.", MessageType.Info);
            
            using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(_nameSpace) || string.IsNullOrEmpty(_componentName))) {
                if (GUILayout.Button("Create")) Create();
            }
        }
        private void Create() {
            // Automatically add Component postfix to script name if user did not include it.
            if (_componentName != null && !_componentName.Contains("Component")) {
                _componentName += "Component";
            }
                
            EditorPrefs.SetString(ComponentNameKey, _componentName);
            EditorPrefs.SetBool(ShouldRunKey, true);
                
            Snippets.Generate(_nameSpace, _componentName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Close();
        }
        /// <summary>
        /// After creating the component script (and editor) this piece of code runs and generates associated asset.
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded() {
            var shouldRun = EditorPrefs.GetBool(ShouldRunKey);
            if (!shouldRun) return;
            EditorPrefs.SetBool(ShouldRunKey, false);

            var componentName = EditorPrefs.GetString(ComponentNameKey);
            if (string.IsNullOrEmpty(componentName)) return;
            
            var componentAsset = CreateInstance(componentName);
            if (componentAsset == null) return;
            // Remove Component postfix from asset name
            if (componentName.Contains("Component")) {
                componentName = componentName.Replace("Component", "");
            }
            var componentAssetPath = Path.Combine(ProjectWindow.GetSelectedPathOrFallback(), componentName + ".asset");
            
            AssetDatabase.CreateAsset(componentAsset, componentAssetPath);
            AssetDatabase.SaveAssets();
        }
    }
}