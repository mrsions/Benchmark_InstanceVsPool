using System;
using System.Diagnostics;
using System.Threading;

public static class MultiThreadTester
{
    static Thread[] threads;

    static readonly object WaitLocker = new();
    static readonly object RunLocker = new();

    static volatile int ThreadLength;
    static volatile int WaiterCount;
    static volatile int RunnerCount;
    static volatile int StartCount = 0;
    static volatile int CompleteCount = 0;

    static Action? TargetAction;

    public static void Init(int count)
    {
        ThrowIfRun();

        if (threads == null)
        {
            threads = new Thread[count];
        }
        else if (threads.Length < count)
        {
            Array.Resize(ref threads, count);
        }

        // 기존 쓰레드의 러닝 상태를 변경한다.
        ThreadLength = count;
        TargetAction = null;
        lock (WaitLocker)
        {
            Monitor.PulseAll(WaitLocker);
        }
        lock (RunLocker)
        {
            Monitor.PulseAll(RunLocker);
        }

        // 필요 쓰레드를 더 생성한다.
        for (int i = 0; i < count; i++)
        {
            if (threads[i] == null)
            {
                threads[i] = new Thread(Run);
                threads[i].Name = "Tester-" + i;
                threads[i].Priority = ThreadPriority.Lowest;
                threads[i].Start(i);
            }
        }

        // 모두 러닝 준비가 될때까지 기다린다.
        while (RunnerCount != ThreadLength)
        {
            Thread.Sleep(1);
        }
    }

    public static void StartAndJoin(Action? action)
    {
        Start(action);
        Join();
    }

    public static void Start(Action? action)
    {
        ThrowIfRun();

        lock (RunLocker)
        {
            StartCount = 0;
            CompleteCount = 0;
            TargetAction = action;
            Log($"[{DateTime.Now:mm:ss.fff}][-] Start");
            Monitor.PulseAll(RunLocker);
            Log($"[{DateTime.Now:mm:ss.fff}][-] Start End");
        }
    }

    private static void ThrowIfRun()
    {
        if (IsRun())
        {
            throw new ApplicationException($"Already running. (Current:{RunnerCount}/{ThreadLength}({threads.Length}),Wait:{WaiterCount},Start:{StartCount}, Comp:{CompleteCount})");
        }
    }

    public static bool IsRun()
    {
        return TargetAction != null && StartCount != CompleteCount;
    }

    public static void Join()
    {
        Log($"[{DateTime.Now:mm:ss.fff}][-] Join");
        // 모두 러닝모드로 돌아올때까지 기다린다.
        while (CompleteCount != ThreadLength)
        {
            Thread.Sleep(1);
        }
        while (RunnerCount != ThreadLength)
        {
            Thread.Sleep(1);
        }
    }

    private static void Run(object obj)
    {
        try
        {
            int id = (int)obj;
            while (true)
            {
                if (id < ThreadLength)
                {
                    lock (RunLocker)
                    {
                        RunnerCount++;
                        Log($"[{DateTime.Now:mm:ss.fff}][{id}] Wait");
                        Monitor.Wait(RunLocker);
                        Log($"[{DateTime.Now:mm:ss.fff}][{id}] Release");
                        RunnerCount--;
                    }

                    try
                    {
                        Log($"[{DateTime.Now:mm:ss.fff}][{id}] Start");
                        Interlocked.Increment(ref StartCount);
                        Log($"[{DateTime.Now:mm:ss.fff}][{id}] Invoke");
                        TargetAction?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        Log($"[{DateTime.Now:mm:ss.fff}][{id}] Complete");
                        Interlocked.Increment(ref CompleteCount);
                    }
                }
                else
                {
                    lock (WaitLocker)
                    {
                        WaiterCount++;
                        Monitor.Wait(WaitLocker);
                        WaiterCount--;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    [Conditional("Logging")]
    private static void Log(string msg)
    {
        Console.WriteLine(msg);
    }
}
