# Cirreum.Core 5.3.0 — Delegation contract surface (D1)

Lands the L3 (Core) abstractions for M2M-on-behalf-of-human delegation — the framework's first-class equivalent of OAuth 2.0 Token Exchange (RFC 8693) for authentication schemes that don't (or can't) get on-behalf-of from an IdP. Strictly additive — no changes to existing types, properties, or behavior. The orchestrator, per-credential delegation configuration, M2M auth-handler integration, and runtime wiring all ship in subsequent coordinated releases across `Cirreum.AuthorizationProvider`, the per-scheme `Cirreum.Authorization.*` packages, `Cirreum.Services.Server`, and `Cirreum.Runtime.Authorization`.

This release alone exposes only the *observable surface* and the *app-facing authorization vocabulary*. Apps consuming `5.3.0` can read `userState.Actor` / `userState.IsDelegated`, write delegation-aware authorizers, and prepare their authorization model — even before the upstream orchestrator (which actually populates `Actor`) is wired in. When that lands in a follow-up cycle, the rules already authored against `5.3.0`'s vocabulary start enforcing automatically.

---

## Why this release exists

Cirreum apps targeting bank-grade scenarios commonly include flows where a non-human caller acts on behalf of a human:

- A bank's IVA (Interactive Voice Agent, typically Twilio-backed) authenticates as itself via ApiKey and answers the phone — but every call concerns a specific borrower whose data the IVA needs to read or mutate on their behalf.
- A 3rd-party LLM partner authenticates via SignedRequest and makes tool calls into the bank's API — same shape, different transport.
- An internal worker process authenticates as itself but needs to execute scheduled work in the audit context of the customer who triggered it.

OAuth 2.0 / RFC 8693 token exchange handles this for audience-based authentication (IdP-issued JWTs). For header-based M2M authentication (ApiKey, SignedRequest, etc.), there is no IdP to do token exchange at — these credentials are validated against the application's own credential store, not an external identity provider. Cirreum delegation is the in-app analog: M2M auth handlers extract delegation evidence presented alongside the wire credential, redeem it to resolve the subject's identity, validate per-credential policy, and synthesize an upgraded `IUserState` where the primary view is the subject (the human) while the actor (the M2M credential) is preserved for non-repudiable audit.

This release lands the data shapes that surface that upgraded state to consuming code. Authorization decisions, telemetry, audit code, and per-operation rules read the new surface; none of the orchestration that actually populates it ships here.

---

## What's new

### `IActorContext` + `ActorContext` (record)

Snapshot of the original M2M actor that initiated a delegated invocation, captured at the moment `IUserState` was upgraded to represent the subject. Aligned with RFC 8693's `act` claim model — actor identity always preserved alongside subject identity. Delegation, never impersonation.

```csharp
public interface IActorContext {
    string Id { get; }                      // actor's stable identifier (M2M credential id)
    string Name { get; }                    // display name for audit
    string Scheme { get; }                  // "ApiKey", "SignedRequest", etc.
    DelegationMetadata Delegation { get; }  // evidence type, scope, timestamp
}

public sealed record ActorContext(
    string Id,
    string Name,
    string Scheme,
    DelegationMetadata Delegation) : IActorContext;
```

`ActorContext` (concrete record) is what upstream M2M auth handlers construct and stamp into `IInvocationContext.Items` under `AuthenticationContextKeys.Actor`; downstream `UserStateAccessor` reads it and surfaces via `IUserState.Actor`.

### `DelegationMetadata` (record)

Captures the runtime metadata about a delegation upgrade — the evidence type that authorized it, the permission scope granted, and when the upgrade was applied:

```csharp
public sealed record DelegationMetadata(
    string EvidenceType,                    // "ivr-session", "phone-pin", etc.
    PermissionSet Scope,                    // intersection of subject/actor/policy scopes
    DateTimeOffset DelegatedAt);
```

Uses `PermissionSet` (the framework's native authorization vocabulary) rather than a parallel string-based scope concept. Authorization rules can directly compose `Scope` with operation-required permissions via standard `Contains` / `ContainsAny` / `ContainsAll` containment checks.

### `IUserState` additions

Two new properties on the foundational user-state contract, both default-implemented so existing `IUserState` implementations don't break:

```csharp
public interface IUserState : IUserSession {
    // ... existing members unchanged ...

    IActorContext? Actor => null;
    bool IsDelegated => this.Actor is not null;
}
```

`UserStateBase` adds a `protected virtual SetActor(IActorContext?)` setter — single-call (throws on second invocation), follows the existing `SetApplicationUser` / `SetAuthenticationBoundary` pattern. The nullable parameter records "delegation was processed, none applied" — distinct from "never processed," which `IsDelegationResolved == false` continues to indicate.

After a successful delegation upgrade:
- `userState.Id`, `Name`, `Principal`, `Profile`, `ApplicationUser` all represent the **subject** (the human the actor is acting for) — every authorization stage, every cache key, every audit log shifts to the subject automatically
- `userState.Actor` is the only surface exposing the original M2M caller
- `userState.IsDelegated` is the boolean predicate authorization rules read

Direct (non-delegated) invocations see `Actor = null` and `IsDelegated = false`. Every existing apps' code reading `userState.Profile.Roles` etc. continues to work without modification.

### `AuthenticationContextKeys.Actor`

Well-known string key for the cross-package transport contract:

```csharp
public const string Actor = "__Cirreum_Actor";
```

Upstream M2M auth handlers stamp the `IActorContext` under this key in `IInvocationContext.Items` / `IInvocationConnection.Items`; the server's `UserStateAccessor` reads it and calls `ServerUser.SetActor(...)`. Same pattern (and same intent) as the existing `AuthenticatedScheme` / `ApplicationUserCache` keys.

Sibling packages that can't take a direct reference on `Cirreum.Core` (per the established L3 sibling layering — `Cirreum.AuthorizationProvider` and its per-scheme implementations) follow the existing mirror-constant convention: a local copy of the literal with a comment pointing back to this canonical definition.

---

## Declarative authorization — 8 delegation attributes

Decorate operations directly to express delegation constraints in the type system. Enforced at Stage 1 Step 1 by a single framework-registered `DelegationConstraint`, before grant resolution, object authorizers, or policy validators run.

| Attribute | Semantics |
|---|---|
| `[RequiresDirectCaller]` | Fails when delegated. The "never via delegation" channel gate. |
| `[RequiresDelegation]` | Fails when not delegated. The "only via delegation" channel gate. |
| `[RequiresDelegationActor(...)]` | Requires delegation **AND** actor scheme matches one of N. |
| `[RequiresDelegationWithin(Minutes = N)]` | Requires delegation **AND** age ≤ N. Anti-replay / staleness. |
| `[RequiresDelegationEvidence(...)]` | Requires delegation **AND** evidence type matches one of N. |
| `[RequiresDelegationScope("feature", "operation")]` | Requires delegation **AND** `Scope.Contains(p)`. |
| `[RequiresAnyDelegationScope("f:o", ...)]` | Requires delegation **AND** `Scope.ContainsAny(p[])`. |
| `[RequiresAllDelegationScopes("f:o", ...)]` | Requires delegation **AND** `Scope.ContainsAll(p[])`. |

```csharp
[RequiresDelegationActor("SignedRequest")]
[RequiresDelegationWithin(Minutes = 5)]
[RequiresDelegationEvidence("ivr-session-validated", "voice-biometric-verified")]
[RequiresAnyDelegationScope("loans:read", "loans:summarize")]
public sealed record GetLoanSummary(string LoanId) : IAuthorizableOperation<LoanSummary>;
```

### Fail-closed by design

The six facet attributes (`RequiresDelegationActor`, `RequiresDelegationWithin`, `RequiresDelegationEvidence`, `RequiresDelegationScope`, `RequiresAnyDelegationScope`, `RequiresAllDelegationScopes`) all derive from `RequiresDelegationCheckAttribute`, which **self-enforces the delegation precondition before the facet runs**. Applying a facet attribute without an explicit `[RequiresDelegation]` companion does not silently fail-open against a direct caller — the operation fails with `DELEGATION_REQUIRED` first.

The framework reads this as: facet attributes are intersections with the requirement that delegation be present, never unions.

The framework's `DelegationConstraint` deduplicates `DELEGATION_REQUIRED` failures by error code, so a direct caller hitting an operation decorated with both `[RequiresDelegation]` and one or more facets sees a single denial, not one per facet.

### App-extensible

Apps add their own declarative delegation checks by subclassing `DelegationCheckAttribute` (for channel gates) or `RequiresDelegationCheckAttribute` (for facets that require delegation to be present). No constraint class to write, no DI registration — the framework's single registered constraint picks them up via `Type.GetCustomAttributes<DelegationCheckAttribute>(inherit: true)`.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class RequiresHighRiskApprovalAttribute : RequiresDelegationCheckAttribute {
    protected override ValidationFailure? CheckDelegated(IUserState userState, IActorContext actor) {
        return actor.Delegation.EvidenceType == "supervisor-approval"
            ? null
            : new ValidationFailure(nameof(RequiresHighRiskApprovalAttribute),
                "Operation requires supervisor-approval evidence.") { ErrorCode = "DELEGATION_NEEDS_SUPERVISOR" };
    }
}
```

---

## Imperative authorization — 8 validators + extensions + `AuthorizerBase` wrappers

Each declarative attribute has a paired Stage 2 validator for authorizers that need conditional or expression-based rules. Drop-in `PropertyValidator<T, IUserState>` implementations matching the existing `HasRoleValidator` / `HasClaimValidator` pattern:

| Validator | Semantics |
|---|---|
| `NotDelegatedValidator` | Passes when `!IsDelegated` |
| `DelegatedValidator` | Passes when `IsDelegated` (explicit "must be delegated" gate) |
| `HasDelegationActorValidator` | Passes for direct callers; for delegated, requires actor scheme matches one of N |
| `HasDelegationWithinValidator` | Passes for direct callers; for delegated, requires delegation age ≤ N |
| `HasDelegationEvidenceValidator` | Passes for direct callers; for delegated, requires evidence type matches one of N |
| `HasDelegationScopeValidator` | Passes for direct callers; for delegated, requires `Scope.Contains(p)` |
| `HasAnyDelegationScopeValidator` | Passes for direct callers; for delegated, requires `Scope.ContainsAny(p[])` |
| `HasAllDelegationScopesValidator` | Passes for direct callers; for delegated, requires `Scope.ContainsAll(p[])` |

**Short-circuit convention:** the six facet validators pass automatically for direct (non-delegated) callers — they constrain only the delegation channel. Apps that need "must be delegated AND fresh AND scoped" combine `Delegated()` with the others.

Note that the imperative-rule short-circuit is by design — authorizer rules are composable and conditional. The declarative attributes' fail-closed semantic is the safer default for the type-system-level contract; the rule-builder API is the place where authors compose intent explicitly.

### Extensions + wrappers

Matching extension methods on `IRuleBuilder<T, IUserState>` plus inline wrappers on `AuthorizerBase<T>` — same pattern the existing `HasRole` / `HasClaim` methods follow:

```csharp
// On IRuleBuilder<T, IUserState>:
ruleBuilder.NotDelegated();
ruleBuilder.Delegated();
ruleBuilder.HasDelegationActor("SignedRequest");
ruleBuilder.HasDelegationWithin(TimeSpan.FromMinutes(5));
ruleBuilder.HasDelegationEvidence("ivr-session-validated");
ruleBuilder.HasDelegationScope(new Permission("loans", "read"));
ruleBuilder.HasAnyDelegationScope(p1, p2);
ruleBuilder.HasAllDelegationScopes(p1, p2);

// On AuthorizerBase<T> — drops the RuleFor(...) boilerplate:
this.NotDelegated();
this.Delegated();
this.HasDelegationActor("ApiKey", "SignedRequest");
this.HasDelegationWithin(TimeSpan.FromMinutes(5));
this.HasDelegationEvidence("ivr-session-validated", "voice-biometric-verified");
this.HasDelegationScope(new Permission("loans", "read"));
this.HasAnyDelegationScope(p1, p2);
this.HasAllDelegationScopes(p1, p2);
```

### `WhenDelegated` / `UnlessDelegated` (AuthorizerBase)

Conditional rule blocks mirroring the existing `WhenRequiresGrant` / `UnlessRequiresGrant` pattern. Both return `IConditionBuilder` for `.Otherwise(...)` chaining:

```csharp
this.WhenDelegated(() => {
    this.HasDelegationActor("SignedRequest");
    this.HasDelegationWithin(TimeSpan.FromMinutes(5));
    this.HasDelegationScope(new Permission("loans", "read"));
})
.Otherwise(() => {
    this.HasRole(Roles.LoanOfficer);
});
```

Reads like a policy specification — "when delegated, enforce these constraints; otherwise, use the standard role gate."

---

## App-facing usage

### Declarative (attribute) style

```csharp
[RequiresDirectCaller]
public sealed record InitiateWireTransfer(...) : IAuthorizableCommand;

[RequiresDelegation]
[RequiresDelegationEvidence("ivr-session-validated", "voice-biometric-verified")]
[RequiresDelegationWithin(Minutes = 2)]
[RequiresAnyDelegationScope("iva:account-inspect", "iva:balance-inquiry")]
public sealed record IvaToolCall(...) : IAuthorizableQuery<ToolCallResult>;
```

### Imperative (authorizer) style

```csharp
public sealed class GetAccountBalanceAuthorizer : AuthorizerBase<GetAccountBalanceQuery> {
    public GetAccountBalanceAuthorizer() {

        // Direct callers: standard role-based gate
        this.UnlessDelegated(() => {
            this.HasAnyRole(Roles.AccountOwner, Roles.SupportAgent);
        });

        // Delegated invocations: stricter — only via SignedRequest, fresh, scope-bounded
        this.WhenDelegated(() => {
            this.HasDelegationActor("SignedRequest");
            this.HasDelegationWithin(TimeSpan.FromMinutes(5));
            this.HasDelegationScope(AccountPermissions.Balance);
        });
    }
}

public sealed class InitiateWireTransferAuthorizer : AuthorizerBase<InitiateWireTransferCommand> {
    public InitiateWireTransferAuthorizer() {

        // Never via delegation, regardless of scope or evidence
        this.NotDelegated();
        this.HasRole(Roles.WireTransferInitiator);
    }
}

public sealed class IvaToolCallAuthorizer : AuthorizerBase<IvaToolCommand> {
    public IvaToolCallAuthorizer() {

        // Only invoked through delegation, with strong evidence and narrow scope
        this.Delegated();
        this.HasDelegationEvidence("ivr-session-validated", "voice-biometric-verified");
        this.HasDelegationWithin(TimeSpan.FromMinutes(2));
        this.HasAnyDelegationScope(
            IvaPermissions.AccountInspect,
            IvaPermissions.BalanceInquiry);
    }
}
```

Authorizer authors get a vocabulary that reads as compliance language. Future `Cirreum.Introspection`-driven security analysis will surface these as structured rule trees in audit reports — recognizable extension calls show up as named nodes; opaque `Must(lambda)` expressions don't.

---

## Audit + telemetry — delegation context surfaces everywhere

Every authorization log line and OpenTelemetry decision tag now carries delegation context when present. Non-delegated requests pay zero allocation (a `DelegationLogContext.None` sentinel struct short-circuits the suffix path).

### Log message suffix

```
User '{UserName}' was DENIED access to '{ObjectName}'. (delegated via {Actor.Name}/{Actor.Scheme}, evidence={Actor.Delegation.EvidenceType})
{DeniedReason}
```

Three log methods updated in `AuthorizationLogging` and two in `ResourceAccessLogging` — same `DelegationSuffix` field threaded through `LogAuthorizingAllowed` / `LogAuthorizingDenied` / `LogResourceAccessAllowed` / `LogResourceAccessDenied`.

### OpenTelemetry tags

```
cirreum.authz.delegation.is_delegated       (bool)
cirreum.authz.delegation.actor_scheme       (string)
cirreum.authz.delegation.evidence_type      (string)
```

`AuthorizationTelemetry.RecordDecision(...)` accepts the three new dimensions; emitted by both `DefaultAuthorizationEvaluator` (per-operation decisions) and `OperationGrantEvaluator` (per-grant resolution).

The combined surface — per-attribute error codes (`DELEGATION_REQUIRED`, `DELEGATION_DENIED`, `DELEGATION_STALE`, `DELEGATION_ACTOR_NOT_ALLOWED`, `DELEGATION_EVIDENCE_NOT_ALLOWED`, `DELEGATION_SCOPE_MISSING`, `DELEGATION_SCOPE_MISSING_ANY`, `DELEGATION_SCOPE_MISSING_ALL`) plus structured telemetry tags plus log suffixes — gives operators a clean delegation observability story without any custom instrumentation in app code.

---

## The cross-cycle composition

This is the first of a coordinated multi-package release sequence:

| Cycle | Package | Layer | Adds |
|---|---|---|---|
| **D1 (this release)** | `Cirreum.Core` | L3 | Contracts + UserState surface + authorization vocabulary + audit/telemetry expansion |
| **D2** | `Cirreum.AuthorizationProvider` | L3 sibling | Orchestrator interface, evidence resolver / policy provider contracts, evidence family pattern, per-credential `DelegationConfiguration` |
| **D3** | `Cirreum.Services.Server` | L4 | `UserStateAccessor` reads `Items[AuthenticationContextKeys.Actor]`, calls `ServerUser.SetActor(...)` |
| **D4** | `Cirreum.Authorization.ApiKey`, `Cirreum.Authorization.SignedRequest` | L3 impls | Per-scheme auth handler integration — extract evidence, invoke orchestrator, stamp Items + swap principal |
| **D5** | `Cirreum.Runtime.Authorization` | L5 | App-facing `AddDelegation<TPolicy>(e => ...)` builder + DI wiring |

Each cycle is independently shippable — adopting `5.3.0` produces no observable change in app behavior, but apps that author authorizers against the new vocabulary are forward-compatible with every downstream cycle as it lands.

---

## Architectural alignment

| RFC 8693 / OAuth 2.0 concept | Cirreum delegation equivalent |
|---|---|
| `client_credentials` token (machine actor) | M2M wire credential (ApiKey, SignedRequest) |
| `subject_token` (evidence of the human) | `DelegationEvidence` (D2) — IVR session, opaque redeemable token, etc. |
| Per-OAuth-client allowed scopes (registered at IdP) | Per-credential `DelegationConfiguration.AllowedScopes` (D2) |
| `may_act` claim (which actors may represent which subjects) | `IDelegationPolicyProvider` (D2) |
| Token exchange endpoint at IdP | `IDelegationOrchestrator` (D2), in-app |
| Resulting JWT with `sub` + `act` claim | `IUserState` with subject as primary view + `Actor` populated |
| Scope intersection (subject ∩ actor ∩ delegated) | `DelegationMetadata.Scope` (`PermissionSet`) |

Cirreum delegation **complements rather than replaces** IdP-managed OBO. For audience-based authentication where the IdP supports token exchange (Entra OBO, etc.), apps continue using that path — the resulting JWT's `act` claim will be surfaced on `IUserState.Actor` natively by a follow-up audience-track integration. Cirreum delegation fills the gap for the M2M / header-based schemes where no IdP is involved and the orchestration must happen in-app against the application's own credential store.

---

## Identity mental model post-upgrade

M2M-with-delegation produces a synthetic "as-if audience-authenticated" `IUserState` for the subject. The end state is indistinguishable from a direct audience sign-in for the subject, except `Actor` carries the M2M caller as audit metadata:

| Property | Direct audience auth (borrower signs into portal) | M2M with delegation (Twilio acts for borrower) |
|---|---|---|
| `Id` | Borrower's stable identifier | Borrower's stable identifier |
| `Principal` | Borrower's claims from IdP | Borrower's claims (synthesized by evidence resolver) |
| `Profile.Roles` | Claim roles + app-user roles merged | Same merge — subject's principal flows through standard claims pipeline |
| `ApplicationUser` | Borrower's `IApplicationUser` via `IApplicationUserResolver` | Same `IApplicationUser` via same resolver (per-scheme dispatch from 5.0.1) |
| `Actor` | `null` | Twilio's `IActorContext` |
| `IsDelegated` | `false` | `true` |

Authorization decisions, data access, and handler logic all see the subject. The only delta is `IsDelegated == true` and the audit trail showing who initiated the request. Existing apps consuming `5.3.0` keep working unchanged; delegation-aware logic opts in by reading `userState.Actor` or authoring rules using the new convenience extensions.

---

## Compatibility

- **Strictly additive.** No existing types, properties, methods, or behaviors changed. Source-compatible with `5.2.x`.
- **Default interface implementations** for `IUserState.Actor` and `IsDelegated` return `null` / `false` — existing `IUserState` implementations need no modification.
- **`UserStateBase.SetActor(...)` is single-call** — second invocation throws `InvalidOperationException`. Multi-hop delegation chains (actor-of-actor) are deferred to a future release if real demand emerges.
- **Imperative validators short-circuit to pass for direct callers** — apps that adopt the new convenience rules in authorizers see no behavioral change on direct (non-delegated) invocations.
- **Declarative facet attributes fail-closed** — `[RequiresDelegation*]` attributes always fail with `DELEGATION_REQUIRED` for direct callers, even without an explicit `[RequiresDelegation]` companion. This is intentional and is the security-correct default for type-level declarations.
- **No new dependencies.** All new types use existing `Cirreum.Authorization` types (`Permission`, `PermissionSet`) and `System.*`.

---

## See also

- `CHANGELOG.md` — condensed change list for `5.3.0`.
- `RELEASE-NOTES-v5.0.1.md` — per-scheme `IApplicationUserResolver` dispatch (the foundation that makes subject-side resolution work cleanly post-upgrade).
- `Authorization/README.md` — full authorization model reference; the new delegation rules slot in alongside roles / claims / grants.
- Follow-up releases — `Cirreum.AuthorizationProvider` (D2), `Cirreum.Services.Server` (D3), `Cirreum.Authorization.ApiKey` / `Cirreum.Authorization.SignedRequest` (D4), `Cirreum.Runtime.Authorization` (D5).
