#if UNITY_ANDROID
#endif
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;

namespace ElephantSDK
{
    public class AndroidPostProcess : IPostGenerateGradleAndroidProject
    {
        private string _manifestFilePath;
        
        public int callbackOrder => 1;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // If needed, add condition checks on whether you need to run the modification routine.
            // For example, specific configuration/app options enabled
            var androidManifest = new AndroidManifest(GetManifestPath(path));

            androidManifest.SetAdIdPermission();

            // Add your XML manipulation routines
            androidManifest.Save();
        }
        
        private string GetManifestPath(string basePath)
        {
            if (string.IsNullOrEmpty(_manifestFilePath))
            {
                var pathBuilder = new StringBuilder(basePath);
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
                pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
                _manifestFilePath = pathBuilder.ToString();
            }
            return _manifestFilePath;
        }
    }
    
    internal class AndroidXmlDocument : XmlDocument
        {
            private string m_Path;
            protected XmlNamespaceManager nsMgr;
            public readonly string AndroidXmlNamespace = "http://schemas.android.com/apk/res/android";
            public AndroidXmlDocument(string path)
            {
                m_Path = path;
                using (var reader = new XmlTextReader(m_Path))
                {
                    reader.Read();
                    Load(reader);
                }
                nsMgr = new XmlNamespaceManager(NameTable);
                nsMgr.AddNamespace("android", AndroidXmlNamespace);
            }

            public string Save()
            {
                return SaveAs(m_Path);
            }

            public string SaveAs(string path)
            {
                using (var writer = new XmlTextWriter(path, new UTF8Encoding(false)))
                {
                    writer.Formatting = Formatting.Indented;
                    Save(writer);
                }
                return path;
            }
        }

        internal class AndroidManifest : AndroidXmlDocument
        {
            private readonly XmlElement _applicationElement;

            public AndroidManifest(string path) : base(path)
            {
                _applicationElement = SelectSingleNode("/manifest/application") as XmlElement;
            }

            private XmlAttribute CreateAndroidAttribute(string key, string value)
            {
                XmlAttribute attr = CreateAttribute("android", key, AndroidXmlNamespace);
                attr.Value = value;
                return attr;
            }

            internal void SetAdIdPermission()
            {
                var manifest = SelectSingleNode("/manifest");
                XmlElement child = CreateElement("uses-permission");
                manifest.AppendChild(child);
                XmlAttribute newAttribute = CreateAndroidAttribute("name", "com.google.android.gms.permission.AD_ID");
                child.Attributes.Append(newAttribute);
            }
        }
}