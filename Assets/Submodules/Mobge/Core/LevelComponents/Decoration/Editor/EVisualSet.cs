using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mobge.Animation;
using UnityEditor;
using UnityEngine;
using static Mobge.InspectorExtensions;

namespace Mobge.Core
{
	[CustomEditor(typeof(VisualSet))]
	public class EVisualSet : Editor
	{
		private VisualSet _visualSet;
		private bool _isVisualSetOpen;
		private bool _isMaterialSetOpen;

		public override void OnInspectorGUI()
		{
			using (new GUILayout.VerticalScope("Box")) {
				DropAreaGUI(_visualSet.name + "  Spritesheet drop zone", Color.grey / 3, HarvestAndSetSprites);
				
				using (new EditorGUI.IndentLevelScope()) {
					EditorGUI.BeginChangeCheck();
					var visuals = _visualSet.visuals;
					EditorLayoutDrawer.CustomListField("Visuals", visuals, (layout, index) => {
						var v = _visualSet.visuals[index];
						if (v.Reference == null) {
							v.Reference = EditorGUI.ObjectField(layout.NextRect(), v.Reference, typeof(Object), false);
						} else {
							v.Reference.name = EditorGUI.TextField(layout.NextRect(), "name:", v.Reference.name);
							var r = layout.NextRect(EditorGUIUtility.currentViewWidth/2);
							v.Reference = EditorGUI.ObjectField(r, v.Reference, typeof(Object), false);
							if (v.Reference is GameObject go) {
								var iVisualComponent = go.GetComponent<IVisual>();
								if (iVisualComponent != null) {
									v.Reference = (Object)iVisualComponent;
								}
							}
							
							layout.NextRect();
						}
						_visualSet.visuals[index] = v;
					}, ref _isVisualSetOpen);
					_visualSet.visuals = visuals;
					if (EditorGUI.EndChangeCheck()) {
						EditorUtility.SetDirty(_visualSet);
					}
				}
				EditorDivider();
				
				using (new EditorGUI.IndentLevelScope()) {
					var materials = _visualSet.materials;
					EditorGUI.BeginChangeCheck();
					EditorLayoutDrawer.CustomListField("Materials", materials, (layout, index) => {
						EditorGUI.BeginChangeCheck();
						_visualSet.materials[index] = EditorGUI.ObjectField(layout.NextRect(), _visualSet.materials[index], typeof(Material), false) as Material;
						if (EditorGUI.EndChangeCheck()) {
							EditorUtility.SetDirty(_visualSet);
						}
					}, ref _isMaterialSetOpen);
					_visualSet.materials = materials;
				}
			}
		}

		private List<Sprite> GetSprites(Texture2D texture)
		{
			string spriteSheet = AssetDatabase.GetAssetPath(texture);
			return AssetDatabase.LoadAllAssetsAtPath(spriteSheet).OfType<Sprite>().ToList();
		}

		private void HarvestAndSetSprites(Object[] objs)
		{
			List<Sprite> _sprites = new List<Sprite>();

			// Collect all sprites from drag and dropped objects
			for (int i = 0; i < objs.Length; i++) {
				switch (objs[i]) {
					// Probably a spritesheet so treat it as such, try to get all sprites in the texture
					case Texture2D _:
						List<Sprite> spritesOfSpriteSheet = GetSprites(objs[i] as Texture2D);
						_sprites.AddRange(spritesOfSpriteSheet);
						break;
					// Single sprite
					case Sprite _:
						_sprites.Add(objs[i] as Sprite);
						break;
					default:
						continue;
				}
			}

			var v = _visualSet.visuals;
			// Check each collected sprite whether already exist in the visualset
			foreach (var s in _sprites) {
				var e = v.GenericEnumerator();
				bool notExist = true;
				// signal is turned off when already existing obj found in the visualset
				while (e.MoveNext())
					notExist &= e.Current.Reference != s;
				// control passed to here without turning off the flag; dropped object is uniq, add it.
				if (notExist) {
					v.AddElement(new Visual {
						Reference = s
					});
				}
			}
			EditorUtility.SetDirty(_visualSet);
		}

		protected void OnEnable()
		{
			_visualSet = target as VisualSet;
		}
	}
}