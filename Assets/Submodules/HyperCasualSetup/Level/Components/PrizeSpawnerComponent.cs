using System;
using System.Collections.Generic;
using Mobge;
using Mobge.Core;
using UnityEngine;

namespace Mobge.HyperCasualSetup
{
    public class PrizeSpawnerComponent : ComponentDefinition<PrizeSpawnerComponent.Data>
    {
        [Serializable]
        public class Data : BaseComponent, IChild
        {
            [SerializeField, HideInInspector] private LogicConnections _connections;
            [SerializeField, HideInInspector] private ElementReference _parent = -1;
            [Header("Optional")] public SpriteRenderer visualRes;
            [Header("Optional")] public Sprite customIcon;

            public float visualScale = 1f;
            public PrizeData[] prizePool;

            public bool updateHudWhenCollected;
            public bool autoSpawn = true;
            public ReusableReference collectEffect;

            private SpriteRenderer _visual;
            private Dictionary<int, BaseComponent> _components;
            private BaseLevelPlayer _player;
            private bool _collected;
            private Transform _parentTR;

            private int _selectedPrize = -1;

            public PrizeData SelectPrize(out int index) {
                index = -1;
                return (prizePool != null && prizePool.Length > 0) ? prizePool[index = UnityEngine.Random.Range(0, prizePool.Length)] : default(PrizeData);
            }

            public override void Start(in InitArgs initData) {
                _components = initData.components;
                _player = (BaseLevelPlayer)initData.player;
                _collected = false;
                _parentTR = initData.parentTr;
                var prize = SelectPrize(out _selectedPrize);
                if (autoSpawn) {
                    InitializeVisual(initData.parentTr);
                }
                if (prize != null) {
                    if (prize.giftType == PrizeData.Type.Score) {
                        _player.TotalScore += prize.CalculateValue(0);
                    }
                }
            }

            public SpriteRenderer CreateAutoVisual() {

                return new GameObject("reward").AddComponent<SpriteRenderer>();
            }

            public void UpdateVisual(SpriteRenderer r, int prizeIndex = -1) {
                var tr = r.transform;
                tr.localScale = visualScale * (visualRes ? visualRes.transform.localScale : Vector3.one);
                if (prizeIndex >= 0) {
                    var prize = prizePool[prizeIndex];
                    var icon = prize.Icon;
                    if (icon == null) {
                        icon = customIcon;
                    }
                    if (icon != null) {
                        r.sprite = icon;
                    }
                }
            }

            private void InitializeVisual(Transform parent) {
                SpriteRenderer v;
                if (visualRes == null) {
                    v = CreateAutoVisual();
                }
                else {
                    v = Instantiate(visualRes);
                }
                var vtr = v.transform;
                vtr.SetParent(parent, false);
                vtr.localPosition = position;

                UpdateVisual(v, _selectedPrize);

                _visual = v;
            }
            public override Transform PrefabReference => visualRes ? visualRes.transform : null;
            public override LogicConnections Connections {
                get => _connections;
                set => _connections = value;
            }
            ElementReference IChild.Parent { get => _parent; set => _parent = value; }
            private bool TryCollect() {

                if (!_collected && _visual != null) {
                    _collected = true;
                    collectEffect.SpawnItem(position, _visual.transform.parent);
                    _visual.gameObject.SetActive(false);
                    var prize = prizePool[this._selectedPrize];
                    var data = _player.Context.GameProgressValue;
                    prize.ApplyToData(_player.Context, data, _player.Score, 0);
                    _player.Context.GameProgress.SaveValue(data);
                    if (updateHudWhenCollected) {
                        var tm = _player.Context.MenuManager.TopMenu;
                        if (tm != null) {
                            tm.Prepare();
                        }
                    }
                    return true;
                }
                return false;
            }
            public override object HandleInput(ILogicComponent sender, int index, object input) {
                switch (index) {
                    default:
                    case 0:
                        TryCollect();
                        break;
                    case 1:
                        InitializeVisual(_parentTR);
                        break;
                }
                return null;
            }
#if UNITY_EDITOR
            public override void EditorInputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("Try Collect", 0));
                slots.Add(new LogicSlot("Try Spawn", 1));
            }
            public override void EditorOutputs(List<LogicSlot> slots) {
                slots.Add(new LogicSlot("On Collected", 0));
            }
#endif
        }
    }
}