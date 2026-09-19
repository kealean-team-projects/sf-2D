using System;
using UnityEngine;

namespace _02._Script._05_Managers
{
    public sealed class CheckPointManager : MonoBehaviour {
        [SerializeField] private Transform savePos;
        [SerializeField] private Transform[] checkPoints = Array.Empty<Transform>();
        public static CheckPointManager Instance { get; private set; }

        // 아직 체크포인트를 저장하지 않은 상태
        public int SaveNum { get; private set; } = -1;

        public int CheckPointCount => checkPoints.Length;
        public int MaxSaveNum => checkPoints.Length - 1;

        private void Awake() {
            if (Instance != null && Instance != this) {
                // 이 오브젝트가 매니저 전용 오브젝트일 때
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy() {
            if (Instance == this) Instance = null;
        }

        public bool SaveCheckpoint(int index) {
            var checkPoint = checkPoints[index];

            SaveNum = index;
            savePos.position = checkPoint.position;

            return true;
        }

        public bool SaveNextCheckpoint() {
            return SaveCheckpoint(SaveNum + 1);
        }
    }
}