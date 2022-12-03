//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//using UnityEngine.AddressableAssets;
//using System;

//namespace Mobge.Core
//{
//    [CustomEditor(typeof(BackGroundRulePlayer), true)]
//    public class EBackGroundRulePlayer : ELevelPlayer
//    {
//        private const Tool c_toolId = (Tool)4949;
//        private Tool _previousTool;
//        public new List<SceneMode> SceneModes { get; private set; }

//        private GridInfo s_tempGridinfo = new GridInfo();

//        private BackGroundRulePlayer _bgPlayer;
//        private Level _level;
//        //private Piece.PieceRef pieceRef;
//        private Vector3 _mouseStartPos;
//        private bool _isSceneToolSilhouetteActive;

//        private Vector3 MouseStart
//        {
//            get
//            {
//                return _mouseStartPos;
//            }
//            set
//            {
//                if (value != Vector3.zero)
//                    _mouseStartPos = MousePos;
//            }
//        }
//        private ElementEditor _elementEditor;
//        private Color _backupColor;

//        public override void OnInspectorGUI()
//        {
//            if (_bgPlayer == null) return;
//            if (_bgPlayer.level != null) _bgPlayer.RefreshTiles();

//            if (_bgPlayer.level)
//            {
//                _bgPlayer.level.decorationSet.SetEditorAsset(EditorLayoutDrawer.ObjectField("Decoration Set", _bgPlayer.level.decorationSet.LoadedAsset));
//                if (_bgPlayer.level.decorationSet.LoadedAsset != null)
//                {
//                    ModeSwitchField();
//                    EnsureTools();
//                    if (_sceneModeNames[_sceneMode] == "Sprite")
//                    {
//                        EditEnabled = true;
//                        _elementEditor.InspectorField();
//                        HideGhostBrush();
//                    }
//                    else if (_sceneModeNames[_sceneMode] == "Rule")
//                    {
//                        EditEnabled = true;
//                        ReportGUIButton();
//                        EnsureGhostBrush();
//                        CacheTexturesFromSprites();
//                        //DrawDecorationSelector("Rule Select");
//                    }
//                    _bgPlayer.RefreshTiles();
//                }
//                else
//                    EditEnabled = false;
//            }
//            else
//                EditEnabled = false;
//        }

//        protected override void OnSceneGUI()
//        {
//            if (!_bgPlayer || !_bgPlayer.level) return;
//            DrawSilhouettes();
//            if (_sceneMode >= 0 && _sceneMode < SceneModes.Count && _bgPlayer.level.decorationSet.LoadedAsset != null)
//            {
//                SceneModes[_sceneMode].SceneGUI();
//            }
//        }

//        private void RuleSceneGUI()
//        {
//            _sceneTools.OnSceneGUI();
//            if (Event.current.type == EventType.Used)
//            {
//                Repaint();
//                _bgPlayer.RefreshTiles();
//            }
//            if (_ghostBrush)
//            {
//                _ghostBrush.transform.position = new Vector3(MousePos.x, MousePos.y);
//            }
//        }
//        public override Vector3 MousePos
//        {
//            get
//            {
//                var ray = _sceneTools.MouseRay;
//                var o = _bgPlayer.transform.InverseTransformPoint(ray.origin);
//                var d = _bgPlayer.transform.InverseTransformVector(ray.direction);
//                var pos = o + d * (o.z / -d.z);
//                return pos;
//            }
//        }
//        private void DrawGrid()
//        {
//            if (_bgPlayer.level == null) return;
//            float xMax = float.NegativeInfinity;
//            float xMin = float.PositiveInfinity;
//            float yMax = float.NegativeInfinity;
//            float yMin = float.PositiveInfinity;
//            if (_bgPlayer.level.atoms.Length > 0)
//            {
//                foreach (Piece.Atom a in _bgPlayer.level.atoms)
//                {
//                    if (xMax < a.rectangle.xMax)
//                    {
//                        xMax = Mathf.RoundToInt(a.rectangle.xMax);
//                    }

