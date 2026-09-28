#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LHS_CATest.EditorTools {
    /// <summary>
    /// 오디오 준비.
    ///  1) Resources/CATest_AudioLibrary.asset 이 없으면 만들고, 있으면 "없는 Key 줄만" 추가한다(넣어 둔 소리는 절대 지우지 않음).
    ///  (배경음 구역은 씬에 배치하지 않는다 — CATestBgmZone.Defaults 표로 실행 중에 생성)
    /// </summary>
    public static partial class CATestBuilder {
        public const string AudioResourcesDir = Root + "/Resources";
        public const string AudioLibraryPath = AudioResourcesDir + "/CATest_AudioLibrary.asset";

        [MenuItem("Tools/LHS_CATest/Audio Library 열기 (소리 넣기)", priority = 60)]
        public static void SelectAudioLibrary() {
            var lib = EnsureAudioLibrary();
            Selection.activeObject = lib;
            EditorGUIUtility.PingObject(lib);
        }

        public static CATestAudioLibrary EnsureAudioLibrary() {
            EnsureFolder(AudioResourcesDir);
            var lib = AssetDatabase.LoadAssetAtPath<CATestAudioLibrary>(AudioLibraryPath);
            if (lib == null) {
                lib = ScriptableObject.CreateInstance<CATestAudioLibrary>();
                AssetDatabase.CreateAsset(lib, AudioLibraryPath);
            }
            var changed = AddMissing(lib.bgm, CATestAudioLibrary.DefaultBgm, 0.8f, 0f);
            changed |= AddMissing(lib.ambience, CATestAudioLibrary.DefaultAmbience, 0.6f, 0f);
            changed |= AddMissing(lib.sfx, CATestAudioLibrary.DefaultSfx, 1f, 0.05f);
            if (changed) {
                EditorUtility.SetDirty(lib);
                AssetDatabase.SaveAssets();
            }
            return lib;
        }

        private static bool AddMissing(List<CATestAudioLibrary.Entry> list, (string key, string note)[] defaults, float vol, float jitter) {
            // 목록에서 빠진 옛 Key 는 "소리를 안 넣은 경우에만" 정리(넣어 둔 소리는 절대 지우지 않음)
            var valid = new HashSet<string>();
            foreach (var d in defaults) valid.Add(d.key);
            var removed = list.RemoveAll(e => e == null || (!valid.Contains(e.key) && (e.clips == null || System.Array.TrueForAll(e.clips, c => c == null))));
            // 설명(note)은 최신 문구로
            foreach (var e in list) foreach (var d in defaults) if (e.key == d.key) e.note = d.note;
            var have = new HashSet<string>();
            foreach (var e in list) if (e != null && !string.IsNullOrEmpty(e.key)) have.Add(e.key);
            var changed = false;
            foreach (var (key, note) in defaults) {
                if (have.Contains(key)) continue;
                list.Add(new CATestAudioLibrary.Entry { key = key, note = note, volume = vol, pitchJitter = jitter });
                changed = true;
            }
            return changed || removed > 0 || true;
        }

    }
}
#endif
