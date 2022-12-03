using Mobge.HyperCasualSetup;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mobge.HyperCasualSetup.UI.ChallengeSystem {
    [CreateAssetMenu(menuName = "Slime Platformer/Challenge Data")]
    public class ChallengeData : ScriptableObject {

        private readonly static EndPointComparer s_endPointComparer = new EndPointComparer();
        public readonly static IntervalComparer s_intervalComparer = new IntervalComparer();


        private static CombinationKeyDictionary<int, int> s_dic = new CombinationKeyDictionary<int, int>();
        private static EndPoint s_tempEndPoint;
        private static HashSet<int> s_emptyCombination = new HashSet<int>();

        private ExposedList<EndPoint> endpoints = new ExposedList<EndPoint>();
        private HashSet<int>[] _combinations;
        public const string c_savePrefix = "chlng:";
        public Level[] levels;


        public bool TryGetLevel(ALevelSet.ID id, out Level entry) {
            for(int i = 0; i < levels.Length; i++) {
                if(levels[i].level.Value == id.Value) {
                    entry = levels[i];
                    return true;
                }
            }
            entry = default;
            return false;
        }


        protected void OnEnable() {
            UpdateInternalStructure();
        }
        public HashSet<int>.Enumerator GetActiveLevels(int time) {
            s_tempEndPoint.time = time;
            int index = Array.BinarySearch(endpoints.array, 0, endpoints.Count, s_tempEndPoint, s_endPointComparer);
            if (index >= 0) {
                return _combinations[endpoints.array[index].combinationIndex].GetEnumerator();
            }
            else {
                index = ~index;
                index -= 1;
                if(index < 0) {
                    return s_emptyCombination.GetEnumerator();
                }
                else {
                    return _combinations[endpoints.array[index].combinationIndex].GetEnumerator();
                }
            }
        }
        public void UpdateInternalStructure() {
            if (levels == null) {
                return;
            }
            endpoints.ClearFast();
            s_dic.Clear(false);
            for (int i = 0; i < levels.Length; i++) {
                var l = levels[i];
                if (l.intervals == null) {
                    continue;
                }
                for (int j = 0; j < l.intervals.Length; j++) {
                    EndPoint e1;
                    e1.isStart = true;
                    e1.level = i;
                    e1.time = l.intervals[j].start;
                    e1.combinationIndex = -1;
                    EndPoint e2;
                    e2.isStart = false;
                    e2.level = i;
                    e2.time = e1.time + l.intervals[j].duration;
                    e2.combinationIndex = -1;

                    endpoints.Add(e1);
                    endpoints.Add(e2);
                }

            }
            Array.Sort(endpoints.array, 0, endpoints.Count, s_endPointComparer);
            int count = endpoints.Count;
            var hashSet = new HashSet<int>();
            endpoints.Add(new EndPoint() {
                time = int.MinValue
            });
            int index = 0;
            int lastHandled = -1;
            for (int i = 0; i < count; i++) {
                var e = endpoints.array[i];
                if (e.isStart) {
                    if (!hashSet.Add(e.level)) {
                        ThrowException();
                    }
                }
                else {
                    if (!hashSet.Remove(e.level)) {
                        ThrowException();
                    }
                }
                if (e.time == endpoints.array[i + 1].time) {

                }
                else {
                    var key = CreateKey(hashSet);
                    if (key.TryGet(out int value, false)) {
                        key.Dispose();
                    }
                    else {
                        value = index;
                        key.Add(index);
                        index++;
                    }
                    for (int h = lastHandled; h < i; ) {
                        h++;
                        endpoints.array[h].combinationIndex = value;
                    }
                    lastHandled = i;
                }
            }
            endpoints.RemoveLast();
            _combinations = new HashSet<int>[s_dic.Count];
            var ee = s_dic.GetEnumerator();
            while (ee.MoveNext()) {
                var cc = ee.Current;
                _combinations[cc.value] = cc.key;
            }
        }
        private CombinationKeyDictionary<int,int>.Key CreateKey(HashSet<int> keys) {
            var key = s_dic.NewKey();
            var e = keys.GetEnumerator();
            while (e.MoveNext()) {
                key.AddKey(e.Current);
            }
            return key;
        }

        private void ThrowException() {

            throw new Exception("Time intervals are not arranged correctly.");
        }

        [Serializable]
        public struct Level {
            public string name;
            public Interval[] intervals;
            public ALevelSet.ID level;
            public Sprite banner;
            public string scoreKey;
            public Score[] scores;

            public int GetLeftTime(int timeOffset, out int intervalIndex) {
                Interval i;
                i.start = timeOffset;
                i.duration = 0;
                var t = Array.BinarySearch(intervals, i, s_intervalComparer);
                if(t < 0) {
                    t = ~t - 1;
                    if(t < 0) {
                        intervalIndex = -1;
                        return int.MinValue;
                    }
                }
                intervalIndex = t;
                return intervals[t].End - timeOffset;
            }
        }

        [Serializable]
        public struct Score {
            public Sprite icon;
            public float value;
            public PrizeData prize;
        }
        [Serializable]
        public struct Interval {
            public int start;
            public int duration;
            public int End => start + duration;
        }
        private struct EndPoint {
            public int level;
            public bool isStart;
            public int time;
            public int combinationIndex;
        }
        public class IntervalComparer : IComparer<Interval> {
            public int Compare(ChallengeData.Interval x, ChallengeData.Interval y) {
                return x.start - y.start;
            }
        }
        private class EndPointComparer : IComparer<ChallengeData.EndPoint> {

            public int Compare(EndPoint e1, EndPoint e2) {
                return e1.time - e2.time;
            }
        }
    }
}