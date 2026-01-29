namespace TaskProject;

internal static class Program
{
  private static void Main(string[] args)
  {
    Console.WriteLine("Hello, World!");
    if (args.Length < 1)
    {
      Console.WriteLine("Specify scenario");
    }

    switch (args[0])
    {
      case "CompletionPortStarvation":
        CompletionPortStarvation.Run();
        break;
      case "ContextSwitching":
        ContextSwitching.Run();
        break;
      case "ThreadPoolStarvation":
        ThreadPoolStarvation.Run();
        break;
      case "CancellingTasksTimeout":
        CancellingTasks.CancelAfterTimeout();
        break;
      case "CancellingTasksManually":
        CancellingTasks.CancelManually();
        break;
      case "MergingCancellationTokens":
        MergingCancellationTokens.Run();
        break;
      case "MultipleAsyncWhenAll":
        MultipleAsync.RunWhenAll();
        break;
      case "MultipleAsyncWhenAny":
        MultipleAsync.RunWhenAny();
        break;
    }
  }
}
