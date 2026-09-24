using System;
using System.Threading;
using _02._Script._04_Interaction;
using _02._Script.Boss.BossPatterns;
using _02._Script.Boss.BossZones;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace _02._Script.Boss {
    public class BossRoom : MonoBehaviour
    {
        [SerializeField] private float startDelay = 1f;
        [SerializeField] private BossTrigger trigger;
        [SerializeField] private BossTimeLine timeLine;
        [SerializeField] private Boss boss;
        [SerializeField] private RoomLightCycle lightCycle;
        [SerializeField] private BossZone zones;

        [SerializeField] private Transform light;

        private InteractLight[] lights;
        private CancellationTokenSource roomCts;
        
        [SerializeField] private Light2D globalLight;
        private float _normalGlobalIntensity;

        public InteractLight[] Lights => lights;

        public bool IsStarted { get; private set; }

        private void Awake() {
            lights = light.GetComponentsInChildren<InteractLight>();

            foreach (var roomLight in lights)
                if (roomLight != null)
                    roomLight.TurnOff();
            
            _normalGlobalIntensity = globalLight.intensity;
        }

        private void OnEnable() {
            trigger.OnEnter += Begin;
        }

        private void OnDisable() {
            trigger.OnEnter -= Begin;
            Stop();
        }

        public void Begin() {
            if (IsStarted || !isActiveAndEnabled) return;
            if (boss == null || lightCycle == null) {
                Debug.LogError("BossRoom의 Boss와 Light Cycle을 연결하세요.", this);
                return;
            }

            IsStarted = true;
            boss.gameObject.SetActive(true);
            roomCts = new CancellationTokenSource();
            Run(roomCts.Token).Forget();
        }

        private async UniTask Run(CancellationToken token) {
            try {
                await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: token);
                foreach (var roomLight in lights)
                    roomLight?.TurnOn();
                
                SetLightBrightness(1f);

                var firstCycle = true;
                while (true) {
                    token.ThrowIfCancellationRequested();
                    SetLightBrightness(1f);
                    
                    await lightCycle.Wait(token); // 추가
                    await lightCycle.Dim(this, token);
                    
                    var darkUntil = Time.time + lightCycle.DarkHoldDuration;

                    if (firstCycle && timeLine != null)
                        await timeLine.Descend(token);

                    firstCycle = false;
                    await boss.RunPatterns(token);

                    while (Time.time < darkUntil)
                        await UniTask.NextFrame(token);

                    SetLightBrightness(1f);
                    await UniTask.NextFrame(token);
                }
            }
            catch (OperationCanceledException) { }
            finally {
                roomCts.Dispose();
                roomCts = null;
                SetLightBrightness(1f);
            }
        }

        public void Stop() {
            roomCts?.Cancel();
        }

        public void SetLightBrightness(float ratio) {
            ratio = Mathf.Clamp01(ratio);

            if (globalLight != null)
                globalLight.intensity = _normalGlobalIntensity * ratio;

            foreach (var roomLight in lights) {
                if (roomLight != null)
                    roomLight.SetBrightness(ratio);
            }
        }

        public InteractLight GetClosestLight(Vector2 position) {
            InteractLight closest = null;
            var closestDistance = float.MaxValue;

            foreach (var roomLight in lights) {
                if (roomLight == null || !roomLight.IsActive) continue;

                var distance = ((Vector2)roomLight.transform.position - position).sqrMagnitude;

                if (distance >= closestDistance) continue;

                closestDistance = distance;
                closest = roomLight;
            }

            return closest;
        }
        
        public BossZone GetZone(Vector2 position) => zones;
    }
}
