namespace _02._Script._01_Players.Interface {
    public interface ICheckClimbWall {
        bool IsClimbed { get; }

        // 지금 붙어 있는 등반 면(없으면 null). 양면 덩굴(ClimbSurface.twoSided) 판별에 사용.
        UnityEngine.Collider2D CurrentSurface { get; }
    }
}