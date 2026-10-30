namespace Worldtree.Domain
{
    /// <summary>Fixed simulation step from ARCHITECTURE.md: 60 Hz, independent of render rate.</summary>
    public static class SimulationConfig
    {
        public const int FixedStepHz = 60;
        public const float FixedStepSeconds = 1f / FixedStepHz;
    }
}
