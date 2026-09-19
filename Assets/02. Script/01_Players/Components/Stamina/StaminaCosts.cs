using UnityEngine;

[System.Serializable]
public class StaminaCosts {
    [Min(0f)] public float runPerSecond;
    [Min(0f)] public float climbPerSecond;
    [Min(0f)] public float wallDash;
    [Min(0f)] public float wallJump;
}