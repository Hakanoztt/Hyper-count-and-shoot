using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Mobge.Core;
using UnityEngine.Tilemaps;

namespace Mobge.Core.Components {
    [CustomEditor(typeof(TilemapComponent))]
    public class ETilemapComponent : EComponentDefinition
    {
        private static Editor _currentEditor;
        public override EditableElement CreateEditorElement(BaseComponent dataObject) {
            return new Editor(dataObject as TilemapComponent.Data, this);
        }
        public class Editor : EditableElement<TilemapComponent.Data>
        {
            private Color _backupColor;
            private EditorTools _sceneTools;
            private LevelPlayer _levelPlayer;
            
            private Vector3 _mouseStartPos;

			private int _selectedDecorID = 0;
			private Texture2D _selectedEditorTileTexture;
            private Texture2D[] _editorTileTextureCache = new Texture2D[0];
            private Sprite[] _editorTileSpriteCache = new Sprite[0];
            private bool _edit;
            private bool _isDirty = true;
            private TilemapComponent.RendererList _renderer;

            private GridInfo s_tempGridinfo = new GridInfo();
			private GhostBrush _brush;

            private DecorationSet _oldDecor;
            private bool _isSceneToolSilhouetteActive;

			private bool HasDecorChanged
            {
                get
                {
                    if(_oldDecor != level.decorationSet.LoadedAsset)
                    {
                        _oldDecor = level.decorationSet.LoadedAsset;
                        return true;
                    }
                    return false;
                }
            }
            public Editor(TilemapComponent.Data component, ETilemapComponent editor) : base(component, editor)
            {
                // todo : need a acceptable way to access levelplayer(s)
                _levelPlayer = FindObjectOfType<LevelPlayer>(); // hack
                EnsureTools();
                EnsureObject();
                s_tempGridinfo.Init(component.atoms);
				_brush = new GhostBrush(this);
            }
            private void EnsureObject() {
                if(DataObjectT.atoms == null) {
                    DataObjectT.atoms = new Piece.Atom[0];
                }
            }

            private void DrawGrid()
            {
                // DrawGrid Culling logic: 
                // Internal camera is too far away for grid to matter; Don't draw it
                if (SceneView.currentDrawingSceneView.camera.orthographicSize > 130) return;
				var sr = DataObjectT.SelfRect;
                //sr.x = sr.x - _position.x;
                //sr.y = sr.y - _position.y;
                if(sr == default)
                {
                    sr.xMax += 1;
                    sr.yMax += 1;
                }
                else
                {
                    sr.xMax -= 0.5f; sr.xMin -= 0.5f; 
                    sr.yMax -= 0.5f; sr.yMin -= 0.5f;
                }

                HandleColor(true, new Color(1, 1, 0, 0.2f));
                for (float i = sr.xMin; i <= sr.xMax; i += 1)
                {
                    Handles.DrawLine(new Vector3(i, sr.yMin, 1), new Vector3(i, sr.yMax, 1));
                }
                for (float j = sr.yMin; j <= sr.yMax; j += 1)
                {
                    Handles.DrawLine(new Vector3(sr.xMin, j, 1), new Vector3(sr.xMax, j, 1));
                }
                Handles.ArrowHandleCap(GUIUtility.GetControlID(75, FocusType.Passive), Vector3.zero, Quaternion.identity, 2, Event.current.type);
                HandleColor();
            }

