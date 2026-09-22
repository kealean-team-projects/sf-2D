using System;
using System.Threading;
using _02._Script._04_Interaction;
using _02._Script.Boss.BossPatterns;
using _02._Script.Boss.BossZones;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _02._Script.Boss {
    public class BossRoom : MonoBehaviour {
        [SerializeField] private BossTrigger trigger;
        [SerializeField] private BossTimeLine timeLine;
        [SerializeField] private Boss boss;
        [SerializeField] private RoomLightCycle lightCycle;
        [SerializeField] private BossZone[] zones;

        [SerializeField] private Transform light;


        private InteractLight[] lights;
        private CancellationTokenSource roomCts;

        public bool IsStarted { get; private set; }

        private void Awake() {
            lights = light.GetComponentsInChildren<InteractLight>();

            foreach (var roomLight in lights)
                if (roomLight != null)
                    roomLight.TurnOff();
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
                foreach (var roomLight in lights)
                    if (roomLight != null)
                        roomLight.TurnOn();
                SetLightBrightness(1f);

                if (timeLine != null)
                    await timeLine.Play(token);

                while (true) {
                    token.ThrowIfCancellationRequested();
                    SetLightBrightness(1f);
                    await lightCycle.Dim(this, token);
                    await boss.RunPatterns(token);
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
            foreach (var roomLight in lights)
                if (roomLight != null)
                    roomLight.SetBrightness(ratio);
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

        public BossZone GetZone(Vector2 position) {
            foreach (var zone in zones)
                if (zone.area.OverlapPoint(position))
                    return zone;

            return null;
        }
    }
}