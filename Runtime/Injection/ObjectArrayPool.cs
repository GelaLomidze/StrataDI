using System;
using System.Collections.Generic;

namespace StrataDI
{
    internal sealed class ObjectArrayPool
    {
        private readonly Dictionary<int, Stack<object[]>> _pools = new();

        public object[] Rent(int length)
        {
            if (length == 0)
            {
                return Array.Empty<object>();
            }

            if (_pools.TryGetValue(
                    length,
                    out Stack<object[]> pool) &&
                pool.Count > 0)
            {
                return pool.Pop();
            }

            return new object[length];
        }

        public void Return(object[] array)
        {
            if (array.Length == 0)
            {
                return;
            }

            Array.Clear(
                array,
                0,
                array.Length);

            if (!_pools.TryGetValue(
                    array.Length,
                    out Stack<object[]> pool))
            {
                pool = new Stack<object[]>();
                _pools.Add(array.Length, pool);
            }

            pool.Push(array);
        }
    }
}