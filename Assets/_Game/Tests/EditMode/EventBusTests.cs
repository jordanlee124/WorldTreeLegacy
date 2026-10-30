using System;
using System.Collections.Generic;
using NUnit.Framework;
using Worldtree.Application.Events;

namespace Worldtree.Tests
{
    public class EventBusTests
    {
        readonly struct Ping { public readonly int N; public Ping(int n) { N = n; } }
        readonly struct Pong { }

        [Test]
        public void Publish_ReachesSubscribers_InOrder()
        {
            var bus = new EventBus();
            var seen = new List<string>();
            bus.Subscribe<Ping>(p => seen.Add("a" + p.N));
            bus.Subscribe<Ping>(p => seen.Add("b" + p.N));
            bus.Publish(new Ping(1));
            CollectionAssert.AreEqual(new[] { "a1", "b1" }, seen);
        }

        [Test]
        public void Publish_OnlyReachesThatEventType()
        {
            var bus = new EventBus();
            var pings = 0;
            bus.Subscribe<Ping>(_ => pings++);
            bus.Publish(new Pong());
            Assert.AreEqual(0, pings);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNothing() => Assert.DoesNotThrow(() => new EventBus().Publish(new Ping(1)));

        [Test]
        public void Dispose_Unsubscribes_AndIsSafeTwice()
        {
            var bus = new EventBus();
            var pings = 0;
            var sub = bus.Subscribe<Ping>(_ => pings++);
            bus.Publish(new Ping(1));
            sub.Dispose();
            sub.Dispose();
            bus.Publish(new Ping(2));
            Assert.AreEqual(1, pings);
        }

        [Test]
        public void UnsubscribingDuringPublish_StillDeliversToTheRest()
        {
            var bus = new EventBus();
            var later = 0;
            IDisposable first = null;
            first = bus.Subscribe<Ping>(_ => first.Dispose());
            bus.Subscribe<Ping>(_ => later++);
            bus.Publish(new Ping(1));
            bus.Publish(new Ping(2));
            Assert.AreEqual(2, later);
        }

        [Test]
        public void Subscribe_RejectsNull() => Assert.Throws<ArgumentNullException>(() => new EventBus().Subscribe<Ping>(null));
    }
}
