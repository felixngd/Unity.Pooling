using System;
using System.Pooling;
using System.Pooling.Statistics;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Unity.Pooling
{
    [Serializable]
    public class UnityPool<T, TPrefab>
        : IUnityPool<T, TPrefab>, IShareable, IDisposable
        where T : UnityEngine.Object
        where TPrefab : IPrefab<T>
    {
        private readonly UniqueQueue<int, T> _queue;

        [SerializeField] private TPrefab _prefab;

        public UnityPool()
        {
            _queue = new UniqueQueue<int, T>();
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public UnityPool(TPrefab prefab)
        {
            _queue = new UniqueQueue<int, T>();
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public UnityPool(UniqueQueue<int, T> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public UnityPool(UniqueQueue<int, T> queue, TPrefab prefab)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            PoolTracker.TrackPoolCreation(this, 2);
        }

        public TPrefab Prefab
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _prefab;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _prefab = value;
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
                if (_queue.TryDequeue(out var _, out var instance))
                {
                    if (onReleased != null)
                        onReleased(instance);
                    else if (_prefab != null)
                        _prefab.Release(instance);
                }

                countRemove--;
            }
            
            PoolTracker.TrackPoolReturnOrRelease(this, countRemove);
        }

        public async UniTask<T> Rent()
        {
            if (_queue.TryDequeue(out var _, out var instance))
            {
                PoolTracker.TrackPoolRentOrCreate(this, instance);
                return instance;
            }

            var newInstance = await _prefab.Instantiate();
            PoolTracker.TrackPoolRentOrCreate(this, newInstance);
            return newInstance;
        }

        public async UniTask<T> Rent(CancellationToken cancelToken)
        {
            if (_queue.TryDequeue(out var _, out var instance))
            {
                PoolTracker.TrackPoolRentOrCreate(this, instance);
                return instance;
            }

            var newInstance = await _prefab.Instantiate(cancelToken);
            PoolTracker.TrackPoolRentOrCreate(this, instance);
            return newInstance;
        }

        public void Return(T instance)
        {
            if (instance == false)
                return;

            ReturnPreprocess(instance);
            _queue.TryEnqueue(instance.GetInstanceID(), instance);
            PoolTracker.TrackPoolReturnOrRelease(this, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void ReturnPreprocess(T instance) { }
    }
}