//                    if (xMin > a.rectangle.xMin)
//                    {
//                        xMin = Mathf.RoundToInt(a.rectangle.xMin);
//                    }

//                    if (yMax < a.rectangle.yMax)
//                    {
//                        yMax = Mathf.RoundToInt(a.rectangle.yMax);
//                    }

//                    if (yMin > a.rectangle.yMin)
//                    {
//                        yMin = Mathf.RoundToInt(a.rectangle.yMin);
//                    }
//                }
//            }
//            else
//            {
//                xMax = 1;
//                xMin = 0;
//                yMax = 1;
//                yMin = 0;
//            }
//            xMax -= 0.5f;
//            xMin -= 0.5f;
//            yMax -= 0.5f;
//            yMin -= 0.5f;
//            Handles.color = new Color(1, 1, 0, 0.2f);
//            for (float i = xMin; i <= xMax; i += 1)
//            {
//                Handles.DrawLine(new Vector3(i, yMin, 1), new Vector3(i, yMax, 1));
//            }
//            for (float j = yMin; j <= yMax; j += 1)
//            {
//                Handles.DrawLine(new Vector3(xMin, j, 1), new Vector3(xMax, j, 1));
//            }
//            Handles.ArrowHandleCap(GUIUtility.GetControlID(75, FocusType.Passive), Vector3.zero, Quaternion.identity, 2, Event.current.type);
//        }

//        protected new void ModeSwitchField()
//        {
//            if (_sceneModeNames == null || _sceneModeNames.Length != SceneModes.Count)
//            {
//                _sceneModeNames = new string[SceneModes.Count];
//            }
//            for (int i = 0; i < SceneModes.Count; i++)
//            {
//                _sceneModeNames[i] = SceneModes[i].name;
//            }
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("edit mode", GUILayout.Width(EditorGUIUtility.labelWidth));
//            _sceneMode = GUILayout.Toolbar((int)_sceneMode, _sceneModeNames);
//            EditorGUILayout.EndHorizontal();
//        }



//        //public new void OnDisable()
//        //{
//        //    if (!_bgPlayer) return;
//        //    if (_bgPlayer.gameObject != null) _bgPlayer.gameObject.SetActive(false);
//        //    if (_bgPlayer.caller != null) _bgPlayer.caller.SetActive(true);
//        //}

//        protected override void OnEnable()
//        {
//            base.OnEnable();
//            _bgPlayer = target as BackGroundRulePlayer;
//            _bgPlayer.gameObject.name = "_BackgroundRuleEditor";
//            _elementEditor = ElementEditor.NewForScene(this, new Plane(new Vector3(0, 0, -1), 0));
//            _bgPlayer.transform.position = Vector3.zero;
//            //_bgPlayer.transform.SetParent(TemporaryEditorObjects.Shared.transform, true);


//            s_tempGridinfo.Init(_bgPlayer.bgr ? _bgPlayer.bgr : _bgPlayer.Setup());
//            SceneModes = new List<SceneMode>
//            {
//                //new SceneMode("Sprite", SpriteSceneGUI),
//                new SceneMode("Rule", RuleSceneGUI)
//            };

//            // todo create this on the fly.
//            var test = AssetDatabase.LoadAssetAtPath("Assets/Test/Core Test/Data/Decorations/BackGroundRule/BackgroundRuleDecoration.asset", typeof(DecorationSet));
//            _bgPlayer.level.decorationSet.SetEditorAsset(test);
//        }
//        #region Scene Tool Capabilities
//        private bool DeleteOneAtom()
//        {
//            GridInfo.Int2 k = new GridInfo.Int2(
//                Mathf.RoundToInt(MousePos.x),
//                Mathf.RoundToInt(MousePos.y)
//            );
//            if (s_tempGridinfo == null) return false;
//            if (s_tempGridinfo.Data.Remove(k))
//            {
//                DumpGridToRule();
//            }
//            if (s_tempGridinfo.Data.Count > 0)
//                _bgPlayer.RefreshTiles();
//            return true;
//        }
//        private bool DrawOneAtom()
//        {
//            if (s_tempGridinfo == null) return false;
//            var mpos = MousePos;
//            AddToGrid(mpos.x, mpos.y);
//            DumpGridToRule();
//            _bgPlayer.RefreshTiles();
//            return true;
//        }

