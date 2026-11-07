namespace Cells.Physiology
{
    /// <summary>
    /// Two-state timed controller for run/tumble. Tumble suppresses commanded
    /// propulsion; steering toward the sampled new heading remains a separate
    /// policy approximation, not a resolved bundle-unbundling model.
    /// </summary>
    public sealed class TumbleMotorState
    {
        public float tumbleDurationSeconds = .22f;
        public float tumbleMotorMultiplier = .12f;
        public float RemainingSeconds { get; private set; }
        public bool IsTumbling => RemainingSeconds > 0f;
        public float MotorMultiplier => IsTumbling ? Clamp01(tumbleMotorMultiplier) : 1f;
        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

        public void Begin()
        {
            RemainingSeconds = tumbleDurationSeconds > 0f ? tumbleDurationSeconds : 0f;
        }

        public void Advance(float dt)
        {
            if (dt > 0f && float.IsFinite(dt))
                RemainingSeconds = RemainingSeconds > dt ? RemainingSeconds - dt : 0f;
        }

        public void Reset() { RemainingSeconds = 0f; }
    }
}
