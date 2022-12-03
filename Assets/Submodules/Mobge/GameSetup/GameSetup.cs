using System;
using System.Collections;
using System.Collections.Generic;
using Mobge.Platformer;
using UnityEngine;

namespace Mobge {
    
    [CreateAssetMenu(menuName="Mobge/Platformer/Game Setup")]
    public class GameSetup : ScriptableObject
    {
        private const int MAX_NUMBER_OF_TEAMS = 32;
        public float PoiseRecoverRate = 0.17f;
        [Layer] public int defaultTriggerType;
        [HideInInspector]
        [SerializeField] private TeamSetup _teamSetup;
        public Camera camera = new Camera() {
            aspectRatio = 1,
            fov = 60,
        };
        [Serializable]
        public class TeamSetup : AutoIndexedMap<Team> {
            [SerializeField]
            private uint[] allyData;
            public void EnsureSetup(bool friendlyFireEnabled = false) {
                if (allyData == null || allyData.Length != MAX_NUMBER_OF_TEAMS) {
                    allyData = new uint[MAX_NUMBER_OF_TEAMS];
                    if (!friendlyFireEnabled) {
                        // set every team ally to itself
                        for (int i = 0; i < MAX_NUMBER_OF_TEAMS; i++) {
                            SetRelation(i, i, false);
                        }
                    }
                }
            }
            private void GetMask(int t1, int t2, out int index, out uint mask){
                if(t1 > t2) {
                    int temp = t1;
                    t1 = t2;
                    t2 = temp;
                }
                mask = 0x1u << t2;
                index = t1;
            }
            public bool IsEnemy(int t1, int t2) {
                uint mask;
                int index;
                GetMask(t1, t2, out index, out mask);
                return (allyData[index] & mask) == 0;
            }
            public void SetRelation(int t1, int t2, bool enemy) {
                uint mask;
                int index;
                GetMask(t1, t2, out index, out mask);
                if(enemy) {
                    allyData[index] = allyData[index] & ~mask;
                }
                else {
                    allyData[index] = allyData[index] | mask;
                }
            }
        }
        public virtual void EnsureSetup(bool friendlyFireEnabled = false) {
            if(_teamSetup == null) {
                _teamSetup = new TeamSetup();
            }
            _teamSetup.EnsureSetup(friendlyFireEnabled);
        }
        
        
        public TeamSetup Teams {
            get{ return _teamSetup; }
        }
        [Serializable]
        public struct Team {
            public string name;
            public override string ToString() {
                return name;
            }
        }
        private static GameSetup _defaultSetup;
        public static GameSetup DefaultSetup{
            get{
                if(!_defaultSetup){
                    _defaultSetup = CreateInstance<GameSetup>();
                    _defaultSetup.EnsureSetup(true);
                }
                return _defaultSetup;
            }
        }
        public static GameSetup NewSetup(bool friendlyFireEnabled = false) {
            return NewSetup<GameSetup>();
        }
        public static T NewSetup<T>(bool friendlyFireEnabled = false) where T : GameSetup {
            var gs = CreateInstance<T>();
            gs.EnsureSetup(friendlyFireEnabled);
            return gs;
        }
        [Serializable]
        public struct Camera {
            [Layer] public int layer;
            public Side2DCameraData default2D;
            public float fov;
            public float maxAspectRatio;
            public float minAspectRatio;
            public float aspectRatio;
        }
    }
    public interface IGameComponent {
        GameSetup GameSetup{get;}
    }
    public class TeamAttribute : PropertyAttribute {

    }
}