
using System;
using System.Collections.Generic;
using Mobge.Core;
using UnityEngine;

namespace Mobge.Core {
    public class ScoreComponent : ComponentDefinition<ScoreComponent.Data> {
        public const string s_scoreDataKey = "mgb_scrdt";
        public static ScoreData GetOrCreateScoreData(LevelPlayer player) {
            if(player.TryGetExtra<ScoreData>(s_scoreDataKey, out var v)) {
                return v;
            }
            v = new ScoreData();
            player.SetExtra(s_scoreDataKey, v);
            return v;
        }
        public static bool TryGetScoreData(LevelPlayer player, out ScoreData value) {
            return player.TryGetExtra<ScoreData>(s_scoreDataKey, out value);
        }
        [Serializable]
        public class Data : BaseComponent {
            public string key = "Score";
            public float maxPotentialValue = 1;

            //public override LogicConnections Connections { get => connections; set => connections = value; }
            //[SerializeField] [HideInInspector] private LogicConnections connections;
            //private Dictionary<int, BaseComponent> _components;

            //private LevelPlayer _player;
            private ScoreData _data;
            public override void Start(in InitArgs initData) {
                _data = GetOrCreateScoreData(initData.player);
                _data.ModifyTotal(key, maxPotentialValue);
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    case 0:
                        return _data.ModifyValue(key, 1);
                    case 1:
                        return _data.ModifyValue(key, (float)input);
                        
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("Increase value one", 0,null,typeof(float)));
                slots.Add(new LogicSlot("Modify value by", 1, typeof(float), typeof(float)));
            }
            //public override void EditorOutputs(List<LogicSlot> slots) {
            //    slots.Add(new LogicSlot("example output", 0));
            //}
#endif
        }
        [Serializable]
        public class ScoreData {
            private Dictionary<string, ScoreValue> _scores;
            internal ScoreData() {
                if (_scores == null) {
                    _scores = new Dictionary<string, ScoreValue>();
                }
            }
            public ScoreValue this[string key] {
                get {
                    _scores.TryGetValue(key, out var d);
                    return d;
                }
                set {
                    _scores[key] = value;
                }
            }
            public float ModifyTotal(string key, float value) {
                _scores.TryGetValue(key, out var v);
                v.totalValue += value;
                _scores[key] = v;
                return v.totalValue;
            }
            public float ModifyValue(string key, float value) {
                _scores.TryGetValue(key, out var v);
                v.value += value;
                _scores[key] = v;
                return v.value;
            }
        }
        [Serializable]
        public struct ScoreValue {
            public float totalValue;
            public float value;
        }
    }
}
            