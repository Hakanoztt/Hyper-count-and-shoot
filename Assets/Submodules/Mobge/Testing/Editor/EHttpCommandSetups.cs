using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Net;
using System.IO;

namespace Mobge.Test {
    [CustomEditor(typeof(HttpCommandSetups))]
    public class EHttpCommandSetups : Editor {
        private HttpCommandSetups setups;
        private int _selectedSetup;
        private void OnEnable() {
            setups = (HttpCommandSetups)target;
        }
        public override void OnInspectorGUI() {
            if(setups == null) {
                return;
            }
            setups.ip = EditorGUILayout.TextField("ip", setups.ip);
            setups.port = EditorGUILayout.IntField("port", setups.port);
            if(setups.setups == null) {
                setups.setups = new HttpCommandSetups.Setup[0];
            }
            Mobge.EditorLayoutDrawer.CustomArrayField("setups", ref setups.setups, (layout, t) => {
                t.name = EditorGUI.TextField(layout.NextRect() ,"name", t.name);
                if(t.methods == null) {
                    t.methods = new HttpCommandSetups.MethodCall[0];
                }
                EditorDrawer.CustomArrayField(layout, "methods", ref t.methods, (layoutM, m) => {
                    m.methodName = EditorGUI.TextField(layoutM.NextRect(), "method", m.methodName);

                    EditorDrawer.CustomArrayField(layoutM, "parameters", ref m.parameters, (layoutP, p) => {
                        var r = layoutP.NextRect();
                        var r1 = r;
                        r1.width *= 0.5f;
                        var r2 = r1;
                        r2.x += r1.width;
                        p.key = EditorGUI.TextField(r1, p.key);
                        p.value = EditorGUI.TextField(r2, p.value);
                        return p;
                    });
                    //if(GUI.Button(layoutM.NextRect(), "Call method")) {
                    //    CallMethod(setups, m);
                    //}
                    return m;
                });
                if (GUI.Button(layout.NextRect(), "Call all methods")) {
                    for(int i = 0; i < t.methods.Length; i++) {
                        CallMethod(setups, t.methods[i]);
                    }
                }
                return t;
            }, ref _selectedSetup);
            if(GUI.changed) {
                EditorExtensions.SetDirty(setups);
            }
        }
        private static void CallMethod(HttpCommandSetups s, HttpCommandSetups.MethodCall m) {
            string html = string.Empty;
            string url = "http://" + s.ip + ":" + s.port + "/" + m.methodName;
            if (m.parameters.Length > 0) {
                url += "?";
                for (int i = 0; i < m.parameters.Length; i++) {
                    var par = m.parameters[i];
                    url += par.key + "=" + par.value;
                    if(i < m.parameters.Length - 1) {
                        url += "&";
                    }
                }
            }
            System.Threading.Thread t = new System.Threading.Thread(() => {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream)) {
                    html = reader.ReadToEnd();
                }

                Debug.Log(html);
            });
            t.Start();
        }
    }

}