using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Mobge.Fx
{
	public class SkinnedProcedural : MonoBehaviour
	{
		public Transform[] ControlTransforms { get => _skin.bones; }

		[SerializeField] private Material _material;
		public Material Material { get => _material; set => _material = value; }
		[HideInInspector] [SerializeField] private SkinnedMeshRenderer _skin;
		public SkinnedMeshRenderer Skin => _skin;

		[HideInInspector] [SerializeField] public Piece[] pieces;
		[HideInInspector] [SerializeField] private float _pieceLength = 0.5f;
		public float PieceLength {
			get => _pieceLength;
			set {
				if (Mathf.Abs(_pieceLength - value) > 0.01f) {
					_pieceLength = value;
					ReConstruct();
				}
			}
		}

		[HideInInspector] [SerializeField] private int _numberOfPieces = 10;
		public int NumberOfPieces {
			get => _numberOfPieces;
			set {
				if (_numberOfPieces > 2) {
					_numberOfPieces = value;
					ReConstruct();
				}
			}
		}

		[HideInInspector] [SerializeField] private float _uvXCenter = 0.5f;
		public float UVXCenter {
			get => _uvXCenter;
			set {
				if (Mathf.Abs(_uvXCenter - value) > 0.01f) {
					_uvXCenter = value;
					ReConstruct();
				}
			}
		}
		[SerializeField] [HideInInspector] private Color _color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		public Color Color {
			get => _color;
			set {
				if (!_color.Equals(value)) {
					_color = value;
					SetColors(_skin.sharedMesh, _color);
				}
			}
		}
		[HideInInspector] [SerializeField] private float _textureScale = 1;
		public float TextureScale {
			get => _textureScale;
			set {
				if (Mathf.Abs(_textureScale - value) > 0.01f && _skin) {
					_textureScale = value;
					ReconstructMesh();
				}
			}
		}
		[HideInInspector] [SerializeField] private float _width;
		public float Width {
			get => _width;
			set {
				_width = value;
				if (Mathf.Abs(_width - value) > 0.01f && _skin) {
					ReconstructMesh();
				}
			}
		}

		public void ReConstruct()
		{
			transform.DestroyAllChildren();

			Vector2[] _startingPoints = new Vector2[_numberOfPieces];
			for (int i = 0; i < _numberOfPieces; i++) {
				_startingPoints[i] = new Vector2(0, i * _pieceLength);
			}

			var l = _startingPoints.Length;
			if (l >= 2) {
				pieces = new Piece[l];
				for (int i = 0; i < l - 1; i++) {
					var p1 = _startingPoints[i];
					var p2 = _startingPoints[i + 1];
					pieces[i] = new Piece(transform, p1, p2, i);
				}

				pieces[pieces.Length - 1] = new Piece(transform,
					_startingPoints[pieces.Length - 2],
					_startingPoints[pieces.Length - 1],
					pieces.Length - 1);

				_skin = pieces[0].tr.gameObject.AddComponent<SkinnedMeshRenderer>();
				_skin.sharedMaterial = _material;
				_skin.rootBone = pieces[0].tr;

				Transform[] bones = new Transform[pieces.Length];

				for (int i = 0; i < pieces.Length; i++) {
					bones[i] = pieces[i].tr;
				}
				_skin.bones = bones;
				_skin.quality = SkinQuality.Bone2;
				ReconstructMesh();

			} else {
				pieces = null;
			}
		}

		private static void SetColors(Mesh m, Color c)
		{
			var count = m.vertexCount;
			var _colors = new Color[count];
			for (int i = 0; i < count; i++) {
				_colors[i] = c;
			}
			m.colors = _colors;
		}

		private static void AddRect(int[] tris, int triStart, int vertStart)
		{
			tris[triStart] = vertStart;
			tris[triStart + 1] = vertStart + 1;
			tris[triStart + 2] = vertStart + 2;
			tris[triStart + 3] = vertStart + 2;
			tris[triStart + 4] = vertStart + 1;
			tris[triStart + 5] = vertStart + 3;
		}

		private void ReconstructMesh()
		{
			InputParams _inputParams = new InputParams {
				material = _skin.material,
				weightValue = 0.8f,
				pieceLength = _pieceLength,
				boneWidth = _width,

				startY = pieces[0].Height,
				surfaceNormal = new Vector3(1, 0, 0),
				surfaceOffset = 0,
				textureHeightMultiplayer = 1,
				textureScale = _textureScale,
				uvxCenter = _uvXCenter,
				uvyStart = 0
			};

			float y = ReconstructMesh(_skin, pieces.Length, (index) => pieces[index].Height, ref _inputParams);
			_skin.localBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(y, y, 0) * 2);
		}

		public static float ReconstructMesh(SkinnedMeshRenderer skin, int pieceCount, Func<int, float> getPieceHeight, ref InputParams inputParams, bool useColor = false, Color color = new Color())
		{
			Mesh _m = skin.sharedMesh;
			if (_m == null) {
				_m = new Mesh();
			} else {
				_m.Clear();
			}
			var _verts = new Vector3[pieceCount * 4];
			var _uv = new Vector2[_verts.Length];
			var _normals = new Vector3[_verts.Length];
			var _tris = new int[pieceCount * 12];
			var _bindposes = new Matrix4x4[pieceCount];
			var _weights = new BoneWeight[_verts.Length];
			float uvh = inputParams.textureHeightMultiplayer * inputParams.pieceLength * 0.25f / inputParams.textureScale;
			inputParams.uvyStart += uvh;

			float y = 0;
			for (int i = 0; i < pieceCount; i++) {
				var bp = Matrix4x4.identity;
				bp.m13 = -y;
				_bindposes[i] = bp;
				inputParams.pieceIndex = i;
				inputParams.bi.current = i;
				inputParams.bi.next = i + 1;

				var i_triStart = i * 12;
				int i_vertexStart = i * 4;

				AddRect(_tris, i_triStart, i_vertexStart);

				if (inputParams.bi.next >= pieceCount) {
					inputParams.bi.next = pieceCount - 1;
				} else {
					AddRect(_tris, i_triStart + 6, i_vertexStart + 2);
				}
				inputParams.phe = getPieceHeight(i);
				InitIndexes(ref inputParams, ref _verts, ref _uv, ref _normals, ref _weights);
				y += inputParams.phe;
				inputParams.startY = y;
				inputParams.bi.previous = i;
			}

			_verts[0].y = 0;
			_verts[1].y = 0;
			_uv[0].y -= uvh;
			_uv[1].y -= uvh;

			var li = _verts.Length - 2;
			_verts[li].y = y;
			_verts[li + 1].y = y;
			_uv[li].y += uvh;
			_uv[li + 1].y += uvh;

			_m.vertices = _verts;
			_m.triangles = _tris;
			_m.normals = _normals;
			_m.uv = _uv;
			_m.bindposes = _bindposes;
			_m.boneWeights = _weights;
			if (useColor) {
				SetColors(_m, color);
			}
			skin.sharedMesh = _m;
			return y;
		}

		private static void InitIndexes(ref InputParams inputParams, ref Vector3[] verts, ref Vector2[] uv, ref Vector3[] normals, ref BoneWeight[] weights)
		{
			var i0 = inputParams.pieceIndex * 4;
			var _weightVal = inputParams.weightValue;
			var m = inputParams.material;

			var xplus = inputParams.boneWidth * (inputParams.surfaceOffset + 0.5f);
			var xminus = inputParams.boneWidth * (inputParams.surfaceOffset - 0.5f);
			var zplus = xplus * inputParams.surfaceNormal.z;
			var zminus = xminus * inputParams.surfaceNormal.z;
			xplus *= inputParams.surfaceNormal.x;
			xminus *= inputParams.surfaceNormal.x;
			verts[i0 + 0] = new Vector3(xminus, inputParams.startY + inputParams.phe * 0.25f, zminus);
			verts[i0 + 1] = new Vector3(xplus, inputParams.startY + inputParams.phe * 0.25f, zplus);
			verts[i0 + 2] = new Vector3(xminus, inputParams.startY + inputParams.phe * 0.75f, zminus);
			verts[i0 + 3] = new Vector3(xplus, inputParams.startY + inputParams.phe * 0.75f, zplus);

			inputParams.phe *= inputParams.textureHeightMultiplayer;
			var uvi = inputParams.uvyStart;
			var uvhw = 0.5f * inputParams.boneWidth / inputParams.textureScale;
			var x1yv = inputParams.uvxCenter - uvhw;
			var x2yv = inputParams.uvxCenter + uvhw;

			var uvv = inputParams.phe * 0.5f / inputParams.textureScale;
			inputParams.uvyStart = uvv * 2 + uvi;

			uv[i0 + 0] = new Vector2(x1yv, uvi);
			uv[i0 + 1] = new Vector2(x2yv, uvi);
			uv[i0 + 2] = new Vector2(x1yv, uvi + uvv);
			uv[i0 + 3] = new Vector2(x2yv, uvi + uvv);

			normals[i0 + 0] = new Vector3(0, 0, -1);
			normals[i0 + 1] = new Vector3(0, 0, -1);
			normals[i0 + 2] = new Vector3(0, 0, -1);
			normals[i0 + 3] = new Vector3(0, 0, -1);

			// make sure that first weight is the bigger one; complience with unity boneweight specification.
			// see : https://docs.unity3d.com/ScriptReference/Mesh-boneWeights.html
			if (_weightVal < 0.5f) {
				_weightVal = 1f - _weightVal;
			}

			BoneWeight bw = new BoneWeight {
				boneIndex0 = inputParams.bi.previous,
				boneIndex1 = inputParams.bi.current,
				weight0 = _weightVal,
				weight1 = 1f - _weightVal,
				weight2 = 0,
				weight3 = 0
			};
			weights[i0 + 0] = bw;
			weights[i0 + 1] = bw;

			bw.boneIndex0 = inputParams.bi.current;
			bw.boneIndex1 = inputParams.bi.next;
			bw.weight0 = _weightVal;
			bw.weight1 = 1f - _weightVal;
			weights[i0 + 2] = bw;
			weights[i0 + 3] = bw;
		}

		#region Internal data structures
		public struct InputParams
		{
			public Material material;
			public BoneIndexes bi;
			public int pieceIndex;
			public float weightValue;
			public float boneWidth;
			public float pieceLength;

			public float phe;
			public float startY;
			public Vector3 surfaceNormal;
			public float surfaceOffset;
			public float textureHeightMultiplayer;
			public float textureScale;
			public float uvxCenter;
			public float uvyStart;
		}

		public struct BoneIndexes
		{
			public int previous;
			public int current;
			public int next;
		}

		[Serializable]
		public class Piece
		{
			public Transform tr;
			[SerializeField]
			private readonly float _height;
			public float Height => _height;
			public Piece(Transform parent, Vector2 pos, Vector2 nextPos, int index)
			{
				var dif = (nextPos - pos);
				var mag = dif.magnitude;
				_height = mag;
				tr = new GameObject(index.ToString()).transform;
				tr.parent = parent;
				tr.localPosition = pos;
				tr.localScale = new Vector3(1, 1, 1);
			}
		}
		#endregion
	}
}
