using Mobge;
using UnityEditor;
using UnityEngine;

namespace Mobge.GitHooks {
    public class GitHooksInstallWindow : EditorWindow {
        private bool _rememberMyChoiceToggle = true;
        private void OnGUI() {
            GUILayout.Label("Do you want to auto install git hooks?", EditorStyles.boldLabel);
            using (Scopes.GUIColor(Color.red)) {
                if (GUILayout.Button("Do Not Install", GUILayout.Width(200))){
                    if (_rememberMyChoiceToggle) {
                        GitHooksInstaller.RememberedChoice = GitHooksInstaller.RememberChoice.DoNotInstall;
                    }
                    Close();
                }
            }
            using (Scopes.GUIColor(Color.yellow)) {
                if (GUILayout.Button("Remove", GUILayout.Width(200))) {
                    GitHooksInstaller.RememberedChoice = GitHooksInstaller.RememberChoice.None;
                    GitHooksInstaller.Remove();
                    Close();
                }
            }
            using (Scopes.GUIColor(Color.green)) {
                if (GUILayout.Button("Install", GUILayout.Width(200))) {
                    if (_rememberMyChoiceToggle) {
                        GitHooksInstaller.RememberedChoice = GitHooksInstaller.RememberChoice.Install;
                    }
                    GitHooksInstaller.Install();
                    Close();
                }
            }
            _rememberMyChoiceToggle = GUILayout.Toggle(_rememberMyChoiceToggle, "Remember My Choice");
        }
    
        [MenuItem("Mobge/Git Hooks Installer")]
        internal static void OpenWindow() {
            EditorWindow window = EditorWindow.FindObjectOfType<GitHooksInstallWindow>();
            if (window == null) {
                window = EditorWindow.GetWindow<GitHooksInstallWindow>();
            }
            window.Show();
        }
    }
}
