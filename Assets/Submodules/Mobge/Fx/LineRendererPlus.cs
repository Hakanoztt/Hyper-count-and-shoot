using System;
using UnityEngine;

namespace Mobge
{
    public class LineRendererPlus : MonoBehaviour
    {

        private static MeshBuilder _meshBuilder = new MeshBuilder();

        public bool reconstructOnAwake = true;
        private ReconstructMode _reconstructMode = ReconstructMode.ReconstructAll;
        protected void Awake() {
            enabled = false;
            if (reconstructOnAwake) {
                Reconstruct(ReconstructMode.ReconstructAll);
            }
        }
        [SerializeField]
        [HideInInspector]
        private float _middleUvHeight;
        public float MiddleUVHeight => _middleUvHeight;
        [OwnComponent(true)]
        [SerializeField]
        private SkinnedMeshRenderer _skin = null;
        public SkinnedMeshRenderer Skin => _skin;
        [SerializeField]
        [HideInInspector]
        private Transform[] _pieces;
        [SerializeField]
        [HideInInspector]
        private float[] _stitchPositions = new float[] { 1f };

        public float[] StitchPositions {
            get => (float[])_stitchPositions.Clone();
            set {
                if (value == null || value.Length == 0) {
                    return;
                }
                if (!IsEqual(value, _stitchPositions)) {
                    _stitchPositions = value;

                    Reconstruct(ReconstructMode.ReconstructMesh);
                }
            }
        }
        private bool IsEqual(float[] l1, float[] l2) {
            if (l1.Length != l2.Length) return false;
            for (int i = 0; i < l1.Length; i++) {
                if (l1[i] != l2[i]) {
                    return false;
                }
            }
            return true;
        }
        [SerializeField]
        [HideInInspector]
        private QualityMode _quality;
        public QualityMode Quality {
            get => _quality;
            set {
                if (_quality != value) {
                    _quality = value;
                    Reconstruct(ReconstructMode.ReconstructMesh);
                }
            }
        }
        [SerializeField]
        [HideInInspector]
        private bool _hasColors;
        public bool HasColors {
            get => _hasColors;
            set {
                if (_hasColors != value) {
                    _hasColors = value;
                    Reconstruct(ReconstructMode.ReconstructMesh);
                }
            }
        }
        [HideInInspector]
        public Gradient color = new Gradient();
        public void RefreshColors() {
            if (_hasColors) {
                Reconstruct(ReconstructMode.ReconstructColors);
            }
        }
        public Transform GetPiece(int index) {
            return _pieces[index];
        }

        public Transform[] GetPieces() {
            return _pieces;
        }

