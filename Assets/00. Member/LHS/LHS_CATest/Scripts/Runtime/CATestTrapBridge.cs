using System.Reflection;
using _02._Script._03_TrapAndEnemy.Enemies;
using _02._Script._03_TrapAndEnemy.Traps;
using _02._Script._05_Managers;
using ClamTrapArt;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LHS_CATest {
    /// <summary>
    /// 기존 함정/적 프리팹(원본 수정 없음)에 비주얼과 재시도 처리를 덧붙이는 연결 스크립트.
    ///  - Wisp : 한 번 발동하면 _isWorked=true 로 영원히 멈추므로, 부활 시 리셋 + 유령 크기 0으로 복구.
    ///  - Clamp : ClamTrap_AI 조개 애니메이션을 닫고, 부활 시 다시 연다.
    ///  - WhaleEye(WhileEye) : 상태(Closed/Opening/Open)를 읽어 눈꺼풀·빛으로 보여준다. 원본은 Debug.Log만 한다.
    /// </summary>
    public sealed class CATestTrapBridge : MonoBehaviour {
        [Header("Wisp")]
        [SerializeField] private Wisp wisp;
        [SerializeField] private Transform wispGhost;

        [Header("Clamp")]
        [SerializeField] private ClamTrapPlayer clamArt;

        [Header("WhaleEye")]
        [SerializeField] private WhaleEye whaleEye;
        [SerializeField] private Transform eyeLid;
        [SerializeField] private Light2D eyeLight;
        [SerializeField] private SpriteRenderer eyeIris;

        private static readonly FieldInfo WispWorked = typeof(Wisp).GetField("_isWorked", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo EyeState = typeof(WhaleEye).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo EyeTimer = typeof(WhaleEye).GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo EyeOpening = typeof(WhaleEye).GetField("eyeOpeningTime", BindingFlags.NonPublic | BindingFlags.Instance);

        private float _lid;

        private void OnEnable() => GameManager.OnRespawnReset += OnRespawn;
        private void OnDisable() => GameManager.OnRespawnReset -= OnRespawn;

        private UniTask OnRespawn() {
            if (wisp != null && WispWorked != null) WispWorked.SetValue(wisp, false);
            if (wispGhost != null) wispGhost.localScale = Vector3.zero;
            if (clamArt != null) clamArt.Reopen();
            return UniTask.CompletedTask;
        }

        private void Update() {
            if (whaleEye == null || EyeState == null) return;
            // WhaleEye.EyeState: 0 Closed, 1 Opening, 2 Open
            var state = System.Convert.ToInt32(EyeState.GetValue(whaleEye));
            var timer = EyeTimer != null ? (float)EyeTimer.GetValue(whaleEye) : 0f;
            var openingTime = EyeOpening != null ? (float)EyeOpening.GetValue(whaleEye) : 1f;
            var target = state switch {
                2 => 1f,
                1 => Mathf.Clamp01(timer / Mathf.Max(0.01f, openingTime)) * 0.35f,
                _ => 0f
            };
            _lid = Mathf.MoveTowards(_lid, target, Time.deltaTime * (state == 2 ? 6f : 2f));
            if (eyeLid != null) eyeLid.localScale = new Vector3(1f, Mathf.Max(0.02f, _lid), 1f);
            if (eyeLight != null) {
                eyeLight.intensity = _lid * (state == 2 ? 1.8f : 0.6f);
                eyeLight.color = state == 2 ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.8f, 0.4f);
            }
            if (eyeIris != null) {
                var c = eyeIris.color;
                c.a = _lid;
                eyeIris.color = c;
            }
        }
    }
}
