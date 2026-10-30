using System;
using NUnit.Framework;
using Worldtree.Domain.Camera;

namespace Worldtree.Tests
{
    public class ViewBoundsTests
    {
        [Test]
        public void InsideBounds_IsUnchanged() => Assert.AreEqual(5f, ViewBounds.ClampAxis(5f, 2f, 0f, 20f));

        [Test]
        public void PastMin_StopsWithViewEdgeOnBound() => Assert.AreEqual(2f, ViewBounds.ClampAxis(-10f, 2f, 0f, 20f));

        [Test]
        public void PastMax_StopsWithViewEdgeOnBound() => Assert.AreEqual(18f, ViewBounds.ClampAxis(99f, 2f, 0f, 20f));

        [Test]
        public void ViewLargerThanBounds_CentresOnBounds() => Assert.AreEqual(5f, ViewBounds.ClampAxis(-3f, 8f, 0f, 10f));

        [Test]
        public void ViewExactlyBoundsSize_CentresOnBounds() => Assert.AreEqual(5f, ViewBounds.ClampAxis(9f, 5f, 0f, 10f));

        [Test]
        public void InvertedBounds_Throw() => Assert.Throws<ArgumentException>(() => ViewBounds.ClampAxis(0f, 1f, 5f, 0f));
    }
}
