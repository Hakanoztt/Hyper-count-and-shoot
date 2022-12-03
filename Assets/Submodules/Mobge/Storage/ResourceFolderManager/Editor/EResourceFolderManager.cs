using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Mobge {
    public class ResourceCashEditor : EditorWindow {
        private const string RootPath =  "Assets/Mobge/Storage/ResourceFolderManager/";
        private const string ASSET_PATH = RootPath + "Editor/ResourceFolderManager.asset";
        private const string ScriptName = "Resources";
        private const string Extension = "cs";

        private const string Namespace = "Mobge";
        private HashSet<char> _exceptedLetters;
        private HashSet<char> _numbers;

        private string _filter = "";

        private Vector2 _scroll;
        private string GetCodeFullPath(string scriptFolder) {
            
            return Path.Combine(scriptFolder, ScriptName + "." + Extension);
            
        }
        [MenuItem("Mobge/Resource Folder Manager")]
        static void Init() {
            //ResourceCashEditor  = new ResourceCashEditor();
            //var rcw = 
            EditorWindow.GetWindow<ResourceCashEditor>();

            //rcw.refreshCash();
        }

        private static ResourceFolderManager _shared;
        public static ResourceFolderManager Shared {
            get {
                if (_shared == null) {
                    _shared = AssetDatabase.LoadAssetAtPath<ResourceFolderManager>(ASSET_PATH);
                }
                return _shared;

            }
        }

        void OnGUI() {
            var data = Shared;
            if (!data) {
                AssetDatabase.CreateAsset(ResourceFolderManager.Construct(), ASSET_PATH);
                data = Shared;
            }
           
            var obj = EditorGUILayout.ObjectField("drag new object here", null, typeof(UnityEngine.Object), false);
            _filter = EditorGUILayout.TextField("filter", _filter);
            if (obj != null) {
                var no = new ResourceFolderManager.ObjectProps();
                no.obj = obj;
                no.generateInstantiateFunc = false;
                data.objList.Add(no);
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < data.objList.Count; ) {
                var name = "null";
                if (data.objList[i].obj) {
                    name = data.objList[i].obj.name;
                    if (name.IndexOf(_filter, StringComparison.InvariantCultureIgnoreCase) < 0) {
                        i++;
                        continue;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                var remove = GUILayout.Button("-", GUILayout.Width(40));
                if (GUILayout.Button("" + name, GUILayout.Width(200))) {

                    Selection.activeObject = data.objList[i].obj;

                }
                EditorGUILayout.LabelField(data.objList[i].obj.GetType().Name);
                //data.objList[i].obj = EditorGUILayout.ObjectField(data.objList[i].obj, typeof(UnityEngine.Object), false);
                if (remove) {
                    data.objList.RemoveAt(i);
                }
                else {
                    i++;
                }
                //data.objList[i].generateInstantiateFunc = EditorGUILayout.Toggle("instantiate func", data.objList[i].generateInstantiateFunc);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("script folder",GUILayout.Width(EditorGUIUtility.labelWidth));
            if(GUILayout.Button(data.scriptFolder)) {
                var s = EditorUtility.OpenFolderPanel(ScriptName + " script path.", data.scriptFolder + "Sd", "");
                if(!string.IsNullOrEmpty(s)) {
                    var path = Application.dataPath;
                    if(s.StartsWith(path)) {
                        data.scriptFolder = s.Substring(path.LastIndexOf("/") + 1);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(data.scriptFolder);
            if (GUILayout.Button("refresh cash script")) {
                _exceptedLetters = new HashSet<char>("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_1234567890");
                _numbers = new HashSet<char>("1234567890");
                List<ResourceInfo> ris = new List<ResourceInfo>();
                foreach (var v in data.objList) {
                    ris.AddRange(ObjectToReourceInfo(v.obj));
                }
                RefreshCash(data.scriptFolder, ris);
            }
            GUI.enabled = true;

            if (GUI.changed) {
                EditorExtensions.SetDirty(data);
            }
        }
        void RefreshCash(string codeDirectory, IEnumerable<ResourceInfo> ris) {
            //Debug.Log(ResourceCash.CashInternal._VideoEditor_vicik_Rigidbody.mass);
            //Debug.Log(ResourceCash.CashInternal._Materials_menuico_Texture2D.width);

            // if (GUILayout.Button("refresh code"))
            {

                if (!Directory.Exists(codeDirectory)) {
                    Directory.CreateDirectory(codeDirectory);
                }
                var codeFullPath = GetCodeFullPath(codeDirectory);
                var writer = File.Create(codeFullPath);
                sw = new StreamWriter(writer);
                GenerateFileContent(ris);
                sw.Close();
                writer.Close();
                AssetDatabase.ImportAsset(codeFullPath);
            }
        }
        #region resource traverse
        IEnumerable<ResourceInfo> AllResourcePaths() {
            DirectoryInfo resDir = new DirectoryInfo("Assets/Resources");
            foreach (var ri in AllResourcePaths("", "", resDir)) {
                //Debug.Log(ri.varName);
                if (ri.type.IsPublic) {
                    yield return ri;
                }
            }
        }
        string Trim(string s) {
            StringBuilder sb = new StringBuilder(s.Length+1);
            if (s.Length > 0 && _numbers.Contains(s[0])) {
                sb.Append("_");
            }
            foreach (var v in s) {
                if (_exceptedLetters.Contains(v)) {
                    sb.Append(v);
                }
                else {
                    sb.Append("_");
                }
            }
            return sb.ToString();
        }
        IEnumerable<ResourceInfo> AllResourcePaths(string varName, string path, DirectoryInfo parent) {
            varName = Trim(varName);
            foreach (var dir in parent.GetDirectories()) {
                var r = AllResourcePaths((varName.Length > 0 ? (varName + "_") : "") + dir.Name, (path.Length > 0 ? (path + "/") : "") + dir.Name, dir);
                foreach (var v in r) {
                    yield return v;
                }
            }
            foreach (var file in parent.GetFiles()) {
                var fileName = file.Name;
                var obj = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/" + path + "/" + file.Name);
                if (!fileName.EndsWith(".meta") && !fileName.EndsWith("DS_Store") && obj) {
                    //Debug.Log(file.FullName);
                    int dotIndex = fileName.LastIndexOf('.');
                    var resName = fileName.Substring(0, dotIndex);
                    var extension = fileName.Substring(dotIndex + 1);
                    var fname = Trim(resName) + ((dotIndex > 0) ? ("_" + extension) : "");
                    var resPath = path + "/" + resName;
                    //Debug.Log(resPath);
                    //Debug.Log(file.Name + " " + fileName + " " + resPath);
                    var fullVarName = varName + "__" + fname + "_" + obj.GetType().Name;
                    yield return new ResourceInfo(fullVarName, obj.GetType(), obj, resPath, extension);
                    //Debug.Log(obj);
                    if (obj is GameObject) {
                        var go = (GameObject)obj;
                        Type lastType = null;
                        foreach (var comp in go.GetComponents<Component>()) {
                            if (lastType != comp.GetType()) {
                                var ri = new ResourceInfo(varName + "__" + fname + "_" + comp.GetType().Name, comp.GetType(), comp, resPath, extension);
                                ri.gameObjectVarName = fullVarName;

                                yield return ri;
                                lastType = comp.GetType();
                            }
                        }
                    }
                    else {
                        //Debug.Log(path);
                    }
                }
            }
        }
        string VarNameFromResourcePath(string resourcePath) {
            var dnames = resourcePath.Split('/');
            if(dnames.Length <= 1){
                return "";
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(dnames[0]);
            for (int i = 1; i < dnames.Length-1; i++) {
                sb.Append("_");
                sb.Append(dnames[i]);
            }
            return Trim(sb.ToString());
        }
        IEnumerable<ResourceInfo> ObjectToReourceInfo(UnityEngine.Object obj) {
            var fileName = AssetDatabase.GetAssetPath(obj);
            string resPath;
            string extension = "";
            string folderName = "/Resources/";
            var folderIndex = fileName.IndexOf(folderName);
            if(folderIndex >= 0) {
            
                int startIndex = folderIndex + folderName.Length;
                var dotIndex = fileName.LastIndexOf('.');
                if(dotIndex < 0 || dotIndex < startIndex) {
                    resPath = fileName.Substring(startIndex);
                }
                else {
                    resPath = fileName.Substring(startIndex, dotIndex - startIndex);
                    extension = fileName.Substring(dotIndex + 1);
                }

                var varName = VarNameFromResourcePath(resPath);
                var lastSlash = fileName.LastIndexOf("/");
                if(lastSlash >=0){
                    fileName = fileName.Substring(lastSlash+1);
                }
                var resName = obj.name;
                var fullVarName = varName + "__" + resName + "_" + extension + "_" + obj.GetType().Name;
                fullVarName = Trim(fullVarName);
                yield return new ResourceInfo(fullVarName, obj.GetType(), obj, resPath, extension);
                
            }
        }
        struct ResourceInfo {
            public string varName;
            public Type type;
            public UnityEngine.Object obj;
            public string resPath;
            public string gameObjectVarName;
            public string internalName;
            public string extension;

            public ResourceInfo(string varName, Type type, UnityEngine.Object obj, string resPath, string extension) {
                this.extension = extension;
                this.varName = varName;
                this.type = type;
                this.obj = obj;
                this.resPath = resPath;
                gameObjectVarName = "";
                internalName = null;
            }
        }
        #endregion

        #region generate code
        int tabChars = 4;
        int indent = 0;
        StreamWriter sw;
        
        void GenerateFileContent(IEnumerable<ResourceInfo> ris) {
            string[] usings = new string[]{
                    "UnityEngine",
                    "System",
                    "System.Collections.Generic",
                    "System.Runtime.CompilerServices",
            };
            foreach (var use in usings) {
                DefineVariable("", "using", use);
            }
            BeginGroup("", "namespace", Namespace);
            {
                BeginGroup("public", "static class", ScriptName);
                {
                    // create load object method
                    BeginGroup("private static", "T", "lo<T>(string path, string extension) where T : UnityEngine.Object");
                    {

                        //Append("#if UNITY_EDITOR"); NewLine();

                        //append("var t = typeof(T);");
                        //append("string extension;");
                        //append("if (!(typeof(ScriptableObject).IsAssignableFrom(t))) {");
                        //append("    extension = \".prefab\";");
                        //append("}");
                        //append("else {");
                        //append("    extension = \".asset\";");
                        //append("}");

                        //Append("return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(\"Assets/Resources/\" + path + extension);"); NewLine();
                        //Append("#else"); NewLine();
                        Append("return UnityEngine.Resources.Load<T>(path);"); NewLine();
                        //Append("#endif"); NewLine();
                        
                        
                    }
                    EndGroup();


                    // defineVariable("public", "CashState", "state");
                    NewLine();
                    List<ResourceInfo> variableNames = new List<ResourceInfo>();
                    int index = 0;
                    foreach (var v in ris) {
                        if (!v.type.ToString().StartsWith("UnityEditor")) {
                            string i_name;
                            PutResourceProperty(v, out i_name, index++);
                            var vc = v;
                            vc.internalName = i_name;
                            variableNames.Add(vc);
                        }

                    }
                    Append("[MethodImpl(MethodImplOptions.NoInlining)]");
                    NewLine();
                    BeginGroup("public static", "void", "load(object obj)");
                    {

                    }
                    EndGroup();
                    BeginGroup("public static", "void", "Clear()");
                    {

                        foreach (var vname in variableNames) {
                            DefineVariable(vname.internalName, " = ", "null");
                        }
                    }
                    EndGroup();
                    /*
                    BeginGroup("public static", "class", "Instantiate");
                    {

                        foreach (var vname in variableNames) {
                            BeginGroup("public static", vname.type.ToString(), vname.varName + "()");
                            {
                                Append("return ");
                                Append("GameObject.Instantiate(");
                                Append(ScriptName);
                                Append(".");
                                Append(vname.varName);
                                Append(");");
                            }
                            EndGroup();
                        }
                    }
                    EndGroup();
                    */
                }
                EndGroup();
            }
            EndGroup();

        }
        void Append(object str) {
            sw.Write(str.ToString());
        }
        void PutResourceProperty(ResourceInfo ri, out string i_name, int index) {
            var internalName = "f" + index;
            i_name = internalName;
            //var boolName = ri.varName + "_b";
            DefineVariable("private static", ri.type.ToString(), internalName);
            //defineVariable("private static", "bool", boolName + " = false");
            BeginGroup("public static", ri.type.ToString(), ri.varName);
            {
                BeginGroup("", "get", "");
                {
                    BeginGroup("", "if", "(!" + internalName + ")");
                    {
                        if (ri.gameObjectVarName.Length > 0) {
                            Append(internalName);
                            Append(" = ");
                            Append(ri.gameObjectVarName);
                            Append(".GetComponent<");
                            Append(ri.type.ToString());
                            Append(">();");
                        }
                        else {
                            Append(internalName);
                            Append(" = lo<");
                            Append(ri.type.ToString());
                            Append(">(");
                            Append("\"");
                            Append(ri.resPath);
                            Append("\", \"");
                            Append("." + ri.extension);
                            Append("\");");
                        }
                    }
                    EndGroup();
                    DefineVariable("", "return", internalName);
                }
                EndGroup();
            }
            EndGroup();
            /*if (ri.obj is GameObject)
            {
                var go = (GameObject)ri.obj;
                foreach (var comp in go.GetComponents<Component>())
                {

                }
            }*/
        }
        void NewLine() {
            Append("\r\n");
            for (int i = 0; i < tabChars * indent; i++) {
                Append(' ');
                //Debug.Log("d");
            }
        }
        void DefineVariable(string protection, string type, string name) {
            Define(protection, type, name);
            Append(';');
            NewLine();
        }
        void Define(string protection, string type, string name) {
            if (protection.Length > 0) {
                Append(protection);
                Append(' ');
            }
            Append(type);
            Append(' ');
            Append(name);
        }
        void BeginGroup(string protection, string type, string name, string[] extensions = null) {
            Define(protection, type, name);
            if (extensions != null && extensions.Length > 0) {
                Append(" : ");
                int count = 0;
                foreach (var v in extensions) {
                    Append(v);
                    if (count < extensions.Length - 1) {
                        Append(", ");
                    }
                    count++;
                }
                //sb.Remove(sb.Length - 2, 2);
            }
            NewLine();
            Append('{');
            indent++;
            NewLine();
        }
        void EndGroup() {
            indent--;
            NewLine();
            Append('}');
            NewLine();
        }
        #endregion
    }
}