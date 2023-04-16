using System.Runtime.InteropServices;

int[] ObjectSizes = new int[] { 8, 16, 32, 64, 128, 256, 512, 1024, 2048 };
int[] ThreadCounts = new int[] { 1, 2, 4, 8 };
//int[] TestTimes = new int[] { 60_000, 600_000, 3600_000 }; // ms단위, 전체 프로세스가 끝나는 ms 단위
int[] TestTimes = new int[] { 3600_000 }; // ms단위, 전체 프로세스가 끝나는 ms 단위
int Iteration = 5;

int nintSize = Marshal.SizeOf<nint>();
Console.WriteLine(nintSize.ToString());

ThreadLocal<ObjectPool<nint[]>> pools = null!;
ThreadLocal<SafeObjectPool<nint[]>> lpools = null!;
SafeObjectPool<nint[]> lpool = null!;

long totalCount = 0;
int elemSize = 1;

TestAction funcA = (ref bool isRun) =>
{
    nint cnt = 0;
    while (isRun)
    {
        var obj = new nint[elemSize];
        obj[cnt % obj.Length] = cnt;
        cnt++;
    }
    Interlocked.Add(ref totalCount, cnt);
};
// 하나의 풀을 모든 쓰레드가 lock하며 사용함
TestAction funcB = (ref bool isRun) =>
{
    nint cnt = 0;
    while (isRun)
    {
        var obj = lpool.Rent();
        obj[cnt % obj.Length] = cnt;
        lpool.Return(obj);
        cnt++;
    }
    Interlocked.Add(ref totalCount, cnt);
};
// 매 호출마다 Thread당 풀을 가져와서 lock없이 사용함
TestAction funcC = (ref bool isRun) =>
{
    nint cnt = 0;
    while (isRun)
    {
        var pool = pools.Value;
        var obj = pool.Rent();
        obj[cnt % obj.Length] = cnt;
        pool.Return(obj);
        cnt++;
    }
    Interlocked.Add(ref totalCount, cnt);
};
// 최초1번 쓰레드에 해당하는 풀을 가져오며 lock없이 사용함.
TestAction funcD = (ref bool isRun) =>
{
    var pool = pools.Value;
    nint cnt = 0;
    while (isRun)
    {
        var obj = pool.Rent();
        obj[cnt % obj.Length] = cnt;
        pool.Return(obj);
        cnt++;
    }
    Interlocked.Add(ref totalCount, cnt);
};
// 매호출마다 쓰레드에 해당하는 풀을 가져오며 lock도 사용함.
TestAction funcE = (ref bool isRun) =>
{
    nint cnt = 0;
    while (isRun)
    {
        var pool = lpools.Value;
        var obj = pool.Rent();
        obj[cnt % obj.Length] = cnt;
        pool.Return(obj);
        cnt++;
    }
    Interlocked.Add(ref totalCount, cnt);
};
// 최초1번 쓰레드에 해당하는 풀을 가져오며 lock도 사용함.
TestAction funcF = (ref bool isRun) =>
{
    var pool = lpools.Value;
    nint cnt = 0;
    while (isRun)
    {
        var obj = pool.Rent();
        obj[cnt % obj.Length] = cnt;
        pool.Return(obj);
        cnt++;
    }
    Interlocked.Add(ref totalCount, cnt);
};

long Run(TestAction act, int it, int lt)
{
    totalCount = 0;
    TimebaseTester.Run(funcA, it, lt);
    return totalCount;
}

void Print(string msg)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
}


using var fs = new FileStream($"d:/test.{DateTime.Now.Ticks}.txt", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
using var writer = new StreamWriter(fs);

foreach (var threadCount in ThreadCounts)
{
    MultiThreadTester.Init(threadCount);
    Print("#Threads " + threadCount);

    foreach (var objSize in ObjectSizes)
    {
        Print("#ObjSize " + objSize);
        int poolSize = threadCount;
        elemSize = objSize / nintSize;
        var newFunc = () => new nint[elemSize];
        pools = new(() => new(newFunc, poolSize));
        lpools = new(() => new(newFunc, poolSize));
        lpool = new(newFunc, poolSize);

        // warmup
        Run(funcA, 2, 50);
        Run(funcB, 2, 50);
        Run(funcC, 2, 50);
        Run(funcD, 2, 50);
        Run(funcE, 2, 50);
        Run(funcF, 2, 50);

        foreach (var testtime in TestTimes)
        {
            var lifetime = testtime / (ObjectSizes.Length * ThreadCounts.Length * Iteration);
            Print($"#Lifetime {lifetime} ({testtime})");

            var a = Run(funcA, Iteration, lifetime);
            var b = Run(funcB, Iteration, lifetime);
            var c = Run(funcC, Iteration, lifetime);
            var d = Run(funcD, Iteration, lifetime);
            var e = Run(funcE, Iteration, lifetime);
            var f = Run(funcF, Iteration, lifetime);

            var msg = $"{threadCount,2} {objSize,5} {lifetime,5} {a,20:N0} {b,20:N0} {c,20:N0} {d,20:N0} {e,20:N0} {f,20:N0}";
            Print(msg);
            writer.WriteLine(msg);
            writer.Flush();
        }

        Console.WriteLine();
        writer.WriteLine();
    }
    Console.WriteLine();
    writer.WriteLine();
}