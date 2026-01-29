# Cancelling Tasks

This scenario demonstrates two ways to cancel tasks using `CancellationToken`.

## CancelAfterTimeout

Creates a `CancellationTokenSource` with a built-in timeout.
The token automatically cancels after the specified duration.

```C#
var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var awaitable = Task.Delay(TimeSpan.FromSeconds(5), cancellationTokenSource.Token);
```

**What happens:**

1. A 5-second delay task is started with a cancellation token
2. The token is configured to cancel after 3 seconds
3. At 3 seconds, the token cancels and throws `TaskCanceledException`
4. The task never completes its full 5-second delay

## CancelManually

Creates a `CancellationTokenSource` and cancels it programmatically from
another task.

```C#
var cancellationTokenSource = new CancellationTokenSource();
var awaitable = Task.Delay(TimeSpan.FromSeconds(5), cancellationTokenSource.Token);
var cancellingTask = Task.Run(async () =>
{
    await Task.Delay(TimeSpan.FromSeconds(3));
    cancellationTokenSource.Cancel();
});
```

**What happens:**

1. A 5-second delay task is started with a cancellation token
2. A separate task waits 3 seconds, then calls `Cancel()`
3. The original task receives the cancellation and throws `TaskCanceledException`

## Key Takeaways

| Method | Use Case |
| ------ | -------- |
| `new CancellationTokenSource(TimeSpan)` | Fixed timeout scenarios |
| `cancellationTokenSource.Cancel()` | Event-driven cancellation |

Both approaches result in a `TaskCanceledException` that should be caught
and handled appropriately.