//        private void DumpGridToRule()
//        {
//            if (!_bgPlayer.level) return;
//            _bgPlayer.level.atoms = s_tempGridinfo.GetOptimizedAtoms();
//            _bgPlayer.bgr.atoms = _bgPlayer.level.atoms;
//            EditorUtility.SetDirty(_bgPlayer.level);
//            _bgPlayer.RefreshTiles();
//        }

//        protected void EnsureTools()
//        {
//            if (_sceneTools == null)
//            {
//                _sceneTools = new EditorTools();
//                _sceneTools.AddTool(new EditorTools.Tool("brush")
//                {
//                    activation = new EditorTools.ActivationRule
//                    {
//                        mouseButton = 0,
//                    },
//                    onPress = DrawOneAtom,
//                    onDrag = () =>
//                    {
//                        DrawOneAtom();
//                    },
//                    onRelease = DumpGridToRule
//                });
//                _sceneTools.AddTool(new EditorTools.Tool("delete brush")
//                {
//                    activation = new EditorTools.ActivationRule
//                    {
//                        mouseButton = 0,
//                        modifiers = EventModifiers.Control
//                    },
//                    onPress = DeleteOneAtom,
//                    onDrag = () => DeleteOneAtom(),
//                });
//                _sceneTools.AddTool(new EditorTools.Tool("line brush")
//                {
//                    activation = new EditorTools.ActivationRule
//                    {
//                        mouseButton = 0,
//                        modifiers = EventModifiers.Alt
//                    },
//                    onPress = SetMouseStartAndSilhuette,
//                    onRelease = () =>
//                    {
//                        DrawAtomLine();
//                        DumpGridToRule();
//                        DisableSceneToolSilhouettes();
//                    }
//                });
//                _sceneTools.AddTool(new EditorTools.Tool("rectangle brush")
//                {
//                    activation = new EditorTools.ActivationRule
//                    {
//                        mouseButton = 0,
//                        modifiers = EventModifiers.FunctionKey
//                    },
//                    onPress = SetMouseStartAndSilhuette,
//                    onRelease = () =>
//                    {
//                        DrawAtomRectangle();
//                        DumpGridToRule();
//                        DisableSceneToolSilhouettes();
//                    }
//                });
//                _sceneTools.AddTool(new EditorTools.Tool("delete rectangle brush")
//                {
//                    activation = new EditorTools.ActivationRule
//                    {
//                        mouseButton = 1,
//                        modifiers = EventModifiers.FunctionKey
//                    },
//                    onPress = SetMouseStartAndSilhuette,
//                    onRelease = () =>
//                    {
//                        DeleteAtomRectangle();
//                        DumpGridToRule();
//                        DisableSceneToolSilhouettes();
//                    }
//                });

