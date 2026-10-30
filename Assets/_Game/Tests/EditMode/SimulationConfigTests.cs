using NUnit.Framework;
using Worldtree.Domain;

namespace Worldtree.Tests
{
    public class SimulationConfigTests
    {
        [Test]
        public void FixedStep_Is60Hz() => Assert.AreEqual(1f / 60f, SimulationConfig.FixedStepSeconds, 1e-6f);
    }
}
