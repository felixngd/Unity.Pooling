﻿using System;
using System.Buffers;
using Collections.Pooled.Generic;

namespace Unity.Pooling
{
    public abstract class PoolBase<T> : IPool<T>, IDisposable
        where T : class
    {
        private readonly Func<T> _instantiate;
        private readonly Queue<T> _queue;

        public PoolBase()
            : this(Instantiator.Instantiate, ArrayPool<T>.Shared)
        { }

        public PoolBase(Func<T> instantiate)
            : this(instantiate, ArrayPool<T>.Shared)
        { }

        public PoolBase(ArrayPool<T> pool)
            : this(Instantiator.Instantiate, pool)
        { }

        public PoolBase(Func<T> instantiate, ArrayPool<T> pool)
        {
            _instantiate = instantiate ?? Instantiator.Instantiate;
            _queue = new Queue<T>(pool ?? ArrayPool<T>.Shared);
        }

        public int Count() => _queue.Count;

        public void Dispose()
        {
            _queue.Dispose();
        }

        /// <inheritdoc/>
        public void ReleaseInstances(int keep, Action<T> onReleased = null)
        {
            var countRemove = _queue.Count - keep;

            while (countRemove > 0)
            {
                var instance = _queue.Dequeue();
                onReleased?.Invoke(instance);
                countRemove--;
            }
        }

        public T Rent()
        {
            if (_queue.Count > 0)
                return _queue.Dequeue();

            return _instantiate();
        }

        public void Return(T instance)
        {
            if (instance is null)
                return;

            ReturnPreprocess(instance);
            _queue.Enqueue(instance);
        }

        public void Return<TEnumerable>(TEnumerable instances)
            where TEnumerable : System.Collections.Generic.IEnumerable<T>
        {
            foreach (var instance in instances)
            {
                Return(instance);
            }
        }

        protected virtual void ReturnPreprocess(T instance) { }

        protected abstract Func<T> GetDefaultInstantiator();

        protected static class Instantiator
        {
            private static readonly Type s_type = typeof(T);

            public static T Instantiate() => (T)Activator.CreateInstance(s_type);
        }
    }
}
