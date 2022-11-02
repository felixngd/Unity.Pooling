namespace System.Pooling
{
    public interface IInstantiable<out T>
    {
        T Instantiate();
    }
}
