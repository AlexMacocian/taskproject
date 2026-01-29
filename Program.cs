namespace TaskProject;

internal static class Program
{
  private static async Task Main()
  {
    Console.WriteLine("Hello, World!");

    ThreadPoolStarvation.Run();
  }
}
