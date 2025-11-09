using System;
using System.Collections.Generic;

namespace Expo.Core
{
    /// <summary>
    /// Global lightweight publish/subscribe event bus for Expo.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _subscribers = new();

        /// <summary>
        /// Register a listener for a specific event type.
        /// </summary>
        public static void Subscribe<T>(Action<T> listener)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var existing))
                _subscribers[type] = Delegate.Combine(existing, listener);
            else
                _subscribers[type] = listener;
        }

        /// <summary>
        /// Remove a previously registered listener.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> listener)
        {
            var type = typeof(T);
            if (_subscribers.TryGetValue(type, out var existing))
            {
                var updated = Delegate.Remove(existing, listener);
                if (updated == null)
                    _subscribers.Remove(type);
                else
                    _subscribers[type] = updated;
            }
        }

        /// <summary>
        /// Broadcast an event instance to all listeners of its type.
        /// </summary>
        public static void Publish<T>(T e)
        {
            if (_subscribers.TryGetValue(typeof(T), out var del))
                (del as Action<T>)?.Invoke(e);
        }

        /// <summary>
        /// Remove all registered listeners.  
        /// Call on scene unload or restart.
        /// </summary>
        public static void Clear() => _subscribers.Clear();
    }
}
