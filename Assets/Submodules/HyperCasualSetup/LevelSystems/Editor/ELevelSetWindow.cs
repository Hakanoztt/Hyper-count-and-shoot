using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEditor;
using UnityEngine;

namespace Mobge.HyperCasualSetup {
    public class ELevelSetWindow : EditorWindow {
        public static void OpenWindow<T>(LevelSet levelSet) where T : ELevelSetWindow {
            var window = EditorWindow.GetWindow<T>();
            window._levelSet = levelSet;
            window._eLevelSet = (ELevelSet)Editor.CreateEditor(levelSet);
            window.Show();
        }
        public static void OpenWindow(LevelSet levelSet)  {
            OpenWindow<ELevelSetWindow>(levelSet);
        }

        protected LevelSet _levelSet;
        private ELevelSet _eLevelSet;
        private ReorderRules _reorderRules;
        private Vector2 _scrollPos;
        private UnityEditor.AnimatedValues.AnimBool _levelsOpen = new UnityEditor.AnimatedValues.AnimBool();
        private EditorFoldGroups _groups = new EditorFoldGroups(EditorFoldGroups.FilterMode.NoFilter);

        public void OnGUI() {
            EditorGUIUtility.labelWidth = 170;
            GUILayout.BeginVertical();
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            _levelSet = EditorLayoutDrawer.ObjectField<LevelSet>("level set", _levelSet, false);
            if (_levelSet == null) return;
            if (_eLevelSet == null) _eLevelSet = (ELevelSet)Editor.CreateEditor(_levelSet);

            _eLevelSet.DrawBaseInspector();
            _levelSet.Levels.Trim();
            var array = _levelSet.Levels.array;

            EditorGUILayout.BeginVertical("Box");
            _levelsOpen.target = EditorGUILayout.Foldout(_levelsOpen.target, "Levels", true);

            if (_levelsOpen.isAnimating) {
                Repaint();
            }
            if (EditorGUILayout.BeginFadeGroup(_levelsOpen.faded)) {
                EditorGUI.BeginChangeCheck();

                EditorLayoutDrawer.CustomArrayField("levels", ref array, (layoutRectSource, pair) => {
                    var key = new ALevelSet.ID(pair.key);
                    if (pair.value == null) {
                        pair.value = new ALevelSet.AddressableLevel();
                    }
                    var addressableLevelReference = pair.value;
                    var isNull = addressableLevelReference.editorAsset == null;
                    layoutRectSource.Indent = key.Depth * 10;
                    var labelWidth = EditorGUIUtility.labelWidth;
                    var rect = layoutRectSource.NextRect();
                    if (isNull) {
                        EditorGUIUtility.labelWidth = layoutRectSource.CurrentRect.width - 50 - layoutRectSource.Indent;
                    }
                    else {
                        var rectTag = rect;
                        rectTag.width = 50;
                        rectTag.x = labelWidth - rectTag.width;
                        addressableLevelReference.editorTag = EditorGUI.IntField(rectTag, addressableLevelReference.editorTag);

                        var rectTag2 = rectTag;
                        rectTag2.x -= rectTag.width - 2;
                        var level = addressableLevelReference.editorAsset as BaseLevel;

                        if (level != null) {
                            level.tag = EditorGUI.IntField(rectTag2, level.tag);
                        }
                    }
                    addressableLevelReference.SetEditorAsset(EditorDrawer.ObjectField(rect, ToString(key, isNull), addressableLevelReference.editorAsset));
                    EditorGUIUtility.labelWidth = labelWidth;
                    return pair;
                }
                    , false);
                if (EditorGUI.EndChangeCheck()) {
                    FixStructure(array);
                }
            }
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndVertical();

            _groups.GuilayoutField(CreateGroups);
            
            // if (GUILayout.Button("Mark All Levels and Dependencies Addressable")) {
            //     var levels = _levelSet.GetAllLevels();
            //     var le = new LevelSet.LevelDataEnumerable(levels);
            //     AddressableFixer.BulkMarkLevelsAndLevelResourcesAddressable(le);
            //     if (_levelSet.extraAddressables != null) {
            //         for (int i = 0; i < _levelSet.extraAddressables.Length; i++) {
            //             AddressableFixer.MarkAssetAddressable(_levelSet.extraAddressables[i].editorAsset);
            //         }
            //     }
            // }
            if (GUILayout.Button("Export to csv file")) {
                var path = EditorUtility.SaveFilePanel("Export file", "", _levelSet.name, "csv");
                if (path != null || path != "") {
                    System.IO.FileStream file = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate);
                    using (var fw = new System.IO.StreamWriter(file)) {
                        fw.WriteLine("IDs,Levels");
                        int levelId = 1;
                        for (int i = 0; i < array.Length; i++) {
                            if (array[i].value != null && array[i].value.editorAsset != null) {
                                fw.WriteLine(levelId.ToString() + "," + array[i].value.editorAsset.name);
                                levelId++;
                            }
                        }
                        fw.Flush();
                        fw.Close();
                    }
                    file.Close();
                }
            }
            if (GUILayout.Button("Export to json file")) {
                var path = EditorUtility.SaveFilePanel("Export file", "", _levelSet.name, "json");
                if (path != null || path != "") {
                    System.IO.FileStream file = System.IO.File.Open(path, System.IO.FileMode.OpenOrCreate);
                    using (var fw = new System.IO.StreamWriter(file)) {
                        var str = "";
                        str += "{\n";
                        int levelId = 1;
                        for (int i = 0; i < array.Length; i++) {
                            if (array[i].value != null && array[i].value.editorAsset != null) {
                                str += $"\"{levelId++}\":\"{array[i].value.editorAsset.name}\",\n";
                            }
                        }
                        str = str.Remove(str.Length - 2, 1);
                        str += "}";
                        fw.Write(str);
                        fw.Flush();
                        fw.Close();
                    }
                    file.Close();
                }
            }

