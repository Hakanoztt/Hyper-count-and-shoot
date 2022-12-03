using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Mobge.DebugMenu {
    public class DebugMenuPasswordScreen : MonoBehaviour, IDebugMenuExtension {
        public string password;
        public Image bg;
        public float bgTransparency;
        public GameObject content;
        public Button backButton;
        public List<Button> numberButtons;
        public Button loginButton;
        public TMP_InputField inputField;
        public TMP_Text errorText;
        private DebugMenu _menu;
        private string _loginkey = "debug_menu_logged";

        private bool AlreadyLoggedIn {
            get {
                return _menu.Context.GameProgressValue.GetBool(_loginkey, false);
            }
            set {
                var val = _menu.Context.GameProgressValue;
                val.SetBool(_loginkey, value);
                _menu.Context.GameProgress.SaveValue(val);
            }
        }

        private bool PasswordIsCorrect => inputField.text.Equals(password);

        public void Init(DebugMenu debugMenu) {
            _menu = debugMenu;
            _menu.OnMenuOpened += OnMenuOpened;

            for (int i = 0; i < numberButtons.Count; i++) {

                int number = i;
                numberButtons[number].onClick.RemoveAllListeners();
                numberButtons[number].onClick.AddListener(delegate { ButtonOnClick(number); });
            }
            loginButton.onClick.AddListener(LoginOnClick);
            errorText.gameObject.SetActive(false);
            backButton.onClick.AddListener(BackButtonOnClick);

        }

        private void BackButtonOnClick() {
            _menu.Hide();
        }

        private void LoginOnClick() {
            if (PasswordIsCorrect) {
                gameObject.SetActive(false);
                content.gameObject.SetActive(true);
                errorText.gameObject.SetActive(false);
                AlreadyLoggedIn = true;
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 1f);
            }
            else {
                inputField.text = string.Empty;
                errorText.gameObject.SetActive(true);
            }
        }

        private void ButtonOnClick(int index) {
            inputField.text += numberButtons[index].GetComponentInChildren<TMP_Text>().text;
        }


        private void OnMenuOpened() {
            inputField.text = string.Empty;

            if (AlreadyLoggedIn) {
                gameObject.SetActive(false);
                content.SetActive(true);
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 1f);
            }
            else {
                gameObject.SetActive(true);
                content.SetActive(false);
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, bgTransparency);
                errorText.gameObject.SetActive(false);
            }
        }
    }
}