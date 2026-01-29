# Context Switching Cost

This scenario demonstrates the performance cost of different execution models by comparing synchronous, async, and thread-based approaches.

## Overview

Context switching occurs when the CPU switches from executing one thread to another. This involves:

- Saving the current thread's state (registers, stack pointer)
- Loading the new thread's state
- Potential cache invalidation

Each switch has a cost, and this scenario measures that cost across different patterns.

## Scenarios Compared

### 1. No-switch (Synchronous)

```C#
static void DoSync()
{
    var workRemaining = workSize;
    while (--workRemaining >= 0)
    {
        WorkUnit();
    }
}
```

**Baseline** - Pure synchronous execution with no context switches. All work runs on a single thread continuously.

### 2. Async without Yield

```C#
static async Task NoYieldHelper(Task task)
{
    WorkUnit();
    await task;  // Task is already completed
}
```

Uses `async/await` but the awaited task is **already completed**. The state machine runs synchronously without yielding control, so no actual context switch occurs.

### 3. Async with Yield

```C#
async delegate
{
    while (--workRemaining >= 0)
    {
        WorkUnit();
        await Task.Yield();  // Forces a reschedule
    }
}
```

`Task.Yield()` forces the task to re-queue itself on the thread pool after each unit of work. This causes **scheduler overhead** but not necessarily a full thread switch.

### 4. Thread Switches

```C#
void worker()
{
    while (workRemaining > 0)
    {
        evt.WaitOne();       // Block until signaled
        workRemaining--;
        WorkUnit();
        evt.Set();           // Signal next thread
    }
}
```

Multiple threads compete for work using an `AutoResetEvent`. Each `WaitOne()`/`Set()` cycle causes an **actual OS-level context switch** between threads.

## Expected Results

| Scenario | Relative Cost | Why |
| -------- | ------------- | --- |
| No-switch | Fastest | No overhead |
| Async w/o yield | ~Same as sync | State machine runs synchronously |
| Async w/ yield | Moderate | Scheduler overhead, re-queuing |
| Thread switches | Slowest | Full OS context switches |

## Key Takeaways

1. **Async is not free** - Even without thread switches, `Task.Yield()` adds scheduler overhead
2. **Thread switches are expensive** - OS-level context switches are orders of magnitude slower
3. **Completed tasks are cheap** - Awaiting an already-completed task has minimal overhead
4. **Prefer async over threads** - For I/O-bound work, async avoids the heavy cost of thread switches