            if (GUI.changed) {
                if (_levelSet.randomConfiguration.enabled) {
                    _levelSet.randomConfiguration.Initialize(true);
                }
                var set = (ISerializationCallbackReceiver)_levelSet;
                set.OnBeforeSerialize();
                EditorExtensions.SetDirty(_levelSet);
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void CreateGroups(EditorFoldGroups.Group obj) {
            obj.AddChild("Ordering", () => {
                EditorGUI.BeginChangeCheck();
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("+ world")) {
                        _levelSet.Levels.Add(ALevelSet.ID.New(-2).Value, new ALevelSet.AddressableLevel());
                    }
                    if (GUILayout.Button("+ level")) {
                        _levelSet.Levels.Add(ALevelSet.ID.New(-2, -2).Value, new ALevelSet.AddressableLevel());
                    }
                    if (GUILayout.Button("+ sub-level")) {
                        _levelSet.Levels.Add(ALevelSet.ID.New(-2, -2, -2).Value, new ALevelSet.AddressableLevel());
                    }
                }
                if (GUILayout.Button("add missing levels from selection as sub-level")) {
                    var e = GetMissingLevelsFromSelection();
                    var key = new ALevelSet.ID(_levelSet.Levels.array[_levelSet.Levels.Count - 1].key);
                    while (e.MoveNext()) {
                        key[2]++;
                        _levelSet.Levels.Add(key.Value, e.Current);
                    }
                }
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("sort by level tag")) {
                        Sort((l1, l2) => {
                            var bl1 = (BaseLevel)l1.editorAsset;
                            var bl2 = (BaseLevel)l2.editorAsset;
                            return bl1.tag - bl2.tag;
                        });
                    }
                    if (GUILayout.Button("sort by tag")) {
                        Sort((l1, l2) => { return l1.editorTag - l2.editorTag; });
                    }
                }
                ReorderField();
                if (EditorGUI.EndChangeCheck()) {
                    FixStructure();
                }
            });
            obj.AddChild("Create Level Order Report", () => {
                ReorderField();
            });
            CreateDistributionGroups(obj);
        }


        private void Sort(Comparison<ALevelSet.AddressableLevel> comparison) {
            List<ALevelSet.AddressableLevel> all = new List<ALevelSet.AddressableLevel>();
            var lvls = _levelSet.GetAllLevels();
            while (lvls.MoveNext()) {
                all.Add(lvls.Current.value);
            }
            _levelSet.Levels.Clear();
            all.Sort(comparison);
            _levelSet.Levels.Add(ALevelSet.ID.New(0).Value, null);
            for (int i = 0; i < all.Count; i++) {
                _levelSet.Levels.Add(ALevelSet.ID.New(0, i).Value, null);
                _levelSet.Levels.Add(ALevelSet.ID.New(0, i, 0).Value, all[i]);
            }
        }
        private void CreateLevelReport() {
            int levelCount = _groups.IntField("Level Count", 500);
            bool useNumbers = _groups.ToggleField("Use numbers instead of indexes");

            ALevelSet.ID next = new ALevelSet.ID();
            next = _levelSet.ToNearestLevelId(next);

        }
        private void ReorderField() {
            _reorderRules.menuOpen = EditorGUILayout.Foldout(_reorderRules.menuOpen, "Reorder options", true);
            if (_reorderRules.menuOpen) {
                using (new EditorGUILayout.VerticalScope("Box")) {
                    _reorderRules.levelPerWorld = EditorGUILayout.IntField("level per world", _reorderRules.levelPerWorld);
                    _reorderRules.subLevelPerLevel = EditorGUILayout.IntField("sub-level per level", _reorderRules.subLevelPerLevel);
                    if (GUILayout.Button("Apply")) {
                        List<ALevelSet.AddressableLevel> allLevels = new List<ALevelSet.AddressableLevel>();
                        var e = _levelSet.GetAllLevels();
                        while (e.MoveNext()) {
                            allLevels.Add(e.Current.value);
                        }
                        _levelSet.Levels.ClearFast();
                        var currentId = ALevelSet.ID.New(-1, _reorderRules.levelPerWorld - 1, _reorderRules.subLevelPerLevel - 1);

                        for (int i = 0; i < allLevels.Count; i++) {
                            currentId[2]++;
                            if (currentId[2] == _reorderRules.subLevelPerLevel) {
                                currentId[2] = 0;
                                currentId[1]++;
                                if (currentId[1] == _reorderRules.levelPerWorld) {
                                    currentId[1] = 0;
                                    currentId[0]++;
                                    _levelSet.Levels.Add(ALevelSet.ID.New(currentId[0]).Value, null);
                                }
                                _levelSet.Levels.Add(ALevelSet.ID.New(currentId[0], currentId[1]).Value, null);
                            }
                            var al = allLevels[i];
                            _levelSet.Levels.Add(currentId.Value, al);
                        }
                    }
                }
            }
        }
        private bool TryGetKey(Level level, out int key) {
            var all = _levelSet.GetAllLevels();
            while (all.MoveNext()) {
                var al = all.Current.value;
                if (al != null && al.editorAsset == level) {
                    key = all.Current.key;
                    return true;
                }
            }
            key = -1;
            return false;
        }
        IEnumerator<ALevelSet.AddressableLevel> GetMissingLevelsFromSelection() {
            var objs = Selection.objects;
            for (int i = 0; i < objs.Length; i++) {
                var level = objs[i] as Mobge.Core.Level;
                if (level != null) {
                    var path = AssetDatabase.GetAssetPath(level);
                    if (!string.IsNullOrEmpty(path)) {
                        if (!TryGetKey(level, out int key)) {
                            ALevelSet.AddressableLevel al = new ALevelSet.AddressableLevel(AssetDatabase.AssetPathToGUID(path));
                            yield return al;
                        }
                    }
                }
            }
        }
        private static string ToString(LevelData.ID id, bool hasPrefix) {
            var d = id.Depth;
            if (hasPrefix) {
                string s;
                switch (d) {
                    case 1:
                    default:
                        s = "world ";
                        break;
                    case 2:
                        s = "level ";
                        break;
                    case 3:
                        s = "sub level ";
                        break;
                    case 0:
                        d = 1;
                        s = "?";
                        break;
                }
                return s + id[d - 1].ToString();
            }
            else {
                return id[d - 1].ToString();
            }
        }
        void FixStructure() {
            ALevelSet.ID lastKey = new ALevelSet.ID();
            int lastDepth = lastKey.Depth;
            int count = _levelSet.Levels.Count;
            var array = _levelSet.Levels.array;
            for (int i = 0; i < count; i++) {
                var key = new ALevelSet.ID(array[i].key);
                var depth = key.Depth;
                if (depth == 0) {
                    depth = 1;
                    key[0] = 0;
                }
                var minDepth = Mathf.Min(depth, lastDepth);
                int j = 0;
                for (; j < minDepth; j++) {
                    key[j] = lastKey[j];
                }
                if (lastDepth >= depth) {
                    key[depth - 1] = lastKey[depth - 1] + 1;
                }

                while (lastDepth < depth) {
                    key[lastDepth] = 0;
                    lastDepth++;
                }


                lastKey = key;
                lastDepth = depth;
                array[i].key = key.Value;
            }
        }
        private void FixStructure(LevelSet.Map.Pair[] array) {
            _levelSet.Levels.SetCountFast(array.Length);
            _levelSet.Levels.array = array;
            FixStructure();
        }



        private void CreateDistributionGroups(EditorFoldGroups.Group obj) {
            obj.AddChild("distribution graph", () => {
                var levelSet = this._levelSet;
                int sampleCount = _groups.IntField("sample count", 10000);
                int[] values = _groups.GetObject<int[]>("values", null);
                Dictionary<string, int> allLevels = _groups.GetObject<Dictionary<string, int>>("allLevels", null);

                if (GUILayout.Button("update graph")) {
                    var al = levelSet.GetAllLevels();
                    int count = 0;
                    allLevels = new Dictionary<string, int>();
                    while (al.MoveNext()) {
                        allLevels[al.Current.value.AssetGUID] = count;
                        count++;
                    }
                    values = new int[count];
                    ALevelSet.ID next = new ALevelSet.ID();
                    next = levelSet.ToNearestLevelId(next);
                    for (int i = 0; i < sampleCount; i++) {
                        int index = allLevels[levelSet[next].AssetGUID];
                        values[index]++;
                        if (!levelSet.TryIncreaseLevel(ref next)) {
                            throw new Exception("cannot increase level");
                        }
                    }
                    _groups.SetObject("values", values);
                    _groups.SetObject("allLevels", allLevels);

                }
                if (values != null) {
                    int max = int.MinValue;
                    for (int i = 0; i < values.Length; i++) {
                        max = Mathf.Max(values[i], max);
                    }
                    Rect r = EditorGUILayout.GetControlRect(false, 300);
                    r.x += 10;
                    r.width -= 20;
                    r.y += 10;
                    r.height -= 20;
                    float xStep = r.width / (values.Length - 1);
                    float bottom = r.yMax;
                    float top = r.yMin;
                    Func<float, float> GetY = (v) => {
                        return bottom + (top - bottom) * v / (float)max;
                    };
                    float currentX = r.x;
                    for (int i = 0; i < values.Length; i++) {
                        var y = GetY(values[i]);
                        Rect c = new Rect(currentX - 1, y, 2, bottom - y);
                        EditorGUI.DrawRect(c, Color.black);
                        currentX += xStep;
                    }
                    int sampleIndex = _groups.IntField("sample index");
                    if (sampleIndex >= 0 && sampleIndex < values.Length) {
                        EditorGUILayout.IntField("sample", values[sampleIndex]);
                    }
                }

            });
        }


        private struct ReorderRules {
            public bool menuOpen;
            public int levelPerWorld;
            public int subLevelPerLevel;
        }

    }

}

