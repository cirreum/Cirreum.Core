
# Authorization Flow

> [!NOTE]
> **Read first:** [Authorization](README.md) — the user guide. This
> document is the request-flow companion: where the pipeline sits between
> HTTP and the handler, and how `Result.Fail(...)` propagates back to the
> client.

High-level view of how an authorized operation moves through Cirreum, from
HTTP entry to the three-stage authorization pipeline and back.

Authority comes from the **app-user layer** (`IOwnedApplicationUser`), not
from IdP claims. Tokens identify who is calling; Cirreum's own user store
decides what they can do.

```mermaid
sequenceDiagram
    participant C as Client
    participant HTTP as ASP.NET / Minimal API
    participant CT as IClaimsTransformer
    participant CN as Conductor Dispatcher
    participant AI as Authorization Intercept
    participant AE as IAuthorizationEvaluator
    participant GE as OperationGrantEvaluator
    participant RR as IAuthorizationRoleRegistry
    participant US as IUserStateAccessor

    Note over C,US: 1. Bootstrap (once)
    RR->>RR: Register roles + inheritance

    Note over C,US: 2. Per-request flow
    C->>HTTP: HTTP request (bearer token)
    HTTP->>CT: Enrich ClaimsPrincipal
    CT->>CT: Load IOwnedApplicationUser<br/>from store (cached)
    CT-->>HTTP: Enriched principal
    HTTP->>CN: Dispatch IOperation / IOperation[T]

    CN->>CN: Resolve handler + intercepts
    Note over CN: Typical path: 4 nested intercepts<br/>Validation ⟶ Authorization ⟶<br/>HandlerPerformance ⟶ QueryCaching ⟶ Handler<br/>(each wraps the next; handler at center)
    CN->>AI: Walk pipeline (cursor)

    AI->>US: GetUser() (cache read)
    US-->>AI: UserState
    AI->>AE: Evaluate(authorizableObject, userState)

    AE->>AE: Authn check<br/>(UnauthenticatedAccessException on fail)
    AE->>RR: Resolve effective roles
    RR-->>AE: Role hierarchy (expanded once)

    Note over AE,GE: Three-stage pipeline<br/>(see SEQUENCE.md)
    AE->>AE: Stage 1 — Scope (short-circuit)

    alt Object is Granted (Mutate / Lookup / Search / Self)
        AE->>GE: EvaluateAsync(authContext)
        Note over GE: Resolve OperationGrant<br/>(L1→L2→cold path)
        GE->>GE: Grant enforcement<br/>(Mutate / Lookup / Search / Self rules)
        GE-->>AE: ValidationResult
    end

    AE->>AE: Stage 1 cont. — Owner + Scope evaluators
    AE->>AE: Stage 2 — Object Authorizers (aggregate, then short-circuit)
    AE->>AE: Stage 3 — Policy (aggregate)

    alt All stages pass
        AE-->>AI: Result.Success
        AI->>AI: Continue pipeline → handler
        CN-->>HTTP: Result / Result[T]
        HTTP-->>C: 200 / 204
    else Not authenticated
        AE-->>AI: Result.Fail(UnauthenticatedAccessException)
        AI-->>CN: Short-circuit
        CN-->>HTTP: Result.Fail
        HTTP-->>C: 401 Unauthorized
    else Not authorized (any stage)
        AE-->>AI: Result.Fail(ForbiddenAccessException)
        AI-->>CN: Short-circuit
        CN-->>HTTP: Result.Fail
        HTTP-->>C: 403 Forbidden
    end
```

## Key Points

- **Identity vs. authority.** `IClaimsTransformer` runs once per principal
  to hydrate the app-user from the store. All authorization decisions use
  that app-user — never IdP claims directly.
- **Intercept placement.** Authorization runs after Validation but before
  the handler. The handler can assume a valid, authorized operation.
- **Grants (Stage 1 Step 0).** When an authorizable object implements a grant interface
  (`IOwnerMutateOperation`, `IOwnerLookupOperation`, `IOwnerSearchOperation`,
  `ISelfMutateOperation`, `ISelfLookupOperation`), the `OperationGrantEvaluator`
  resolves the caller's `OperationGrant` (owner-scoped) or performs identity
  matching (self-scoped) before any authorization constraint runs. If the
  object is not granted, this step is a no-op pass. See the
  [Grants README](Operations/Grants/README.md) for details.
- **Grant requirements are declarative.** `[RequiresGrant]` attributes declare
  the permissions an operation needs on the target owner. Stage 1 enforces
  them against the caller's grants. The resolved `PermissionSet` is also
  available on `AuthorizationContext.RequiredGrants` for *inspection* by
  Stage 2 object authorizers and Stage 3 policy validators — those stages
  do not enforce the attribute themselves.
- **Result, not exceptions.** Authentication/authorization failures are
  returned as `Result.Fail(...)` through the pipeline. Exceptions inside
  an evaluator are caught and converted at the pipeline boundary.
- **Effective roles are computed once** per evaluation and passed to every
  stage via `AuthorizationContext<TAuthorizableObject>`.
- **Short-circuit per stage.** Each stage denies the whole evaluation on
  failure; later stages don't run. Within Stage 2 and Stage 3, failures
  from *all* evaluators in that stage are aggregated before denial.
- **Object-level ACLs are separate.** The pipeline handles *request-time*
  authorization. For *data-time* checks on specific objects (folders, projects),
  handlers use `IResourceAccessEvaluator` after loading the data. See the
  [Resources README](Resources/README.md) for details.
