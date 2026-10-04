using System;
using System.Collections.Generic;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// ★ CATest 오디오 목록 ★ — 효과음·배경음을 "칸에 끌어다 놓기"만 하면 바로 재생되게 하는 목록 에셋.
    ///
    /// ■ 위치: Assets/00. Member/LHS/LHS_CATest/Resources/CATest_AudioLibrary.asset
    ///   (Resources 폴더에 있어야 실행 중에 Resources.Load 로 찾을 수 있다 — 빌드에서도 동작)
    /// ■ 사용법
    ///   1) Project 창에서 위 에셋을 클릭 → Inspector 에 BGM / 환경음 / 효과음 목록이 보인다.
    ///   2) 원하는 줄의 Clips 칸에 오디오 파일(wav/ogg/mp3)을 끌어다 놓는다. 효과음은 여러 개 넣으면 매번 무작위로 하나 재생.
    ///   3) Volume(0~1) 로 개별 크기, Pitch Jitter 로 재생마다 음높이를 살짝 흔들어 반복감을 줄인다.
    ///   → Play 하면 끝. 코드 수정·재빌드 필요 없음. 비워 둔 칸은 조용히 무시된다.
    /// ■ Key 는 코드가 소리를 찾는 이름이라 바꾸면 안 된다. 설명(Note)은 자유롭게 고쳐도 됨.
    /// ■ 빌더(Build ALL)는 이 에셋을 덮어쓰지 않는다. 없는 Key 만 새 줄로 추가한다 → 넣어 둔 소리는 유지.
    /// </summary>
    [CreateAssetMenu(menuName = "LHS_CATest/Audio Library", fileName = "CATest_AudioLibrary")]
    public sealed class CATestAudioLibrary : ScriptableObject {
        public const string ResourcePath = "CATest_AudioLibrary";

        [Serializable]
        public sealed class Entry {
            public string key;
            [TextArea(1, 2)] public string note;
            public AudioClip[] clips = Array.Empty<AudioClip>();
            [Range(0f, 1.5f)] public float volume = 1f;
            [Range(0f, 0.3f)] public float pitchJitter = 0.04f;

            public AudioClip Pick() {
                if (clips == null || clips.Length == 0) return null;
                var c = clips[UnityEngine.Random.Range(0, clips.Length)];
                if (c != null) return c;
                foreach (var x in clips) if (x != null) return x;
                return null;
            }
        }

        [Header("배경음 (구역마다 자동 전환, 크로스페이드)")]
        public List<Entry> bgm = new();

        [Header("환경음 (바람·물 같은 반복 배경 소리, 배경음과 동시에 재생)")]
        public List<Entry> ambience = new();

        [Header("효과음")]
        public List<Entry> sfx = new();

        [Header("공통")]
        [Tooltip("배경음이 바뀔 때 크로스페이드 시간(초)")]
        public float bgmFade = 1.6f;

        private Dictionary<string, Entry> _bgm, _amb, _sfx;

        public Entry Bgm(string key) => Find(ref _bgm, bgm, key);
        public Entry Ambience(string key) => Find(ref _amb, ambience, key);
        public Entry Sfx(string key) => Find(ref _sfx, sfx, key);

        private static Entry Find(ref Dictionary<string, Entry> cache, List<Entry> list, string key) {
            if (string.IsNullOrEmpty(key)) return null;
            if (cache == null || cache.Count != list.Count) {
                cache = new Dictionary<string, Entry>();
                foreach (var e in list) if (e != null && !string.IsNullOrEmpty(e.key)) cache[e.key] = e;
            }
            return cache.TryGetValue(key, out var r) ? r : null;
        }

        private void OnValidate() { _bgm = _amb = _sfx = null; }

        // ─────────── 기본 목록 (빌더가 없는 칸을 추가할 때 사용) ───────────
        public static readonly (string key, string note)[] DefaultBgm = {
            ("title", "메인 타이틀"),
            ("prologue_room", "프롤로그 — 현실의 방(새벽, 조용함)"),
            ("forest_sunny", "화창한 숲(튜토리얼)"),
            ("forest_lush", "울창한 숲"),
            ("forest_fog", "안개 골짜기"),
            ("forest_dream", "몽환의 숲"),
            ("mountain", "바람의 산"),
            ("deepsea", "심해"),
            ("boss_hunt", "보스 — 심연의 눈이 수색 중"),
            ("boss_chase", "보스 — 분노 추격"),
            ("twilight_canyon", "백색 협곡"),
            ("twilight_sanctum", "백색 성역"),
            ("dragon_nest", "용의 둥지(도착)"),
            ("dragon_encounter", "수호룡 등장~선택"),
            ("ending_a", "엔딩 A 귀환"),
            ("ending_b", "엔딩 B 잔류"),
            ("ending_c", "엔딩 C 도전(배드)"),
        };

        public static readonly (string key, string note)[] DefaultAmbience = {
            ("amb_room", "방 — 시계 초침, 창밖 새소리"),
            ("amb_forest", "숲 — 새소리, 잎 스치는 소리"),
            ("amb_river", "강물 흐르는 소리"),
            ("amb_fog", "안개 — 낮은 바람"),
            ("amb_wind", "산 — 강한 바람"),
            ("amb_underwater", "심해 — 물속 웅웅거림"),
            ("amb_twilight", "황혼 — 멀리서 부는 바람, 모래"),
        };

        public static readonly (string key, string note)[] DefaultSfx = {
            // UI
            ("ui_move", "UI 메뉴 이동(방향키/마우스 오버)"),
            ("ui_select", "UI 확인/클릭"),
            ("ui_back", "UI 닫기/취소"),
            ("ui_toggle", "설정 켜짐/꺼짐 전환"),
            ("ui_pause", "ESC 메뉴 열기"),
            // 플레이어
            ("player_footstep", "발소리(걸을 때 반복)"),
            ("player_jump", "점프"),
            ("player_land", "착지"),
            ("death", "사망"),
            ("save", "세이브 지점 저장"),
            // 프롤로그 / 오프닝
            ("room_wake", "침대에서 일어남(이불 소리)"),
            ("door_open", "방문 여는 소리"),
            ("realm_whoosh", "문 너머로 빨려 들어감"),
            ("crevice_roll", "틈을 굴러 내려옴"),
            ("crevice_land", "숲에 떨어짐"),
            // 숲
            ("lever", "레버 당김"),
            ("gate_open", "문/덩굴문 열림"),
            ("plate_press", "압력판 눌림"),
            ("crumble", "무너지는 발판"),
            ("wall_break", "부서지는 벽"),
            ("collectible", "수집품 획득"),
            ("water_splash", "물에 빠짐"),
            // 심해
            ("clam_close", "조개가 입을 다묾"),
            ("boss_wake", "심연의 눈이 뜸"),
            ("boss_alert", "발각(경고)"),
            ("boss_roar", "분노"),
            ("boss_hit", "굴 입구 들이받기"),
            // 황혼
            ("realm_transition", "심해 → 황혼 차원 이동"),
            ("tile_crack", "금 간 타일 무너짐"),
            ("icicle_fall", "고드름 낙하"),
            ("rune_press", "룬 발판 밟음"),
            ("rune_wrong", "룬 순서 틀림"),
            ("rune_solved", "룬 퍼즐 해결 / 문 열림"),
            ("beam_warn", "심판의 빛 경고"),
            ("beam_fire", "심판의 빛 발사"),
            // 용 / 엔딩
            ("dragon_fly", "용 날갯짓/비행"),
            ("dragon_roar", "용 포효"),
            ("dragon_strike", "용의 일격"),
            ("ending_card", "엔딩 카드 등장"),
        };
    }
}
