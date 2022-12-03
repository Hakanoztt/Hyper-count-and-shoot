using System;
using Facebook.Unity;
using UnityEngine;

namespace Mobge.Telemetry {
    public class FacebookSdkInitializer : MonoBehaviour {
        private void Awake() {
            DontDestroyOnLoad(this.gameObject);
            if (FB.IsInitialized) {
                FB.ActivateApp();
            }
            else {
                FB.Init(() =>
                {
                    FB.ActivateApp();
                    FB.Mobile.SetAdvertiserTrackingEnabled(true);
                });
            }
        }

        private void OnApplicationPause(bool paused) {
            if (paused) return;
            if (FB.IsInitialized) {
                FB.ActivateApp();
            }
            else {
                FB.Init(FB.ActivateApp);
            }
        }
    }
}