//                Func<bool> existAction = () =>
//                {
//                    _selectedDecorID = 0;
//                    _ghostBrushSpiriteRenderer.sprite = _editorTileSpriteCache[0];
//                    _selectedEditorTileTexture = _editorTileTextureCache[0];
//                    return true;
//                };
//                Func<bool> dontExistAction = () =>
//                {
//                    _selectedDecorID = 1;
//                    _ghostBrushSpiriteRenderer.sprite = _editorTileSpriteCache[1];
//                    _selectedEditorTileTexture = _editorTileTextureCache[1];
//                    return true;
//                }; Func<bool> dontCareAction = () =>
//                {
//                    _selectedDecorID = 2;
//                    _ghostBrushSpiriteRenderer.sprite = _editorTileSpriteCache[2];
//                    _selectedEditorTileTexture = _editorTileTextureCache[2];
//                    return true;
//                };
//                _sceneTools.AddTool(new EditorTools.Tool("setExist")
//                {
//                    activation = new EditorTools.ActivationRule
//                    {
//                        key = KeyCode.Z
//                    },
//                    onPress = existAction,
//                });
//                _sceneTools.AddTool(new EditorTools.Tool("setDontExist")
//                {
//                    activation = new EditorTools.ActivationRule
//                    {
//                        key = KeyCode.X
//                    },
//                    onPress = dontExistAction,
//                });
//                _sceneTools.AddTool(new EditorTools.Tool("setDontCare")
//                {
//                    activation = new EditorTools.ActivationRule
//                    {
//                        key = KeyCode.C
//                    },
//                    onPress = dontCareAction,
//                });
//            }
//        }
//        private void DrawSilhouettes()
//        {
//            if (_sceneModeNames != null && _sceneModeNames[_sceneMode] == "Rule")
//            {
//                DrawGrid();
//            }
//            if (_isSceneToolSilhouetteActive)
//            {
//                switch (_sceneTools.ActiveTool.name)
//                {
//                    case "rectangle brush":
//                    case "delete rectangle brush":
//                        DrawRectangleSilhuette(MouseStart, MousePos);
//                        break;
//                    case "line brush":
//                        DrawLineSilhuette();
//                        break;
//                }
//                DrawPointSilhuette(MousePos);
//            }
//        }
//        private bool SetMouseStartAndSilhuette()
//        {
//            _isSceneToolSilhouetteActive = true;
//            MouseStart = MousePos;
//            return true;
//        }
//        private void DisableSceneToolSilhouettes()
//        {
//            _isSceneToolSilhouetteActive = false;
//        }

//        private bool DrawAtomLine()
//        {
//            if (s_tempGridinfo == null) return false;
//            s_tempGridinfo.InsertLine(MouseStart, MousePos, _selectedDecorID);
//            DumpGridToRule();
//            _bgPlayer.RefreshTiles();
//            //_visualsTransform = CreateVisuals();
//            return true;
//        }
//        private void HandleColor(bool isSet = false, Color color = default)
//        {
//            if (isSet)
//            {
//                _backupColor = Handles.color;
//                Handles.color = color;
//            }
//            else
//                Handles.color = _backupColor;
//        }
//        private void DrawPointSilhuette(Vector2 point, float scale = 1)
//        {
//            HandleColor(true, Color.yellow);
//            var size = HandleUtility.GetHandleSize(point);
//            Handles.DrawSolidDisc(point, Vector3.forward, scale * size * 0.04f);
//            HandleColor();
//        }
//        private void DrawLineSilhuette()
//        {
//            HandleColor(true, Color.yellow);
//            Handles.DrawLine(MouseStart, MousePos);
//            HandleColor();
//        }
//        private void DrawRectangleSilhuette(Vector2 p1, Vector2 p2)
//        {
//            var rect = new Rect(p1, (p2 - p1));
//            Handles.DrawSolidRectangleWithOutline(rect, Color.yellow / 2, Color.blue);
//        }
//        private void AddToGrid(float x, float y)
//        {
//            var k = new GridInfo.Int2(
//                Mathf.RoundToInt(x),
//                Mathf.RoundToInt(y));
//            s_tempGridinfo.AddToGrid(k, _selectedDecorID);
//        }
//        private bool DrawAtomRectangle()
//        {
//            if (s_tempGridinfo == null) return false;
//            s_tempGridinfo.InsertRectangle(MouseStart, MousePos, _selectedDecorID);
//            DumpGridToRule();
//            _bgPlayer.RefreshTiles();
//            return true;
//        }
//        private bool DeleteAtomRectangle()
//        {
//            if (s_tempGridinfo == null) return false;
//            s_tempGridinfo.DeleteRectangle(MouseStart, MousePos);
//            _bgPlayer.RefreshTiles();
//            return true;
//        }
//        private void ReportGUIButton()
//        {
//            if (GUILayout.Button("Report"))
//            {
//                var stopWatch = new SimpleStopwatch();
//                var b = _bgPlayer.bgr;
//                b.DumpRules();
//                int fCount = 0;
//                int tCount = 0;
//                for (int i = 0; i < b.RuleCount; i++)
//                {
//                    if (b.GetRule(i))
//                    {
//                        tCount++;
//                    }
//                    else
//                    {
//                        fCount++;
//                    }
//                }
//                Debug.Log("Size of the matrix " + b.MatrixCount + "  |    Total number of rules : " + b.RuleCount + "       |      True count :" + tCount + "     False count :" + fCount + "     |  Time elapsed : " + stopWatch.ReportMeasurement());
//            }
//        }
//        #endregion
//        //private void SpriteSceneGUI()
//        //{
//        //    if (_bgPlayer.level)
//        //    {
//        //        _elementEditor.UpdateMatrix(_bgPlayer.transform, Vector3.zero);
//        //        _elementEditor.Field(UpdateElements);
//        //    }
//        //}
//        //private AEditableElement AddSprite()
//        //{
//            //var dn = new DecorationSet.Node();
//            //var d = _bgPlayer.bgr.decorationSet.LoadedAsset.spriteData;
//            //if(d.nodes.Length == 0) d.Ensure();
//            //ArrayUtility.Add(ref _bgPlayer.bgr.decorationSet.LoadedAsset.spriteData.nodes, dn);
//            //UpdateElements();
//            //return new SpriteElement(this, d, d.nodes.Length - 1, AddSprite);
//        //}

        
//        //private void UpdateElements()
//        //{
//        //    UpdateElements(_bgPlayer.level, _elementEditor);
//        //}
//        //protected void UpdateElements(Level level, ElementEditor elements)
//        //{
//        //    //var data = _bgPlayer.bgr.decorationSet.LoadedAsset.spriteData;
//        //    elements.AddButtonData(new ElementEditor.NewButtonData("Sprite", AddSprite));
//        //    if (data.nodes == null)
//        //    {
//        //        data.Ensure();
//        //    }
//        //    for (int i = 0; i < data.nodes.Length; i++)
//        //    {
//        //        elements.AddElement(new SpriteElement(this, data, i, AddSprite));
//        //    }
//        //}

