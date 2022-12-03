using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mobge.Fx
{
	[CustomEditor(typeof(ElasticBand))]
	public class EElasticBand : Editor
	{
		public float height = 0.2f;
		public float width = 0.125f;
		public float uvxcenter = 0.5f;
		public float textureScale = 0.125f;

		private ElasticBand _go;
		private float LerpVal;
		private bool isSetPos;
		private Vector3 randomPosToGoTo;

		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("Trigger Snap")) {
				_go.isSnapTriggered = true;
			}
			if (GUILayout.Button("Set test position")) {
				randomPosToGoTo = _go.gameObject.transform.position + new Vector3(Random.Range(-10.0f, 10.0f), Random.Range(-10.0f, 10.0f));
				isSetPos = true;
			}

			EditorGUI.BeginChangeCheck();
			_go.material = (Material)EditorGUILayout.ObjectField("Material", _go.material, typeof(Material), false);
			_go.setting.type = (ElasticBand.DataType)EditorGUILayout.EnumPopup("Type", _go.setting.type);
			_go.setting.width = EditorGUILayout.IntField("Width", _go.setting.width);
			_go.setting.numberOfSpring = EditorGUILayout.DelayedIntField("Number of springs", _go.setting.numberOfSpring);
			_go.setting.mass = EditorGUILayout.FloatField("Mass", _go.setting.mass);
			_go.setting.springConstant = EditorGUILayout.FloatField("Spring constant", _go.setting.springConstant);
			EditorDivider();

			_go.setting.gravityScale = EditorGUILayout.FloatField("Gravity scale", _go.setting.gravityScale);
			_go.setting.drag = EditorGUILayout.DelayedFloatField("Linear drag", _go.setting.drag);
			if (_go.setting.drag > 10000) _go.setting.drag = 10000;
			EditorDivider();

			height = EditorGUILayout.FloatField("Graphic height", height);
			width = EditorGUILayout.FloatField("Graphic width", width);
			uvxcenter = EditorGUILayout.FloatField("Uvx center", uvxcenter);
			textureScale = EditorGUILayout.FloatField("Texture scale", textureScale);
			if (EditorGUI.EndChangeCheck()) {
				Regene();
			}
		}

		private void OnSceneGUI()
		{
			if (LerpVal > 1) {
				LerpVal = 0;
				isSetPos = false;
				return;
			}
			if (isSetPos) {
				LerpVal += 0.01f;
				_go.gameObject.transform.position = Vector3.Lerp(_go.gameObject.transform.position, randomPosToGoTo, LerpVal);
			}
			if (Application.isPlaying) {
				_go.Data.EndPoint = _go.transform.position;
			}
		}
		private void Regene()
		{
			_go.GenerateData(_go.AnchorPoint.transform.position, _go.setting);
			_go.Data.PrepareBand(_go.transform.position, _go.AnchorPoint.transform.position, _go.setting);
			if (_go.Graphics) {
				_go.Graphics.Skin.PieceLength = height;
				_go.Graphics.Skin.Width = width;
				_go.Graphics.Skin.UVXCenter = uvxcenter;
				_go.Graphics.Skin.TextureScale = textureScale;
				_go.Graphics.Skin.NumberOfPieces = _go.setting.numberOfSpring;
				_go.Graphics.ReConstruct(_go.material);
			}
		}

		private void EditorDivider(int thickness = 1)
		{
			EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, thickness), new Color(0.5f, 0.5f, 0.5f, 1));
		} 

		protected void OnEnable()
		{
			_go = target as ElasticBand;
		}
	}
}