using UnityEditor;
using UnityEngine;
using static Mobge.InspectorExtensions;

namespace Mobge.Core
{
	[CustomEditor(typeof(DecorationSet))]
	public class EDecorationSet : Editor
	{
		private DecorationSet _ds;
		private bool _showTilesets;
		private bool _showPolygonVisualizers;
		private bool _showVisualSets;

		public override void OnInspectorGUI()
		{
			var lightGray = Color.grey / 3;
			using (new EditorGUILayout.VerticalScope("Box")) {
				using (new EditorGUI.IndentLevelScope()) {
					var vs = _ds.VisualSets;
					EditorGUI.BeginChangeCheck();
					EditorLayoutDrawer.CustomListField("Decoration Visuals", vs, (layout, index) => {
						vs[index] = EditorGUI.ObjectField(layout.NextRect(), vs[index], typeof(VisualSet), false) as VisualSet;
					}, ref _showVisualSets);
					if (EditorGUI.EndChangeCheck()) {
						_ds.VisualSets = vs;
						EditorUtility.SetDirty(_ds);
					}
				}
				EditorDivider();

				using (new EditorGUI.IndentLevelScope()) {
					var visuals = _ds.PolygonVisualizers;
					EditorLayoutDrawer.CustomListField("Polygon visualizers", visuals, (layout, index) => {
						var v = _ds.PolygonVisualizers[index];
						EditorGUI.BeginChangeCheck();
						if (v.Reference == null) {
							v.Reference = EditorGUI.ObjectField(layout.NextRect(), v.Reference, typeof(Object), false);
						} else {
							EditorGUI.indentLevel --;
							var r = layout.NextRect();
							v.Reference	 = EditorGUI.ObjectField(r, v.Reference, typeof(Components.IPolygonVisualizer), false);
							r = layout.NextRect(EditorGUIUtility.currentViewWidth / 2);
							EditorGUI.DrawTextureTransparent(r, v.Visualizer.EditorVisual(), ScaleMode.ScaleToFit);
							EditorGUI.indentLevel++;
						}
						if (EditorGUI.EndChangeCheck()) {
							visuals[index] = v;
							_ds.PolygonVisualizers = visuals;
							EditorUtility.SetDirty(_ds);
						}
					}, ref _showPolygonVisualizers);
				}
				EditorDivider();

				using (new EditorGUI.IndentLevelScope()) {
					var visuals = _ds.Tilesets;
					EditorLayoutDrawer.CustomListField("Tilesets", visuals, (layout, index) => {
						var v = _ds.Tilesets[index];
						EditorGUI.BeginChangeCheck();
						v.Reference = EditorGUI.ObjectField(layout.NextRect(), v.Reference, typeof(IPieceVisualizer), false);
						if (EditorGUI.EndChangeCheck()) {
							visuals[index] = v;
							_ds.Tilesets = visuals;
							EditorUtility.SetDirty(_ds);
						}
					}, ref _showTilesets);
				}
			}
		}

		protected void OnEnable()
		{
			_ds = target as DecorationSet;
		}
	}
}