        [SerializeField] [HideInInspector] private float _pieceLength = 1f;
        public float PieceLength {
            get => _pieceLength;
            set {
                value = Mathf.Max(0.001f, value);
                if (!Mathf.Approximately(_pieceLength, value)) {
                    _isPropertiesValid = false;
                    _pieceLength = value;
                    Reconstruct(ReconstructMode.ReconstructPieces);
                }
            }
        }
        [SerializeField] [HideInInspector] private int _pieceCount = 5;
        public int PieceCount {
            get => _pieceCount;
            set {
                value = Mathf.Max(1, value);
                if (_pieceCount != value) {
                    _isPropertiesValid = false;
                    _pieceCount = value;
                    Reconstruct(ReconstructMode.ReconstructAll);
                }
            }
        }
        [SerializeField]
        [HideInInspector]
        private bool _isPropertiesValid = false;
        [SerializeField]
        [HideInInspector]
        private Piece[] _pieceProperties;
        public Piece this[int index] {
            get {
                if (!_isPropertiesValid) {
                    var p = new Piece();
                    p.length = _pieceLength;
                    return p;
                }
                return _pieceProperties[index];
            }
            set {
                ValidateProperties();
                var p = _pieceProperties[index];
                if (!p.Equals(value)) {
                    _pieceProperties[index] = value;
                    Reconstruct(ReconstructMode.ReconstructPieces);
                }
            }
        }
        [SerializeField]
        [HideInInspector]
        private float _totalLength;
        public float TotalLength => _totalLength;
        void ValidateProperties() {

            if (!_isPropertiesValid) {
                if (_pieceProperties == null || _pieceProperties.Length != _pieceCount) {
                    _pieceProperties = new Piece[_pieceCount];
                }
                for (int i = 0; i < _pieceProperties.Length; i++) {
                    _pieceProperties[i].length = _pieceLength;
                }
                _isPropertiesValid = true;
            }

        }
        [SerializeField] [HideInInspector] private Sprites _sprites;
        public Sprites SpriteSet {
            get => _sprites;
            set {
                if (!_sprites.Equals(value)) {
                    _sprites = value;
                    Reconstruct(ReconstructMode.ReconstructMesh);
                }
            }
        }
        [HideInInspector] [SerializeField] private float _width = 1f;
        public float Width {
            get => _width;
            set {
                value = Mathf.Max(0.001f, value);
                if (!Mathf.Approximately(_width, value)) {
                    _width = value;
                    Reconstruct(ReconstructMode.ReconstructMesh);
                }
            }
        }
        public PiecePhysics[] AddPhysics(in PhysicalProperties properties) {
            ReconstructImmediate();
            int pieceCount = _pieceCount;
            PiecePhysics[] bodies = new PiecePhysics[pieceCount];
            ValidateProperties();
            for (int i = 0; i < pieceCount; i++) {
                var p = _pieces[i];
                bodies[i] = AddPhysics(properties, _pieceProperties[i].length, p);
            }
            int prevI = 0;
            Rigidbody2D prevBody = bodies[0].body;

            for(int i = 1; i < pieceCount; prevI = i, i++) {
                Rigidbody2D nextBody = bodies[i].body;
                var hj = prevBody.gameObject.AddComponent<HingeJoint2D>();
                hj.autoConfigureConnectedAnchor = false;
                var pl = _pieceProperties[prevI].length;
                hj.anchor = new Vector2(0, pl);
                hj.connectedBody = nextBody;
                hj.connectedAnchor = Vector2.zero;



                prevBody = nextBody;
            }
            return bodies;
        }
        private PiecePhysics AddPhysics(in PhysicalProperties properties, float length, Transform p) {
            var pgo = p.gameObject;
            PiecePhysics pp;
            pp.body = pgo.AddComponent<Rigidbody2D>();
            pp.body.mass = properties.pieceMass;
            pp.body.drag = properties.pieceDrag;
            pp.body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (properties.hasCollider) {
                pp.collider = pgo.AddComponent<CapsuleCollider2D>();
                pp.collider.offset = new Vector2(0, length * 0.5f);
                pp.collider.direction = CapsuleDirection2D.Vertical;
                float thickness = _width * properties.thicknessMultiplayer;
                pp.collider.size = new Vector2(thickness, thickness + length);
            }
            else {
                pp.collider = null;
            }
            return pp;
        }
        private void Reconstruct(ReconstructMode value) {
            _reconstructMode = (ReconstructMode)Mathf.Max((int)value, (int)_reconstructMode);
            enabled = true;
        }
        public bool IsValuesValid(out string error) {
            if (!_skin) {
                error = "Skin must be set.";
                return false;
            }
            var mat = _skin.sharedMaterial;
            if (!mat) {
                error = "Material of skin must be set.";
                return false;

            }
            if (_stitchPositions == null || _stitchPositions.Length == 0) {
                error = nameof(_stitchPositions) + " must have more than 0 elements.";
                return false;
            }
            var t = mat.mainTexture;
            if (!t) {
                error = "Material of skin must have a main texture";
                return false;
            }
            if (!_sprites.middle) {
                error = "Middle sprite must be set.";
                return false;
            }
            bool textureMismatch = _sprites.middle.texture != t;
            if (!textureMismatch) {
                if (_sprites.bottom) {
                    textureMismatch = textureMismatch || (_sprites.bottom.texture != t);
                }
                if (_sprites.top) {
                    textureMismatch = textureMismatch || (_sprites.top.texture != t);
                }
            }
            if (textureMismatch) {
                error = "Textures of all sprites must be the same with main texture of material.";
                return false;
            }
            error = null;
            return true;

        }
        public void ReconstructImmediate() {
            Update();
        }

        public void Initialize(in Vector2[] positions, float width) {
            PieceCount = positions.Length - 1;
            Width = width;
            GeneratePieces(in positions);
            ReconstructImmediate();
            SetRotationsPositions(in positions);
        }

