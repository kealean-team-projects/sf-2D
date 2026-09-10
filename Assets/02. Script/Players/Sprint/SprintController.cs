using _02._Script.Players.Interface;

namespace _02._Script.Players.Sprint {
    public sealed class SprintController {
        private readonly float _staminaCostPerSecond;
        private readonly IStats _stats;

        public SprintController(IStats stats, float staminaCostPerSecond) {
            _stats = stats;
            _staminaCostPerSecond = staminaCostPerSecond;
        }

        public bool IsSprinting { get; private set; }

        public float MoveSpeedMultiplier => IsSprinting ? 2f : 1f;

        public void StartSprint() {
            IsSprinting = true;
        }

        public void StopSprint() {
            IsSprinting = false;
        }

        public void Tick(bool isMoving) {
            if (!IsSprinting)
                return;

            if (isMoving)
                _stats.UseStamina(_staminaCostPerSecond, false);

            if (_stats.Stamina <= 0f)
                StopSprint();
        }
    }
}