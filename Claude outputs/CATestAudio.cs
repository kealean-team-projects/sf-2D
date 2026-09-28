using System.Collections.Generic;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// CATest 소리 재생기 (씬에 놓을 필요 없음 — 처음 쓰일 때 자동 생성, 씬이 바뀌어도 유지).
    ///
    /// ■ 코드에서 쓰는 법
    ///   CATestAudio.PlaySfx("rune_press", 위치)   → 효과음(위치가 화면 왼쪽/오른쪽이면 소리도 좌/우로, 화면 밖으로 멀어지면 작아짐)
    ///   CATestAudio.PlayUi("ui_select")            → UI 효과음(위치 없음)
    ///   CATestAudio.SetOverrideBgm("boss_chase")  → 구역과 상관없이 이 곡을 우선 재생(보스 추격, 엔딩 등). ClearOverrideBgm() 로 해제
    ///   구역 배경음은 CATestBgmZone(맵에 배치된 사각형 영역)이 알아서 바꾼다.
    ///
    /// ■ 소리 파일은 CATestAudioLibrary 에셋의 칸에 끌어다 놓는다(비어 있으면 조용히 무시).
    ///
    /// ■ 구조
    ///   배경음 : AudioSource 2개를 번갈아 쓰며 크로스페이드(한쪽 볼륨↓ 다른 쪽↑)
    ///   환경음 : 배경음과 같은 방식, 별도 2개
    ///   효과음 : AudioSource 16개를 돌려 가며 사용(동시에 여러 소리)
    ///   볼륨   : 최종 = 칸의 Volume × 설정(배경음/효과음) × 전체 볼륨(AudioListener)
    ///
    /// ■ 원본 음악과의 관계
    ///   CoreScene 의 원본 StageAudio(BG_1)는 CATest 배경음 칸에 소리가 있으면 꺼 두고, 비어 있으면 다시 켠다.
    ///   → 아직 아무 곡도 안 넣었을 때는 지금처럼 원본 BGM 이 나온다.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class CATestAudio : MonoBehaviour {
        private const int SfxVoices = 16;

        private static CATestAudio _instance;
        private CATestAudioLibrary _lib;

        private AudioSource[] _bgm = new AudioSource[2];
        private AudioSource[] _amb = new AudioSource[2];
        private float[] _bgmBase = new float[2], _ambBase = new float[2];
        private int _bgmCur, _ambCur;
        private string _bgmKey, _ambKey;
        private readonly AudioSource[] _sfx = new AudioSource[SfxVoices];
        private int _sfxNext;

        private string _zoneBgm, _zoneAmb, _overrideBgm;
        private readonly Dictionary<string, float> _lastPlay = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        private static CATestAudio I {
            get {
                if (_instance != null) return _instance;
                var go = new GameObject("[CATest] Audio");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<CATestAudio>();
                return _instance;
            }
        }

        public static CATestAudioLibrary Library => I._lib;

        private void Awake() {
            _lib = Resources.Load<CATestAudioLibrary>(CATestAudioLibrary.ResourcePath);
            for (var i = 0; i < 2; i++) {
                _bgm[i] = MakeSource("BGM_" + i, true);
                _amb[i] = MakeSource("AMB_" + i, true);
            }
            for (var i = 0; i < SfxVoices; i++) _sfx[i] = MakeSource("SFX_" + i, false);
            CATestBgmZone.CreateDefaults(transform); // 구역별 배경음 영역(씬에 배치하지 않고 여기서 생성)
            CATestSettings.EnsureLoaded();
        }

        private AudioSource MakeSource(string n, bool loop) {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f; // 2D(원근 카메라가 100유닛 떨어져 있어 3D 감쇠를 쓰면 전부 작게 들림) → 좌우 팬만 직접 계산
            s.ignoreListenerPause = true;
            return s;
        }

        // ───────────────────── 효과음 ─────────────────────
        public static void PlaySfx(string key, Vector3 worldPos) => I.PlayInternal(key, worldPos, true);
        public static void PlaySfx(string key) => I.PlayInternal(key, Vector3.zero, false);
        public static void PlayUi(string key) => I.PlayInternal(key, Vector3.zero, false);

        private void PlayInternal(string key, Vector3 pos, bool positional) {
            if (_lib == null) return;
            var e = _lib.Sfx(key);
            var clip = e?.Pick();
            if (clip == null) return;
            // 같은 소리가 한 프레임에 여러 번(예: 여러 물체가 동시에) 겹쳐 터지는 것 방지
            var now = Time.unscaledTime;
            if (_lastPlay.TryGetValue(key, out var last) && now - last < 0.03f) return;
            _lastPlay[key] = now;

            var vol = e.volume * CATestSettings.SfxVolume;
            var pan = 0f;
            if (positional) {
                var cam = Camera.main;
                if (cam != null) {
                    // 화면 절반 폭(월드 단위) 추정: 카메라 거리 × tan(FOV/2) × 화면비
                    var d = Mathf.Max(1f, -cam.transform.position.z + pos.z);
                    var halfH = d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    var halfW = halfH * cam.aspect;
                    var dx = pos.x - cam.transform.position.x;
                    var dy = pos.y - cam.transform.position.y;
                    pan = Mathf.Clamp(dx / Mathf.Max(1f, halfW), -1f, 1f) * 0.6f;
                    // 화면 밖으로 벗어난 만큼 작아짐(화면 폭의 0.6배 더 나가면 0)
                    var outX = Mathf.Max(0f, Mathf.Abs(dx) - halfW) / Mathf.Max(1f, halfW * 0.6f);
                    var outY = Mathf.Max(0f, Mathf.Abs(dy) - halfH) / Mathf.Max(1f, halfH * 0.6f);
                    vol *= Mathf.Clamp01(1f - Mathf.Max(outX, outY));
                }
            }
            if (vol <= 0.001f) return;
            var s = _sfx[_sfxNext];
            _sfxNext = (_sfxNext + 1) % SfxVoices;
            s.clip = clip;
            s.volume = vol;
            s.panStereo = pan;
            s.pitch = 1f + Random.Range(-e.pitchJitter, e.pitchJitter);
            s.Play();
        }

        // ───────────────────── 배경음 / 환경음 ─────────────────────
        /// <summary>구역이 바뀔 때 CATestBgmZone 이 호출. key 가 비어 있으면 그 채널은 그대로 둔다.</summary>
        public static void SetZone(string bgmKey, string ambKey) {
            var a = I;
            if (!string.IsNullOrEmpty(bgmKey)) a._zoneBgm = bgmKey;
            if (ambKey != null) a._zoneAmb = ambKey;
        }

        public static void SetOverrideBgm(string key) => I._overrideBgm = key;
        public static void ClearOverrideBgm() { if (_instance != null) _instance._overrideBgm = null; }

        /// <summary>타이틀 등 구역이 없는 곳에서 바로 곡을 지정.</summary>
        public static void PlayBgmNow(string key, string ambKey = "") {
            var a = I;
            a._overrideBgm = null;
            CATestBgmZone.ResetCurrent();
            a._zoneBgm = key;
            a._zoneAmb = ambKey;
        }

        private void Update() {
            if (_lib == null) return;
            var want = !string.IsNullOrEmpty(_overrideBgm) ? _overrideBgm : _zoneBgm;
            if (want != _bgmKey) Switch(_bgm, _bgmBase, ref _bgmCur, ref _bgmKey, want, _lib.Bgm(want));
            if (_zoneAmb != _ambKey) Switch(_amb, _ambBase, ref _ambCur, ref _ambKey, _zoneAmb, _lib.Ambience(_zoneAmb));

            // 크로스페이드: 현재 채널은 목표 볼륨으로, 다른 채널은 0 으로
            var step = Time.unscaledDeltaTime / Mathf.Max(0.05f, _lib.bgmFade);
            Fade(_bgm, _bgmBase, _bgmCur, CATestSettings.BgmVolume, step);
            Fade(_amb, _ambBase, _ambCur, CATestSettings.SfxVolume, step);
            UpdateStageAudio();
            UpdatePlayerSounds();
        }

        // ───────────────────── 플레이어 발소리 / 점프 / 착지 ─────────────────────
        // 원본 Player 코드를 고치지 않고, 매 프레임 상태(땅에 닿음, 속도)를 읽어서 소리를 낸다.
        private bool _wasGrounded = true;
        private float _airTime, _stepTimer;

        private void UpdatePlayerSounds() {
            var p = CATestHUD.Instance != null ? CATestHUD.CurrentPlayer : null;
            if (p == null || !p.isActiveAndEnabled || p.IsDead) { _wasGrounded = true; _airTime = 0f; return; }
            var rb = p.GetComponent<Rigidbody2D>();
            if (rb == null || !rb.simulated) { _wasGrounded = true; _airTime = 0f; return; }
            var grounded = p.IsGrounded;
            var pos = p.transform.position;
            if (!grounded) _airTime += Time.deltaTime;
            if (grounded && !_wasGrounded && _airTime > 0.22f) {
                PlaySfx("player_land", pos);
                _stepTimer = 0.15f;
            }
            if (!grounded && _wasGrounded && rb.linearVelocityY > 2f) PlaySfx("player_jump", pos);
            if (grounded) {
                _airTime = 0f;
                var vx = Mathf.Abs(rb.linearVelocityX);
                if (vx > 1.2f) {
                    _stepTimer -= Time.deltaTime;
                    if (_stepTimer <= 0f) {
                        PlaySfx("player_footstep", pos);
                        _stepTimer = Mathf.Clamp(0.6f - vx * 0.025f, 0.22f, 0.5f); // 빠를수록 발소리 간격이 짧게
                    }
                }
                else _stepTimer = 0.05f;
            }
            _wasGrounded = grounded;
        }

        private static void Switch(AudioSource[] ch, float[] baseVol, ref int cur, ref string curKey, string key, CATestAudioLibrary.Entry e) {
            curKey = key;
            var clip = e?.Pick();
            if (ch[cur].clip == clip && clip != null) return; // 같은 곡이면 이어서 재생
            cur = 1 - cur;
            ch[cur].clip = clip;
            baseVol[cur] = e != null ? e.volume : 0f;
            ch[cur].volume = 0f;
            if (clip != null) ch[cur].Play();
            else ch[cur].Stop();
        }

        private static void Fade(AudioSource[] ch, float[] baseVol, int cur, float setting, float step) {
            for (var i = 0; i < 2; i++) {
                var target = i == cur && ch[i].clip != null ? baseVol[i] * setting : 0f;
                ch[i].volume = Mathf.MoveTowards(ch[i].volume, target, step * Mathf.Max(0.2f, baseVol[i]));
                if (i != cur && ch[i].isPlaying && ch[i].volume <= 0.001f) ch[i].Stop();
            }
        }

        /// <summary>CATest 배경음이 실제로 나오고 있으면 원본 StageAudio 를 끄고, 아니면 켠다.</summary>
        private void UpdateStageAudio() {
            if (_stage == null) {
                if (Time.unscaledTime < _nextStageSearch) return;
                _nextStageSearch = Time.unscaledTime + 1f;
                _stage = FindAnyObjectByType<_02._Script._05_Managers.StageAudio>(FindObjectsInactive.Include);
                if (_stage == null) return;
            }
            var ours = _bgm[_bgmCur].clip != null;
            if (_stage.enabled == ours) _stage.enabled = !ours;
        }
        private _02._Script._05_Managers.StageAudio _stage;
        private float _nextStageSearch;
    }
}
