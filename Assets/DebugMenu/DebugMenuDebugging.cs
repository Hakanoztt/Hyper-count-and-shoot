using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mobge.DebugMenu {

    public class DebugMenuDebugging : MonoBehaviour, IDebugMenuExtension {
        public LogViewer logViewerRes;
        public Button openLogViewerButton;
        public Button resetProgressButton;
        LogViewer _logViewer;
        private DebugMenu _menu;

        public LogViewer LogViewerInstance {
            get {
                if (_logViewer == null) {
                    _logViewer = Instantiate(logViewerRes, _menu.transform.parent);
                }
                return _logViewer;
            }
        }
        
        public void Init(DebugMenu debugMenu) {
            _menu = debugMenu;
            openLogViewerButton.onClick.RemoveAllListeners();
            openLogViewerButton.onClick.AddListener(OpenLogViewer);
            LogViewerInstance.Close();
            resetProgressButton.onClick.RemoveAllListeners();
            resetProgressButton.onClick.AddListener(ResetProgress);
        }

        private void ResetProgress() {
            var progressValue = _menu.Context.GameProgressValue;
            progressValue.ResetAllData();
            PlayerPrefs.DeleteAll();
            var progress = _menu.Context.GameProgress;
            progress.SaveValue(progressValue);
        }

        public void OpenLogViewer() {
            _menu.Hide();
            LogViewerInstance.Open();
            LogViewerInstance.OnClose -= CloseLogViewer;
            LogViewerInstance.OnClose += CloseLogViewer;
        }

        private void CloseLogViewer() {
            _menu.Show();
        }
    }
}