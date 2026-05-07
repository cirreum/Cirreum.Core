# Backlog

Deferred work for **Cirreum.Core**. Items here are tracked but not yet ready
to ship — either because the cost outweighs the benefit in isolation, or
because they're waiting on a forcing function (a related change, a consumer
upgrade, a coordinated multi-repo rollout).

## How this file works

- Each item is a `###` heading so it can be linked to and parsed.
- Each item declares **`SemVer:`** (`Patch` | `Minor` | `Major` | `Unspecified`),
  **`Trigger:`** (the human-readable condition that will make it ready), and
  **`Noted:`** (the date the item was added).
- The Cirreum DevOps release scripts (`PatchRelease`, `MinorRelease`,
  `MajorRelease`) surface items at-or-below the requested bump level so the
  operator can decide whether to fold them in before tagging.
- Items that ship: move from this file to `docs/CHANGELOG.md` under
  `[Unreleased]`. Items that grow into design discussions: promote to an ADR.

## Queued

### Document `IUserStateAccessor` lifetime / usage contract

- **SemVer:** Patch
- **Trigger:** Next Cirreum.Core patch release (no specific blocker)
- **Noted:** 2026-05-07

**Why:** The `IUserStateAccessor` interface XML doc currently says:

```csharp
/// <summary>
/// Defines the service for resolving the Cirreum.Security.IUserState
/// for the current user.
/// </summary>
public interface IUserStateAccessor {
    /// <summary>Gets the current user's state</summary>
    /// <returns>The current user's state</returns>
    ValueTask<IUserState> GetUser();
}
```

That documentation does not communicate the load-bearing lifetime invariants that consumers must respect:

- The service depends on an **active invocation context** (HTTP request,
  SignalR Hub method, WebSocket frame, queue trigger, etc.) populated by
  the host's invocation-source adapter at the inbound seam.
- It is intended to be called from **within an executing invocation** —
  typically from CQRS handlers, authorization evaluators, repositories,
  or other invocation-scoped framework code.
- The returned `IUserState` reflects the *current* invocation only;
  consumers must not capture it in singletons, static state, or
  fire-and-forget background work — analogous to the standard
  `IHttpContextAccessor.HttpContext` capture warning.
- When called outside an active invocation (background `IHostedService`,
  timer-driven work without a synthesized invocation), the result is an
  anonymous `IUserState`. This is a contract guarantee, not an
  implementation detail.

Consumers reading the interface today have no signal that any of these
constraints exist. The omission is especially harmful for the
"capture-and-stash" anti-pattern: the leak surface is silent until the
captured reference is read against an unrelated invocation.

Patch-eligible: docs-only change, no API surface or behavior change.
Could fold into any future Core patch; no specific blocking trigger.

### `IUserStateAccessor.GetUser()` → `GetUserState()`

- **SemVer:** Major
- **Trigger:** Bundle with next other Cirreum.Core breaking change
- **Noted:** 2026-05-07

**Why:** Naming inconsistency. The interface is `IUserStateAccessor` returning
`IUserState`, but the method verb says "GetUser". For naming honesty and
symmetry the method should be `GetUserState()`.

The cost of the rename in isolation is real:

- `Cirreum.Core` 6.0.0 (interface method rename = breaking → major bump)
- `MIGRATION-v6.md` required by the DevOps release scripts
- Every Cirreum framework package consuming `IUserStateAccessor` needs
  updates: `OperationContextFactory`, `DefaultAuthorizationEvaluator`,
  `DefaultRepository<T>`, all three `UserStateAccessor` variants
  (Server/Serverless/Wasm), tests
- Floor-version bumps cascade through every dependent package's `.csproj`
- Every app's call sites need updating

In isolation the readability win does not justify a Core major. Defer until
another Core breaker is queued so the migration cost amortizes across
multiple fixes; at that point the rename becomes a free-rider on the major
that's already happening.