//        //private class SpriteElement : AEditableElement
//        //{
//        //    private bool _refreshNeeded;
//        //    private DecorationSet.Group _group;
//        //    private int _index;
//        //    private EBackGroundRulePlayer _editor;
//        //    public SpriteElement(EBackGroundRulePlayer editor, DecorationSet.Group group, int spriteElementIndex, Func<AEditableElement> createNew) : base(createNew)
//        //    {
//        //        _editor = editor;
//        //        _group = group;
//        //        _index = spriteElementIndex;
//        //    }
//        //    public override Vector3 Position
//        //    {
//        //        get { return _group.nodes[_index].position; }
//        //        set
//        //        {
//        //            if (_index == -1)
//        //            {
//        //                _group.nodes = new DecorationSet.Node[1];
//        //                _index = 0;
//        //            }
//        //            _group.nodes[_index].position = value;
//        //            UpdateVisuals(_editor._bgPlayer.transform);
//        //        }
//        //    }
//        //    public override void UpdateVisuals(Transform instance)
//        //    {
//        //        if (_refreshNeeded)
//        //        {
//        //            _editor._bgPlayer.RefreshTiles();
//        //            _refreshNeeded = false;
//        //        }
//        //        if (instance)
//        //        {
//        //            // todo implement this, broken atm because
//        //            //_group.nodes[_index].UpdateInstance(instance);
//        //        }
//        //    }
//        //    public override void GUILayout()
//        //    {
//        //        if (UnityEngine.GUILayout.Button("edit"))
//        //        {
//        //            //todo show sprite stash or something
//        //        }
//        //    }
//        //    public override string Name => "Decoration";
//        //    public override object DataObject => _group.nodes[_index];
//        //    public override bool Delete()
//        //    {
//        //        //ArrayUtility.RemoveAt(ref _editor._bgPlayer.bgr.decorationSet.LoadedAsset.spriteData.nodes, _index);
//        //        return true;
//        //    }
//        //}
//    }
//}
