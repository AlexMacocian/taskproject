# Thread Pool Starvation

This scenario demonstrates what happens when blocking tasks exhaust the
thread pool, causing subsequent tasks to wait for available threads.

## Overview

The .NET `ThreadPool` maintains a limited number of worker threads.
When all threads are blocked (e.g., by `Thread.Sleep` or synchronous I/O),
new tasks must wait in a queue until a thread becomes available.

## What the Code Does

### 1. Limit the Thread Pool

```C#
const int minWorker = 4;
const int minIOC = 4;
ThreadPool.SetMinThreads(minWorker, minIOC);
ThreadPool.SetMaxThreads(minWorker, minIOC);
```

Forces the thread pool to exactly 4 worker threads to easily demonstrate starvation.

### 2. Create More Tasks Than Threads

```C#
for (var i = 1; i <= minWorker * 2; i++)  // 8 tasks, but only 4 threads
{
    var task = Task.Run(() =>
    {
        Console.WriteLine($"Task {taskId} STARTED...");
        Thread.Sleep(3000);  // Block the thread for 3 seconds
        Console.WriteLine($"Task {taskId} COMPLETED...");
    });
    tasks.Add(task);
}
```

Creates 8 tasks but only 4 threads are available.
Each task blocks its thread for 3 seconds.

### 3. Observe the Starvation

Tasks 1-4 start immediately, but tasks 5-8 must wait ~3 seconds
until the first batch completes.

## Expected Output

```text
ThreadPool configured: Min/Max = 4 worker threads
Starting 8 blocking tasks to cause starvation...

Task 1 STARTED on thread 4 (delay: 3ms)
Task 2 STARTED on thread 6 (delay: 3ms)
Task 3 STARTED on thread 7 (delay: 4ms)
Task 4 STARTED on thread 8 (delay: 4ms)

Waiting for all tasks to complete...
Notice how tasks beyond the thread pool size are delayed!

Task 1 COMPLETED on thread 4
Task 2 COMPLETED on thread 6
Task 3 COMPLETED on thread 7
Task 4 COMPLETED on thread 8
Task 5 STARTED on thread 4 (delay: 3005ms)   <-- Delayed!
Task 6 STARTED on thread 6 (delay: 3005ms)   <-- Delayed!
Task 7 STARTED on thread 7 (delay: 3005ms)   <-- Delayed!
Task 8 STARTED on thread 8 (delay: 3005ms)   <-- Delayed!
Task 5 COMPLETED on thread 4
Task 6 COMPLETED on thread 6
Task 7 COMPLETED on thread 7
Task 8 COMPLETED on thread 8

All tasks completed in 6.0 seconds
With enough threads, this would take ~3 seconds, not longer.
```

## Key Takeaways

| Problem | Impact |
| ------- | ------ |
| Blocking calls (`Thread.Sleep`, sync I/O) | Holds thread hostage |
| More tasks than threads | Tasks queue up waiting |
| Total time increases | 8 tasks × 3s / 4 threads = 6s instead of 3s |

## How to Avoid Starvation

1. **Use async/await** - `await Task.Delay()` instead of `Thread.Sleep()`
2. **Avoid blocking calls** - Use async versions of I/O operations
3. **Don't block on async code** - Avoid `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`
4. **Keep tasks short** - Long-running work should use `TaskCreationOptions.LongRunning`
