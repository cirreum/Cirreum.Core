
# Authorization Flow

High-level view of how an authorized request moves through Cirreum, from
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
    participant GE as GrantEvaluator
    participant RR as IAuthorizationRoleRegistry
    participant US as IUserStateAccessor

    Note over C,US: 1. Bootstrap (once)
    RR->>RR: Register roles + inheritance

    Note over C,US: 2. Per-request flow
    C->>HTTP: HTTP request (bearer token)
    HTTP->>CT: Enrich ClaimsPrincipal
    CT->>CT: Load IOwnedApplicationUser<br/>from store (cached)
    CT-->>HTTP: Enriched principal
    HTTP->>CN: Dispatch IRequest / IRequest[T]

    CN->>CN: Resolve handler + intercepts
    Note over CN: Typical path: 4 nested intercepts<br/>Validation ⟶ Authorization ⟶<br/>HandlerPerformance ⟶ QueryCaching ⟶ Handler<br/>(each wraps the next; handler at center)
    CN->>AI: Walk pipeline (cursor)

    AI->>US: GetUser() (cache read)
    US-->>AI: UserState
    AI->>AE: Evaluate(resource, operation)

    AE->>AE: Authn check<br/>(UnauthenticatedAccessException on fail)
    AE->>RR: Resolve effective roles
    RR-->>AE: Role hierarchy (expanded once)

    Note over AE,GE: Three-stage pipeline<br/>(see SEQUENCE.md)
    AE->>AE: Stage 1 — Scope (short-circuit)

    alt Resource is Granted (Mutate / Lookup / Search / Self)
        AE->>GE: EvaluateAsync(authContext)
        Note over GE: Resolve AccessGrant<br/>(L1→L2→cold path)
        GE->>GE: Grant enforcement<br/>(Mutate / Lookup / Search / Self rules)
        GE-->>AE: ValidationResult
    end

    AE->>AE: Stage 1 cont. — Owner + Scope evaluators
    AE->>AE: Stage 2 — Resource (aggregate, then short-circuit)
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
  the handler. The handler can assume a valid, authorized request.
- **Grants (Stage 1 Step 0).** When a resource implements a grant interface
  (`IGrantMutateRequest`, `IGrantLookupRequest`, `IGrantSearchRequest`,
  `IGrantMutateSelfRequest`, `IGrantLookupSelfRequest`), the `GrantEvaluator`
  resolves the caller's `AccessGrant` (owner-scoped) or performs identity
  matching (self-scoped) before any other scope evaluator runs. If the
  resource is not granted, this step is a no-op pass. See the
  [Grants README](Grants/README.md) for details.
- **Permissions are general-purpose.** `[RequiresPermission]` attributes are
  resolved for all authorizable resources — not just granted ones. The
  resolved `PermissionSet` is available on `AuthorizationContext.Permissions`
  for use by resource authorizers (Stage 2) and policy validators (Stage 3).
- **Result, not exceptions.** Authentication/authorization failures are
  returned as `Result.Fail(...)` through the pipeline. Exceptions inside
  an evaluator are caught and converted at the pipeline boundary.
- **Effective roles are computed once** per evaluation and passed to every
  stage via `AuthorizationContext<TResource>`.
- **Short-circuit per stage.** Each stage denies the whole evaluation on
  failure; later stages don't run. Within Stage 2 and Stage 3, failures
  from *all* evaluators in that stage are aggregated before denial.
