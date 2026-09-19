namespace _02._Script._01_Players.Interface {
    public interface IFacingController {
        bool IsFacingLeft { get; }

        void UpdateFacing(float xMove);
    }
}