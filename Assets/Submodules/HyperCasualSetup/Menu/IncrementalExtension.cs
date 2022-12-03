using Mobge.Telemetry;
using Mobge.UI;
using UnityEngine;

namespace Mobge.HyperCasualSetup.UI {
    [System.Serializable]
    public class IncrementalExtension : MonoBehaviour, BaseMenu.IExtension {
        [SerializeField] private ItemGroupData items;
        [SerializeField] private ItemSet _set;
        public bool active;
        public int startLevel;
        public IncrementalMenu incrementalMenuRes;
        private SubMenuManager<IncrementalMenu> _incrementalInstance;
        private MenuManager _menuManager;

        private ItemGroupData _itemsToSend;

        public void Prepare(BaseMenu menu) {
            PrepareGroupData();
            if (_itemsToSend == null || incrementalMenuRes == null) return;

            if (_incrementalInstance == null) {
                _incrementalInstance = new SubMenuManager<IncrementalMenu>(incrementalMenuRes, menu);
                _menuManager = (HyperCasualSetup.UI.MenuManager)menu.MenuManager;
            }
            if (_menuManager.TryGetLastOpenedLinearIndex(out int index) && startLevel - 1 <= index && active) {
                _incrementalInstance.SetEnabled(true);
                _incrementalInstance.Menu.Prepare(_itemsToSend);
            }
            else {
                _incrementalInstance.SetEnabled(false);
            }
        }
        private ItemGroupData PrepareGroupData() {
            if (_itemsToSend != null) {
                return _itemsToSend;
            }
            
            if (items != null) {
                _itemsToSend = items;
            }
            else if (_set != null) {
                ItemGroupData.RuleSet ruleSet = new ItemGroupData.RuleSet();
                ruleSet.name = _set.name;
                ruleSet.selectMode = ItemGroupData.SelectMode.FirstAvailable;
                var rule = new ItemGroupData.Rule();
                rule.label = -1;
                rule.set = _set;
                ruleSet.rules = new ItemGroupData.Rule[] { rule };
                _itemsToSend = ItemGroupData.Create(new ItemGroupData.RuleSet[] { ruleSet });
            }
            return _itemsToSend;
        }
    }
}