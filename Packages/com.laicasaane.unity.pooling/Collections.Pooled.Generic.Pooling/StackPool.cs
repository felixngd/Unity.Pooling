﻿using System;
using System.Buffers;
using System.Pooling;
using System.Runtime.CompilerServices;

namespace Collections.Pooled.Generic.Pooling
{
    public sealed class StackPool<T> : PoolBase<Stack<T>>
    {
        public static readonly StackPool<T> Shared = new StackPool<T>();

        public StackPool()
            : base(Instantiate, ArrayPool<Stack<T>>.Shared)
        { }

        public StackPool(Func<Stack<T>> instantiate)
            : base(instantiate, ArrayPool<Stack<T>>.Shared)
        { }

        public StackPool(ArrayPool<Stack<T>> pool)
            : base(Instantiate, pool)
        { }

        public StackPool(Func<Stack<T>> instantiate, ArrayPool<Stack<T>> pool)
            : base(instantiate, pool)
        { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Func<Stack<T>> GetInstantiator()
            => Instantiate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void ReturnPreprocess(Stack<T> instance)
            => instance.Clear();

        private static Stack<T> Instantiate()
            => new Stack<T>();
    }
}