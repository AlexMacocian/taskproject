using System.Diagnostics;

namespace TaskProject;

//https://devblogs.microsoft.com/premier-developer/the-cost-of-context-switches/
public static class ContextSwitching
{
  const int unitSize = 500;
  const int workSize = 500000;
  const string spacing = "{0,-20}{1,-20}{2,-20}";

  public static void Run()
  {
    Console.WriteLine("Executing {0} work cycles of {1} iterations each, in different ways...", workSize, unitSize);

    Console.WriteLine(spacing, "Scenario", "Total time (ms)", "Time per unit (μs)");
    Scenario("No-switch", DoSync);
    Scenario("Async w/o yield", DoAsyncNoYield);
    Scenario("Async w/ yield", DoAsyncWithYield);
    Scenario("Thread switches", ThreadSwitch);
  }

  static void Scenario(string name, Action operation)
  {
    GC.Collect();
    operation(); // warm it up
    var timer = Stopwatch.StartNew();
    operation();
    timer.Stop();
    Console.WriteLine(spacing, name, timer.ElapsedMilliseconds, MicroSecondsPerItem(timer));
  }

  static void ThreadSwitch()
  {
    var workRemaining = workSize;
    var evt = new AutoResetEvent(true);
    void worker()
    {
      while (workRemaining > 0)
      {
        evt.WaitOne();
        workRemaining--;
        WorkUnit();
        evt.Set();
      }
    }

    var threads = new Thread[Environment.ProcessorCount];
    for (var i = 0; i < threads.Length; i++)
    {
      threads[i] = new Thread(worker);
      threads[i].Start();
    }

    for (var i = 0; i < threads.Length; i++)
    {
      threads[i].Join();
    }
  }

  static void DoAsyncNoYield()
  {
    static async Task NoYieldHelper(Task task)
    {
      WorkUnit();
      await task;
    }

    var tcs = new TaskCompletionSource<object>();
    tcs.SetResult(null!);
    var task = tcs.Task;
    Task.Run(
        async delegate
        {
          var workRemaining = workSize;
          while (--workRemaining >= 0)
          {
            await NoYieldHelper(task);
          }
        }).Wait();
  }

  static void DoAsyncWithYield()
  {
    Task.Run(
        async delegate
        {
          var workRemaining = workSize;
          while (--workRemaining >= 0)
          {
            WorkUnit();
            await Task.Yield();
          }
        }).Wait();
  }

  static void DoSync()
  {
    var workRemaining = workSize;
    while (--workRemaining >= 0)
    {
      WorkUnit();
    }
  }

  private static double MicroSecondsPerItem(Stopwatch timer)
  {
    var ticksPerItem = (double)timer.ElapsedTicks / workSize;
    return TimeSpan.FromTicks((long)(ticksPerItem * 1000)).TotalMilliseconds;
  }

  static void WorkUnit()
  {
    for (int i = 0; i < unitSize; i++)
    {
    }
  }
}
