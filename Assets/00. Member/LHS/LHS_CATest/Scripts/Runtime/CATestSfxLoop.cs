using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 반복 효과음(바람 기둥, 물살, 해파리 전기 소리 등). 물체에 붙어 있고, 플레이어가 가까울수록 크게 들린다.
    /// 소리 파일은 CATestAudioLibrary 효과음 칸의 key 에 넣는다(첫 번째 파일을 반복 재생).
    /// CATestAudioHooks 가 맵이 로드될 때 원본 함정(WaterCurrent, ElectronicEnemy)에 자동으로 붙인다 → 씬 파일 수정 없음.
    /// </summary>
    public sealed class CATestSfxLoop : MonoBehaviour {
        public string key;
        [Tooltip("이 거리 안에서 들림(월드 유닛). 가까울수록 크게")]
        public float range = 18f;
        private AudioSource _src;
        private CATestAudioLibrary.Entry _e;

        private void Start() {
            var lib = CATestAudio.Library;
            _e = lib != null ? lib.Sfx(key) : null;
            var clip = _e?.Pick();
            if (clip == null) { enabled = false; return; }
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = clip;
            _src.loop = true;
            _src.playOnAwake = false;
            _src.spatialBlend = 0f;
            _src.volume = 0f;
            _src.time = Random.Range(0f, clip.length); // 여러 개가 같은 박자로 겹치지 않게
            _src.Play();
        }

        private void Update() {
            if (_src == null) return;
            var p = CATestHUD.PlayerTransform;
            var k = 0f;
            if (p != null) {
                var d = Vector2.Distance(p.position, transform.position);
                k = Mathf.Clamp01(1f - d / range);
                k *= k;
                _src.panStereo = Mathf.Clamp((transform.position.x - p.position.x) / range, -1f, 1f) * 0.6f;
            }
            _src.volume = Mathf.MoveTowards(_src.volume, k * _e.volume * CATestSettings.SfxVolume, Time.unscaledDeltaTime * 2f);
        }
    }
}
