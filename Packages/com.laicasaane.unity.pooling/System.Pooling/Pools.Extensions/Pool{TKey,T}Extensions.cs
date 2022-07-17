﻿using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Collections.Pooled;

namespace System.Pooling
{
    public static partial class Pool_TKey_T_Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool Rent<TPool, TKey, T>(this TPool pool, TKey key, T[] output)
            where TPool : IRentable<TKey, T>
            where T : class
            => Rent(pool, key, output.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool Rent<TPool, TKey, T>(this TPool pool, TKey key, T[] output, int count)
            where TPool : IRentable<TKey, T>
            where T : class
            => Rent(pool, key, output.AsSpan(), count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool Rent<TPool, TKey, T>(this TPool pool, TKey key, in Span<T> output)
            where TPool : IRentable<TKey, T>
            where T : class
            => Rent(pool, key, output, output.Length);

        public static TPool Rent<TPool, TKey, T>(this TPool pool, TKey key, in Span<T> output, int count)
            where TPool : IRentable<TKey, T>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if ((uint)count > (uint)output.Length)
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();

            for (var i = 0; i < count; i++)
            {
                output[i] = pool.Rent(key);
            }

            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRentable<TKey, T> Rent<TKey, T, TOutput>(this IRentable<TKey, T> pool, TKey key, TOutput output)
            where TOutput : ICollection<T>
            where T : class
            => Rent(pool, key, output, 1);

        public static IRentable<TKey, T> Rent<TKey, T, TOutput>(this IRentable<TKey, T> pool, TKey key, TOutput output, int count)
            where TOutput : ICollection<T>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (output is null)
                throw new ArgumentNullException(nameof(output));

            if (count < 0)
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            for (var i = 0; i < count; i++)
            {
                output.Add(pool.Rent(key));
            }

            return pool;
        }

        public static IRentable<TKey, T> Rent<TKey, T, TKeys, TOutput>(this IRentable<TKey, T> pool, TKeys keys, TOutput output)
            where TKeys : IEnumerable<TKey>
            where TOutput : ICollection<KeyValuePair<TKey, T>>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            if (keys is null)
                throw new ArgumentNullException(nameof(keys));

            if (output is null)
                throw new ArgumentNullException(nameof(output));

            foreach (var key in keys)
            {
                if (key is null)
                    continue;

                try
                {
                    output.Add(new KeyValuePair<TKey, T>(key, pool.Rent(key)));
                }
                catch
                { }
            }

            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRentable<TKey, T> Rent<TKey, T, TOutput>(this IRentable<TKey, T> pool, TKey[] keys, TOutput output)
            where TOutput : ICollection<KeyValuePair<TKey, T>>
            where T : class
            => Rent(pool, (ReadOnlySpan<TKey>)keys, output);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRentable<TKey, T> Rent<TKey, T, TOutput>(this IRentable<TKey, T> pool, in Span<TKey> keys, TOutput output)
            where TOutput : ICollection<KeyValuePair<TKey, T>>
            where T : class
            => Rent(pool, (ReadOnlySpan<TKey>)keys, output);

        public static IRentable<TKey, T> Rent<TKey, T, TOutput>(this IRentable<TKey, T> pool, in ReadOnlySpan<TKey> keys, TOutput output)
            where TOutput : ICollection<KeyValuePair<TKey, T>>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            if (output is null)
                throw new ArgumentNullException(nameof(output));

            for (int i = 0, len = keys.Length; i < len; i++)
            {
                ref readonly TKey key = ref keys[i];

                if (key is null)
                    continue;

                try
                {
                    output.Add(new KeyValuePair<TKey, T>(key, pool.Rent(key)));
                }
                catch
                { }
            }

            return pool;
        }

        public static IReturnable<TKey, T> Return<TKey, T, TInstances>(this IReturnable<TKey, T> pool, TKey key, TInstances instances)
            where TInstances : System.Collections.Generic.IEnumerable<T>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            foreach (var instance in instances)
            {
                pool.Return(key, instance);
            }

            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool Return<TPool, TKey, T>(this TPool pool, TKey key, T[] instances)
            where TPool : IReturnable<TKey, T>
            where T : class
            => Return(pool, key, instances.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool Return<TPool, TKey, T>(this TPool pool, TKey key, in Span<T> instances)
            where TPool : IReturnable<TKey, T>
            where T : class
            => Return(pool, key, (ReadOnlySpan<T>)instances);

        public static TPool Return<TPool, TKey, T>(this TPool pool, TKey key, in ReadOnlySpan<T> instances)
            where TPool : IReturnable<TKey, T>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            for (int i = 0, len = instances.Length; i < len; i++)
            {
                pool.Return(key, instances[i]);
            }

            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool Return<TPool, TKey, T>(this TPool pool, TKey key, in Span<Entry<T>> entries)
            where TPool : IReturnable<TKey, T>
            where T : class
            => Return(pool, key, (ReadOnlySpan<Entry<T>>)entries);

        public static TPool Return<TPool, TKey, T>(this TPool pool, TKey key, in ReadOnlySpan<Entry<T>> entries)
            where TPool : IReturnable<TKey, T>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            for (int i = 0, len = entries.Length; i < len; i++)
            {
                ref readonly Entry<T> entry = ref entries[i];

                if (entry.Next >= -1)
                {
                    pool.Return(key, entry.Value);
                }
            }

            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool Return<TPool, TKey, T>(this TPool pool, TKey key, in Span<Entry<TKey, T>> entries)
            where TPool : IReturnable<TKey, T>
            where T : class
            => Return(pool, key, (ReadOnlySpan<Entry<TKey, T>>)entries);

        public static TPool Return<TPool, TKey, T>(this TPool pool, TKey key, in ReadOnlySpan<Entry<TKey, T>> entries)
            where TPool : IReturnable<TKey, T>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            for (int i = 0, len = entries.Length; i < len; i++)
            {
                ref readonly Entry<TKey, T> entry = ref entries[i];

                if (entry.Next >= -1)
                {
                    pool.Return(key, entry.Value);
                }
            }

            return pool;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool Return<TPool, TKey, T>(this TPool pool, in Span<Entry<TKey, T>> entries)
            where TPool : IReturnable<TKey, T>
            where T : class
            => Return(pool, (ReadOnlySpan<Entry<TKey, T>>)entries);

        public static TPool Return<TPool, TKey, T>(this TPool pool, in ReadOnlySpan<Entry<TKey, T>> entries)
            where TPool : IReturnable<TKey, T>
            where T : class
        {
            if (pool is null)
                throw new ArgumentNullException(nameof(pool));

            for (int i = 0, len = entries.Length; i < len; i++)
            {
                ref readonly Entry<TKey, T> entry = ref entries[i];

                if (entry.Next >= -1)
                {
                    pool.Return(entry.Key, entry.Value);
                }
            }

            return pool;
        }
    }
}