using System;
using UnityEngine;

namespace _02._Script.Boss.BossZones {
    [Serializable]
    public class BossZone {
        public BoxCollider2D area;
        public Transform spawnPoint;
        public Transform scanPoint;
    }
}