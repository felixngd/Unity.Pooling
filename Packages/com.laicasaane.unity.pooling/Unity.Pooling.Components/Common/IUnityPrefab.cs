﻿namespace Unity.Pooling.Components
{
    public interface IUnityPrefab
    {
        int PrepoolingAmount { get; set; }

        PrepoolTiming PrepoolTiming { get; set; }
    }
}
