using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace Mobge.Core
{
    [CustomEditor(typeof(RuleTile), true)]
    public class RuleTileEditor : Editor
    {
        private const string s_XIconString = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABoSURBVDhPnY3BDcAgDAOZhS14dP1O0x2C/LBEgiNSHvfwyZabmV0jZRUpq2zi6f0DJwdcQOEdwwDLypF0zHLMa9+NQRxkQ+ACOT2STVw/q8eY1346ZlE54sYAhVhSDrjwFymrSFnD2gTZpls2OvFUHAAAAABJRU5ErkJggg==";
        private const string s_Arrow0 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACYSURBVDhPzZExDoQwDATzE4oU4QXXcgUFj+YxtETwgpMwXuFcwMFSRMVKKwzZcWzhiMg91jtg34XIntkre5EaT7yjjhI9pOD5Mw5k2X/DdUwFr3cQ7Pu23E/BiwXyWSOxrNqx+ewnsayam5OLBtbOGPUM/r93YZL4/dhpR/amwByGFBz170gNChA6w5bQQMqramBTgJ+Z3A58WuWejPCaHQAAAABJRU5ErkJggg==";
        private const string s_Arrow1 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABqSURBVDhPxYzBDYAgEATpxYcd+PVr0fZ2siZrjmMhFz6STIiDs8XMlpEyi5RkO/d66TcgJUB43JfNBqRkSEYDnYjhbKD5GIUkDqRDwoH3+NgTAw+bL/aoOP4DOgH+iwECEt+IlFmkzGHlAYKAWF9R8zUnAAAAAElFTkSuQmCC";
        private const string s_Arrow2 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAAC0SURBVDhPjVE5EsIwDMxPKFKYF9CagoJH8xhaMskLmEGsjOSRkBzYmU2s9a58TUQUmCH1BWEHweuKP+D8tphrWcAHuIGrjPnPNY8X2+DzEWE+FzrdrkNyg2YGNNfRGlyOaZDJOxBrDhgOowaYW8UW0Vau5ZkFmXbbDr+CzOHKmLinAXMEePyZ9dZkZR+s5QX2O8DY3zZ/sgYcdDqeEVp8516o0QQV1qeMwg6C91toYoLoo+kNt/tpKQEVvFQAAAAASUVORK5CYII=";
        private const string s_Arrow3 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAAB2SURBVDhPzY1LCoAwEEPnLi48gW5d6p31bH5SMhp0Cq0g+CCLxrzRPqMZ2pRqKG4IqzJc7JepTlbRZXYpWTg4RZE1XAso8VHFKNhQuTjKtZvHUNCEMogO4K3BhvMn9wP4EzoPZ3n0AGTW5fiBVzLAAYTP32C2Ay3agtu9V/9PAAAAAElFTkSuQmCC";
        private const string s_Arrow5 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABqSURBVDhPnY3BCYBADASvFx924NevRdvbyoLBmNuDJQMDGjNxAFhK1DyUQ9fvobCdO+j7+sOKj/uSB+xYHZAxl7IR1wNTXJeVcaAVU+614uWfCT9mVUhknMlxDokd15BYsQrJFHeUQ0+MB5ErsPi/6hO1AAAAAElFTkSuQmCC";
        private const string s_Arrow6 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACaSURBVDhPxZExEkAwEEVzE4UiTqClUDi0w2hlOIEZsV82xCZmQuPPfFn8t1mirLWf7S5flQOXjd64vCuEKWTKVt+6AayH3tIa7yLg6Qh2FcKFB72jBgJeziA1CMHzeaNHjkfwnAK86f3KUafU2ClHIJSzs/8HHLv09M3SaMCxS7ljw/IYJWzQABOQZ66x4h614ahTCL/WT7BSO51b5Z5hSx88AAAAAElFTkSuQmCC";
        private const string s_Arrow7 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAABQSURBVDhPYxh8QNle/T8U/4MKEQdAmsz2eICx6W530gygr2aQBmSMphkZYxqErAEXxusKfAYQ7XyyNMIAsgEkaYQBkAFkaYQBsjXSGDAwAAD193z4luKPrAAAAABJRU5ErkJggg==";
        private const string s_Arrow8 = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAYdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuNWWFMmUAAACYSURBVDhPxZE9DoAwCIW9iUOHegJXHRw8tIdx1egJTMSHAeMPaHSR5KVQ+KCkCRF91mdz4VDEWVzXTBgg5U1N5wahjHzXS3iFFVRxAygNVaZxJ6VHGIl2D6oUXP0ijlJuTp724FnID1Lq7uw2QM5+thoKth0N+GGyA7IA3+yM77Ag1e2zkey5gCdAg/h8csy+/89v7E+YkgUntOWeVt2SfAAAAABJRU5ErkJggg==";
        private const string s_MirrorX = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG1JREFUOE+lj9ENwCAIRB2IFdyRfRiuDSaXAF4MrR9P5eRhHGb2Gxp2oaEjIovTXSrAnPNx6hlgyCZ7o6omOdYOldGIZhAziEmOTSfigLV0RYAB9y9f/7kO8L3WUaQyhCgz0dmCL9CwCw172HgBeyG6oloC8fAAAAAASUVORK5CYII=";
        private const string s_MirrorY = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwgAADsIBFShKgAAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAG9JREFUOE+djckNACEMAykoLdAjHbPyw1IOJ0L7mAejjFlm9hspyd77Kk+kBAjPOXcakJIh6QaKyOE0EB5dSPJAiUmOiL8PMVGxugsP/0OOib8vsY8yYwy6gRyC8CB5QIWgCMKBLgRSkikEUr5h6wOPWfMoCYILdgAAAABJRU5ErkJggg==";
        private const string s_Rotated = "iVBORw0KGgoAAAANSUhEUgAAAA8AAAAPCAYAAAA71pVKAAAABGdBTUEAALGPC/xhBQAAAAlwSFlzAAAOwQAADsEBuJFr7QAAABh0RVh0U29mdHdhcmUAcGFpbnQubmV0IDQuMC41ZYUyZQAAAHdJREFUOE+djssNwCAMQxmIFdgx+2S4Vj4YxWlQgcOT8nuG5u5C732Sd3lfLlmPMR4QhXgrTQaimUlA3EtD+CJlBuQ7aUAUMjEAv9gWCQNEPhHJUkYfZ1kEpcxDzioRzGIlr0Qwi0r+Q5rTgM+AAVcygHgt7+HtBZs/2QVWP8ahAAAAAElFTkSuQmCC";

        private static Texture2D Base64ToTexture(string base64)
        {
            Texture2D t = new Texture2D(1, 1);
            t.hideFlags = HideFlags.HideAndDontSave;
            t.LoadImage(System.Convert.FromBase64String(base64));
            return t;
        }

        public Type m_NeighborType { get { return typeof(RuleTile.Rule.Neighbor); } }

        private RuleTile tile { get { return (target as RuleTile); } }
        private ReorderableList m_ReorderableList;

        internal const float k_DefaultElementHeight = 48f;
        internal const float k_PaddingBetweenRules = 26f;
        internal const float k_SingleLineHeight = 16f;
        internal const float k_LabelWidth = 80f;

        public void OnEnable()
        {
            if (tile.rules == null)
                tile.rules = new List<RuleTile.Rule>();

            m_ReorderableList = new ReorderableList(tile.rules, typeof(RuleTile.Rule), true, true, true, true);
            m_ReorderableList.drawHeaderCallback = OnDrawHeader;
            m_ReorderableList.drawElementCallback = OnDrawElement;
            m_ReorderableList.elementHeightCallback = GetElementHeight;
            m_ReorderableList.onReorderCallback = ListUpdated;
            //m_ReorderableList.onAddCallback = OnAddElement;
        }

        private void ListUpdated(ReorderableList list)
        {
            SaveTile();
        }

        private void OnAddElement(ReorderableList list)
        {
            RuleTile.Rule rule = new RuleTile.Rule
            {
                outputSprite = RuleTile.Rule.OutputSprite.Single
            };
            rule.sprites[0] = tile.m_DefaultSprite;
            //rule.m_GameObject = tile.m_DefaultGameObject;
            tile.rules.Add(rule);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            EditorGUI.BeginChangeCheck();
            tile.m_DefaultSprite = EditorGUILayout.ObjectField("Default Sprite", tile.m_DefaultSprite, typeof(Sprite), false) as Sprite;
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(tile);
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            var baseFields = typeof(RuleTile).GetFields().Select(field => field.Name);
            var fields = target.GetType().GetFields().Select(field => field.Name).Where(field => !baseFields.Contains(field));
            foreach (var field in fields)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field), true);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (m_ReorderableList != null && tile.rules != null)
                m_ReorderableList.DoLayoutList();
        }

        private float GetElementHeight(int index)
        {
            if (tile.rules != null && tile.rules.Count > 0)
            {
                switch (tile.rules[index].outputSprite)
                {
                    case RuleTile.Rule.OutputSprite.Random:
                        return k_DefaultElementHeight + k_SingleLineHeight * (tile.rules[index].sprites.Length + 3) + k_PaddingBetweenRules;
                    case RuleTile.Rule.OutputSprite.Animation:
                        return k_DefaultElementHeight + k_SingleLineHeight * (tile.rules[index].sprites.Length + 2) + k_PaddingBetweenRules;
                }
            }
            return k_DefaultElementHeight + k_PaddingBetweenRules;
        }

        private static Texture2D[] s_Arrows;
        public static Texture2D[] arrows
        {
            get
            {
                if (s_Arrows == null)
                {
                    s_Arrows = new Texture2D[10];
                    s_Arrows[0] = Base64ToTexture(s_Arrow0);
                    s_Arrows[1] = Base64ToTexture(s_Arrow1);
                    s_Arrows[2] = Base64ToTexture(s_Arrow2);
                    s_Arrows[3] = Base64ToTexture(s_Arrow3);
                    s_Arrows[5] = Base64ToTexture(s_Arrow5);
                    s_Arrows[6] = Base64ToTexture(s_Arrow6);
                    s_Arrows[7] = Base64ToTexture(s_Arrow7);
                    s_Arrows[8] = Base64ToTexture(s_Arrow8);
                    s_Arrows[9] = Base64ToTexture(s_XIconString);
                }
                return s_Arrows;
            }
        }

        private void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, "Tiling Rules");
        }


        internal virtual void RuleOnGUI(Rect rect, int arrowIndex, int neighbor)
        {
            switch (neighbor)
            {
                case RuleTile.Rule.Neighbor.DontCare:
                    break;
                case RuleTile.Rule.Neighbor.This:
                    GUI.DrawTexture(rect, arrows[arrowIndex]);
                    break;
                case RuleTile.Rule.Neighbor.NotThis:
                    GUI.DrawTexture(rect, arrows[9]);
                    break;
                default:
                    var style = new GUIStyle();
                    style.alignment = TextAnchor.MiddleCenter;
                    style.fontSize = 10;
                    GUI.Label(rect, neighbor.ToString(), style);
                    break;
            }
            var allConsts = tile.m_NeighborType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy);
            foreach (var c in allConsts)
            {
                if ((int)c.GetValue(null) == neighbor)
                {
                    GUI.Label(rect, new GUIContent("", c.Name));
                    break;
                }
            }
        }

        private void SaveTile()
        {
            EditorUtility.SetDirty(target);
            SceneView.RepaintAll();
        }

        private void OnDrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            RuleTile.Rule rule = tile.rules[index];

            float yPos = rect.yMin + 2f;
            float height = rect.height - k_PaddingBetweenRules;
            float matrixWidth = k_DefaultElementHeight;

            Rect inspectorRect = new Rect(rect.xMin, yPos, rect.width - matrixWidth * 2f - 20f, height);
            Rect matrixRect = new Rect(rect.xMax - matrixWidth * 2f - 10f, yPos, matrixWidth, k_DefaultElementHeight);
            Rect spriteRect = new Rect(rect.xMax - matrixWidth - 5f, yPos, matrixWidth, k_DefaultElementHeight);
            ///// TODO
            /// 
            //Rect colliderRect = new Rect(rect.xMax - matrixWidth - 5f, yPos + k_LabelWidth, matrixWidth, k_DefaultElementHeight);
            //new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.outputSprite);
            //y += k_SingleLineHeight;

            EditorGUI.BeginChangeCheck();
            RuleInspectorOnGUI(inspectorRect, rule);
            RuleMatrixOnGUI(matrixRect, rule);
            SpriteOnGUI(spriteRect, rule);
            //ColliderEnumOnGUI(colliderRect, rule);
            if (EditorGUI.EndChangeCheck())
                SaveTile();
        }

        // TODO : WIP
        internal static void ColliderEnumOnGUI(Rect rect, RuleTile.Rule tilingRule)
        {
            string[] enumNames = Enum.GetNames(typeof(TileCollider.ColliderTypes));
            var enumValues = Enum.GetValues(typeof(TileCollider.ColliderTypes));
            int buttonHeight = 18;
            int k_ButtonColumn = 8;
            var button = new Vector2
            {
                x = (EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth) / k_ButtonColumn,
                y = buttonHeight
            };
            //button.y = (p.height / Mathf.Ceil(enumNames.Length * 1.0f / k_ButtonColumn));
            int bValue = 0;
            bool[] isBPressed = new bool[enumNames.Length];
            //EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), "Collider Deneme");

            EditorGUI.BeginChangeCheck();
            {
                for (int i = 0; i < enumNames.Length; i++)
                {
                    if ( ((int)enumValues.GetValue(i) & (1 << i)) == 1 << i)
                    {
                        isBPressed[i] = false;
                    }
                    float i_row = Mathf.Floor(i / (k_ButtonColumn/2));
                    Rect buttonPos = new Rect(rect.x + EditorGUIUtility.labelWidth + button.x * (i % k_ButtonColumn),
                                              rect.y + (i_row * button.y),
                                              button.x,
                                              button.y);
                    isBPressed[i] = GUI.Toggle(buttonPos, isBPressed[i], enumNames[i], "Button");
                    if (isBPressed[i])
                    {
                        bValue += 1 << i;
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                tilingRule.m_ColliderType = (TileCollider.ColliderTypes)bValue;
                Debug.Log((int)tilingRule.m_ColliderType);
            }
        }



        //    GUI.Label(new Rect(rect.xMin, rect.yMin, k_LabelWidth, k_SingleLineHeight), "Collider Deneme Posizyonu");
        //}

        //public float GetPropertyHeight(SerializedProperty property, GUIContent label)
        //{
        //    //int length = Mathf.CeilToInt(property.enumNames.Length * 1.0f / 8);
        //    int length = 1;
        //    return (length * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
        //}

        //// TODO : WIP
        //// EnumFlagEditor'u adapte et.
        //private void CalculateButtonConstants(ref Rect p, ref int enumLenght, ref SerializedProperty sp, out Vector2 button)
        //{
        //    button.x = (p.width - EditorGUIUtility.labelWidth) / enumLenght;
        //    button.y = (p.height / Mathf.Ceil(sp.enumNames.Length * 1.0f / enumLenght));
        //    //Debug.Log("Button width:" + button.x + " Button height:" + button.y);
        //}



        internal virtual bool ContainsMousePosition(Rect rect)
        {
            return rect.Contains(Event.current.mousePosition);
        }

        private static int GetMouseChange()
        {
            return Event.current.button == 1 ? -1 : 1;
        }

        internal void RuleNeighborUpdate(Rect rect, RuleTile.Rule tilingRule, int index)
        {
            if (Event.current.type == EventType.MouseDown && ContainsMousePosition(rect))
            {
                var allConsts = tile.m_NeighborType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                var neighbors = allConsts.Select(c => (int)c.GetValue(null)).ToList();
                neighbors.Sort();

                int oldIndex = neighbors.IndexOf(tilingRule.neighbors[index]);
                int newIndex = (int)Mathf.Repeat(oldIndex + GetMouseChange(), neighbors.Count);
                tilingRule.neighbors[index] = neighbors[newIndex];
                GUI.changed = true;
                Event.current.Use();
            }
        }

        internal static void SpriteOnGUI(Rect rect, RuleTile.Rule tilingRule)
        {
            tilingRule.sprites[0] = EditorGUI.ObjectField(new Rect(rect.xMax - rect.height, rect.yMin, rect.height, rect.height), tilingRule.sprites[0], typeof(Sprite), false) as Sprite;
        }

        internal virtual void RuleMatrixOnGUI(Rect rect, RuleTile.Rule tilingRule)
        {
            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.2f);
            int index = 0;
            float w = rect.width / 3f;
            float h = rect.height / 3f;

            for (int y = 0; y <= 3; y++)
            {
                float top = rect.yMin + y * h;
                Handles.DrawLine(new Vector3(rect.xMin, top), new Vector3(rect.xMax, top));
            }
            for (int x = 0; x <= 3; x++)
            {
                float left = rect.xMin + x * w;
                Handles.DrawLine(new Vector3(left, rect.yMin), new Vector3(left, rect.yMax));
            }
            Handles.color = Color.white;

            for (int y = 0; y <= 2; y++)
            {
                for (int x = 0; x <= 2; x++)
                {
                    Rect r = new Rect(rect.xMin + x * w, rect.yMin + y * h, w - 1, h - 1);
                    if (x != 1 || y != 1)
                    {
                        RuleOnGUI(r, y * 3 + x, tilingRule.neighbors[index]);
                        RuleNeighborUpdate(r, tilingRule, index);
                        index++;
                    }
                    //else
                    //{
                    //    RuleTransformOnGUI(r, tilingRule.m_RuleTransform);
                    //    RuleTransformUpdate(r, tilingRule);
                    //}
                }
            }
        }

        internal static void RuleInspectorOnGUI(Rect rect, RuleTile.Rule tilingRule)
        {
            float y = rect.yMin;
            EditorGUI.BeginChangeCheck();
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Rule");
            tilingRule.m_RuleTransform = (RuleTile.Rule.Transform)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_RuleTransform);
            y += k_SingleLineHeight;
            //GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Game Object");
            //tilingRule.m_GameObject = (GameObject)EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), "", tilingRule.m_GameObject, typeof(GameObject), false);
            //y += k_SingleLineHeight;
            //GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Collider");
            //tilingRule.m_ColliderType = (Tile.ColliderType)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_ColliderType);
            //y += k_SingleLineHeight;
            GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Output");
            tilingRule.outputSprite = (RuleTile.Rule.OutputSprite)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.outputSprite);
            y += k_SingleLineHeight;


            //GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Deneme Pos");
            //y += k_SingleLineHeight;

            //ColliderEnumOnGUI((new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight)),tilingRule);

            //tilingRule.outputSprite = (RuleTile.Rule.OutputSprite)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.outputSprite);
            y += k_SingleLineHeight;

            if (tilingRule.outputSprite == RuleTile.Rule.OutputSprite.Animation)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Speed");
                tilingRule.m_AnimationSpeed = EditorGUI.FloatField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_AnimationSpeed);
                y += k_SingleLineHeight;
            }
            if (tilingRule.outputSprite == RuleTile.Rule.OutputSprite.Random)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Noise");
                tilingRule.m_PerlinScale = EditorGUI.Slider(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_PerlinScale, 0.001f, 0.999f);
                y += k_SingleLineHeight;

                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Shuffle");
                tilingRule.m_RandomTransform = (RuleTile.Rule.Transform)EditorGUI.EnumPopup(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.m_RandomTransform);
                y += k_SingleLineHeight;
            } 

            if (tilingRule.outputSprite != RuleTile.Rule.OutputSprite.Single)
            {
                GUI.Label(new Rect(rect.xMin, y, k_LabelWidth, k_SingleLineHeight), "Size");
                EditorGUI.BeginChangeCheck();
                int newLength = EditorGUI.DelayedIntField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.sprites.Length);
                if (EditorGUI.EndChangeCheck())
                    Array.Resize(ref tilingRule.sprites, Math.Max(newLength, 1));
                y += k_SingleLineHeight;

                for (int i = 0; i < tilingRule.sprites.Length; i++)
                {
                    tilingRule.sprites[i] = EditorGUI.ObjectField(new Rect(rect.xMin + k_LabelWidth, y, rect.width - k_LabelWidth, k_SingleLineHeight), tilingRule.sprites[i], typeof(Sprite), false) as Sprite;
                    y += k_SingleLineHeight;
                }
            }
        }
    }
}