using System.Diagnostics;

namespace TaskProject;

public static class MultipleAsync
{
  public static void RunWhenAll()
  {
    Task.Run(RunWhenAllAsync).Wait();
  }

  public static void RunWhenAny()
  {
    Task.Run(RunWhenAnyAsync).Wait();
  }

  private static async Task RunWhenAllAsync()
  {
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
    var sw = Stopwatch.StartNew();
    var awaitable1 = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
    var awaitable2 = Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
    var awaitable3 = Task.Delay(TimeSpan.FromSeconds(6), cts.Token);

    await Task.WhenAll(awaitable1, awaitable2, awaitable3);
    Console.WriteLine($"WhenAny returned after {sw.Elapsed.TotalMilliseconds}ms");
    await awaitable1;
    await awaitable2;
    await awaitable3;
    Console.WriteLine($"Completed tasks after {sw.Elapsed.TotalMilliseconds}ms");
  }

  private static async Task RunWhenAnyAsync()
  {
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
    var sw = Stopwatch.StartNew();
    var awaitable1 = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
    var awaitable2 = Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
    var awaitable3 = Task.Delay(TimeSpan.FromSeconds(6), cts.Token);

    await Task.WhenAny(awaitable1, awaitable2, awaitable3);
    Console.WriteLine($"WhenAny returned after {sw.Elapsed.TotalMilliseconds}ms");
    await awaitable1;
    await awaitable2;
    await awaitable3;
    Console.WriteLine($"Completed tasks after {sw.Elapsed.TotalMilliseconds}ms");
  }
}
