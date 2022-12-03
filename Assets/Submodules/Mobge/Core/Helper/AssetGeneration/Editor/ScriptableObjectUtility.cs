using UnityEngine;
using UnityEditor;
using System.IO;

namespace Mobge.Core
{
    public static class ScriptableObjectUtility
    {
        /// <summary>
        //  This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static void CreateAsset<T>() where T : ScriptableObject
        {
            
            T asset = ScriptableObject.CreateInstance<T>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets/Data";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + typeof(T).ToString() + ".asset");
             
            ProjectWindowUtil.CreateAsset(ScriptableObject.CreateInstance<T>(), assetPathAndName);
            // AssetDatabase.CreateAsset(asset, assetPathAndName);

            // AssetDatabase.SaveAssets();
            // AssetDatabase.Refresh();
            // EditorUtility.FocusProjectWindow();
            // Selection.activeObject = asset;
        }


        public static Piece DuplicatePiece(Piece p)
        {
            // TODO Handle inner pieces (create copy instances, modify rectangle locations etc.) 
            //Piece asset = Piece.Create(p.atoms, p.pieces);
            Piece asset = Piece.CreateInstance<Piece>();
            asset.name += " " + Random.Range(0, int.MaxValue);
            string path = "Assets/Core Test/Data/Pieces";
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/New " + p.name + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            var newasset = AssetDatabase.LoadAssetAtPath<Piece>(assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return newasset;
        }
    }
}
