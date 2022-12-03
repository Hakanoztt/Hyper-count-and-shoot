using Mobge.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.HyperCasualSetup.UI {
    public class LevelTextExtension : MonoBehaviour, BaseMenu.IExtension {
        public TextReference text;
        public string levelTextPrefix = "LEVEL ";
        public string levelTextPostFix = "";
        public int indexOffset = 0;
        
        public void Prepare(BaseMenu menu) {
            var manager = menu.MenuManager as MenuManager;
            if (manager == null) return;
            var hasLi = manager.TryGetLastOpenedLinearIndex(out int index);
            if (hasLi) {
                var printedIndex = (index + 1 + indexOffset).ToString();
                text.Text = $"{levelTextPrefix}{printedIndex}{levelTextPostFix}";
            }
            else {
                text.Text = "";
            }
        }
    }
}