using System.IO;

namespace Mobge {
    public static class SettingsManagerRuntime {
        
        private static readonly string SettingsRuntimeFolderPath = "MobgeSettings" + Path.DirectorySeparatorChar;
        public static bool TryGetRuntimeValue<T>(string path, string key, out T value){
            var filePath = Path.Combine(SettingsRuntimeFolderPath, path, key);
            return TryGetRuntimeValue(filePath, out value);
        }
        private static bool TryGetRuntimeValue<T>(string filePath, out T value) {
            var holder = UnityEngine.Resources.Load<SettingDataHolder>(filePath);
            if (holder == null) {
                value = default;
                return false;
            }
            var data = holder.GetObject<T>();
            value = data;
            return true;
        }

        // public static SettingEnumerator<T> EnumerateRuntime<T>(string path) {
        //     var folderPath = Path.Combine(SettingsRuntimeFolderPath, path);
        //     return new SettingEnumerator<T>(folderPath);
        // }
        // public class SettingEnumerator<T> : IEnumerator<T>, IEnumerable<T> {
        //     private readonly IEnumerator<string> _enumerator;
        //     public SettingEnumerator(string path) {
        //         _enumerator = Directory.EnumerateFiles(path).GetEnumerator();
        //     }
        //     public bool MoveNext() {
        //         bool notFinished = _enumerator.MoveNext();
        //         while (
        //                 //only skip if not finished
        //                 notFinished &&
        //                 //skip meta file
        //                 (_enumerator.Current != null && _enumerator.Current.EndsWith(".meta") ||
        //                 //skip can't get value
        //                 !TryGetRuntimeValue<T>(_enumerator.Current, out _)) 
        //         ) {
        //             notFinished = _enumerator.MoveNext();
        //         }
        //         return notFinished;
        //     }
        //     public void Reset() {
        //         _enumerator.Reset();
        //     }
        //     public T Current {
        //         get {
        //             TryGetRuntimeValue<T>(_enumerator.Current, out var val);
        //             return val;
        //         }
        //     }
        //     object IEnumerator.Current => Current;
        //     public void Dispose() {
        //         _enumerator.Dispose();
        //     }
        //     public IEnumerator<T> GetEnumerator() {
        //         return this;
        //     }
        //     IEnumerator IEnumerable.GetEnumerator() {
        //         return GetEnumerator();
        //     }
        // }
    }
}




