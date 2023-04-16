// 간단한 오브젝트 풀
public class ObjectPool<T>
    where T : class
{
    private readonly Stack<T> _pool;
    private readonly Func<T> _newFunc;

    public ObjectPool(Func<T> newFunc, int initSize)
    {
        _pool = new Stack<T>(initSize);
        _newFunc = newFunc;
        for (int i = 0; i < initSize; i++)
        {
            _pool.Push(newFunc());
        }
    }

    public T Rent()
    {
        if(_pool.Count > 0)
        {
            return _pool.Pop();
        }
        Console.WriteLine("NewInstance");
        return _newFunc();
    }

    public void Return(T obj)
    {
        _pool.Push(obj);
    }
}