namespace Unity.Pooling
{
    public interface IReleasable<in T>
    {
        void Release(T instance);
    }
}