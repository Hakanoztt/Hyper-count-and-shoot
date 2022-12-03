using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Mobge {
    public class AssetReferenceTyped<T> : AssetReference where T : class {
        public AssetReferenceTyped(string guid) : base(guid)
        {
        }
        public AssetReferenceTyped() : base(null)
        {
        }

        public T LoadedAsset{
            get {
#if UNITY_EDITOR
                if(Application.isPlaying){
                    return base.Asset as T;
                }
                else{
                    return base.editorAsset as T;
                }
#else
                return base.Asset as T;
#endif
            }
        }
#if UNITY_EDITOR
        public T EditorAsset => editorAsset as T;
#endif
    }
}