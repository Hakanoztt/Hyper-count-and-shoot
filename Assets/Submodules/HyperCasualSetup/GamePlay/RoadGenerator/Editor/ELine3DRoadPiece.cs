using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup.RoadGenerator
{
	[CustomEditor(typeof(Line3DRoadPiece))]
	public class ELine3DRoadPiece : Editor
	{
		private Line3DRoadPiece _line3DRoadPiece;
		
		private void OnEnable() {
			EnsureTarget();
			AlignPoints();
		}

		private void OnDisable() {
			EnsureTarget();
			AlignPoints();
		}

		private void AlignPoints() {
			if (_line3DRoadPiece.alignEndPoints) {
				_line3DRoadPiece.AlignEndPoints();
			}
		}

		private void EnsureTarget() {
			
			if (_line3DRoadPiece == null) {
				_line3DRoadPiece = (Line3DRoadPiece)target;
			}
		}
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();
			if (_line3DRoadPiece == null) {
				return;
			}

			SimpleSetupField();


			if (GUI.changed) {
				EditorExtensions.SetDirty(_line3DRoadPiece);
			}
		}
		void SimpleSetupField() {
			if(_line3DRoadPiece.line == null) {
				return;
			}
			ref var ss = ref _line3DRoadPiece.simpleSetup;
			ss.enabled = EditorGUILayout.BeginToggleGroup("simple setup enabled", ss.enabled);
			EditorGUI.BeginChangeCheck();

			ss.length = EditorGUILayout.FloatField("length", ss.length);
			ss.deltaHeight = EditorGUILayout.FloatField("delta height", ss.deltaHeight);
			ss.turnDegree = EditorGUILayout.FloatField("turn degree", ss.turnDegree);

			var scl = _line3DRoadPiece.line.MeshScale;
			scl = EditorGUILayout.Vector3Field("mesh scale", scl);
			_line3DRoadPiece.line.MeshScale = scl;

			if (EditorGUI.EndChangeCheck()) {
				ss.Apply(_line3DRoadPiece.line);
				_line3DRoadPiece.line.ReconstructImmediate();
			}
			EditorGUILayout.EndToggleGroup();
		}
		protected void OnSceneGUI() {
			if (_line3DRoadPiece == null) {
				return;
			}
			var line = _line3DRoadPiece.line;
			bool changed = false;
			if (line != null) {
				while(line.path.Points.Count < 2) {
					line.path.Points.Add(new BezierPath3D.Point());
					changed = true;
				}
				
			}
			if (changed) {
				EditorExtensions.SetDirty(_line3DRoadPiece);
			}
		}
	}
}