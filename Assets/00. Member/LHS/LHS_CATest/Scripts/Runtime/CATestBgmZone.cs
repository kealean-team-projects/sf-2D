using System.Collections.Generic;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 배경음 구역. 플레이어가 이 사각형 안에 있으면 bgmKey / ambienceKey 의 소리가 나온다.
    /// 여러 구역이 겹치면 priority 가 높은 쪽. 소리 파일은 CATestAudioLibrary 에서 같은 Key 칸에 넣는다.
    /// (구역을 벗어나 어느 구역에도 속하지 않으면 마지막 곡이 계속 나온다.)
    ///
    /// ■ 기본 구역은 씬에 배치하지 않고 실행 중에 만든다(아래 Defaults 표, CreateDefaults).
    ///   → 맵 씬/CoreScene 파일을 건드리지 않는다. 구역 범위나 곡을 바꾸려면 Defaults 표만 고치면 된다.
    ///   맵 씬에 직접 CATestBgmZone 을 추가해도 같이 동작한다(priority 로 우선순위 조절).
    /// </summary>
    public sealed class CATestBgmZone : MonoBehaviour {
        public Rect area = new(0, 0, 100, 60);
        [Tooltip("CATestAudioLibrary 의 배경음 Key (예: forest_sunny). 비우면 배경음은 바꾸지 않음")]
        public string bgmKey;
        [Tooltip("CATestAudioLibrary 의 환경음 Key (예: amb_forest). 비우면 환경음 끔")]
        public string ambienceKey;
        public int priority;
        [Tooltip("이 구역에서 가끔 무작위로 나는 소리(효과음 Key, 예: amb_bird). 비우면 없음")]
        public string oneShotKey;
        [Tooltip("무작위 소리 간격(최소, 최대 초)")]
        public Vector2 oneShotInterval = new(6f, 14f);

        private static readonly List<CATestBgmZone> Zones = new();

        // (이름, 범위, 배경음 Key, 환경음 Key, 우선순위) — 범위는 각 맵의 스트리밍 범위와 같다
        // 배경음: 숲은 튜토리얼 숲 / 울창한 숲(+안개 골짜기) / 몽환의 숲(+바람의 산) 3곡, 심해는 기본 / 보스전 2곡
        public static readonly (string name, Rect area, string bgm, string amb, int prio, string oneShot, float min, float max)[] Defaults = {
            ("Prologue", CATestWorld.PrologueBounds, "prologue_room", "amb_room", 0, "amb_room_oneshot", 10f, 20f),
            ("Tutorial", new Rect(-1965, -45, 705, 190), "forest_tutorial", "amb_forest", 0, "amb_bird", 4f, 10f),
            ("TutorialRiver", new Rect(-1530, -10, 100, 40), "forest_tutorial", "amb_river", 1, "amb_bird", 5f, 12f),
            ("Lush", new Rect(-1290, -45, 546, 150), "forest_lush", "amb_forest_deep", 0, "amb_bird_deep", 6f, 14f),
            ("Fog", new Rect(-744, -45, 284, 150), "forest_lush", "amb_fog", 1, "amb_fog_oneshot", 8f, 18f),
            ("Dream", new Rect(-480, -35, 228, 190), "forest_dream", "amb_dream", 0, "amb_dream_oneshot", 6f, 14f),
            ("Mountain", new Rect(-252, -35, 172, 190), "forest_dream", "amb_wind", 1, "amb_wind_howl", 8f, 16f),
            ("DeepSea", new Rect(225, -132, 875, 150), "deepsea", "amb_underwater", 0, "amb_whale", 12f, 26f),
            ("BossArena", new Rect(506, -100, 504, 40), "deepsea_boss", "amb_underwater", 1, "", 0f, 0f),
            ("Canyon", new Rect(1360, -40, 280, 150), "twilight_canyon", "amb_twilight", 0, "amb_twilight_oneshot", 10f, 20f),
            ("Sanctum", new Rect(1640, -40, 245, 150), "twilight_sanctum", "amb_twilight", 1, "amb_twilight_oneshot", 10f, 20f),
            ("Nest", new Rect(1885, -40, 165, 150), "dragon_nest", "amb_twilight", 2, "", 0f, 0f),
        };

        /// <summary>기본 구역들을 parent 아래에 만든다(이미 만들었으면 아무것도 안 함).</summary>
        public static void CreateDefaults(Transform parent) {
            if (parent.Find("BgmZones") != null) return;
            var root = new GameObject("BgmZones").transform;
            root.SetParent(parent, false);
            foreach (var d in Defaults) {
                var go = new GameObject("Bgm_" + d.name);
                go.transform.SetParent(root, false);
                var z = go.AddComponent<CATestBgmZone>();
                z.area = d.area; z.bgmKey = d.bgm; z.ambienceKey = d.amb; z.priority = d.prio;
                z.oneShotKey = d.oneShot; z.oneShotInterval = new Vector2(d.min, d.max);
            }
        }
        private static CATestBgmZone _current;

        /// <summary>다음 판정에서 현재 구역의 곡을 다시 지정하게 함(타이틀 → 게임 복귀 등).</summary>
        public static void ResetCurrent() => _current = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Zones.Clear(); _current = null; }

        private void OnEnable() => Zones.Add(this);
        private void OnDisable() { Zones.Remove(this); if (_current == this) _current = null; }

        private void Update() {
            // 구역 중 하나만(목록의 첫 번째) 대표로 판정을 돌린다
            if (Zones.Count == 0 || Zones[0] != this) return;
            var p = CATestHUD.PlayerTransform;
            if (p == null) return;
            Vector2 pos = p.position;
            CATestBgmZone best = null;
            foreach (var z in Zones)
                if (z.area.Contains(pos) && (best == null || z.priority > best.priority)) best = z;
            if (best == null || best == _current) return;
            _current = best;
            CATestAudio.SetZone(best.bgmKey, best.ambienceKey ?? "");
            CATestAudio.SetZoneOneShots(best.oneShotKey, best.oneShotInterval);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.35f);
            Gizmos.DrawWireCube(area.center, area.size);
            UnityEditor.Handles.Label(new Vector3(area.xMin + 1f, area.yMin + 2f), "♪ " + bgmKey + " / " + ambienceKey);
        }
#endif
    }
}
