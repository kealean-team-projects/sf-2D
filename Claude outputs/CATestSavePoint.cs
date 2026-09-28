using _02._Script._01_Players;
using _02._Script._05_Managers;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 세이브 포인트. 플레이어가 영역 안에서 "땅을 딛고 서 있는 순간" 저장한다.
    ///
    /// 저장 원리 (기존 시스템 재사용)
    ///  SaveManager.RequestCapture() → PlayerProgress.CaptureProgress() 가 호출되어
    ///  현재 플레이어 위치/스태미나가 부활 데이터(_respawnData)와 Save.sf2d 파일에 기록된다.
    ///  공중에서 저장되면 부활 위치가 허공이 되므로 IsGrounded 일 때만 저장한다.
    ///
    /// 기존 CheckPointManager는 CoreScene에서 맵 씬의 Transform 배열을 참조해야 해서
    /// (씬 간 직렬화 참조 불가) 스트리밍 구조와 맞지 않아 사용하지 않았다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class CATestSavePoint : MonoBehaviour {
        [SerializeField] private Light2D glow;
        [SerializeField] private float idleIntensity = 0.35f;
        [SerializeField] private float activeIntensity = 1.4f;
        [SerializeField] private Color idleColor = new(0.55f, 0.6f, 0.7f);
        [SerializeField] private Color activeColor = new(1f, 0.78f, 0.45f);
        [SerializeField] private SpriteRenderer[] tintTargets;
        [SerializeField] private ParticleSystem activateBurst;
        [SerializeField] private ParticleSystem loopParticles;
        [SerializeField] private Transform bobTarget;
        [Tooltip("타이틀 [이어하기]에 표시될 장소 이름. 비우면 마지막으로 표시된 지역 이름을 쓴다.")]
        [SerializeField] private string placeName;

        private bool _activated;
        private float _pulse;
        private Vector3 _bobBase;

        public static Vector2? LastSavePosition { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => LastSavePosition = null;

        private void Awake() {
            GetComponent<BoxCollider2D>().isTrigger = true;
            if (bobTarget != null) _bobBase = bobTarget.localPosition;
            Apply(0f);
            if (loopParticles != null) loopParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnTriggerStay2D(Collider2D other) {
            if (_activated) return;
            var player = other.GetComponentInParent<Player>();
            if (player == null || player.IsDead || !player.IsGrounded) return;
            Activate(player);
        }

        private void Activate(Player player) {
            _activated = true;
            LastSavePosition = player.transform.position;
            if (SaveManager.Instance != null) CATestSave.SaveHere(placeName); // 원본 저장 + 장소 이름(타이틀 이어하기 표시용)
            else Debug.LogWarning("[CATestSavePoint] SaveManager가 없어 저장하지 못했습니다.", this);
            if (activateBurst != null) activateBurst.Play(true);
            if (loopParticles != null) loopParticles.Play(true);
            _pulse = 1f;
        }

        private void Update() {
            var t = _activated ? 1f : 0f;
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 0.8f);
            Apply(t);
            if (bobTarget != null)
                bobTarget.localPosition = _bobBase + Vector3.up * (Mathf.Sin(Time.time * 1.6f) * 0.08f);
        }

        private void Apply(float t) {
            var flicker = 1f + Mathf.Sin(Time.time * 3.1f) * 0.05f + Mathf.Sin(Time.time * 7.3f) * 0.03f;
            if (glow != null) {
                glow.intensity = Mathf.Lerp(idleIntensity, activeIntensity, t) * flicker + _pulse * 2f;
                glow.color = Color.Lerp(idleColor, activeColor, t);
            }
            if (tintTargets == null) return;
            foreach (var sr in tintTargets)
                if (sr != null) sr.color = Color.Lerp(idleColor, Color.white, 0.4f + 0.6f * t);
        }
    }
}
