using Mobge.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.DebugMenu {
    public class LogViewer : MonoBehaviour {
        private List<Log> logList = new List<Log>();
        [OwnComponent(true)] public Canvas canvas;
        public Toggle transparentToggle;
        public Image bg;
        public float bgTransparency;
        public UIItem defaultLog;
        public Button clearButton;
        public TMP_Text stackTraceText;
        public Action OnClose;
        public Button backButton;
        private const int ERROR_ICON_INDEX = 0;
        private const int WARNING_ICON_INDEX = 1;
        private const int INFO_ICON_INDEX = 2;


        void Start() {
            Application.logMessageReceivedThreaded += LogMessageReceivedThreaded;

            transparentToggle.onValueChanged.AddListener(delegate { OnToggleValueChanged(transparentToggle); });
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(Clear);
            Clear();
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Close);
        }

        private void OnToggleValueChanged(Toggle transparentToggle) {
            if (transparentToggle.isOn) {
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, bgTransparency);
            }
            else {
                bg.color = new Color(bg.color.r, bg.color.g, bg.color.b, 255);
            }
        }

        private void LogMessageReceivedThreaded(string condition, string stackTrace, LogType type) {
            logList.Add(new Log(condition, stackTrace, type));
            if (canvas.enabled)
                UpdateVisual();
        }

        private void UpdateVisual() {
            int index = logList.Count - 1;

            var log = logList[index];
            var item = Instantiate(defaultLog.gameObject, defaultLog.transform.parent).GetComponent<UIItem>();
            item.gameObject.SetActive(true);
            var time = "[" + DateTime.Now.ToShortTimeString() + "]";
            item.textsTMPro[0].text = time + " " + log.condition;
            item.buttons[0].button.onClick.AddListener(delegate { ShowStackTrace(index); });

            switch (log.type) {
                case LogType.Error:
                    item.images[ERROR_ICON_INDEX].gameObject.SetActive(true);
                    item.images[WARNING_ICON_INDEX].gameObject.SetActive(false);
                    item.images[INFO_ICON_INDEX].gameObject.SetActive(false);
                    break;
                case LogType.Warning:
                    item.images[ERROR_ICON_INDEX].gameObject.SetActive(false);
                    item.images[WARNING_ICON_INDEX].gameObject.SetActive(true);
                    item.images[INFO_ICON_INDEX].gameObject.SetActive(false);
                    break;
                case LogType.Log:
                    item.images[ERROR_ICON_INDEX].gameObject.SetActive(false);
                    item.images[WARNING_ICON_INDEX].gameObject.SetActive(false);
                    item.images[INFO_ICON_INDEX].gameObject.SetActive(true);
                    break;
                case LogType.Exception:
                    item.images[ERROR_ICON_INDEX].gameObject.SetActive(true);
                    item.images[WARNING_ICON_INDEX].gameObject.SetActive(false);
                    item.images[INFO_ICON_INDEX].gameObject.SetActive(false);
                    break;
                default:
                    item.images[ERROR_ICON_INDEX].gameObject.SetActive(false);
                    item.images[WARNING_ICON_INDEX].gameObject.SetActive(false);
                    item.images[INFO_ICON_INDEX].gameObject.SetActive(true);
                    break;
            }
        }

        private void ShowStackTrace(int index) {
            stackTraceText.text = logList[index].stackTrace;
        }

        private void Clear() {
            logList.Clear();
            stackTraceText.text = string.Empty;
            var parent = defaultLog.transform.parent;
            for (int i = 1; i < parent.childCount; i++) {
                Destroy(parent.GetChild(i).gameObject);
            }
        }

        public void Open() {
            canvas.enabled = true;
        }

        public void Close() {
            canvas.enabled = false;
            if (OnClose != null) {
                OnClose();
            }
        }

        struct Log {
            public string condition;
            public string stackTrace;
            public LogType type;

            public Log(string condition, string stackTrace, LogType type) {
                this.condition = condition;
                this.stackTrace = stackTrace;
                this.type = type;
            }
        }
    }
}