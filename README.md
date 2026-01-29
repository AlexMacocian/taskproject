# Demystifying Tasks

<!--toc:start-->
- [Demystifying Tasks](#demystifying-tasks)
  - [TAP - Task asynchronous programming model](#tap---task-asynchronous-programming-model)
    - [Overview](#overview)
    - [Task and ValueTask](#task-and-valuetask)
    - [Cancellation](#cancellation)
    - [Tasks vs Threads](#tasks-vs-threads-6)
    - [The managed thread pool](#the-managed-thread-pool-7)
      - [Worker Threads vs I/O Completion Port (IOCP) Threads](#worker-threads-vs-io-completion-port-iocp-threads)
      - [Managing the ThreadPool](#managing-the-threadpool)
    - [Async and await](#async-and-await)
      - [The State Machine](#the-state-machine-9)
      - [Awaiting Multiple Tasks](#awaiting-multiple-tasks)
  - [Scenarios](#scenarios)
  - [Documentation](#documentation)
<!--toc:end-->

## TAP - Task asynchronous programming model

The general goal of [TAP][1] is to improve responsiveness of an application
and avoid bottlenecks. It tries to abstract threading and parallelism
away and provide a common interface for all concurrent work.

### Overview

The standard .NET library provides async overloads for most of the
operations that interact with resources outside of the current application
context:

```C#

// HttpClient async
using var client = new HttpClient()
var response = await client.GetAsync("https://foo.bar");

// File operations async
var lines = await File.ReadAllLinesAsync("text1.txt");
var text = await File.ReadAllTextAsync("text1.txt");

// Stream async operations
using var ms = new MemoryStream();
using var textWriter = new StreamWriter(ms);
textWriter.WriteLineAsync("Hello world");
```

### Task and ValueTask

```C#
var t1 = new Task(() => {});
var t2 = new Task<string>(() => "Hello");
var t3 = new ValueTask();
var t4 = new ValueTask<string>("Hello");

await t1;
var hello2 = await t2;
await t3;
var hello4 = await t4;
```

Each time a `Task` is created, either through `Task.Run`, `Task.Factory.StartNew` or
returned by some async library, an object is allocated on the heap. This is normally
not a big issue, but for performance critical paths, allocating many objects on the heap
will create GC pressure, which in turn will cause the GC to pause the application periodically
to clean up the dangling references.

`ValueTask` was made to avoid this exact scenario. It's a struct (so it gets allocated on the stack).
This means that the compiler can deterministically resolve its lifetime and manage the memory layout
without relying on the GC. Most of the time, this means that there is actually no dynamic memory allocation.

As a general rule of thumb, `ValueTask` should be used whenever you want to await immediately and
the underlying call can resolve the result synchronously the large majority of the time. Passing around
`ValueTask` on the stack is slower than passing a simple `nint` reference to a `Task`.

Example of a good usage of task [5][5]:

```C#
public ValueTask<User> GetUserAsync(Guid id)
{
    if (this.cache.TryGetValue(id, out var user))
    {
        return new ValueTask<User>(user);
    }

    return new ValueTask<User>(_database.LoadUserAsync(id));
}
```

### Cancellation

TAP provides a standardized way to manage async lifetimes through `CancellationToken`.
> **Note:** `CancellationToken` is particularly useful in ASP.NET Core applications. Each request has a
> `HttpContext.RequestAborted` token that is scoped to the lifetime of the request. Passing this
> token to async calls allows you to cancel early when the client disconnects, avoiding unnecessary
> computation for a closed connection.

To manage the cancellation of a new task, pass `CancellationToken` along the call tree, up until the
actual async method. CancellationTokens are managed by a `CancellationTokenSource` that allows you
to modify the token's parameters, cancelling it. CancellationTokenSources can also accept a time
parameter, automatically cancelling the managed token after that time.

You can also create composite token comprised of multiple cancellation token sources, useful in
cases where your method already receives one or more tokens and you want to cancel when any one of those
are cancelling.

### Tasks vs Threads [6][6]

> **Note:** ASP.NET Core uses Tasks to power its parallelism across requests. This means that your
> degree of parallelism is both dictated by the hardware capabilities of your machine, as well as
> the parameters of the `ThreadPool` managed by the .NET runtime.

A `Thread` is an OS-level construct - a dedicated execution context with its own stack, managed by the operating
system scheduler. Creating and destroying threads is expensive, and each thread consumes memory for its stack.

A `Task` is a higher-level abstraction representing a unit of work. Tasks are scheduled onto a managed
thread pool, allowing the runtime to efficiently reuse a limited number of threads across many tasks.

| Aspect | Thread | Task |
| ------ | ------ | ---- |
| Abstraction level | OS primitive | Runtime managed |
| Creation cost | High (~1MB stack) | Low (object allocation) |
| Parallelism | Guaranteed | Not guaranteed |
| Return values | Manual (shared state) | Built-in (`Task<T>`) |
| Exception handling | Manual | Propagated on `await` |
| Cancellation | Manual | `CancellationToken` |

**Key insight:** When you `await` a Task, you're not blocking a thread. The thread is released back to the pool
and can process other work. When the async operation completes, a thread pool thread picks up the continuation.

```C#
// Thread - expensive, blocks for entire duration
var thread = new Thread(() => DoWork());
thread.Start();
thread.Join(); // Blocks calling thread

// Task - lightweight, non-blocking
await Task.Run(() => DoWork()); // Calling thread is free while waiting
```

### The managed thread pool [7][7]

The .NET runtime provides a standard managed `ThreadPool` which is used across all of
the TPL library. It is used by `Timers`, `Tasks`, `Parallel.For`, `System.Net.Socket` connections.
It is also used by the ASP.NET Core library to manage requests, with each request having its own
scoped async context.

#### Worker Threads vs I/O Completion Port (IOCP) Threads

The ThreadPool actually manages **two separate pools**:

| Pool | Purpose | Used By |
| ---- | ------- | ------- |
| **Worker Threads** | CPU-bound work, `Task.Run`, `Parallel.For` | Synchronous computations, callbacks |
| **IOCP Threads** | I/O completion callbacks | File I/O, network sockets, async streams |

When you call an async I/O method like `File.ReadAllTextAsync`, the OS handles the actual I/O operation.
When it completes, an `IOCP thread` picks up the completion notification and runs your continuation.
For more details, see [8][8].

```md
┌─────────────────┐     ┌─────────────────┐
│  Worker Pool    │     │   IOCP Pool     │
│  (CPU work)     │     │  (I/O callbacks)│
├─────────────────┤     ├─────────────────┤
│ Task.Run()      │     │ File.ReadAsync  │
│ Parallel.For    │     │ Socket.SendAsync│
│ ThreadPool.Queue│     │ HttpClient      │
└─────────────────┘     └─────────────────┘
```

> **Note:** Starving either pool can cause performance issues. Blocking worker threads with
> `Thread.Sleep` or synchronous I/O prevents other tasks from running. Similarly, blocking IOCP
> threads prevents async I/O completions from being processed.

#### Managing the ThreadPool

The ThreadPool can be managed by the application through methods like the following:

```C#
ThreadPool.GetMaxThreads(out int workerThreads, out int iocpThreads);
ThreadPool.GetMinThreads(out int workerThreads, out int iocpThreads);
ThreadPool.SetMaxThreads(workerCount, iocpCount);
ThreadPool.SetMinThreads(workerCount, iocpCount);
```

You can manually register work items on the ThreadPool by queueing direct work items using
one of the overloads of `ThreadPool.QueueUserWorkItem`.

### Async and await

The `async` and `await` keywords are syntactic sugar that the compiler transforms into a **state machine**. This allows methods to pause execution at `await` points and resume later without blocking a thread.

#### The State Machine [9][9]

When you write:

```C#
async Task<string> GetDataAsync()
{
    var response = await httpClient.GetAsync(url);
    var content = await response.Content.ReadAsStringAsync();
    return content;
}
```

The compiler generates a struct implementing `IAsyncStateMachine` with:

- **State field** - Tracks which `await` the method is at (0, 1, 2, ...)
- **MoveNext() method** - Contains your code as a switch statement over states
- **Local variables** - Hoisted to fields so they survive across awaits

```md
┌─────────────────────────────────────────┐
│  State Machine (compiler generated)     │
├─────────────────────────────────────────┤
│  state: int                             │
│  response: HttpResponseMessage          │
│  content: string                        │
├─────────────────────────────────────────┤
│  MoveNext()                             │
│    switch(state)                        │
│      case 0: start GetAsync...          │
│      case 1: start ReadAsStringAsync... │
│      case 2: return content             │
└─────────────────────────────────────────┘
```

> **Key insight:** No thread is blocked while waiting. The state machine registers a continuation
> with the task, and when the async operation completes, a thread pool thread resumes execution
> at the next state. See [There is no thread][4] for more details.

#### Awaiting Multiple Tasks

When you need to await multiple operations, TAP provides `Task.WhenAll` and `Task.WhenAny`:

```C#
var task1 = Task.Delay(TimeSpan.FromSeconds(5));
var task2 = Task.Delay(TimeSpan.FromSeconds(3));
var task3 = Task.Delay(TimeSpan.FromSeconds(6));

// WhenAll - waits for ALL tasks to complete
await Task.WhenAll(task1, task2, task3);  // Completes after ~6 seconds

// WhenAny - waits for the FIRST task to complete
await Task.WhenAny(task1, task2, task3);  // Completes after ~3 seconds
```

| Method | Completes When | Use Case |
| ------ | -------------- | -------- |
| `Task.WhenAll` | All tasks complete | Parallel fetches, batch operations |
| `Task.WhenAny` | First task completes | Timeout patterns, racing requests |

> **Note:** `WhenAny` returns when the first task completes, but the other tasks **continue running**.
> Remember to handle or cancel them appropriately.

## Scenarios

- [Cancelling Tasks](Documentation/CancellingTasks.md)
- [Merging Cancellation Tokens](Documentation/MergingCancellationTokens.md)
- [Thread Pool Starvation](Documentation/ThreadPoolStarvation.md)
- [IO Port Completion Starvation](Documentation/CompletionPortStarvation.md)
- [Context Switching Cost](Documentation/ContextSwitching.md)
- [Multiple Async](Documentation/MultipleAsync.md)

## Documentation

1. [Task asynchronous programming model - TAP][1]
2. [Asynchronous programming with async and await][2]
3. [The cost of context switches][3]
4. [There is no thread][4]
5. [Task vs ValueTask in C#][5]
6. [Task And Thread in C#][6]
7. [The managed thread pool][7]
8. [I/O completion ports][8]
9. [How Async/Await Really Works in C#][9]

[1]: https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/task-asynchronous-programming-model
[2]: https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/
[3]: https://devblogs.microsoft.com/premier-developer/the-cost-of-context-switches/
[4]: https://blog.stephencleary.com/2013/11/there-is-no-thread.html
[5]: https://adrianbailador.github.io/blog/37-taskvsvaluetask/
[6]: https://www.c-sharpcorner.com/article/task-and-thread-in-c-sharp/
[7]: https://learn.microsoft.com/en-us/dotnet/standard/threading/the-managed-thread-pool
[8]: https://learn.microsoft.com/en-us/windows/win32/fileio/i-o-completion-ports
[9]: https://devblogs.microsoft.com/dotnet/how-async-await-really-works/