        /// <summary>
        /// Sets the rotations and positions of pieces
        /// </summary>
        /// <param name="positions">Positions.</param>
        public void SetRotationsPositions(in Vector2[] positions) {
            // Set pieces rotation & position
            var pos = positions[0];
            for (int i = 1; i < positions.Length; i++) {
                var newPos = positions[i];
                var dif = newPos - pos;
                Transform tr = GetPiece(i - 1);
                tr.localRotation = Quaternion.LookRotation(Vector3.forward, dif);
                tr.localPosition = pos;
                pos = newPos;
            }
        }
        /// <summary>
        /// Generates the pieces.
        /// </summary>
        /// <param name="positions">Positions.</param>
        public void GeneratePieces(in Vector2[] positions) {
            Vector2 pos = positions[0];
            for (int i = 1; i < positions.Length; i++) {
                var newPos = positions[i];
                var dif = newPos - pos;
                Piece p;
                p.length = dif.magnitude;
                this[i - 1] = p;
                pos = newPos;
            }
        }

        protected void Update() {
            if (_reconstructMode != ReconstructMode.None && !IsValuesValid(out string error)) {
                Debug.LogError(error, this);
                enabled = false;
                return;
            }
            switch (_reconstructMode) {
                case ReconstructMode.ReconstructColors:
                    ReconstructColorsDirect();
                    break;
                case ReconstructMode.ReconstructMesh:
                    ReconstructMeshDirect(_meshBuilder);
                    break;
                case ReconstructMode.ReconstructPieces:
                    ReconstructPiecesDirect();
                    break;
                case ReconstructMode.ReconstructAll:
                    ReconstructAllDirect();
                    break;
            }
            _reconstructMode = ReconstructMode.None;
            enabled = false;
        }
        private void ReconstructAllDirect() {
            var parent = transform;
            parent.DestroyAllChildren();
            _pieces = new Transform[_pieceCount];
            for (int i = 0; i < _pieceCount; i++) {
                Transform p;
                p = new GameObject("p " + i).transform;
                p.SetParent(parent, false);
                _pieces[i] = p;
            }
            _skin.bones = _pieces;

            _skin.rootBone = _pieces[0];
            ReconstructPiecesDirect();
        }
        private void ReconstructPiecesDirect() {
            Vector3 pos = Vector3.zero;
            var mb = _meshBuilder;
            if (_isPropertiesValid) {
                //Debug.Log("l c: " + _pieces.Length);
                for (int i = 0; i < _pieceCount; i++) {
                    //Debug.Log("1-l: " + i);
                    _pieces[i].localPosition = pos;
                    pos.y += _pieceProperties[i].length;
                    //Debug.Log("l: " + i + " " + _pieceProperties[i].length);
                }
            }
            else {
                for (int i = 0; i < _pieceCount; i++) {
                    _pieces[i].localPosition = pos;
                    pos.y += _pieceLength;
                }
            }
            for (int i = 0; i < _pieceCount; i++) {
                Matrix4x4 m = Matrix4x4.Translate(new Vector3(0, -_pieces[i].localPosition.y, 0));
                mb.bindposes.Add(m);
            }
            _totalLength = pos.y;

            ReconstructMeshDirect(mb);
        }
        private void ReconstructMeshDirect(MeshBuilder mb) {
            if (_quality == QualityMode.Bone2) {
                _skin.quality = SkinQuality.Bone2;
            }
            else {
                _skin.quality = SkinQuality.Bone4;
            }
            var mesh = _skin.sharedMesh;
            if (mesh == null) {
                mesh = new Mesh();
                _skin.sharedMesh = mesh;
            }

            var sp = _sprites;

            Rect startRect = GetUvRect(sp.bottom);
            Rect middleRect = GetUvRect(sp.middle);
            Rect endRect = GetUvRect(sp.top);
            float maxWidth;
            maxWidth = startRect.width;
            maxWidth = Mathf.Max(maxWidth, middleRect.width);
            maxWidth = Mathf.Max(maxWidth, endRect.width);
            float startHeight = (startRect.height / maxWidth) * _width;
            float middleHeight = (middleRect.height / maxWidth) * _width;
            float endHeight = (endRect.height / maxWidth) * _width;
            float middleRealHeight;
            if (startHeight + endHeight > _totalLength) {
                float rate = _totalLength / (startHeight + endHeight);
                startHeight *= rate;
                endHeight *= rate;
                middleRealHeight = 0;
            }
            else {
                middleRealHeight = _totalLength - startHeight - endHeight;
            }
            StichProgress progress = new StichProgress(_stitchPositions);
            PutSprite(mb, startRect, _width * startRect.width / maxWidth, startHeight, ref progress);
            _middleUvHeight = middleRect.height * middleRealHeight / middleHeight;
            if (_sprites.loopMiddle) {
                middleRect.height = _middleUvHeight;
                middleRect.y = -_middleUvHeight;
                PutSprite(mb, middleRect, _width * middleRect.width / maxWidth, middleRealHeight, ref progress);
            }
            else {
                while (middleRealHeight > middleHeight) {
                    PutSprite(mb, middleRect, _width * middleRect.width / maxWidth, middleHeight, ref progress);
                    middleRealHeight -= middleHeight;

                }
                PutSprite(mb, middleRect, _width * middleRect.width / maxWidth, middleRealHeight, ref progress);
            }
            PutSprite(mb, endRect, _width * endRect.width / maxWidth, endHeight, ref progress);

            if (_hasColors) {
                FillColors(mb);
            }
            _meshBuilder.BuildMesh(mesh);
            _meshBuilder.Clear();
            CalculateBoundingBox();
        }
        private void FillColors(MeshBuilder mb) {
            for (int i = 0; i < mb.vertices.Count; i++) {
                var v = mb.vertices[i];
                var progress = v.y / _totalLength;
                var c = color.Evaluate(progress);
                mb.colors.Add(c);
            }
        }
        private void ReconstructColorsDirect() {
            if (!_hasColors) {
                return;
            }
            var m = _skin.sharedMesh;
            m.GetVertices(_meshBuilder.vertices);
            FillColors(_meshBuilder);
            m.SetColors(_meshBuilder.colors);
            _meshBuilder.colors.Clear();
            _meshBuilder.vertices.Clear();

        }
        private Rect GetUvRect(Sprite sprite) {
            if (sprite == null) {
                return new Rect(0f, 0f, 1f, 0f);
            }
            var t = sprite.texture;
            var ts = new Vector2(1f / t.width, 1f / t.height);
            var r = sprite.rect;
            r = new Rect(r.position * ts, r.size * ts);
            return r;
        }
        private void PutSpriteTest(MeshBuilder mb, Rect uvRect, float width, float realLength, ref StichProgress progress) {
            if (realLength == 0) {
                return;
            }
            float uvx1 = uvRect.xMin;
            float uvx2 = uvRect.xMax;
            float uvy1 = uvRect.yMin;
            float uvy2 = uvRect.yMax;
            int indice = mb.vertices.Count;
            mb.vertices.Add(new Vector3(-width * 0.5f, progress.offset, 0));
            mb.vertices.Add(new Vector3(width * 0.5f, progress.offset, 0));
            mb.uvs.Add(new Vector2(uvx1, uvy1));
            mb.uvs.Add(new Vector2(uvx2, uvy1));
            progress.offset += realLength;
            mb.vertices.Add(new Vector3(-width * 0.5f, progress.offset, 0));
            mb.vertices.Add(new Vector3(width * 0.5f, progress.offset, 0));
            mb.uvs.Add(new Vector2(uvx1, uvy2));
            mb.uvs.Add(new Vector2(uvx2, uvy2));
            mb.AddTriangle(indice + 0, indice + 3, indice + 1);
            mb.AddTriangle(indice + 0, indice + 2, indice + 3);
        }
        private void PutSprite(MeshBuilder mb, Rect uvRect, float width, float realLength, ref StichProgress progress) {
            if (realLength == 0) {
                return;
            }
            float startOffset = progress.offset;
            float target = progress.offset + realLength;
            float rLength = 1f / realLength;
            float uvx1 = uvRect.xMin;
            float uvx2 = uvRect.xMax;
            float uvy1 = uvRect.yMin;
            float uvy2 = uvRect.yMax;
            int indice = mb.vertices.Count;
            mb.vertices.Add(new Vector3(-width * 0.5f, progress.offset, 0));
            mb.vertices.Add(new Vector3(width * 0.5f, progress.offset, 0));
            mb.uvs.Add(new Vector2(uvx1, uvy1));
            mb.uvs.Add(new Vector2(uvx2, uvy1));
            BoneWeight bw = new BoneWeight();
            if (_quality == QualityMode.Bone2) {
                progress.CurrentWeight(_pieceCount, this, ref bw);
            }
            else {
                progress.CurrentWeightHQ(_pieceCount, this, ref bw);
            }
            mb.boneWeights.Add(bw);
            mb.boneWeights.Add(bw);
            bool cont;
            do {
                cont = progress.MoveProgress(target, this);
                mb.vertices.Add(new Vector3(-width * 0.5f, progress.offset, 0));
                mb.vertices.Add(new Vector3(width * 0.5f, progress.offset, 0));
                float uvProgress = (progress.offset - startOffset) * rLength;
                float uvy = uvy1 + (uvy2 - uvy1) * uvProgress;
                mb.uvs.Add(new Vector2(uvx1, uvy));
                mb.uvs.Add(new Vector2(uvx2, uvy));
                if (_quality == QualityMode.Bone2) {
                    progress.CurrentWeight(_pieceCount, this, ref bw);
                }
                else {
                    progress.CurrentWeightHQ(_pieceCount, this, ref bw);
                }
                mb.boneWeights.Add(bw);
                mb.boneWeights.Add(bw);

                mb.AddTriangle(indice + 0, indice + 3, indice + 1);
                mb.AddTriangle(indice + 0, indice + 2, indice + 3);
                indice += 2;
            }
            while (cont);
        }
        public void CalculateBoundingBox() {
            _skin.localBounds = new Bounds(Vector3.zero, new Vector3(_totalLength * 2, _totalLength * 2));
        }
        public void CalculateBoundingBoxTight() {
            _skin.sharedMesh.RecalculateBounds();
            _skin.localBounds = _skin.sharedMesh.bounds;
        }
        [Serializable]
        public struct Piece
        {
            public float length;
        }

