# SoundWave Logging Rules & Manifesto

> This document defines the team's structured logging standards for the SoundWave project.
> It is the single source of truth for logging decisions. When in doubt, refer here.

---

## 1. Log Levels — When to Use Each

| Level | Use When |
|---|---|
| `Trace` | Extremely detailed internal flow — rarely used, never in production |
| `Debug` | Developer-relevant state changes (notification dispatched, cache miss), disabled in prod |
| `Information` | Normal business flow milestones (user registered, email enqueued, handler started/finished) |
| `Warning` | Business rule rejections or expected-but-notable conditions (email already exists, auth failure) |
| `Error` | Unexpected failures that require investigation (unhandled exceptions, DB errors, SMTP failures) |
| `Fatal` | Application cannot start or is in an unrecoverable state |

> [!IMPORTANT]
> **Never use `Information` for things that happen hundreds of times per request** (e.g., per-entity loops).
> Use `Debug` or avoid logging entirely for hot paths.

---

## 2. Structured Logging — Always Use Message Templates

✅ **Correct** — Structured properties, searchable in Seq:
```csharp
logger.LogInformation("User {UserId} registered successfully", userId);
logger.LogWarning("Registration rejected — email {Email} already exists", email);
```

❌ **Wrong** — String interpolation, not searchable:
```csharp
logger.LogInformation($"User {userId} registered successfully");
logger.LogInformation("User " + userId + " registered successfully");
```

> [!TIP]
> Named properties in `{}` become first-class structured fields in Seq, enabling fast filtering like `UserId = "abc-123"`.

---

## 3. Sensitive Data — What NEVER to Log

| Data | Rule |
|---|---|
| Passwords / hashes | ❌ Never |
| JWT / refresh tokens | ❌ Never |
| Full credit card / payment info | ❌ Never |
| Email addresses | ⚠️ Only at `Debug` in handlers; `Warning` for rejections is acceptable |
| User IDs (GUIDs) | ✅ Safe to log — non-reversible identifier |
| Request payloads | ⚠️ Never log full payloads unless in a special debug build — can contain PII |

---

## 4. Layer Responsibility — Where to Log What

### HTTP Layer (`UseSerilogRequestLogging`)
- **Already handled automatically** by Serilog's request logging middleware
- Logs: method, path, status code, elapsed time for every HTTP request
- ✅ **Do NOT add additional HTTP-level logging** in endpoint handlers — it's redundant

### MediatR Layer (`LoggingBehavior`)
- **Already handled automatically** by `LoggingBehavior<TRequest, TResponse>`
- Logs: handler entry (`Handling {RequestName}`), exit with elapsed ms, exceptions
- ✅ **Do NOT re-log entry/exit inside handlers** — it's already covered

### Business Logic Layer (Command/Query Handlers)
- Log **business decisions** only:
  - Business rule rejections → `Warning`
  - Successful state changes → `Information`
  - Side effects dispatched (events, notifications) → `Debug`
- Example:
  ```csharp
  logger.LogWarning("Registration rejected — email {Email} already exists", email);
  logger.LogInformation("User {UserId} registered successfully", userId);
  logger.LogDebug("UserRegisteredNotification published for {UserId}", userId);
  ```

### Notification/Event Handlers
- Log when a side effect is **enqueued or dispatched**:
  ```csharp
  logger.LogInformation("Welcome email job enqueued for {ToEmail}", notification.Email);
  ```

### Background Jobs (`SendEmailJob`, etc.)
- Log **start**, **success**, and **failure** of each job:
  ```csharp
  logger.LogInformation("Executing SendEmailJob for {ToEmail}", request.ToEmail);
  logger.LogInformation("SendEmailJob finished successfully for {ToEmail}", request.ToEmail);
  logger.LogError(ex, "SendEmailJob failed for {ToEmail}", request.ToEmail);
  ```

### Infrastructure Services (`EmailService`, `CachingService`, etc.)
- Log **significant operations** and **failures**:
  - Template resolved → `Debug`
  - Email sent → `Information`
  - Connection errors → `Error`

---

## 5. What NOT to Duplicate

| Scenario | Already Logged By |
|---|---|
| HTTP request start/end | `UseSerilogRequestLogging()` |
| Handler entry/exit/elapsed | `LoggingBehavior` |
| Unhandled exceptions with stack trace | `GlobalExceptionHandlerMiddleware` |
| Validation failures | `GlobalExceptionHandlerMiddleware` + `ValidationBehavior` |

> [!WARNING]
> Adding duplicate log statements inflates log volume and adds noise to Seq. If in doubt, check the layer table above.

---

## 6. Naming Conventions — Log Property Names

Use **PascalCase** for all log property names to maintain consistency across the project:

| Property | Example |
|---|---|
| `UserId` | `{UserId}` |
| `Email` | `{Email}` |
| `RequestName` | `{RequestName}` |
| `ElapsedMs` | `{ElapsedMs}` |
| `ToEmail` | `{ToEmail}` |
| `JobId` | `{JobId}` |
| `Template` | `{Template}` |
| `StatusCode` | `{StatusCode}` |

---

## 7. Performance — Avoiding Expensive Logging

For any log that requires non-trivial object construction or serialization, guard it with `IsEnabled`:

```csharp
// Only construct the expensive object if Debug is actually enabled
if (logger.IsEnabled(LogLevel.Debug))
{
    var snapshot = JsonSerializer.Serialize(someObject);
    logger.LogDebug("Object state: {Snapshot}", snapshot);
}
```

---

## 8. Enrichers Active on Every Log Event

The following context is automatically attached to every log event via `appsettings.json`:

| Enricher | Property Added |
|---|---|
| `FromLogContext` | Any properties pushed via `LogContext.PushProperty(...)` |
| `WithMachineName` | `MachineName` |
| `WithThreadId` | `ThreadId` |
| `WithClientIp` | `ClientIp` |
| `Application` property | `Application = "SoundWave"` |

---

## 9. Sinks Configuration

| Environment | Sinks Active |
|---|---|
| Development (`WriteToSeq: true`) | Console + Seq (http://localhost:5341) |
| Production (`WriteToSeq: false`) | Console + File (logs/log-{Date}.txt, rolling daily) |

> [!TIP]
> In development, always run Seq via Docker: `docker run -e ACCEPT_EULA=Y -p 5341:5341 -p 80:80 datalust/seq`
