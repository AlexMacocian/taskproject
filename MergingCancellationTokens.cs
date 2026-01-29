namespace TaskProject;

public sealed class MergingCancellationTokens
{
  public static void Run()
  {
    Task.Run(RunAsync).Wait();
  }

  private static async Task RunAsync()
  {
    var originalCancellationTokenSource = new CancellationTokenSource();
    try
    {
      await DoOperationAsync(originalCancellationTokenSource.Token);
      Console.WriteLine("This should not happen. The task should have already been cancelled");
    }
    catch (TaskCanceledException)
    {
      Console.WriteLine("Task cancelled successfully");
    }
  }

  private static async Task DoOperationAsync(CancellationToken cancellationToken)
  {
    var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    var mergedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);
    await Task.Delay(TimeSpan.FromSeconds(5), mergedTokenSource.Token);
  }
}