            public override void DrawGUILayout() {
                _currentEditor = this;
                EditToggle();
                DrawSelector("Select Tile ");
            }
            public override void UpdateVisuals(Transform instance) {
                if (_renderer != null && _renderer.Transform && _isDirty)
                {
                    DataObjectT.UpdateRenderer(level, _renderer, Vector3.zero);
                    _isDirty = false;
                }
            }
            public override Vector3 Position { 
                get => Vector3Int.RoundToInt(base.Position); 
                set => base.Position = Vector3Int.RoundToInt(value); 
            }
            public override Transform CreateVisuals()
            {
                // gets called on edit root
                _renderer = DataObjectT.CreateRenderer(level, null, Vector3.zero, false);
                return _renderer.Transform;
            }
            public override bool SceneGUI(in SceneParams @params) {
				var tempMatrix = Handles.matrix;
				Handles.matrix = tempMatrix * Matrix4x4.Translate(@params.position);
				if (_edit)
                {
                    _sceneTools.OnSceneGUI();
                    _brush.UnHide();
                }
                DrawSilhouettes();
                if (@params.selected)
                {
                    DrawGrid();
                    EnsureObject();
                }
                if (_brush != null)
                {
					_brush.UpdatePosition();
                }
				Handles.matrix = tempMatrix;
                var t = Event.current.type;
                return _edit && (t == EventType.Used || t == EventType.MouseUp);
			}
			private void DrawSilhouettes()
            {
                if (_isSceneToolSilhouetteActive)
                {
                    switch (_sceneTools.ActiveTool.name)
                    {
                        case "rectangle brush":
                        case "delete rectangle brush":
                            DrawRectangleSilhuette(MouseStart, MousePos);
                            break;
                        case "line brush":
                            DrawLineSilhuette();
                            break;
                    }
                    DrawPointSilhuette(MousePos);
                }
            }
            public Vector3 MousePos
            {
                get
                {
                    var ray = _sceneTools.MouseRay;
					var m = Handles.matrix.inverse;
					var o = m.MultiplyPoint3x4(ray.origin);
                    var d = m.MultiplyVector(ray.direction);
                    var pos = o + d * (o.z / -d.z);
                    return pos;
                }
            }
            private void EnsureTools()
            {
                if (_sceneTools == null)
                {
                    _sceneTools = new EditorTools();
                    _sceneTools.AddTool(new EditorTools.Tool("brush")
                    {
                        activation = new EditorTools.ActivationRule
                        {
                            mouseButton = 0,
                        },
                        onPress = DrawOneAtom,
                        onDrag = () => DrawOneAtom(),
                        onRelease = DumpGridToElement
                    });
                    _sceneTools.AddTool(new EditorTools.Tool("delete brush")
                    {
                        activation = new EditorTools.ActivationRule
                        {
                            mouseButton = 0,
                            modifiers = EventModifiers.Control
                        },
                        onPress = DeleteOneAtom,
                        onDrag = () => DeleteOneAtom(),
                    });
                    _sceneTools.AddTool(new EditorTools.Tool("line brush")
                    {
                        activation = new EditorTools.ActivationRule
                        {
                            mouseButton = 0,
                            modifiers = EventModifiers.Alt
                        },
                        onPress = SetMouseStartAndSilhuette,
                        onRelease = () =>
                        {
                            DrawAtomLine();
                            DisableSceneToolSilhouettes();
                        }
                    });
                    _sceneTools.AddTool(new EditorTools.Tool("rectangle brush")
                    {
                        activation = new EditorTools.ActivationRule
                        {
                            mouseButton = 0,
                            modifiers = EventModifiers.Shift
                        },
                        onPress = SetMouseStartAndSilhuette,
                        onRelease = () =>
                        {
                            DrawAtomRectangle();
                            DisableSceneToolSilhouettes();
                        }
                    });
                    _sceneTools.AddTool(new EditorTools.Tool("delete rectangle brush")
                    {
                        activation = new EditorTools.ActivationRule
                        {
                            mouseButton = 1,
                            modifiers = EventModifiers.Shift
                        },
                        onPress = SetMouseStartAndSilhuette,
                        onRelease = () =>
                        {
                            DeleteAtomRectangle();
                            DisableSceneToolSilhouettes();
                        }
                    });
                }
            }
			public class GhostBrush
			{
				private GameObject _brushGO;
				private SpriteRenderer _brushRenderer;
				private Editor _editorRef;

				public GhostBrush(Editor editor)
				{
					_editorRef = editor;
					EnsureGhostBrush();
				}

				public void UpdatePosition()
				{
					Vector3 mpos = MousePos;
					_brushGO.transform.localPosition = new Vector3(mpos.x, mpos.y);
				}

				public void UpdateSilhuette(Sprite sprite)
				{
					UnHide();
					_brushRenderer.sprite = sprite;
				}

				private void EnsureGhostBrush()
				{
					if (_brushGO == null) {
						_brushGO = new GameObject("_ghostBrush");
						_brushRenderer = _brushGO.AddComponent<SpriteRenderer>();
						_brushRenderer.color = new Color(_brushRenderer.color.r,_brushRenderer.color.g,_brushRenderer.color.b,0.3f);
						TemporaryEditorObjects.Shared.SetObject(_editorRef._levelPlayer, _brushGO.transform, _brushGO.transform.position);
					}
				}
				public void Hide()
				{
					if (_brushGO == null || !_brushGO.activeSelf) return;
					_brushGO.SetActive(false);
				}
				public void UnHide()
				{
					if (_brushGO == null || _brushGO.activeSelf) return;
					_brushGO.SetActive(true);
				}

