using Mobge.Core;
using Mobge.HyperCasualSetup;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge.IdleGame {
    public class WalletComponent : MonoBehaviour {
        public const string c_tag = "WalletComponent";


        public static bool TryGet(Collider c, out WalletComponent wallet) {
            if (c.CompareTag(c_tag)) {
                return c.TryGetComponent(out wallet);
            }
            wallet = default;
            return false;
        }
        public static WalletComponent Create(BaseLevelPlayer player, GameObject gameObject, ItemSet moneyUnits, bool persistent) {
            var wo = gameObject.AddComponent<WalletComponent>();
            wo._player = player;
            wo.persistent = persistent;
            wo.moneyUnits = moneyUnits;
            return wo;
        }

        [SerializeField] private bool persistent;
        [SerializeField] private ItemSet moneyUnits;
        



        private ItemCluster _values;
        private BaseLevelPlayer _player;

        public Action<WalletComponent> onChange;

        public bool Initialized => _values != null;
        public ItemSet CurrencySet => moneyUnits;

        protected void Awake() {
            gameObject.tag = c_tag;
        }

        public ListIndexer<ItemCluster.ItemContent> Items {
            get => _values.Items;
        }

        public WalletComponent EnsureInitialization() {
            if(_player == null) {
                _player = GetComponentInParent<BaseLevelPlayer>();
                if (_player == null) {
                    throw new Exception("Instances of " + typeof(WalletComponent) + " must be instantiated under a " + typeof(BaseLevelPlayer) + ".");
                }
            }
            if (_values == null) {
                _values = new ItemCluster(this.moneyUnits);
                if (persistent) {
                    _values.UpdateFromItemSet(_player.Context);
                }
            }
            return this;
        }


        protected void Start() {
            EnsureInitialization();
        }
        private void AmountChanged(out AGameProgress prog) {
            prog = null;
            if (persistent) {
                prog = _player.Context.GameProgressValue;
                var setData = prog.GetQuantityItemSet(this.moneyUnits);
                setData.Clear();
                for(int i = 0; i < _values.Items.Count; i++) {
                    var it = _values.Items[i];
                    setData.AddItem(it.id, it.count);
                }
                _player.Context.GameProgress.SaveValue(prog);
            }
            if (onChange != null) {
                onChange(this);
            }
        }

        public bool HasCollider(Collider collider) {
            return collider.gameObject == gameObject;
        }

        public bool HasEnough(ItemCluster moneyAmount, out bool typeMismatch) {
            return this._values.Contains(moneyAmount, out typeMismatch);

        }
        public bool HasEnough(in ItemCluster.ItemContent item) {
            return this._values.Contains(item);
        }
        public int GetAmount(int currencyId) {
            return _values.GetCount(currencyId, out _);
        }
        public void Add(ItemCluster money, out bool typeMismatch) {
            _values.Add(money, out typeMismatch);
            AmountChanged(out _);
        }
        public bool TryRemove(ItemCluster money, out bool typeMismatch) {
            var result = _values.TryRemove(money, out typeMismatch);
            if (result) {
                AmountChanged(out _);
            }
            return result;
        }
        public bool TryRemove(in ItemCluster.ItemContent item) {
            var result = _values.TryRemove(item);
            if (result) {
                AmountChanged(out _);
            }
            return result;
        }
        public void Change(in ItemCluster.ItemContent item, string source = null) {
            if (item.count > 0) {
                _values.Add(item.id, item.count);
            }
            else if (item.count < 0) {
                _values.TryRemove(item.id, -item.count);
            }
            if (item.count != 0) {
                AmountChanged(out var p);
                if (p != null && !string.IsNullOrEmpty(source)) {
                    p.FireMoneyEvent(_player.Context, item.count, source);
                }
            }
        }
    }
}