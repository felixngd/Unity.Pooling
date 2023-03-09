namespace System.Pooling.Statistics
{
    public struct TrackEntry
    {
        public string formattedType;
        public int trackingId;
        public int countInactive;
        public int countActive;
        public DateTime addTime;
        public string stackTrace;
        public PoolObjectType poolObjectType;
        public int maxItemsRecorded;
        
        public WeakList<object> weakList;

        public override string ToString()
        {
            return string.Format("Type: {0}, TrackingId: {1}, CountInactive: {2}, CountActive: {3}, AddTime: {4}, StackTrace: {5}, PoolObjectType: {6}, MaxItemsRecorded: {7}",
                formattedType, trackingId, countInactive, countActive, addTime, stackTrace, poolObjectType, maxItemsRecorded);
        }
    }

    public enum PoolObjectType
    {
        None,
        Unity,
        Csharp
        
    }
}