				private Vector3 MousePos {
					get {
						var ray = _editorRef._sceneTools.MouseRay;
						var o = _editorRef._levelPlayer.transform.InverseTransformPoint(ray.origin);
						var d = _editorRef._levelPlayer.transform.InverseTransformVector(ray.direction);
						var pos = o + d * (o.z / -d.z);
						return pos;
					}
				}
			}


			private Vector3 MouseStart { 
                get { return _mouseStartPos; }
                set {
                    if (value != Vector3.zero)
                    _mouseStartPos = MousePos;
                }
            }
            private bool SetMouseStartAndSilhuette()
            {
                _isSceneToolSilhouetteActive = true;
                MouseStart = MousePos;
                return true;
            }
            private void DisableSceneToolSilhouettes()
            {
                _isSceneToolSilhouetteActive = false;
            }
            private void CacheTexturesFromSprites(bool ignoreCache = false)
            {
                if (level.decorationSet.LoadedAsset == null) return;
                if (!ignoreCache)
                {
                    if (_editorTileTextureCache.Length != 0) return;
                    if (_editorTileSpriteCache.Length != 0) return;
                }
                Sprite s;
                _editorTileTextureCache = new Texture2D[level.decorationSet.LoadedAsset.TilesetCount];
                _editorTileSpriteCache = new Sprite[level.decorationSet.LoadedAsset.TilesetCount];

                for (int i = 0; i < level.decorationSet.LoadedAsset.TilesetCount; i++)
                {
                    IPieceVisualizer v = level.decorationSet.LoadedAsset.GetPieceVisualizer(i);
                    //if (v.GetType() == typeof(RuleTile))
                    //{
                    //    var rt = (RuleTile)v;
                    //    s = rt.m_DefaultSprite;
                    //    s.name = rt.name;
                    //    SetTextureToCache(i, ref s);
                    //}
                    //else if (v.GetType() == typeof(UnityRuleTileAdapter))
                    //{
                    //    var rt = (UnityRuleTileAdapter)v;
                    //    s = rt.m_DefaultSprite;
                    //    s.name = rt.name;
                    //    SetTextureToCache(i, ref s);
                    //}
                }
            }
            private void SetTextureToCache(int i, ref Sprite s)
            {
                if (s.rect.width != s.texture.width)
                {
                    Texture2D nt = new Texture2D((int)s.rect.width, (int)s.rect.height);
                    Color[] newColors = s.texture.GetPixels((int)s.textureRect.x,
                                                             (int)s.textureRect.y,
                                                             (int)s.textureRect.width,
                                                             (int)s.textureRect.height);
                    nt.SetPixels(newColors);
                    nt.Apply();
                    _editorTileTextureCache[i] = nt;
                    _editorTileSpriteCache[i] = s;
                }
                else
                {
                    _editorTileTextureCache[i] = s.texture;
                    _editorTileSpriteCache[i] = s;
                }
            }
            protected void DrawSelector(string buttonText)
            {
                if (level.decorationSet.LoadedAsset == null) return;

                if (HasDecorChanged)
                {
                    CacheTexturesFromSprites(ignoreCache: true);
                    if (_editorTileTextureCache.Length > 0 && _editorTileTextureCache[0] != null)
                    {
                        _selectedEditorTileTexture = _editorTileTextureCache[0];
                    }
                    if (_editorTileSpriteCache.Length > 0 && _editorTileSpriteCache[0] != null)
                    {
						_brush.UpdateSilhuette(_editorTileSpriteCache[0]);
                    }
                }
                else
                {
                    CacheTexturesFromSprites();
                }

                var r = EditorGUILayout.GetControlRect(false, 2);
                var tmpColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.yellow;

                var gs = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(5, 5, 5, 5)
                };
                if (UnityEngine.GUILayout.Button(buttonText, gs))
                {
                    EditorPopup p = new EditorPopup((rects, popup) => {
                        for (int i = 0; i < _editorTileTextureCache.Length; i++)
                        {

                            var gc = new GUIContent(_editorTileSpriteCache[i].name, _editorTileTextureCache[i]);
                            if (GUI.Button(rects.NextRect(40), gc, gs))
                            {
                                _selectedDecorID = i;
                                _brush.UpdateSilhuette(_editorTileSpriteCache[i]);
                                _selectedEditorTileTexture = _editorTileTextureCache[i];
                                popup.Close();
                            }
                        }
                    });
                    // Works on MacOSX as intended, broken on windows.
                    //p.Show(new Rect(new Vector2(r.position.x - 4f,
                    //                      r.position.y + EditorGUIUtility.singleLineHeight * 1.5f),
                    //                      Vector2.zero),
                    // new Vector2(r.size.x - 6,
                    //            (Screen.height / 2) - (r.position.y + (EditorGUIUtility.singleLineHeight * 4)))
                    //);
                    p.Show(new Rect(Event.current.mousePosition, Vector2.zero), new Vector2(300, 300));
                }
                if (_selectedEditorTileTexture == null)
                {
                    _selectedDecorID = 0;
                    _selectedEditorTileTexture = _editorTileTextureCache[0];
					_brush.UpdateSilhuette(_editorTileSpriteCache[0]);
				}
                var gcselected = new GUIContent("Curently selected", _selectedEditorTileTexture);
                GUI.Label(new Rect(r.width - EditorGUIUtility.fieldWidth - 60,
                                   r.position.y + 7,
                                   r.size.x - EditorGUIUtility.labelWidth,
                                   r.size.y * 10), gcselected);
                GUI.backgroundColor = tmpColor;
            }

