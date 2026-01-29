using System.Diagnostics;

namespace TaskProject;

public static class ThreadPoolStarvation
{
  public static void Run()
  {
    // Force a small thread pool to easily demonstrate starvation
    const int minWorker = 4;
    const int minIOC = 4;
    ThreadPool.SetMinThreads(minWorker, minIOC);
    ThreadPool.SetMaxThreads(minWorker, minIOC);

    Console.WriteLine($"ThreadPool configured: Min/Max = {minWorker} worker threads");
    Console.WriteLine($"Starting {minWorker * 2} blocking tasks to cause starvation...\n");

    var tasks = new List<Task>();
    var sw = Stopwatch.StartNew();

    // Create more tasks than available threads
    for (var i = 1; i <= minWorker * 2; i++)
    {
      int taskId = i;
      var task = Task.Run(() =>
      {
        var delay = sw.Elapsed;
        Console.WriteLine($"Task {taskId} STARTED on thread {Environment.CurrentManagedThreadId} " +
                                $"(delay: {delay.TotalMilliseconds:F0}ms)");

        // Simulate blocking work (bad practice - causes starvation)
        Thread.Sleep(3000);

        Console.WriteLine($"Task {taskId} COMPLETED on thread {Environment.CurrentManagedThreadId}");
      });
      tasks.Add(task);
    }

    Console.WriteLine("\nWaiting for all tasks to complete...");
    Console.WriteLine("Notice how tasks beyond the thread pool size are delayed!\n");

    Task.WaitAll([.. tasks]);

    Console.WriteLine($"\nAll tasks completed in {sw.Elapsed.TotalSeconds:F1} seconds");
    Console.WriteLine("With enough threads, this would take ~3 seconds, not longer.");
  }
}
