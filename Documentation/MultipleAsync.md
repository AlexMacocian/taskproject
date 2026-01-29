# Multiple Async Operations

This scenario demonstrates how to await multiple tasks concurrently using `Task.WhenAll` and `Task.WhenAny`.

## Overview

When you have multiple independent async operations, you can run them concurrently rather than sequentially. TAP provides two methods for coordinating multiple tasks:

| Method | Returns When | Returns |
| ------ | ------------ | ------- |
| `Task.WhenAll` | All tasks complete | Aggregated results |
| `Task.WhenAny` | First task completes | The completed task |

## What the Code Does

### RunWhenAll

```C#
var awaitable1 = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
var awaitable2 = Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
var awaitable3 = Task.Delay(TimeSpan.FromSeconds(6), cts.Token);

await Task.WhenAll(awaitable1, awaitable2, awaitable3);
```

**What happens:**

1. Three delay tasks start concurrently (5s, 3s, 6s)
2. `WhenAll` waits for **all** tasks to complete
3. Returns after ~6 seconds (the longest task)

### RunWhenAny

```C#
var awaitable1 = Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
var awaitable2 = Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
var awaitable3 = Task.Delay(TimeSpan.FromSeconds(6), cts.Token);

await Task.WhenAny(awaitable1, awaitable2, awaitable3);
```

**What happens:**

1. Three delay tasks start concurrently (5s, 3s, 6s)
2. `WhenAny` returns when the **first** task completes
3. Returns after ~3 seconds (the shortest task)
4. The other tasks **continue running** in the background

## Expected Output

### WhenAll

```text
WhenAll returned after 6000ms
Completed tasks after 6000ms
```

### WhenAny

```text
WhenAny returned after 3000ms
Completed tasks after 6000ms
```

> **Note:** In `WhenAny`, awaiting the remaining tasks after `WhenAny` returns still takes until
> all tasks complete (~6 seconds total), because they were already running.

## Use Cases

| Method | Use Case |
| ------ | -------- |
| `WhenAll` | Fetch data from multiple APIs in parallel |
| `WhenAll` | Process multiple files concurrently |
| `WhenAny` | Return first successful response from redundant services |
| `WhenAny` | Implement timeout patterns |

## Key Takeaways

1. **Tasks start immediately** - Not when you `await` them
2. **WhenAny doesn't cancel others** - Remaining tasks keep running unless you cancel them
3. **Use CancellationToken** - Pass tokens to cancel remaining tasks after `WhenAny` returns
4. **Sequential vs Concurrent** - Awaiting tasks one-by-one is sequential; use `WhenAll` for concurrency

```C#
// Sequential - takes 14 seconds total
await Task.Delay(5000);
await Task.Delay(3000);
await Task.Delay(6000);

// Concurrent - takes 6 seconds total
await Task.WhenAll(
    Task.Delay(5000),
    Task.Delay(3000),
    Task.Delay(6000));
```
