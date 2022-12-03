using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Mobge.HyperCasualSetup {
    [CustomEditor(typeof(ItemSet), true)]
    public class EItemSet : Editor {
        private static MultiKeyDictionary<float, List<int>> s_itemCache = new MultiKeyDictionary<float, List<int>>(0);
        private static StringBuilder s_sb = new StringBuilder();
        private static int[] s_defaultFilters = new int[0];
        private static CostPair[] s_defaultCosts = new CostPair[0];

        private ItemSet _go;
        private bool _itemsOpen;

        private int _currentLevel;

        private EditorFoldGroups _groups = new EditorFoldGroups(EditorFoldGroups.FilterMode.NoFilter);

        private void OnEnable() {
            _go = target as ItemSet;
        }
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            if (!_go) {
                return;
            }
            if (_go.items == null) {
                _go.items = new ItemSet.Map();
            }
            ItemSetOperationsField();
            _go.editorEquipped = EditorLayoutDrawer.Popup("Test Item", _go.items, _go.editorEquipped, "none");
            _go.defaultItem = EditorLayoutDrawer.Popup("Default Item", _go.items, _go.defaultItem, "none");
            EditorLayoutDrawer.CustomListField("items", _go.items, (l, i) =>
            {
                var item = _go.items[i];
                EditorGUI.LabelField(l.NextRect(), "Id", i.ToString());
                item.name = EditorGUI.TextField(l.NextRect(), "name", item.name);
                item.sprite = EditorDrawer.ObjectField(l.NextRect(100), "icon", item.sprite, false);
                item.cost = EditorGUI.IntField(l.NextRect(), "cost", item.cost);
                item.costIncreaseForLevel = EditorGUI.FloatField(l.NextRect(), "cost increase for level", item.costIncreaseForLevel);
                item.geometricCostIncreaseForLevel = EditorGUI.FloatField(l.NextRect(), "geometric cost increase for level", item.geometricCostIncreaseForLevel);
                item.costMax = EditorGUI.FloatField(l.NextRect(), "max cost", item.costMax);

                EditorGUI.LabelField(l.NextRect(), "Cost", item.GetCost(_currentLevel).ToString());


                if (item.contents == null) {
                    item.contents = new UnityEngine.Object[0];
                }
                EditorDrawer.CustomArrayField(l, "contents", ref item.contents, (l2, t) =>
                {
                    t = EditorDrawer.ObjectField(l2, null, t, false);
                    return t;
                });
                var la = EditorDrawer.CustomArrayField(l, "custom values", ref item.values, (l2, index) =>
                {
                    string label = _go.valueLabels == null || _go.valueLabels.Length <= index ? index.ToString() : _go.valueLabels[index];
                    item.values[index] = EditorGUI.FloatField(l2.NextRect(), label, item.values[index]);
                });
                la.constantElements = _go.valueLabels;
                la.ignoreConstantElementCounts = true;

                var r = l.NextRect(2f);
                EditorGUI.DrawRect(r, Color.black);
                r = l.NextRect(2f);
                r = l.NextRect(2f);
                EditorGUI.DrawRect(r, Color.black);
                r = l.NextRect(5f);



            }, ref _itemsOpen);

            _currentLevel = EditorGUILayout.IntField("cost display level", _currentLevel);

            if (GUILayout.Button("Export Icons With Ids")) {
                var f = EditorUtility.SaveFolderPanel("Export Location", null, null);
                if (!string.IsNullOrEmpty(f)) {
                    var e = _go.items.GetKeyEnumerator();
                    while (e.MoveNext()) {
                        var key = e.Current;
                        var c = _go.items[key];
                        var fn = Path.Combine(f, key + ".png");
                        var sprite = c.sprite;
                        var p = AssetDatabase.GetAssetPath(sprite.texture);
                        var bytes = File.ReadAllBytes(p);
                        Texture2D copy = new Texture2D(sprite.texture.width, sprite.texture.height);
                        copy.LoadImage(bytes);
                        var croppedTexture = new Texture2D((int)sprite.textureRect.width, (int)sprite.textureRect.height);

                        var pixels = copy.GetPixels((int)sprite.textureRect.x,
                                                                (int)sprite.textureRect.y,
                                                                (int)sprite.textureRect.width,
                                                                (int)sprite.textureRect.height);
                        croppedTexture.SetPixels(pixels);
                        croppedTexture.Apply();
                        bytes = croppedTexture.EncodeToPNG();
                        croppedTexture.DestroySelf();
                        copy.DestroySelf();



                        File.WriteAllBytes(fn, bytes);
                    }
                    Debug.Log("All sprites are exported to: " + f);
                }
            }

            if (GUI.changed) {
                EditorExtensions.SetDirty(_go);
            }
        }
        void ItemSetOperationsField() {
            _groups.GuilayoutField((root) =>
            {
                GroupField("Coin Distribution", root, (subset) =>
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < subset.Count; i++) {
                        ItemSet.Item item = _go.items[subset[i]];
                        GUILayout.BeginVertical();
                        if (item.sprite) {
                            InspectorExtensions.PreviewPicker.DrawPreview(GUILayoutUtility.GetRect(40, 35), item.sprite);
                        }
                        else {
                            EditorGUI.LabelField(GUILayoutUtility.GetRect(40, 35), "no image");
                        }
                        EditorGUI.LabelField(GUILayoutUtility.GetRect(40, EditorGUIUtility.singleLineHeight), "C: " + item.cost);
                        EditorGUI.LabelField(GUILayoutUtility.GetRect(40, EditorGUIUtility.singleLineHeight), "C+: " + item.costIncreaseForLevel);
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                    CostPair[] costs = _groups.GetObject("Costs", s_defaultCosts);
                    EditorLayoutDrawer.CustomArrayField("Costs", ref costs, (lr, c) =>
                    {
                        c.cost = EditorGUI.IntField(lr.NextRect(), "Cost", c.cost);
                        c.costPerLevel = EditorGUI.IntField(lr.NextRect(), "Cost Per Level", c.costPerLevel);
                        return c;
                    });
                    _groups.SetObject("Costs", costs);
                    if (costs.Length > 0 && GUILayout.Button("Distribute Costs")) {
                        for (int i = 0; i < subset.Count; i++) {
                            var item = _go.items[subset[i]];
                            var c = costs[i % costs.Length];
                            item.cost = c.cost;
                            item.costIncreaseForLevel = c.costPerLevel;
                        }
                        EditorExtensions.SetDirty(_go);
                    }
                });
            }, "operations");
        }
        void GroupField(string label, EditorFoldGroups.Group parent, Action<List<int>> field) {
            parent.AddChild(label, () =>
            {
                EditorGUI.BeginChangeCheck();
                int[] labels = _groups.GetObject<int[]>("Filters", s_defaultFilters);
                EditorLayoutDrawer.CustomArrayField("Filters", ref labels, (lr, value) =>
                {
                    value = EditorDrawer.Popup(lr, null, _go.valueLabels, value);
                    return value;
                });
                _groups.SetObject("Filters", labels);
                if (EditorGUI.EndChangeCheck()) {
                    _groups.Refresh();
                }

            }, (cg) =>
            {
                int[] labels = _groups.GetObject<int[]>("Filters", s_defaultFilters);
                if (labels == null) {
                    return;
                }
                s_itemCache.Clear(labels.Length);

                var e = _go.items.GetKeyEnumerator();

                while (e.MoveNext()) {
                    var c = e.Current;
                    var val = _go.items[c];
                    var k = s_itemCache.NewKey();
                    for (int i = 0; i < labels.Length; i++) {
                        float value;
                        int index = labels[i];
                        if (val.values == null || val.values.Length <= index) {
                            value = 0;
                        }
                        else {
                            value = val.values[index];
                        }
                        k.AddKey(value);
                    }
                    if (!k.TryGet(out List<int> group, false)) {
                        group = new List<int>();
                        k.Add(group);
                    }
                    else {
                        k.Dispose();
                    }
                    group.Add(c);
                }


                var grps = s_itemCache.GetEnumerator();
                while (grps.MoveNext()) {
                    var grp = grps.Current;
                    s_sb.Clear();
                    for (int i = 0; i < grp.key.Count; i++) {
                        s_sb.Append(grp.key[i]);
                        s_sb.Append(" ");
                    }
                    cg.AddChild(s_sb.ToString(), () =>
                    {
                        field(grp.value);
                    });
                }
            });
        }
        struct CostPair {
            public int cost;
            public int costPerLevel;
        }
    }
}