using ElephantSDK;
using Mobge.HyperCasualSetup;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Mobge.DebugMenu {
    public class DebugMenuRemoteOverride : MonoBehaviour, IDebugMenuExtension {
        public TMPro.TMP_Dropdown dropdown;
        public Button reset;
        public Button apply;
        public TMPro.TMP_Text successText;
        public TMPro.TMP_Text errorText;
        public string serverRemoteTagsAddress;
        public string serverPreAddress;

        public const string save_overridedRemoteSet = "save_overridedRemoteSet";

        private GameContext _context;
        private string _jsonText;

        public void Init(DebugMenu debugMenu) {
            _context = debugMenu.Context;

            if (PlayerPrefs.HasKey(save_overridedRemoteSet)) {
                _jsonText = PlayerPrefs.GetString(save_overridedRemoteSet);
                RemoteConfig.GetInstance().Init(_jsonText);
            }
            debugMenu.OnMenuOpened += OnMenuOpen;
        }

        private void PrepareDropdownList(string[] obj) {
            dropdown.ClearOptions();

            var optionList = new List<TMPro.TMP_Dropdown.OptionData>();

            for (int i = 0; i < obj.Length; i++) {
                optionList.Add(new TMPro.TMP_Dropdown.OptionData(obj[i]));
            }
            dropdown.AddOptions(optionList);
        }

        private void OnMenuOpen() {
            dropdown.ClearOptions();
            errorText.gameObject.SetActive(false);
            successText.gameObject.SetActive(false);
            GetKeys(PrepareDropdownList);

            reset.onClick.RemoveAllListeners();
            reset.onClick.AddListener(ResetButtonOnClick);

            apply.onClick.RemoveAllListeners();
            apply.onClick.AddListener(ApplyButtonOnClick);
        }


        private void ApplyButtonOnClick() {
            OverrideServerSet(dropdown.options[dropdown.value].text,
                () => {
                    successText.gameObject.SetActive(true);
                    errorText.gameObject.SetActive(false);
                },
                () => {
                    errorText.gameObject.SetActive(true);
                    successText.gameObject.SetActive(false);
                });

        }

        private void ResetButtonOnClick() {
            PlayerPrefs.DeleteKey(save_overridedRemoteSet);
        }

        public void GetKeys(Action<string[]> getKeyAction) {
            _context.StartCoroutine(GetKeysFromServer(getKeyAction));
        }

        IEnumerator GetKeysFromServer(Action<string[]> getKeyAction) {
            UnityWebRequest www = UnityWebRequest.Get(serverRemoteTagsAddress);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogWarning("There is no remote override key.");
            }
            else {
                string str = www.downloadHandler.text;

                str = String.Join("", str.Split('[', '"', ']'));
                var keys = str.Split(',');

                getKeyAction?.Invoke(keys);
            }
        }


        public void OverrideServerSet(string keyName, Action onSuccess, Action onFail) {
            _context.StartCoroutine(GetJsonTextFromMobgeServer(keyName, onSuccess, onFail));
        }

        IEnumerator GetJsonTextFromMobgeServer(string keyName, Action onSuccess = null, Action onFail = null) {
            UnityWebRequest www = UnityWebRequest.Get(serverPreAddress + keyName);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) {
                onFail?.Invoke();

            }
            else {
                _jsonText = www.downloadHandler.text;
                PlayerPrefs.SetString(save_overridedRemoteSet, _jsonText);

                onSuccess?.Invoke();
            }
        }


    }
}