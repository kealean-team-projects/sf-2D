using UnityEngine;

namespace CSILib.SoundManager.RunTime {
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour {
        private static T _instance;
        private static bool IsDestroyed;

        public static T Instance {
            get {
                if (IsDestroyed)
                    _instance = null;

                if (_instance == null) {
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                        Debug.LogError($"{typeof(T).Name} singleton is not exist");
                    else
                        IsDestroyed = false;
                }

                return _instance;
            }
        }

        private void OnDestroy() {
            IsDestroyed = true;
        }
    }
}