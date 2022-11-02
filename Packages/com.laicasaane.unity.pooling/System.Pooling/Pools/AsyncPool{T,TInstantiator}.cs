using System.Pooling.Statistics;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace System.Pooling
{
    public class AsyncPool<T, TInstantiator> : IAsyncPool<T>, IShareable, IDisposable
        where T : class
        where TInstantiator : IAsyncInstantiable<T>
    {
        private readonly TInstantiator _instantiator;
        private readonly UniqueQueue<T> _queue;

        public AsyncPool()
            : this(new UniqueQueue<T>())
        {
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public AsyncPool(UniqueQueue<T> queue)
        {
            _instantiator = new ActivatorInstantiator<TInstantiator>().Instantiate();
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public AsyncPool(TInstantiator instantiator)
            : this(instantiator, new UniqueQueue<T>())
        {
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public AsyncPool(TInstantiator instantiator, UniqueQueue<T> queue)
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

        public async UniTask<T> Rent()
        {
            if (_queue.TryDequeue(out var instance))
            {
                PoolTracker.TrackPoolRentOrCreate(this, instance);
                return instance;
            }

            var newInstance = await _instantiator.Instantiate();
            PoolTracker.TrackPoolRentOrCreate(this, newInstance);
            return newInstance;
        }

        public async UniTask<T> Rent(CancellationToken cancelToken)
        {
            if (_queue.TryDequeue(out var instance))
                return instance;

            var newInstance = await _instantiator.Instantiate(cancelToken);
            PoolTracker.TrackPoolRentOrCreate(this, newInstance);
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
