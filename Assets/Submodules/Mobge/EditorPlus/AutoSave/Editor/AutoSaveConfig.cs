using System;

namespace Mobge.AutoSave {
    [Serializable]
    public class AutoSaveConfig {
        public int saveIntervalSeconds = 300;
        public bool isDebugOn = false;
        public bool isAutoSaveOn = true;
    }
}