using Mobge.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Mobge.War {
    public class CharacterPool : MonoBehaviour {
        
        public const string c_key = "hmChrPl";


        public static CharacterPool Get(LevelPlayer player) {
            if(!player.TryGetExtra(c_key, out CharacterPool p)) {
                p = new CharacterPool();
                player.SetExtra(c_key, p);
            }
            return p;
        }


        private PrefabCache<Character> _characterCache;
        public PrefabCache<Character> Pool => _characterCache;
        private CharacterPool() {
            _characterCache = new PrefabCache<Character>(true, true);
        }

    }
}