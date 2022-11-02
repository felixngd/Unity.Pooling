using System.Pooling.Statistics;
using System.Runtime.CompilerServices;

namespace System.Pooling
{
    public class Pool<T, TInstantiator> : IPool<T>, IShareable, IDisposable
        where T : class
        where TInstantiator : IInstantiable<T>
    {
        private readonly TInstantiator _instantiator;
        private readonly UniqueQueue<T> _queue;

        public Pool()
            : this(new UniqueQueue<T>())
        {
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public Pool(UniqueQueue<T> queue)
        {
            _instantiator = new ActivatorInstantiator<TInstantiator>().Instantiate();
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public Pool(TInstantiator instantiator)
            : this(instantiator, new UniqueQueue<T>())
        {
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public Pool(TInstantiator instantiator, UniqueQueue<T> queue)
        {
            _instantiator = instantiator ?? throw new ArgumentNullException(nameof(instantiator));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public int Count => _queue.Count;

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
                if (_queue.TryDequeue(out var instance))
                    onReleased?.Invoke(instance);

                countRemove--;
            }
            
            PoolTracker.TrackPoolReturnOrRelease(this, countRemove);
        }

        public T Rent()
        {
            if (_queue.TryDequeue(out var instance))
            {
                PoolTracker.TrackPoolRentOrCreate(this, instance);
                return instance;
            }

            var newInstance = _instantiator.Instantiate();
            PoolTracker.TrackPoolRentOrCreate(this, instance);
            return newInstance;
        }

        public void Return(T instance)
        {
            if (instance is null)
                return;

            ReturnPreprocess(instance);
            _queue.TryEnqueue(instance);
            PoolTracker.TrackPoolReturnOrRelease(this, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void ReturnPreprocess(T instance) { }
    }
}


