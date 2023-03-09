using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace System.Pooling.Statistics
{
    public static class PoolTracker
    {
#if UNITY_EDITOR

        static int s_trackingId = 0;

        public const string EnableAutoReloadKey = "PoolTrackerWindow_EnableAutoReloadKey";
        public const string EnableTrackingKey = "PoolTrackerWindow_EnableTrackingKey";
        public const string EnableStackTraceKey = "PoolTrackerWindow_EnableStackTraceKey";

        public static class EditorEnableState
        {
            static bool enableAutoReload;

            public static bool EnableAutoReload
            {
                get { return enableAutoReload; }
                set
                {
                    enableAutoReload = value;
                    UnityEditor.EditorPrefs.SetBool(EnableAutoReloadKey, value);
                }
            }

            static bool enableTracking;

            public static bool EnableTracking
            {
                get { return enableTracking; }
                set
                {
                    enableTracking = value;
                    UnityEditor.EditorPrefs.SetBool(EnableTrackingKey, value);
                }
            }

            static bool enableStackTrace;

            public static bool EnableStackTrace
            {
                get { return enableStackTrace; }
                set
                {
                    enableStackTrace = value;
                    UnityEditor.EditorPrefs.SetBool(EnableStackTraceKey, value);
                }
            }
        }

#endif

        static List<KeyValuePair<ICountable, TrackEntry>> s_listPool =
            new List<KeyValuePair<ICountable, TrackEntry>>();

        static readonly WeakDictionary<ICountable, TrackEntry> s_tracking =
            new WeakDictionary<ICountable, TrackEntry>();

        //int trackId, int count
        static readonly Dictionary<int, int> s_trackingMaxRecord =
            new Dictionary<int, int>();
        //tracking pool items that are weakly referenced
        // static readonly Dictionary<ICountable, WeakList<object>> s_trackingPoolItem =
        //     new Dictionary<ICountable, WeakList<object>>();

        public static void TrackPoolCreation(ICountable countable, int skipFrame)
        {
#if UNITY_EDITOR
            s_dirty = true;
            if (!EditorEnableState.EnableTracking) return;
            var stackTrace = EditorEnableState.EnableStackTrace
                ? new StackTrace(skipFrame, true).CleanupAsyncStackTrace()
                : "";

            string typeName;
            if (EditorEnableState.EnableStackTrace)
            {
                var sb = new StringBuilder();
                TypeBeautify(countable.GetType(), sb);
                typeName = sb.ToString();
            }
            else
            {
                typeName = countable.GetType().Name;
            }

            PoolObjectType poolObjectType = PoolObjectType.None;
            //if type inherit from Unity.Pooling.IUnityPool interface in assembly Unity.Pooling
            if (countable.GetType().GetInterface("Unity.Pooling.IUnityPool") != null)
            {
                poolObjectType = PoolObjectType.Unity;
            }
            else if (countable.GetType().GetInterface("System.Pooling.IPool") != null)
            {
                poolObjectType = PoolObjectType.Csharp;
            }

            //is inherit from IAsyncPool<>
            var asyncPool = countable.GetType().GetInterface("System.Pooling.IAsyncPool`1") != null;

            var entry = new TrackEntry {
                trackingId = Interlocked.Increment(ref s_trackingId),
                formattedType = asyncPool ? typeName + " (Async)" : typeName,
                stackTrace = stackTrace,
                addTime = DateTime.UtcNow,
                countInactive = countable.Count,
                countActive = 0,
                poolObjectType = poolObjectType,
                weakList = new WeakList<object>()
            };
            lock (s_listPool)
            {
                s_tracking.TryAdd(countable, entry);
            }
#endif
        }

        public static void TrackPoolRentOrCreate<T>(ICountable countable, T item, int prePoolCount = 0)
        {
#if UNITY_EDITOR

            if (!EditorEnableState.EnableTracking) return;
            if (countable.GetType().GetInterface("IUnityPool") != null && s_tracking.TryGetValue(countable, out var e))
            {
                if (s_trackingMaxRecord.TryGetValue(e.trackingId, out var max2))
                {
                    if (max2 < e.weakList.AliveCount)
                    {
                        s_trackingMaxRecord[e.trackingId] = e.weakList.AliveCount;
                    }
                }
                else
                {
                    s_trackingMaxRecord.Add(e.trackingId, e.weakList.AliveCount);
                }
            }

            if (s_tracking.TryGetValue(countable, out var entry))
            {
                entry.countInactive = countable.Count;
                entry.weakList.Add(item);
            }
            else
            {
                Debug.LogError("TrackPoolRentOrCreate: can't find entry for " + countable.GetType().Name);
            }

            s_dirty = true;
#endif
        }

        public static void TrackPoolReturnOrRelease(ICountable countable, int count)
        {
#if UNITY_EDITOR
            s_dirty = true;
            if (!EditorEnableState.EnableTracking) return;
#endif
        }

        static bool s_dirty;

        public static bool CheckAndResetDirty()
        {
            var current = s_dirty;
            s_dirty = false;
            return current;
        }

        static void TypeBeautify(Type type, StringBuilder sb)
        {
            if (type.IsNested)
            {
                // TypeBeautify(type.DeclaringType, sb);
                sb.Append(type.DeclaringType.Name);
                sb.Append(".");
            }

            if (type.IsGenericType)
            {
                var genericsStart = type.Name.IndexOf("`");
                if (genericsStart != -1)
                {
                    sb.Append(type.Name.Substring(0, genericsStart));
                }
                else
                {
                    sb.Append(type.Name);
                }

                sb.Append("<");
                var first = true;
                foreach (var item in type.GetGenericArguments())
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }

                    first = false;
                    TypeBeautify(item, sb);
                }

                sb.Append(">");
            }
            else
            {
                sb.Append(type.Name);
            }
        }

        public static void ForEachActivePool(Action<TrackEntry> action)
        {
            lock (s_listPool)
            {
                var count = s_tracking.ToList(ref s_listPool, clear: false);
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        var entry = new TrackEntry() {
                            trackingId = s_listPool[i].Value.trackingId,
                            formattedType = s_listPool[i].Value.formattedType,
                            stackTrace = s_listPool[i].Value.stackTrace,
                            addTime = s_listPool[i].Value.addTime,
                            countInactive = s_listPool[i].Key.Count,
                            countActive = s_listPool[i].Value.weakList.AliveCount - s_listPool[i].Key.Count,
                            poolObjectType = s_listPool[i].Value.poolObjectType,
                            maxItemsRecorded =
                                s_trackingMaxRecord.TryGetValue(s_listPool[i].Value.trackingId, out var max) ? max : 0,
                            weakList = s_listPool[i].Value.weakList
                        };

                        action(entry);
                        s_listPool[i] = default;
                    }
                }
                catch
                {
                    s_listPool.Clear();
                    throw;
                }
            }
        }

        //GetDuplicatePoolType
        public static List<TrackEntry> GetDuplicatePoolType(string poolType)
        {
            List<TrackEntry> duplicatePoolType = new List<TrackEntry>();
            lock (s_listPool)
            {
                var count = s_tracking.ToList(ref s_listPool, clear: false);
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (s_listPool[i].Value.formattedType == poolType)
                        {
                            var entry = new TrackEntry() {
                                trackingId = s_listPool[i].Value.trackingId,
                                formattedType = s_listPool[i].Value.formattedType,
                                stackTrace = s_listPool[i].Value.stackTrace,
                                addTime = s_listPool[i].Value.addTime,
                                countInactive = s_listPool[i].Key.Count,
                                countActive = s_listPool[i].Value.weakList.AliveCount - s_listPool[i].Key.Count,
                                poolObjectType = s_listPool[i].Value.poolObjectType,
                                maxItemsRecorded =
                                    s_trackingMaxRecord.TryGetValue(s_listPool[i].Value.trackingId, out var max)
                                        ? max
                                        : 0,
                                weakList = s_listPool[i].Value.weakList
                            };
                            duplicatePoolType.Add(entry);
                        }

                        s_listPool[i] = default;
                    }
                }
                catch
                {
                    s_listPool.Clear();
                    throw;
                }
            }

            return duplicatePoolType;
        }
    }
}