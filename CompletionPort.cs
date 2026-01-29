namespace TaskProject;

public static class CompletionPort
{
    private static readonly int maxIOCompletionThreads = Environment.ProcessorCount;
    private static readonly CountdownEvent countdownEvent = new(maxIOCompletionThreads);

    private static int outstandingIOCall = 0;

    public static async Task Run()
    {
        ConsumeCompletionPort();
        countdownEvent.Wait();

        PrintThreadPoolInformation(out int _, out int _, out int _, out int _);
        string readValue = await File.ReadAllTextAsync("TextFile1.txt");
        PrintThreadPoolInformation(out _, out _, out _, out _);

        Console.WriteLine($"File content {readValue}");
        Console.ReadLine();
    }

    private static void PrintThreadPoolInformation(out int workerThread, out int completionPortThreads, out int maxWorkerThread, out int maxCompletionPortThreads)
    {
        ThreadPool.GetAvailableThreads(out workerThread, out completionPortThreads);
        ThreadPool.GetMaxThreads(out maxWorkerThread, out maxCompletionPortThreads);
        Console.WriteLine($"Available completion port thread {completionPortThreads} and worker thread {workerThread}");
        Console.WriteLine($"Max completion port thread {maxCompletionPortThreads} and worker thread {maxWorkerThread}");
        Console.WriteLine();
    }

    private static unsafe void ConsumeCompletionPort()
    {
        PrintThreadPoolInformation(out int workerThread, out int completionPortThreads, out int maxWorkerThread, out int maxCompletionPortThreads);
        ThreadPool.SetMaxThreads(workerThread, maxIOCompletionThreads);
        ThreadPool.SetMinThreads(workerThread, maxIOCompletionThreads);
        PrintThreadPoolInformation(out workerThread, out completionPortThreads, out maxWorkerThread, out maxCompletionPortThreads);

        int packCount = 0;
        for (var i = 0; i < maxIOCompletionThreads; i++)
        {
            packCount++;
            Overlapped overlapped = new();
            NativeOverlapped* pOverlap = overlapped.Pack(IOCompletionCallback, packCount);
            ThreadPool.UnsafeQueueNativeOverlapped(pOverlap);
        }
    }

    static unsafe void IOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlap)
    {
        var newOutstandingIOCall = Interlocked.Increment(ref outstandingIOCall);
        Console.WriteLine($"Thread id: {Environment.CurrentManagedThreadId} Outstanding io completion callback number: {newOutstandingIOCall}");
        countdownEvent.Signal();
        Thread.Sleep(10000);
        Console.WriteLine($"Thread id: {Environment.CurrentManagedThreadId} Outstanding io completion callback number: {newOutstandingIOCall} works done");
    }
}