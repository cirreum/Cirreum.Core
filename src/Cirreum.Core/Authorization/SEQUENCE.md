# Authorization Pipeline — Detailed Sequence

Detailed view of the three-stage authorization pipeline executed by
`DefaultAuthorizationEvaluator`. For the high-level request flow showing
where this pipeline fits, see [FLOW.md](./FLOW.md).

```mermaid
sequenceDiagram
    participant AI as Authorization<br/>Intercept
    participant AE as DefaultAuthorization<br/>Evaluator
    participant RR as IAuthorizationRole<br/>Registry
    participant OE as OwnerScope<br/>Evaluator
    participant SE as IScopeEvaluator[]
    participant RA as IResourceAuthorizer[T]
    participant PV as IPolicyValidator[]

    AI->>AE: Evaluate(resource, operation)

    Note over AE: Preflight
    AE->>AE: Check operation.IsAuthenticated
    alt Not authenticated
        AE-->>AI: Result.Fail(Unauthenticated)
    end
    AE->>AE: Assert runtime type == compile-time type
    AE->>AE: Resolve evaluators from DI<br/>(scope, resource, policy arrays)
    AE->>AE: Short-circuit if all arrays empty<br/>and no owner-gate applies
    AE->>RR: GetRoleFromString(userRoles)
    RR-->>AE: Role[]
    alt No registered roles
        AE-->>AI: Result.Fail(Forbidden "no roles")
    end
    AE->>RR: GetEffectiveRoles(roles)
    RR-->>AE: Effective roles (inheritance expanded)
    AE->>AE: Build AuthorizationContext[TResource]

    rect rgba(80, 140, 240, 0.25)
    Note over AE,SE: Stage 1 — Scope (first-failure short-circuit)

    alt Resource is IAuthorizableOwnerScopedResource<br/>and OwnerScopeEvaluator registered
        AE->>OE: EvaluateAsync(authContext) — Step 0
        OE-->>AE: ValidationResult
        alt !IsValid
            AE-->>AI: Result.Fail(Forbidden)
        end
    end

    loop each IScopeEvaluator — Step 1
        AE->>SE: EvaluateAsync(authContext, ct)
        SE-->>AE: ValidationResult
        alt !IsValid
            AE-->>AI: Result.Fail(Forbidden)
        end
    end
    end

    rect rgba(240, 160, 60, 0.25)
    Note over AE,RA: Stage 2 — Resource (aggregate, then short-circuit)

    alt Multiple registered (misconfiguration)
        AE-->>AI: throw InvalidOperationException
    end
    alt Exactly one authorizer registered
        AE->>RA: ValidateAsync(validationContext, ct)
        RA-->>AE: ValidationResult
        AE->>AE: Accumulate failures
    end
    alt Any failures
        AE-->>AI: Result.Fail(Forbidden — aggregated)
    end
    end

    rect rgba(80, 200, 120, 0.25)
    Note over AE,PV: Stage 3 — Policy (aggregate)

    AE->>AE: Filter policies: SupportedRuntimeTypes<br/>+ AppliesTo(resource, runtime, timestamp),<br/>sorted by Order

    loop each applicable policy (sequential)
        AE->>PV: ValidateAsync(authContext, ct)
        PV-->>AE: ValidationResult
        alt !IsValid
            AE->>AE: Accumulate failures
        end
    end
    alt Any failures
        AE-->>AI: Result.Fail(Forbidden — aggregated)
    end
    end

    AE-->>AI: Result.Success
```

## Stage Semantics

| Stage | Purpose | Strategy | Short-circuit |
|---|---|---|---|
| **1 Step 0** — Owner gate | Enforce `OwnerId` presence + match for `IAuthorizableOwnerScopedResource` | First failure | Within Stage 1 |
| **1 Step 1** — Scope evaluators | Tenant / access-scope / ambient constraints | First failure, registration order | Within Stage 1 |
| **2** — Resource authorizer | Role and rule checks specific to this resource type | Single `ResourceAuthorizerBase<T>` per `T`; multiple FluentValidation rules aggregate within it | Stage 2 → Stage 3 |
| **3** — Policy validators | Cross-cutting runtime policies (hours, quotas, kill-switches) | Sequential by `Order`, aggregate within stage | End of pipeline |

## Why the Strategy Differs Per Stage

- **Stage 1 short-circuits aggressively** because scope failures ("wrong
  tenant", "not the owner") make every downstream check meaningless.
- **Stage 2 has a single authorizer per resource type** (by contract),
  but its FluentValidation rules aggregate all failures so developers
  see *every* denial at once (useful during dev/UI iteration). On
  denial, the pipeline **short-circuits** — policies (Stage 3) are
  irrelevant and often expensive (DB / external state) once
  resource-level access is denied.
- **Stage 3 aggregates** to report all failing policies together. Policy
  checks are typically the expensive ones, so by the time we run them
  we've already confirmed Stage 1 and Stage 2 passed; aggregating their
  failures gives callers the complete picture without extra cost.

## Allocation Notes

The hot path is engineered for minimal allocations:

- DI arrays from `GetService<IEnumerable<T>>()!` are cast, not copied.
- Effective roles are computed **once**.
- The failure list is lazily allocated — zero allocations on the
  authorized (happy) path.
- Policy filter + sort is a single-pass walk into a pre-sized `List`.
- Resource-authorizer tasks are stored in a pre-sized `Task[]`.
