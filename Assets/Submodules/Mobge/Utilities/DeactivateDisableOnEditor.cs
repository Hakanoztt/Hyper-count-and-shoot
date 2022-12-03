using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobge {
    public class DeactivateDisableOnEditor : MonoBehaviour {
#if UNITY_EDITOR
        public GameObject[] deactivateList;
        public Behaviour[] disableList;

        protected void Awake() {
            if (deactivateList != null) {
                for (int i = 0; i < deactivateList.Length; i++) {
                    deactivateList[i].gameObject.SetActive(false);
                }
            }
            if (disableList != null) {

                for (int i = 0; i < disableList.Length; i++) {
                    disableList[i].enabled = false;
                }
            }
        }
#endif
    }
}