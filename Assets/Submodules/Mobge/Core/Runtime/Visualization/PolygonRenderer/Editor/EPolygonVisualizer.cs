using System.Collections.Generic;
using System.Linq;
using Mobge.Core.Components;
using UnityEditor;
using UnityEngine;

namespace Mobge {
    
    [CustomEditor(typeof(PolygonVisualizer), true)]
    public class EPolygonVisualizer : Editor {
        
        private PolygonVisualizer _mobgePolygonVisualizer;

        #region Drawing Inspector
        
        private List<Object> _previewPickerObjectCache;
        private readonly List<InspectorExtensions.PreviewPicker> _previewPickers = new List<InspectorExtensions.PreviewPicker>();
        
        private bool _showCorners = true;
        private bool _showEdges = true;
        private bool _showSettings = true;
        
        private static bool _isEditorPreviewOn = false;
        private static PolygonRenderer _previewPolygonRenderer;
        
        protected void OnEnable() {
            _mobgePolygonVisualizer = target as PolygonVisualizer;
            RebuildEditorCaches();
        }
        public override void OnInspectorGUI() {
            bool errorExist = DrawWarningBoxes();
            DrawMaterialPickers();
            if (errorExist) return;
            
            using (Scopes.Vertical("box")) {
                if (GUILayout.Button("Auto Map Sprites")) {
                    AutoMapSprites();
                    RebuildEditorCaches();
                }

                int i = 0;
                DrawCornerSelectors(ref i); 
                DrawEdgeSelectors(ref i);
                DrawSettings();
                DrawPreviewSettings();
            }
            if (GUI.changed) {
                EditorExtensions.SetDirty(_mobgePolygonVisualizer);
            }
        }
        private bool DrawWarningBoxes() {
            if (_mobgePolygonVisualizer.edgeMaterial != null) {
                if (_mobgePolygonVisualizer.edgeMaterial.mainTexture == null) {
                    EditorGUILayout.HelpBox("Edge material has to have a main texture", MessageType.Error);
                    return true;
                }
                string path = AssetDatabase.GetAssetPath(_mobgePolygonVisualizer.edgeMaterial.mainTexture);
                TextureImporter importer = (TextureImporter) AssetImporter.GetAtPath(path);
                if (importer.textureType != TextureImporterType.Sprite) {
                    EditorGUILayout.HelpBox("Make sure material texture is imported as \"Sprite (2D and UI)\"",
                        MessageType.Warning);
                    if (GUILayout.Button("Fix it.")) {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.SaveAndReimport();
                        RebuildEditorCaches();
                    }
                    return true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Multiple) {
                    EditorGUILayout.HelpBox("Make sure material textures Sprite mode is imported as \"Multiple\"",
                        MessageType.Warning);
                    if (GUILayout.Button("Fix it.")) {
                        importer.spriteImportMode = SpriteImportMode.Multiple;
                        importer.SaveAndReimport();
                        RebuildEditorCaches();
                    }
                    return true;
                }
            }
            return false;
        }
        private void DrawMaterialPickers() {
            using (Scopes.Vertical("box")) {
                EditorGUI.BeginChangeCheck();
                _mobgePolygonVisualizer.edgeMaterial = (Material) EditorGUILayout.ObjectField("Edge Material", _mobgePolygonVisualizer.edgeMaterial, typeof(Material), false);
                _mobgePolygonVisualizer.fillMaterial = (Material) EditorGUILayout.ObjectField("Fill Material", _mobgePolygonVisualizer.fillMaterial, typeof(Material), false);
                _mobgePolygonVisualizer.wallMaterial = (Material) EditorGUILayout.ObjectField("Wall Material", _mobgePolygonVisualizer.wallMaterial, typeof(Material), false);
                if (EditorGUI.EndChangeCheck()) {
                    RebuildEditorCaches();
                }
            }
        }
        private void RebuildEditorCaches() {
            var previewObjectCache = GetPreviewObjectsFromEdgeMaterial();
            if (previewObjectCache != null) {
                _previewPickerObjectCache = previewObjectCache.ToList();
                CreateSelectors();
            }
        }
        private void CreateSelectors() {
            var selectorCount =
                + _mobgePolygonVisualizer.topEdgeSprites?.Length
                + _mobgePolygonVisualizer.leftEdgeSprites?.Length
                + _mobgePolygonVisualizer.rightEdgeSprites?.Length
                + _mobgePolygonVisualizer.bottomEdgeSprites?.Length
                + _mobgePolygonVisualizer.topLeftCornerSprites?.Length
                + _mobgePolygonVisualizer.topRightCornerSprites?.Length
                + _mobgePolygonVisualizer.bottomLeftCornerSprites?.Length
                + _mobgePolygonVisualizer.bottomRightCornerSprites?.Length
                + _mobgePolygonVisualizer.topInnerLeftCornerSprites?.Length
                + _mobgePolygonVisualizer.topInnerRightCornerSprites?.Length
                + _mobgePolygonVisualizer.bottomInnerLeftCornerSprites?.Length
                + _mobgePolygonVisualizer.bottomInnerRightCornerSprites?.Length;
            _previewPickers.Clear();
            for (int i = 0; i < selectorCount; i++) {
                _previewPickers.Add(new InspectorExtensions.PreviewPicker());
            }
        }
        private void DrawCornerSelectors(ref int i) {
            if (_mobgePolygonVisualizer.edgeMaterial == null) return;
            _showCorners = InspectorExtensions.FoldoutFieldInTheBox(_showCorners, "Corners", true);
            if (!_showCorners) return;
            
            SpriteListSelectorField("Number of Top Left corners", "Top Left Corner", ref _mobgePolygonVisualizer.topLeftCornerSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of Top Right corners", "Top Right Corner", ref _mobgePolygonVisualizer.topRightCornerSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of Bottom Left corners", "Bottom Left Corner", ref _mobgePolygonVisualizer.bottomLeftCornerSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of Bottom Right corners", "Bottom Right Corner", ref _mobgePolygonVisualizer.bottomRightCornerSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of Top Inner Left corners", "Top Inner Left Corner", ref _mobgePolygonVisualizer.topInnerLeftCornerSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of Top Inner Right corners", "Top Inner Right Corner", ref _mobgePolygonVisualizer.topInnerRightCornerSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of Bottom Inner Left corners", "Bottom Inner Left Corner", ref _mobgePolygonVisualizer.bottomInnerLeftCornerSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of Bottom Inner Right corners", "Bottom Inner Right Corner", ref _mobgePolygonVisualizer.bottomInnerRightCornerSprites, _previewPickers, ref i);
        }
        private void DrawEdgeSelectors(ref int i) {
            if (_mobgePolygonVisualizer.edgeMaterial == null) return;
            _showEdges = InspectorExtensions.FoldoutFieldInTheBox(_showEdges, "Edges", true);
            if (!_showEdges) return;
            
            SpriteListSelectorField("Number of top edges", "Top Edge", ref _mobgePolygonVisualizer.topEdgeSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of bottom edges", "Bottom Edge", ref _mobgePolygonVisualizer.bottomEdgeSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of left edges", "Left Edge", ref _mobgePolygonVisualizer.leftEdgeSprites, _previewPickers, ref i);
            SpriteListSelectorField("Number of right edges", "Right Edge", ref _mobgePolygonVisualizer.rightEdgeSprites, _previewPickers, ref i);
        }
        private void SpriteListSelectorField(string arrayLengthLabelString, string selectorLabelString, ref Sprite[] spriteArray, List<InspectorExtensions.PreviewPicker> selectors, ref int selectorStartCount) {
            if (spriteArray == null) spriteArray = new Sprite[0];
            using (Scopes.GUIBackgroundColor(InspectorExtensions.EditorColors.PastelOrange)) {
                using (Scopes.Vertical("Box")) {
                    using (Scopes.GUIBackgroundColor(InspectorExtensions.EditorColors.Default)) {
                        EditorGUI.BeginChangeCheck();
                        int numberOfEdges = EditorGUILayout.DelayedIntField(arrayLengthLabelString, spriteArray.Length, InspectorExtensions.EditorStyles.BoldTextField);
                        if (EditorGUI.EndChangeCheck()) {
                            SetArraySize(ref spriteArray, numberOfEdges);
                            CreateSelectors();
                        }
                    }
                    for (int i = 0; i < spriteArray.Length; i++) {
                        using (Scopes.OnChangeDirty(_mobgePolygonVisualizer)) {
                            spriteArray[i] = (Sprite) selectors[selectorStartCount++].Draw(selectorLabelString + " " + (i + 1), _previewPickerObjectCache, spriteArray[i], false, true);
                        }
                    }
                }
            }
        } 
        private void DrawSettings() {
            _showSettings = InspectorExtensions.FoldoutFieldInTheBox(_showSettings, "Settings", true);
            if (!_showSettings) return;
            using (Scopes.OnChangeDirty(_mobgePolygonVisualizer)) {
                using (Scopes.LabelWidth(EditorGUIUtility.labelWidth * 1.5f)) {
                    var fieldStyle = new GUIStyle(EditorStyles.textField) {alignment = TextAnchor.MiddleRight};
                    using (new GUILayout.VerticalScope("Box")) {
                        using (Scopes.GUIBackgroundColor(InspectorExtensions.EditorColors.PastelOrange)) {
                            EditorGUILayout.LabelField("Edge Offsets", EditorStyles.boldLabel);
                            InspectorExtensions.EditorDivider();
                            _mobgePolygonVisualizer.topEdgeOffset = EditorGUILayout.Slider("Top edge offset", _mobgePolygonVisualizer.topEdgeOffset, -0.499f, 0.499f);
                            _mobgePolygonVisualizer.bottomEdgeOffset = EditorGUILayout.Slider("Bottom edge offset", _mobgePolygonVisualizer.bottomEdgeOffset, -0.499f, 0.499f);
                            _mobgePolygonVisualizer.leftEdgeOffset = EditorGUILayout.Slider("Left edge offset", _mobgePolygonVisualizer.leftEdgeOffset, -0.499f, 0.499f);
                            _mobgePolygonVisualizer.rightEdgeOffset = EditorGUILayout.Slider("Right edge offset", _mobgePolygonVisualizer.rightEdgeOffset, -0.499f, 0.499f);
                        }
                    }
                    using (new GUILayout.VerticalScope("Box")) {
                        using (Scopes.GUIBackgroundColor(InspectorExtensions.EditorColors.PastelOliveGreen)) {
                            EditorGUILayout.LabelField("Other Settings", EditorStyles.boldLabel);
                            InspectorExtensions.EditorDivider();
                            _mobgePolygonVisualizer.globalScale = EditorGUILayout.FloatField("Global scale", _mobgePolygonVisualizer.globalScale, fieldStyle);
                            _mobgePolygonVisualizer.innerTextureScale = EditorGUILayout.FloatField("Inner Texture Scale", _mobgePolygonVisualizer.innerTextureScale, fieldStyle);
                            _mobgePolygonVisualizer.innerTextureUVAngle = EditorGUILayout.FloatField("Inner Texture Angle", _mobgePolygonVisualizer.innerTextureUVAngle, fieldStyle);
                            _mobgePolygonVisualizer.edgeSpriteMinimumStretchValue = EditorGUILayout.FloatField("Edge Sprite Minimum Stretch Value", _mobgePolygonVisualizer.edgeSpriteMinimumStretchValue, fieldStyle);
                            _mobgePolygonVisualizer.edgeZOffset = EditorGUILayout.FloatField("Edge Z Offset", _mobgePolygonVisualizer.edgeZOffset, fieldStyle);
                            _mobgePolygonVisualizer.minimumEdgeDrawLength = EditorGUILayout.FloatField("Minimum Edge Draw Length", _mobgePolygonVisualizer.minimumEdgeDrawLength, fieldStyle);
                            _mobgePolygonVisualizer.minimumCornerAngle = EditorGUILayout.Slider("Minimum Corner Angle", _mobgePolygonVisualizer.minimumCornerAngle, 0f, 180f);
                            _mobgePolygonVisualizer.maximumCornerAngle = EditorGUILayout.Slider("Maximum Corner Angle", _mobgePolygonVisualizer.maximumCornerAngle, 0f, 180f);
                            _mobgePolygonVisualizer.calculateNormals = EditorGUILayout.Toggle("Calculate Normals", _mobgePolygonVisualizer.calculateNormals);
                            _mobgePolygonVisualizer.calculateTangents = EditorGUILayout.Toggle("Calculate Tangents", _mobgePolygonVisualizer.calculateTangents);
                            _mobgePolygonVisualizer.joinInnerOuterAndWallMeshesIntoOneObject = EditorGUILayout.Toggle("Join Inner Outer And Wall Meshes Into One Object", _mobgePolygonVisualizer.joinInnerOuterAndWallMeshesIntoOneObject);
                        }
                    }
                }
            }
        }
        #endregion Drawing Inspector

        #region Preview
        private void DrawPreviewSettings() {
            InspectorExtensions.EditorDivider();
            using (Scopes.GUIIndent(EditorGUI.indentLevel + 1)) {
                _isEditorPreviewOn = GUILayout.Toggle(_isEditorPreviewOn, "Preview Enabled");
            }
            if (_isEditorPreviewOn) {
                RenderPreviewObject();
            }
            else {
                DestroyPreviewObject();
            }
        }
        private void RenderPreviewObject() {
            if (!_previewPolygonRenderer) {
                var go = EditorUtility.CreateGameObjectWithHideFlags("Mobge Visualizer Preview Object", HideFlags.DontSave);
                go.transform.SetParent(TemporaryEditorObjects.Shared.transform);
                go.transform.position = Vector3.zero;
                _previewPolygonRenderer = go.AddComponent<PolygonRenderer>();
                _previewPolygonRenderer.data.polygons = CreateSamplePolygon();
                _previewPolygonRenderer.EnsureInstance();
            }
            _previewPolygonRenderer.data.visualizer = _mobgePolygonVisualizer;
            _previewPolygonRenderer.UpdateVisuals();
        }
        private static Polygon[] CreateSamplePolygon() {
            const float sizeScale = 10;
            var corners = new Corner[12];
            corners[0] = new Corner(new Vector2(sizeScale * 2, 0));
            corners[1] = new Corner(new Vector2(sizeScale * 2, sizeScale * 2));
            corners[2] = new Corner(new Vector2(sizeScale, sizeScale * 2));
            corners[3] = new Corner(new Vector2(sizeScale, sizeScale));
            corners[4] = new Corner(new Vector2(-sizeScale, sizeScale));
            corners[5] = new Corner(new Vector2(-sizeScale, sizeScale * 2));
            corners[6] = new Corner(new Vector2(-sizeScale * 2, sizeScale * 2));
            corners[7] = new Corner(new Vector2(-sizeScale * 2, 0));
            corners[8] = new Corner(new Vector2(-sizeScale, 0));
            corners[9] = new Corner(new Vector2(-sizeScale, -sizeScale));
            corners[10] = new Corner(new Vector2(sizeScale, -sizeScale));
            corners[11] = new Corner(new Vector2(sizeScale, 0));
            var p = new[] {new Polygon(corners)};
            p[0].corners.ReverseDirection();
            return p;
        }
        private static void DestroyPreviewObject() {
            if (_previewPolygonRenderer) _previewPolygonRenderer.gameObject.DestroySelf();
        }
        #endregion Preview

        #region Helpers
        private void AutoMapSprites() {
            var sprites = GetPreviewObjectsFromEdgeMaterial().OfType<Sprite>().ToArray();
            _mobgePolygonVisualizer.topEdgeSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "-in  top -bottom -left -right")).ToArray();
            _mobgePolygonVisualizer.leftEdgeSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "-in -top -bottom  left -right")).ToArray();
            _mobgePolygonVisualizer.rightEdgeSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "-in -top -bottom -left  right")).ToArray();
            _mobgePolygonVisualizer.bottomEdgeSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "-in -top  bottom -left -right")).ToArray();
            _mobgePolygonVisualizer.topLeftCornerSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "-in  top -bottom  left -right")).ToArray();
            _mobgePolygonVisualizer.topRightCornerSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "-in  top -bottom -left  right")).ToArray();
            _mobgePolygonVisualizer.bottomLeftCornerSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "-in -top  bottom  left -right")).ToArray();
            _mobgePolygonVisualizer.bottomRightCornerSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "-in -top  bottom -left  right")).ToArray();
            _mobgePolygonVisualizer.topInnerLeftCornerSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "in   top -bottom  left -right")).ToArray();
            _mobgePolygonVisualizer.topInnerRightCornerSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "in   top -bottom -left  right")).ToArray();
            _mobgePolygonVisualizer.bottomInnerLeftCornerSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "in  -top  bottom  left -right")).ToArray();
            _mobgePolygonVisualizer.bottomInnerRightCornerSprites = sprites.Where(sprite => InspectorExtensions.TextMatchesSearch(sprite.name, "in  -top  bottom -left  right")).ToArray();
            EditorUtility.SetDirty(_mobgePolygonVisualizer);
        }
        private static void SetArraySize<T>(ref T[] spriteArray, int size) {
            if (spriteArray == null) {
                spriteArray = new T[0];
            }
            int delta = size - spriteArray.Length;
            if (delta > 0)
                for (int i = 0; i < delta; i++)
                    ArrayUtility.Add(ref spriteArray, default);
            else if (delta < 0)
                for (int i = 0; i < -delta; i++)
                    ArrayUtility.RemoveAt(ref spriteArray, spriteArray.Length - 1);
        }
        private Object[] GetPreviewObjectsFromEdgeMaterial() {
            if (!_mobgePolygonVisualizer.edgeMaterial) return null;
            string spriteSheet = AssetDatabase.GetAssetPath(_mobgePolygonVisualizer.edgeMaterial.mainTexture);
            return AssetDatabase.LoadAllAssetsAtPath(spriteSheet);
        }
        #endregion Helpers

    }
}