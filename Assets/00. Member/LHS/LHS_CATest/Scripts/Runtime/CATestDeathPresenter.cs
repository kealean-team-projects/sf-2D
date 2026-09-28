using System.Collections.Generic;
using _02._Script._01_Players;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 사망 연출: "그대로 멈춤 → 플레이어가 점점 투명해짐 → 화면이 검게 → 체크포인트에서 다시 밝아짐".
    ///
    /// 원본 흐름(GameManager.Restart)은 그대로 두고 그 위에 얹는다.
    ///  1) GameManager 가 사망 순간 Time.timeScale = 0 으로 게임을 멈춘다  → "잠깐 그대로 멈춤"
    ///  2) 동시에 EffectManager 가 화면 덮개(fade Image)를 InExpo 곡선으로 불투명하게 만든다.
    ///     InExpo 는 처음엔 거의 안 변하다가 끝에서 빨라지므로, 앞부분 동안 화면은 거의 그대로다.
    ///     CATest CoreScene 빌더에서 이 덮개 색을 빨강 → 검정으로, 시간을 1.5초로 바꿔 두었다.
    ///  3) 이 스크립트는 멈춘 동안(시간 정지라 unscaled 시간 사용) holdTime 만큼 기다린 뒤
    ///     플레이어의 모든 SpriteRenderer 알파를 fadeTime 동안 0 으로 내린다 → "점점 투명해짐"
    ///  4) 화면이 완전히 검어진 뒤 부활 위치로 옮겨지면(위치가 바뀌면) 알파를 원래대로 되돌린다
    ///     → 다시 밝아질 때는 멀쩡한 모습으로 체크포인트에 서 있다.
    /// 추가로 사망 시 컷신 잠금/검은 띠가 남아 있지 않도록 정리한다.
    /// </summary>
    public sealed class CATestDeathPresenter : MonoBehaviour {
        [SerializeField] private float holdTime = 0.35f;
        [SerializeField] private float fadeTime = 0.7f;

        private bool _wasDead;
        private int _version;

        private void Update() {
            var p = CATestHUD.CurrentPlayer;
            if (p == null) return;
            if (p.IsDead && !_wasDead) Run(p).Forget();
            _wasDead = p.IsDead;
        }

        private async UniTaskVoid Run(Player p) {
            var version = ++_version;
            var renderers = p.GetComponentsInChildren<SpriteRenderer>(true);
            var baseAlpha = new List<float>(renderers.Length);
            foreach (var r in renderers) baseAlpha.Add(r != null ? r.color.a : 1f);
            var deathPos = p.transform.position;

            // 컷신 도중 죽었다면 잠금/띠를 푼다(부활 후 조작 불가 방지)
            if (CATestCutscene.IsPlaying) CATestCutscene.ForceUnlock();

            for (var t = 0f; t < holdTime; t += Time.unscaledDeltaTime) {
                if (this == null || version != _version) return;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            for (var t = 0f; t < fadeTime; t += Time.unscaledDeltaTime) {
                if (this == null || version != _version || p == null) return;
                var k = 1f - Mathf.Clamp01(t / fadeTime);
                SetAlpha(renderers, baseAlpha, k * k);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            SetAlpha(renderers, baseAlpha, 0f);

            // 부활 위치로 옮겨지거나(위치 변화), 부활이 끝날 때까지 기다린 뒤 원래 모습으로
            var guard = 0f;
            while (p != null && p.IsDead && (p.transform.position - deathPos).sqrMagnitude < 0.01f && guard < 6f) {
                guard += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            if (p == null) return;
            SetAlpha(renderers, baseAlpha, 1f);
        }


        private static void SetAlpha(SpriteRenderer[] rs, List<float> baseAlpha, float k) {
            for (var i = 0; i < rs.Length; i++) {
                var r = rs[i];
                if (r == null) continue;
                var c = r.color;
                c.a = baseAlpha[i] * k;
                r.color = c;
            }
        }
    }
}
