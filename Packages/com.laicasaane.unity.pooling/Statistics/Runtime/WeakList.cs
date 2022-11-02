using System.Collections;
using System.Collections.Generic;

namespace System.Pooling.Statistics
{
    public class WeakList<T> : IEnumerable<T> where T : class
    {
        private readonly List<WeakReference> _weakReferences = new List<WeakReference>();
        private readonly object _syncRoot = new object();
        
        public int AliveCount
        {
            get
            {
                lock (_syncRoot)
                {
                    //only count alive objects with while loop
                    int aliveCount = 0;
                    for (int i = 0; i < _weakReferences.Count; i++)
                    {
                        if (_weakReferences[i].IsAlive)
                            aliveCount++;
                    }
                    return aliveCount;
                }
            }
        }

        public void Add(T item)
        {
            lock (_syncRoot)
            {
                _weakReferences.Add(new WeakReference(item));
            }
        }

        public void Remove(T item)
        {
            lock (_syncRoot)
            {
                for (int i = 0; i < _weakReferences.Count; i++)
                {
                    if (_weakReferences[i].Target == item)
                    {
                        _weakReferences.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_syncRoot)
            {
                for (int i = 0; i < _weakReferences.Count; i++)
                {
                    var target = _weakReferences[i].Target as T;
                    if (target != null)
                    {
                        yield return target;
                    }
                    else
                    {
                        _weakReferences.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
