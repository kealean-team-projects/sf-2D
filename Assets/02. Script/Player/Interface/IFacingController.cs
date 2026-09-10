namespace _02._Script.Player.Interface {
    public interface IFacingController {
        bool IsFacingLeft { get; }

        void UpdateFacing(float xMove);
    }
}