﻿using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Unity.Pooling
{
    public readonly struct AsyncDisposableContext<TKey, T>
        where T : class
    {
        internal readonly IAsyncPool<TKey, T> _pool;

        internal AsyncDisposableContext(IAsyncPool<TKey, T> pool)
        {
            _pool = pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async UniTask<Disposable<TKey, T>> RentAsync(TKey key)
        {
            var result = await _pool.RentAsync(key);
            return new Disposable<TKey, T>(_pool, key, result);
        }
    }
}