using System.IO;
using _02._Script._02_Core;
using _02._Script._05_Managers;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 이어하기 / 엔딩 기록 / 진행도 초기화.
    ///
    /// ■ 이어하기 위치는 "원본 저장 시스템"을 그대로 쓴다.
    ///   세이브 포인트에 닿으면 → SaveManager.RequestCapture() → PlayerProgress.CaptureProgress()
    ///   → SaveManager.SaveProgress() 가 Application.persistentDataPath/Save.sf2d 에 (위치, 기력, 스테이지)를 암호화해 저장한다.
    ///   원본에는 이 파일을 "게임 시작 때 읽는" 코드가 없어서(RestoreProgress 를 부르는 곳이 없음) 이어하기가 안 됐다.
    ///   → CATest 에서는 타이틀의 [이어하기]가 이 파일을 읽어 그 위치에서 시작한다(CATestBootstrap).
    ///
    /// ■ CATest 전용 부가 정보는 PlayerPrefs:
    ///   - 마지막 세이브 지점 이름(타이틀에 "이어하기 · 백색 성역" 처럼 표시)
    ///   - 본 엔딩(A/B/C) 기록 → 타이틀에 수집 현황 표시
    /// </summary>
    public static class CATestSave {
        public const string SaveFile = "Save.sf2d"; // 원본 SaveManager 가 쓰는 파일 이름
        private const string P = "CATest.Save.";
        public const int EndingCount = 3;

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFile);
        public static bool HasContinue => File.Exists(SavePath);

        public static UserData LoadContinue() => HasContinue ? SaveSystem.Load<UserData>(SaveFile) : null;

        public static string LastPlaceName {
            get => PlayerPrefs.GetString(P + "Place", "");
            set { PlayerPrefs.SetString(P + "Place", value ?? ""); PlayerPrefs.Save(); }
        }

        /// <summary>세이브 포인트/컷신에서 호출: 원본 저장 + 장소 이름 기록.</summary>
        public static void SaveHere(string placeName = null) {
            if (SaveManager.Instance != null) SaveManager.Instance.RequestCapture();
            var place = !string.IsNullOrEmpty(placeName) ? placeName : CATestHUD.LastTitle;
            if (!string.IsNullOrEmpty(place)) LastPlaceName = place;
        }

        public static void DeleteContinue() {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (IOException e) { Debug.LogWarning($"[CATest] 세이브 파일 삭제 실패: {e.Message}"); }
            PlayerPrefs.DeleteKey(P + "Place");
            PlayerPrefs.Save();
        }

        // ───────────── 엔딩 ─────────────
        public static bool EndingSeen(int index) => PlayerPrefs.GetInt(P + "Ending" + index, 0) == 1;

        public static void MarkEnding(int index) {
            PlayerPrefs.SetInt(P + "Ending" + index, 1);
            PlayerPrefs.Save();
        }

        public static int EndingsSeenCount {
            get {
                var n = 0;
                for (var i = 0; i < EndingCount; i++) if (EndingSeen(i)) n++;
                return n;
            }
        }

        /// <summary>진행도 초기화: 이어하기 위치 + 엔딩 기록을 모두 지운다. (설정값은 유지)</summary>
        public static void ResetAll() {
            DeleteContinue();
            for (var i = 0; i < EndingCount; i++) PlayerPrefs.DeleteKey(P + "Ending" + i);
            PlayerPrefs.Save();
        }
    }
}