            private bool DeleteOneAtom()
            {
                GridInfo.Int2 k = new GridInfo.Int2(
                    Mathf.RoundToInt(MousePos.x),
                    Mathf.RoundToInt(MousePos.y)
                );
                if (s_tempGridinfo == null) return false;
                if (s_tempGridinfo.Data.Remove(k))
                    DumpGridToElement();
                return true;
            }
            private bool DrawOneAtom()
            {
                if (s_tempGridinfo == null) return false;
                var mpos = MousePos;
                AddToGrid(mpos.x, mpos.y);
                DumpGridToElement();
                return true;
            }
            private bool DrawAtomLine()
            {
                if (s_tempGridinfo == null) return false;
                s_tempGridinfo.InsertLine(MouseStart, MousePos, _selectedDecorID);
                DumpGridToElement();
                return true;
            }
            private bool DrawAtomRectangle()
            {
                if (s_tempGridinfo == null) return false;
                s_tempGridinfo.InsertRectangle(MouseStart, MousePos, _selectedDecorID);
                DumpGridToElement();
                return true;
            }
            private bool DeleteAtomRectangle()
            {
                if (s_tempGridinfo == null) return false;
                s_tempGridinfo.DeleteRectangle(MouseStart, MousePos);
                DumpGridToElement();
                return true;
            }
            private void DumpGridToElement()
            {
                DataObjectT.atoms = s_tempGridinfo.GetOptimizedAtoms();
                UpdateVisuals(null);
                UpdateData();
                _isDirty = true;
                SceneView.RepaintAll();
            }
            private void HandleColor(bool isSet = false, Color color = default)
            {
                if (isSet)
                {
                    _backupColor = Handles.color;
                    Handles.color = color;
                }
                else
                    Handles.color = _backupColor;
            }
            private void DrawPointSilhuette(Vector2 point, float scale = 1)
            {
                HandleColor(true, Color.yellow);
                var size = HandleUtility.GetHandleSize(point);
                Handles.DrawSolidDisc(point, Vector3.forward, scale * size * 0.04f);
                HandleColor();
            }
            private void DrawLineSilhuette()
            {
                HandleColor(true, Color.yellow);
                Handles.DrawLine(MouseStart, MousePos);
                HandleColor();
            }
            private void DrawRectangleSilhuette(Vector2 p1, Vector2 p2)
            {
                var rect = new Rect(p1,(p2 - p1));
                Handles.DrawSolidRectangleWithOutline(rect, Color.yellow/2, Color.blue);
            }
            private void AddToGrid(float x, float y)
            {
                var k = new GridInfo.Int2(
                    Mathf.RoundToInt(x),
                    Mathf.RoundToInt(y));
                s_tempGridinfo.AddToGrid(k, _selectedDecorID);
            }

            private void EditToggle()
            {
                _edit = ExclusiveEditField("edit on scene");
            }

            public override string Name => "Tilemap";
        }
    }
}
