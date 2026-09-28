namespace LHS_CATest {
    /// <summary>문/다리 같은 장치를 움직이는 "신호" (압력판, 레버 등).</summary>
    public interface ICATestSignal {
        bool IsOn { get; }
    }
}
