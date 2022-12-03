using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mobge {
	public static class InspectorExtensions {
		public static class EditorColors {
			public static Color32 PastelOliveGreen => new Color32(226, 239, 204, 255); // pastel olive green
			public static Color32 PastelBlue => new Color32(199, 207, 233, 255); // pastel blue
			public static Color32 PastelOrange => new Color32(254, 218, 195, 255); // pastel orange
			public static Color32 Default => new Color32(255, 255, 255, 255);
			public static Color AlternatingColor {
				get {
					var c = Color.gray / 4;
					if (_isAlternate) {
						if (EditorGUIUtility.isProSkin)
							c = c / 2;
						else
							c = c / 4;
					}
					_isAlternate = !_isAlternate;
					return c;
				}
			}
			private static bool _isAlternate;
		}
		public static class LayoutHelper {
			public static Rect DivideLayoutRectHorizontallyWithPercentage(Rect rect, float startPercentage, float sizePercentage) {
				rect.x += ((rect.width / 100) * startPercentage);
				rect.width = ((rect.width / 100) * sizePercentage);
				return rect;
			}
		}
		public static class EditorStyles {
			public static GUIStyle IconAreaWithBG => new GUIStyle("ProjectBrowserIconAreaBg");
			public static GUIStyle SearchTextField => new GUIStyle("SearchTextField");
			public static GUIStyle SearchCancelEmpty => new GUIStyle("SearchCancelButtonEmpty");
			public static GUIStyle SearchCancel => new GUIStyle("SearchCancelButton");
			public static GUIStyle SearchCancelButton(string s) {
				if (string.IsNullOrEmpty(s))
					return SearchCancelEmpty;
				return SearchCancel;
			}
			public static GUIStyle BoldTextField => new GUIStyle(GUI.skin.textField) { fontStyle = FontStyle.Bold };
			public static GUIStyle VisualSelectorButton => new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(5, 5, 5, 5)};

			public static Texture2D IconScaleLocked => EditorGUIUtility.IconContent("ScaleTool").image as Texture2D;
			public static Texture2D IconScaleUnLocked => EditorGUIUtility.IconContent("ScaleTool On").image as Texture2D;
		}
		public static class CustomFields {
			public static string SearchField(LayoutRectSource rects, string filter, Action onEnterPressed = null) {
				return SearchField(rects, default, filter, onEnterPressed);
			}
			public static string SearchField(LayoutRectSource rects, string label, string filter, Action onEnterPressed = null) {
				rects.NextRect(EditorGUIUtility.singleLineHeight / 4);
				var r = rects.NextRect(20);
				Rect rfilter;
				if (label == default) {
					rfilter = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 0, 100);
				} else {
					var rlabel = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 0, 30);
					rfilter = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 30, 65);
					EditorGUI.LabelField(rlabel, label, UnityEditor.EditorStyles.boldLabel);
				}
				var rbutton = LayoutHelper.DivideLayoutRectHorizontallyWithPercentage(r, 95, 5);
				rects.NextRect(EditorGUIUtility.singleLineHeight / 4);
				filter = EditorGUI.TextField(rfilter, filter, EditorStyles.SearchTextField);
				if (GUI.Button(rbutton, GUIContent.none, EditorStyles.SearchCancelButton(filter))) {
					filter = string.Empty;
					GUIUtility.keyboardControl = 0;
				}
				var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
				te.cursorIndex = 1;
				if ((Event.current.type == EventType.KeyUp) && (Event.current.keyCode == KeyCode.Return) &&
				    (GUI.GetNameOfFocusedControl() == "SearchTextField")) {
					onEnterPressed?.Invoke();
				}
				return filter;
			}
			public static Vector3 Vector3InputField(string label, Vector3 value, bool lockX, bool lockY, bool lockZ, Action extraButtonLogic = null) {
				Vector3 originalValue = value;
				Vector3 newValue = value;
				GUIContent[] Labels = new GUIContent[3];
				Labels[0] = new GUIContent("X", "");
				Labels[1] = new GUIContent("Y", "");
				Labels[2] = new GUIContent("Z", "");
				using (new GUILayout.HorizontalScope()) {
					EditorGUILayout.PrefixLabel(label);
					extraButtonLogic?.Invoke();
					using (Scopes.LabelWidth(12)) {
						EditorGUI.BeginChangeCheck();
						using (Scopes.GUIEnabled(!lockX)) {
							newValue.x = EditorGUILayout.FloatField(Labels[0], newValue.x, UnityEditor.EditorStyles.textField);
						}
						using (Scopes.GUIEnabled(!lockY)) {
							newValue.y = EditorGUILayout.FloatField(Labels[1], newValue.y, UnityEditor.EditorStyles.textField);
						}
						using (Scopes.GUIEnabled(!lockZ)) {
							newValue.z = EditorGUILayout.FloatField(Labels[2], newValue.z, UnityEditor.EditorStyles.textField);
						}
						if (EditorGUI.EndChangeCheck()) {
							float difference = newValue.x / originalValue.x;
							if (lockY)
								newValue.y = originalValue.y * difference;
							if (lockZ)
								newValue.z = originalValue.z * difference;
						}
					}
				}
				return newValue;
			}
			public static class LabelPicker {
				private static int selectedLabel = -1;
				private static readonly Dictionary<Type, string[]> TypeLabelArrayCache = new Dictionary<Type, string[]>();
				private static readonly Dictionary<TypeLabel, PopupCacheEntry> LabelToObjectListCache = new Dictionary<TypeLabel, PopupCacheEntry>();
				private struct TypeLabel {
					public Type type;
					public string label;
					public TypeLabel(Type type, string label) {
						this.type = type;
						this.label = label;
					}
				}
				private struct PopupCacheEntry {
					public List<string> names;
					public string[] namesArray;
					public List<Object> objects;
					public List<string> paths;
				}
				private static PopupCacheEntry GetPopupCacheEntry(Type type, string labelToSearch) {
					if (LabelToObjectListCache.TryGetValue(new TypeLabel(type, labelToSearch), out var cacheEntry)) return cacheEntry;
					cacheEntry = new PopupCacheEntry();
					var filter = $"t:{type.Name} ";
					if (!string.IsNullOrWhiteSpace(labelToSearch)) {
						filter += $"l:{labelToSearch} ";
					}
					var guids = AssetDatabase.FindAssets(filter);
					cacheEntry.names = new List<string>(guids.Length);
					cacheEntry.objects = new List<Object>(guids.Length); 
					cacheEntry.paths = new List<string>(guids.Length);
					cacheEntry.names.Add("None");
					cacheEntry.objects.Add(null);
					cacheEntry.paths.Add(null);
					foreach (var guid in guids) {
						var path = AssetDatabase.GUIDToAssetPath(guid);
						//this can return null if asset search found a different typed object
						var o = AssetDatabase.LoadAssetAtPath(path, type);
						if (o == null) continue;
						cacheEntry.paths.Add(path);
						cacheEntry.objects.Add(o);
						cacheEntry.names.Add(o.name);
					}
					cacheEntry.namesArray = cacheEntry.names.ToArray();
					LabelToObjectListCache.Add(new TypeLabel(type, labelToSearch), cacheEntry);
					return cacheEntry;
				}
				private static string[] GetLabelArray(Type type) {
					if (TypeLabelArrayCache.TryGetValue(type, out var allLabelsArray)) return allLabelsArray;
					var allLabels = new HashSet<string>();
					if (type.IsInterface) {
						//if interface, get all classes that implements that interface
						var types = AppDomain.CurrentDomain.GetAssemblies()
							.SelectMany(s => s.GetTypes())
							.Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
						foreach (var t in types) {
							SearchAssetDatabaseAndFindLabels(t, ref allLabels);
						}
					}
					else {
						SearchAssetDatabaseAndFindLabels(type, ref allLabels);
					}
					allLabelsArray = allLabels.ToArray();
					TypeLabelArrayCache.Add(type, allLabelsArray);
					return allLabelsArray;
				}
				private static void SearchAssetDatabaseAndFindLabels(Type type, ref HashSet<string> allLabels) {
					var allAssets = AssetDatabase.FindAssets("t:" + type.Name);
					foreach (var guid in allAssets) {
						var path = AssetDatabase.GUIDToAssetPath(guid);
						var o = AssetDatabase.LoadAssetAtPath(path, type);
						if (o == null) continue;
						foreach (var l in AssetDatabase.GetLabels(o)) {
							allLabels.Add(l);
						}
					}
				}
				static LabelPicker() {
					EditorApplication.projectChanged += InvalidateCache;
				}
				private static void InvalidateCache() {
					LabelToObjectListCache.Clear();
					TypeLabelArrayCache.Clear();
					selectedLabel = -1;
				}
				public static T DrawLabeledObjectPicker<T>(string label, T obj, string labelToSearch = null, bool allowSceneObjects = false) where T: class {
					return DrawLabeledObjectPicker(
						label, 
						obj as UnityEngine.Object, 
						typeof(T), 
						labelToSearch, 
						allowSceneObjects) as T;
				}
				public static T DrawLabeledObjectPicker<T>(Rect position, string label, T obj, string labelToSearch = null, bool allowSceneObjects = false) where T: class {
					return DrawLabeledObjectPicker(
						position, 
						label, 
						obj as UnityEngine.Object, 
						typeof(T), 
						labelToSearch,
						allowSceneObjects) as T;
				}
				public static Object DrawLabeledObjectPicker(string label, Object obj, Type type,  string labelToSearch = null, bool allowSceneObjects = false) {
					var rect = GUILayoutUtility.GetRect(10, EditorGUIUtility.singleLineHeight);
					return DrawLabeledObjectPicker(rect, label, obj, type, labelToSearch, allowSceneObjects);
				}
				public static Object DrawLabeledObjectPicker(Rect position, string label, Object obj, Type type, string labelToSearch = null, bool allowSceneObjects = false) {
					float startX = position.x;
					var fullWidth = position.width;
					float objectPickerWidth = fullWidth - EditorGUIUtility.singleLineHeight;
					//Draw Label Picker
					if (labelToSearch == null) {
						var allLabelsArray = GetLabelArray(type);
						objectPickerWidth = fullWidth * .7f;
						position.width = (fullWidth * .3f) - EditorGUIUtility.singleLineHeight;
						position.x = startX + (fullWidth * .7f);
						using (Scopes.GUIIndent(0)) {
							selectedLabel = EditorGUI.Popup(position, selectedLabel, allLabelsArray);
						}
						labelToSearch = 
							selectedLabel >= 0 && selectedLabel < allLabelsArray.Length ?
							allLabelsArray[selectedLabel]
							: "";
					}
					var cacheEntry = GetPopupCacheEntry(type, labelToSearch);
					//find current index
					//why paths -> because 2 objects with different types can reference the same object
					//ex: Micron Component and Micron Prefab, same reference path, but not the same object
					var oldIndex = 0;
					var currentAssetPath = AssetDatabase.GetAssetPath(obj);
					for (int i = 0; i < cacheEntry.paths.Count; i++) {
						var loopPath = cacheEntry.paths[i];
						if (currentAssetPath != loopPath) continue;
						oldIndex = i;
						break;
					}
					// Draw Object Picker
					position.width = objectPickerWidth;
					position.x = startX;
					var newObj = EditorGUI.ObjectField(position, label, obj, type, allowSceneObjects);
					//Draw Popup
					position.x += fullWidth - EditorGUIUtility.singleLineHeight;
					position.width = EditorGUIUtility.singleLineHeight;
					int newIndex;
					using (Scopes.GUIIndent(0)) {
						newIndex = EditorGUI.Popup(position, oldIndex, cacheEntry.namesArray);
					}
					if (obj != newObj) return newObj;
					if (oldIndex != newIndex) return cacheEntry.objects[newIndex];
					return obj;
				}
			}
			public static class Scale {
				private static bool _uniformScaling;
				public static Vector3 Draw(Vector3 scale) {
					using (Scopes.Horizontal()) {
						using (Scopes.LabelWidth(EditorGUIUtility.labelWidth - 24)) {
							scale = Vector3InputField("Scale", scale, false, _uniformScaling, _uniformScaling, DrawLock);
						}
					}
					return scale;
				}
				private static void DrawLock() {
					Texture2D @lock = _uniformScaling ? EditorStyles.IconScaleLocked : EditorStyles.IconScaleUnLocked;
					string tooltip = _uniformScaling ? "Disable Uniform Scaling" : "Enable Uniform Scaling";
					var gc = new GUIContent("", @lock, tooltip);
					if (GUILayout.Button(gc, EditorStyles.IconAreaWithBG, GUILayout.Width(18), GUILayout.Height(18))) {
						_uniformScaling = !_uniformScaling;
					}
				}
			}
			public static class Rotation {
				public static Quaternion DrawAsVector3(Quaternion quaternion) {
					EditorGUI.BeginChangeCheck();
					var v = EditorGUILayout.Vector3Field("Rotation", quaternion.eulerAngles);
					if (EditorGUI.EndChangeCheck()) {
						return Quaternion.Euler(v.x, v.y, v.z);
					}
					return quaternion;
				}
			}
		}
		public class PreviewPicker {
			private string _sFilter = "";
			private bool _chosenSomething = false;
			private Object _chosenObject;
			private int _chosenObjectIndex;
			public Object Draw(string buttonText, List<Object> objects, Object currentObject, bool isFilterEnabled = false, bool isNullIncluded = false, bool returnOnePixelSpriteInsteadOfNull = false) {
				int currentIndex = -1;
				for (int i = 0; i < objects.Count; i++) {
					if (objects[i] == currentObject) {
						currentIndex = i;
					}
				}
				InternalDrawButton(buttonText, objects, currentIndex, isFilterEnabled, isNullIncluded);
				//_chosenSomething exists because we cannot get return value immediately from internal draw button.
				//it works delayed because it draws a popup window that it does not control 
				if (_chosenSomething) {
					GUI.changed = true;
					_chosenSomething = false;
					if (returnOnePixelSpriteInsteadOfNull) {
						if (_chosenObject == null) {
							return NullValues.Sprite;
						}
					}
					return _chosenObject;
				}
				return currentObject;
			}

			public int Draw(string buttonText, List<Object> objects, int currentIndex, bool isFilterEnabled = false, bool isNullIncluded = false) {
				InternalDrawButton(buttonText, objects, currentIndex, isFilterEnabled, isNullIncluded);
				//_chosenSomething exists because we cannot get return value immediately from internal draw button.
				//it works delayed because it draws a popup window that it does not control 
				if (_chosenSomething) {
					GUI.changed = true;
					_chosenSomething = false;
					return _chosenObjectIndex;
				}
				return currentIndex;
			}

			private void InternalDrawButton(string buttonText, List<Object> objects, int currentIndex, bool isFilterEnabled = false, bool isNullIncluded = false) {
				using (Scopes.GUIBackgroundColor(EditorColors.PastelOliveGreen)) {
					var rect = EditorGUILayout.GetControlRect(false, 2);
					//Draw filter and popup
					SelectorButton(buttonText, objects, isFilterEnabled, isNullIncluded);
					if (currentIndex >= 0 && currentIndex < objects.Count && objects[currentIndex] != null) {
						var sprite = objects[currentIndex];
						DrawPreview(new Rect(rect.width - EditorGUIUtility.fieldWidth - 60,
							rect.position.y + 7,
							rect.size.x - EditorGUIUtility.labelWidth,
							rect.size.y * 10), sprite);
					}
				}
			}
			static Dictionary<UnityEngine.Object, Editor> s_editorCache = new Dictionary<UnityEngine.Object, Editor>();
			public static void DrawPreview(Rect rect, UnityEngine.Object o) {
				if (o is MonoBehaviour monoBehaviour) {
					o = monoBehaviour.gameObject;
				}
				if(o is Sprite s) {
					DrawTexturePreview(rect, s);
					return;
				}
				if(!s_editorCache.TryGetValue(o, out Editor e)) {
					e = Editor.CreateEditor(o);
					s_editorCache.Add(o, e);
				}
				e.DrawPreview(rect);
			}
			public static void DrawTexturePreview(Rect position, Sprite sprite) {
				Vector2 fullSize = new Vector2(sprite.texture.width, sprite.texture.height);
				Vector2 size = new Vector2(sprite.textureRect.width, sprite.textureRect.height);

				Rect coords = sprite.textureRect;
				coords.x /= fullSize.x;
				coords.width /= fullSize.x;
				coords.y /= fullSize.y;
				coords.height /= fullSize.y;

				Vector2 ratio;
				ratio.x = position.width / size.x;
				ratio.y = position.height / size.y;
				float minRatio = Mathf.Min(ratio.x, ratio.y);

				Vector2 center = position.center;
				position.width = size.x * minRatio;
				position.height = size.y * minRatio;
				position.center = center;

				GUI.DrawTextureWithTexCoords(position, sprite.texture, coords);
			}
			private void SelectorButton(string buttonText, List<Object> objects, bool isFilterEnabled = false, bool isNullIncluded = false) {
				//Main Button
				if (GUILayout.Button(buttonText, EditorStyles.VisualSelectorButton)) {
					// Popup Drawer
					var p = new EditorPopup((rects, popup) => {
						//Filter
						if (isFilterEnabled) {
							_sFilter = CustomFields.SearchField(rects, "Filter:", _sFilter);
						}
						//Null Button
						if (isNullIncluded) {
							if (Button(rects, "None")) {
								_chosenObjectIndex = -1;
								_chosenObject = null;
								_chosenSomething = true;
								popup.Close();
							}
						}
						//Other Buttons
						for (int i = 0; i < objects.Count; i++) {
							if (!TextMatchesSearch(objects[i].name, _sFilter)) continue;
							if (Button(rects, objects[i].name, objects[i])) {
								_chosenObjectIndex = i;
								_chosenObject = objects[i];
								_chosenSomething = true;
								popup.Close();
							}
						}
					});
					p.Show(new Rect(Event.current.mousePosition, Vector2.zero), new Vector2(300, 300));
				}
			}
			private bool Button(LayoutRectSource rects, string name, Object obj = null) {
				var rect = rects.NextRect(40);
				var b = GUI.Button(rect, name, EditorStyles.VisualSelectorButton);
				if (obj) {
					rect.width *= 0.5f;
					rect.x += rect.width;
					DrawPreview(rect, obj);
				}
				return b;
			}
			private Texture2D ConvertSpriteToTexture2D(Sprite s) {
				//TODO "old ver -> s.rect.width != s.texture.width" verify this works
				if (s.rect.width != s.texture.width && s.rect.height != s.texture.height) {
					// Sprite is part of a sprite sheet
					Texture2D nt = new Texture2D(Mathf.RoundToInt(s.textureRect.width), Mathf.RoundToInt(s.textureRect.height));
					Color[] newColors = s.texture.GetPixels(
						Mathf.RoundToInt(s.textureRect.x), 
						Mathf.RoundToInt(s.textureRect.y), 
						Mathf.RoundToInt(s.textureRect.width), 
						Mathf.RoundToInt(s.textureRect.height));

					nt.SetPixels(newColors);
					nt.Apply();
					return nt;
				} else {
					// Sprite is the only image in the texture
					return s.texture;
				}
			}
			public static class NullValues {
				private const string base64RedXCross = "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAMAAACdt4HsAAAAA3NCSVQICAjb4U/gAAAACXBIWXMAAAITAAACEwGa0+JEAAAAGXRFWHRTb2Z0d2FyZQB3d3cuaW5rc2NhcGUub3Jnm+48GgAAAGZQTFRF/////1UA5k0z6lUr7FUm71Ip7lEm8lEo71In8VEn71Mn8FIp8FEn71Ip8FEo8FEp8VMo8FIo71Io8VMp8FMp8FIo8FIo8FIo71Eo8FEo8VIo8FIo8FIo8FIo8FIo8FIo8FIo8FIo1u3aRwAAACF0Uk5TAAMKDBsfPExOW2JkaHB0io6YpLC2vsDL1djl6vL4+fz+eaAKEAAAAYdJREFUWMOtV9uygkAMC4IKAoIIAgJi/v8nz4N65FJgd2oeM01mL91uCyzA9eMkK+q2rYssiX0XVvCivOMIXR55purduXxSwLM87wzkTnjnIu6hs6U/VlxFdVyV72/cxG2/rD80NEBzWNKfHjTC4ySfXkpjpMJZOlda4Dp3SGmFdLZ/WmJyDoeHrcFjdBf7htZohvkwzZ/+0nOTug3ydxocIOg3Kf5ntVPNgjEJFyiy+txlKOjH4QJFkuH7/d8l/TBcoF6v+1UfzrL+Gy5Qb5wBAOWC/hMuUB+UAOA9l/SvcIH6VjkPQDS0vEwyPOgneuAyjI8A5FxeARDMiNEecsDtuOqwqmfnwictHGbZ6COmhcNMzxgJzR3meibIaOwg6JmhoKmDpGeBmoYOop41Wpo5yHq2egP1FtSHqL5GdSKpU1n9mNTPWV1Q9CVNXVTVZV3/sai/NvXnqv/e1Q2GvsXRN1nqNk/faOpbXX2zrW739QPHD0Ye/dClH/t+MHjqR98fDN924/8fV5w3qNc+PE0AAAAASUVORK5CYII=";
				private static Sprite _nullSprite;
				private static Texture2D _nulltexture;
				public static Texture2D Texture {
					get {
						if (!_nulltexture) {
							byte[] b = System.Convert.FromBase64String(base64RedXCross);
							_nulltexture = new Texture2D(1, 1);
							_nulltexture.LoadImage(b);
						}
						return _nulltexture;
					}
				}
				public static Sprite Sprite {
					get {
						if (!_nullSprite) {
							Color[] color = new Color[1];
							color[0] = Color.black;
							var tex = new Texture2D(0, 0, TextureFormat.RGBA32, false);
							tex.SetPixels(0, 0, 0, 0, color);
							tex.Apply();
							_nullSprite = Sprite.Create(tex, new Rect(Vector2.zero, new Vector2(tex.width, tex.height)), Vector2.zero);
							_nullSprite.name = "Null";
						}
						return _nullSprite;
					}
				}
			}
		}
		public static class AutoIndexWrapper {
			public static int WrapExecute<T>(AutoIndexedMap<T> map, int mapIndex, Func<List<T>, int, int> func) {
				//fill flat list
				var flatList = new List<T>();
				int flatIndex = -1;
				var e = map.GetPairEnumerator();
				while (e.MoveNext()) {
					flatList.Add(e.Current.Value);
					if (e.Current.Key == mapIndex) {
						flatIndex = flatList.Count - 1;
					}
				}
				var solvedIndex = func(flatList, flatIndex);
				//find and return auto indexed key
				if (solvedIndex >= 0 && solvedIndex < flatList.Count) {
					e.Reset();
					while (e.MoveNext()) {
						if (Equals(e.Current.Value, flatList[solvedIndex])) {
							return e.Current.Key;
						}
					}
				}
				//couldn't find, return original
				return mapIndex;
			}
		}
		public static bool FoldoutFieldInTheBox(bool value, string labelString, bool isDivide = false) {
			if (isDivide) EditorDivider();
			using (new EditorGUI.IndentLevelScope()) {
				value = EditorGUILayout.Foldout(value, labelString, true);
			}
			return value;
		}
		public static void EditorDivider() {
			Rect rect = EditorGUILayout.GetControlRect(false, 1);
			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
		}
		private static Rect DrawDropBox(string name, Color color) {
			Rect dropRect = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
			if (color == default) {
				GUI.Box(dropRect, name);
			} else {
				using (Scopes.GUIBackgroundColor(color)) {
					GUI.Box(dropRect, name);
				}
			}
			return dropRect;
		}
		public static UnityEngine.Object[] DropAreaGUI(string name, Color color = default, Action<UnityEngine.Object[]> PostProcessingFunc = null) {
			Rect dropRect = DrawDropBox(name, color);
			Event @event = Event.current;
			switch (@event.type) {
				case EventType.DragUpdated:
				case EventType.DragPerform:
					if (!dropRect.Contains(@event.mousePosition)) return null;
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					if (@event.type == EventType.DragPerform) {
						DragAndDrop.AcceptDrag();
						PostProcessingFunc?.Invoke(DragAndDrop.objectReferences);
						return DragAndDrop.objectReferences;
					}
					break;
			}
			return null;
		}
		public static bool TextMatchesSearch(string text, string keywords){
			if (keywords == null) {
				return true;
			}
			var keywordArray = keywords.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < keywordArray.Length; i++) {
				var keyword = keywordArray[i];
				//if an exclusive keyword does match
				if (keyword[0] == '-') {
					if(text.IndexOf(keyword.Substring(1), StringComparison.CurrentCultureIgnoreCase) >= 0){
						return false;
					}
				}
				//if keyword does not match 
				else {
					if(!(text.IndexOf(keywordArray[i], StringComparison.CurrentCultureIgnoreCase) >= 0)){
						return false;
					}
				}
			}
			return true;
		}
		public static int LayerMaskField(string label, int layermask) {
			return FieldToGameLayerMask(EditorGUILayout.MaskField(label, GameLayerMaskToField(layermask), InternalEditorUtility.layers));
		}
		public static int LayerMaskField(Rect position, string label, int layermask) {
			return FieldToGameLayerMask(EditorGUI.MaskField(position, label, GameLayerMaskToField(layermask), InternalEditorUtility.layers));
		}
		private static int FieldToGameLayerMask(int field) {
			if (field == -1) return -1;
			int mask = 0;
			var layers = InternalEditorUtility.layers;
			for (int c = 0; c < layers.Length; c++)
			{
				if ((field & (1 << c)) != 0)
				{
					mask |= 1 << LayerMask.NameToLayer(layers[c]);
				}
				else {
					mask &= ~(1 << LayerMask.NameToLayer(layers[c]));
				}
			}
			return mask;
		}
		private static int GameLayerMaskToField(int mask) {
			if (mask == -1) return -1;
			int field = 0;
			var layers = InternalEditorUtility.layers;
			for (int c = 0; c < layers.Length; c++)
			{
				if ((mask & (1 << LayerMask.NameToLayer(layers[c]))) != 0)
				{
					field |= 1 << c;
				}
			}
			return field;
		}
	}
}