        private struct StichProgress
        {
            public int boneIndex;
            public float offset;
            float _height;
            private float[] _stitchPositions;
            private int _nextStitch;
            private float _prevLength;

            public StichProgress(float[] stitchPosition) {
                _stitchPositions = stitchPosition;
                boneIndex = 0;
                _height = 0;
                offset = 0;
                _nextStitch = stitchPosition.Length - 1;
                _prevLength = 0;
            }

            public void CurrentWeight(int boneCount, LineRendererPlus renderer, ref BoneWeight bw) {
                float progress = _height / renderer[boneIndex].length;
                if (progress < 0.5f) {
                    bw.boneIndex0 = Mathf.Max(boneIndex - 1, 0);
                    bw.boneIndex1 = boneIndex;
                    bw.weight0 = 0.5f - progress;
                    bw.weight1 = 1 - bw.weight0;
                }
                else {
                    bw.boneIndex0 = boneIndex;
                    bw.boneIndex1 = Mathf.Min(boneIndex + 1, boneCount - 1);
                    bw.weight0 = 1.5f - progress;
                    bw.weight1 = 1 - bw.weight0;
                }
            }
            public void CurrentWeightHQ(int boneCount, LineRendererPlus renderer, ref BoneWeight bw) {
                float progress = _height / renderer[boneIndex].length;
                bw.boneIndex0 = Mathf.Max(boneIndex - 1, 0);
                bw.boneIndex1 = boneIndex;
                bw.boneIndex2 = Mathf.Min(boneIndex + 1, boneCount - 1);
                bw.weight2 = progress * 0.5f;
                bw.weight1 = 0.5f;
                bw.weight0 = 0.5f - bw.weight2;
            }
            public bool MoveProgress(float target, LineRendererPlus renderer) {
                var pieceLength = renderer[boneIndex].length;
                if (_nextStitch == _stitchPositions.Length) {
                    _nextStitch = 0;
                    boneIndex++;
                    _prevLength += pieceLength;

                    if (boneIndex == renderer._pieceCount) {
                        offset = _prevLength;
                        _height = pieceLength;
                        boneIndex--;
                        return false;
                    }
                }
                float nextHeight = _stitchPositions[_nextStitch] * pieceLength;
                float next = _prevLength + nextHeight;
                _height = nextHeight;
                if (target <= next) {
                    offset = target;
                    _height -= next - target;
                    return false;
                }
                else {
                    offset = next;
                    _nextStitch++;
                    return true;
                }
            }
        }
        private enum ReconstructMode
        {
            None = 0,
            ReconstructColors = 1,
            ReconstructMesh = 2,
            ReconstructPieces = 3,
            ReconstructAll = 4
        }

        [Serializable]
        public struct Sprites
        {
            [SerializeField] public Sprite middle;
            [SerializeField] public Sprite bottom;
            [SerializeField] public Sprite top;
            [SerializeField] public bool loopMiddle;
        }
        public enum QualityMode
        {
            Bone2 = 0,
            Bone4 = 1,
        }

        [Serializable]
        public struct PhysicalProperties
        {
            public float pieceMass;
            public float thicknessMultiplayer;
            public float pieceDrag;
            public bool hasCollider;
        }
        [Serializable]
        public struct PiecePhysics
        {
            public Rigidbody2D body;
            public CapsuleCollider2D collider;
        }
    }
}