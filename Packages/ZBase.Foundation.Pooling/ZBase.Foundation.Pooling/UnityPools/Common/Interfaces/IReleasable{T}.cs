namespace ZBase.Foundation.Pooling.UnityPools
{
    public interface IReleasable<in T>
    {
        void Release(T instance);
    }
}