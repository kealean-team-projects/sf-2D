using UnityEngine;

namespace LHS_CATest {
    /// <summary>
    /// 실행 코드와 빌더가 같이 쓰는 월드 좌표 상수(프롤로그 방 위치 등).
    ///
    /// ■ 프롤로그 방: 화창한 숲(-1965~)에서 멀리 떨어진 x -2402 부터.
    ///   방 그림은 "설계 좌표"(폭 30, 천장 9.6)로 배치한 뒤 RoomScale 배 줄여서 놓는다.
    ///   → 플레이어 키(약 2.5 유닛)에 맞춰 문 높이 ≈ 3.4, 천장 ≈ 6.2 가 되도록 0.65.
    ///   방 크기를 바꾸려면 RoomScale 만 바꾸고 "7. Rebuild Prologue only" 를 실행하면 된다.
    /// </summary>
    public static class CATestWorld {
        public const float RoomScale = 0.65f;
        public const float RoomX0 = -2402f;
        public const float RoomX1 = RoomX0 + 30f * RoomScale;       // -2382.5
        public const float RoomTop = 9.6f * RoomScale;              // 6.24
        public const float RoomDoorX = RoomX0 + 26f * RoomScale;    // -2385.1
        public static readonly Vector2 PrologueStart = new(RoomX0 + 11.4f * RoomScale, 1.6f);
        public static readonly Rect PrologueBounds = new(RoomX0 - 8f, -10f, RoomX1 - RoomX0 + 16f, 30f);

        /// <summary>방 설계 좌표 → 월드 좌표</summary>
        public static float RoomX(float designX) => RoomX0 + (designX - RoomX0) * RoomScale;
        public static float RoomY(float designY) => designY * RoomScale;
    }
}
