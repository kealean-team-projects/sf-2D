namespace _02._Script.Players.Interface {
    public interface IFacingController {
        bool IsFacingLeft { get; }

        void UpdateFacing(float xMove);
    }
}