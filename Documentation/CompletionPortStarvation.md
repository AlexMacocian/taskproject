# IO Completion Port Starvation

This scenario demonstrates what happens when all IOCP threads are blocked and async I/O operations cannot complete.

## Overview

The .NET ThreadPool manages two separate pools:

- **Worker threads** - for CPU-bound work (`Task.Run`, `Parallel.For`)
- **IOCP threads** - for I/O completion callbacks (file I/O, sockets, HTTP)

When all IOCP threads are blocked, async I/O completions queue up waiting for a thread to process them.

## What the Code Does

### 1. Limit the IOCP Pool

```C#
ThreadPool.SetMaxThreads(workerThread, maxIOCompletionThreads);
ThreadPool.SetMinThreads(workerThread, maxIOCompletionThreads);
```

Restricts IOCP threads to `Environment.ProcessorCount` (e.g., 8 threads on an 8-core machine).

### 2. Saturate IOCP Threads with Native Overlapped Callbacks

```C#
Overlapped overlapped = new();
NativeOverlapped* pOverlap = overlapped.Pack(IOCompletionCallback, packCount);
ThreadPool.UnsafeQueueNativeOverlapped(pOverlap);
```

Queues work directly to the IOCP pool using native overlapped I/O - this bypasses the worker pool entirely.

### 3. Block All IOCP Threads

```C#
static unsafe void IOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlap)
{
    countdownEvent.Signal();  // Signal "I've started"
    Thread.Sleep(10000);       // Block for 10 seconds
}
```

Each callback blocks its IOCP thread for 10 seconds.

### 4. Attempt Async File I/O

```C#
countdownEvent.Wait();  // Wait until all IOCP threads are blocked
string readValue = await File.ReadAllTextAsync("TextFile1.txt");
```

**This is the critical moment:**

- `File.ReadAllTextAsync` initiates an async read (completes almost instantly at the OS level)
- The completion callback needs an IOCP thread to run
- All IOCP threads are blocked in `Thread.Sleep`
- The `await` hangs until an IOCP thread becomes available (~10 seconds)

## Expected Output

| Time | Event |
| ---- | ----- |
| 0s | IOCP threads start, all block in `Sleep` |
| 0s | `ReadAllTextAsync` initiates file read |
| 0-10s | File read **completes at OS level** but callback waits |
| ~10s | IOCP threads wake up, one processes the file read completion |
| ~10s | `readValue` finally available |

## Key Takeaway

Even though the actual file I/O is instant, **IOCP starvation delays the completion callback**. This can cause unexpected latency in I/O-heavy applications when IOCP threads are blocked by:

- Synchronous operations inside async callbacks
- Long-running computations on IOCP threads
- Excessive blocking calls (`Thread.Sleep`, synchronous I/O)
