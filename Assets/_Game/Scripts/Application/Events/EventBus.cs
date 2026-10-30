using System;
using System.Collections.Generic;

namespace Worldtree.Application.Events
{
    /// <summary>
    /// Synchronous, single-threaded publish/subscribe keyed by event type. Handlers run in subscription order;
    /// a handler's exception propagates to the publisher. Events are plain readonly structs so they stay serializable.
    /// </summary>
    public sealed class EventBus
    {
        readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>Dispose the returned handle to unsubscribe.</summary>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (!handlers.TryGetValue(typeof(T), out var list)) handlers[typeof(T)] = list = new List<Delegate>();
            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        public void Publish<T>(T evt)
        {
            if (!handlers.TryGetValue(typeof(T), out var list)) return;
            // Copy: a handler may unsubscribe (or subscribe) while this publish is running.
            foreach (var handler in list.ToArray()) ((Action<T>)handler)(evt);
        }

        sealed class Subscription : IDisposable
        {
            Action unsubscribe;
            public Subscription(Action unsubscribe) { this.unsubscribe = unsubscribe; }

            public void Dispose()
            {
                unsubscribe?.Invoke();
                unsubscribe = null;
            }
        }
    }
}
