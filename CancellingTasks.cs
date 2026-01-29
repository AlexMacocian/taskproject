namespace TaskProject;

public static class CancellingTasks
{
  public static void CancelAfterTimeout()
  {
    Task.Run(CancelAfterTimeoutAsync).Wait();
  }

  public static void CancelManually()
  {
    Task.Run(CancelManuallyAsync).Wait();
  }

  private static async Task CancelAfterTimeoutAsync()
  {
    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    var awaitable = Task.Delay(TimeSpan.FromSeconds(5), cancellationTokenSource.Token);

    try
    {
      await awaitable;
      Console.WriteLine("This should not happen. The task should have already been cancelled");
    }
    catch (TaskCanceledException)
    {
      Console.WriteLine("Task cancelled successfully");
    }
  }

  private static async Task CancelManuallyAsync()
  {
    var cancellationTokenSource = new CancellationTokenSource();
    var awaitable = Task.Delay(TimeSpan.FromSeconds(5), cancellationTokenSource.Token);
    var cancellingTask = Task.Run(async () =>
        {
          await Task.Delay(TimeSpan.FromSeconds(3));
          cancellationTokenSource.Cancel();
        });

    try
    {
      await awaitable;
      Console.WriteLine("This should not happen. The task should have been cancelled");
    }
    catch (TaskCanceledException)
    {
      Console.WriteLine("Task cancelled successfully");
    }
  }
}
