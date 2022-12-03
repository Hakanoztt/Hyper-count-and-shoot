using UnityEngine;
using UnityEngine.EventSystems;

namespace Mobge.Telemetry {
    [RequireComponent(typeof(EventSystem))]
    public class DeleteOtherEventSystemsThatAreInDoNotDestroyOnLoadScene : MonoBehaviour {
        protected void Awake() {
            var thisEventSystem = GetComponent<EventSystem>();
            var systems = FindObjectsOfType<EventSystem>();
            for (int i = 0; i < systems.Length; i++) {
                var eventSystem = systems[i];
                if (eventSystem == thisEventSystem) {
                    continue;
                }
                //if don't destroy on load
                if (eventSystem.gameObject.scene.buildIndex == -1) {
                    gameObject.DestroySelf();
                    break;
                }
            }
        }
    }
}
