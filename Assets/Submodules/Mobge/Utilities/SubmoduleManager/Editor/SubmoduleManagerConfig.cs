using System;
using static SubmoduleManager.SubmoduleManager;

namespace SubmoduleManager {
    [Serializable]
    public struct SubmoduleManagerConfig {
        public Submodule[] submodules;

        public static SubmoduleManagerConfig DefaultConfig = new SubmoduleManagerConfig {
            submodules = new Submodule[0]
        };
    }  
}

