# Merging Cancellation Tokens

This scenario demonstrates how to combine multiple `CancellationToken` sources into a single token using `CreateLinkedTokenSource`.

## Overview

Sometimes you need to cancel an operation when **any one of multiple conditions** is met:

- A user-provided token is cancelled
- A timeout expires
- An external event occurs

`CancellationTokenSource.CreateLinkedTokenSource` creates a composite token that cancels when **any** of its source tokens cancel.

## What the Code Does

### 1. Receive an External Token

```C#
private static async Task DoOperationAsync(CancellationToken cancellationToken)
```

The method receives a token from the caller (e.g., `HttpContext.RequestAborted` in ASP.NET).

### 2. Create a Timeout Token

```C#
var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
```

Creates a token that automatically cancels after 3 seconds.

### 3. Merge the Tokens

```C#
var mergedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
    cancellationToken, 
    timeoutCancellationTokenSource.Token);
```

The merged token will cancel when **either**:

- The original `cancellationToken` is cancelled, **OR**
- The 3-second timeout expires

### 4. Use the Merged Token

```C#
await Task.Delay(TimeSpan.FromSeconds(5), mergedTokenSource.Token);
```

The 5-second delay uses the merged token. Since the timeout is 3 seconds, it will cancel before completion.

## Expected Output

```text
Task cancelled successfully
```

The task cancels at 3 seconds (timeout) even though the original token was never explicitly cancelled.

## Use Cases

| Scenario | Tokens to Merge |
| -------- | --------------- |
| HTTP request with timeout | `HttpContext.RequestAborted` + timeout token |
| User cancel + deadline | User action token + absolute deadline token |
| Parent + child operation | Parent scope token + operation-specific token |

## Key Takeaways

1. **Linked tokens cancel on any source** - First cancellation wins
2. **Dispose the linked source** - `CreateLinkedTokenSource` allocates resources; dispose when done
3. **Common in ASP.NET** - Combine request-scoped tokens with operation timeouts

```C#
// Best practice: dispose the linked source
using var mergedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
    cancellationToken, 
    timeoutToken);
```
