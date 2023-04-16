
public delegate void TestAction(ref bool isRun);

public static class TimebaseTester
{
    private static bool isRun;

    public static void Run(TestAction func, int iteration, int lifetime)
    {
        Action act = () =>
        {
            func(ref isRun);
        };

        GC.Collect(2);
        GC.WaitForFullGCComplete();

        for (int i = 0; i < iteration; i++)
        {
            isRun = true;
            MultiThreadTester.Start(act);
            Thread.Sleep(lifetime);
            isRun = false;
            MultiThreadTester.Join();
        }
    }
}