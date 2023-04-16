// 단일 환경을 구현하기 위하여 상속을 사용하지않음
public class SafeObjectPool<T>
    where T : class
{
    private readonly Stack<T> _pool;
    private readonly Func<T> _newFunc;

    public SafeObjectPool(Func<T> newFunc, int capacity)
    {
        _pool = new Stack<T>(capacity);
        _newFunc = newFunc;

        for (int i = 0; i < capacity; i++)
        {
            _pool.Push(newFunc());
        }
    }

    public T Rent()
    {
        lock (this)
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            Console.WriteLine("NewInstance");
            return _newFunc();
        }
    }

    public void Return(T obj)
    {
        lock (this)
        {
            _pool.Push(obj);
        }
    }
}