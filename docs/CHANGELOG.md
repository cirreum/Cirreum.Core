# Cirreum.Core Changelog

All notable changes to **Cirreum.Core** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

### Added

- **`Cirreum.Security.IActorContext`** — interface capturing the original M2M actor that initiated a delegated invocation. Read by audit, telemetry, and delegation-aware authorization rules. Surfaces actor identifier, display name, authentication scheme, and the delegation metadata.
- **`Cirreum.Security.ActorContext`** — sealed concrete record implementing `IActorContext`. Constructed by upstream M2M auth handlers at delegation time and stamped into `IInvocationContext.Items` under `AuthenticationContextKeys.Actor`.
- **`Cirreum.Security.DelegationMetadata`** — sealed record capturing runtime metadata about a delegation upgrade: the evidence type that authorized it, the permission scope granted (as the framework's native `PermissionSet`), and the timestamp it was applied. Aligned with RFC 8693 Token Exchange semantics; uses `PermissionSet` rather than a parallel string-scope concept so the authorization pipeline can consume it directly.
- **`Cirreum.Security.IUserState.Actor`** — new default-implemented property exposing the captured `IActorContext` when this user state represents a delegated identity, or `null` when it does not. Existing `IUserState` implementations need no modification; the default impl returns `null`.
- **`Cirreum.Security.IUserState.IsDelegated`** — boolean convenience property equivalent to `Actor is not null`. Provided for authorization rules and audit predicates that need to distinguish delegated from direct invocations.
- **`Cirreum.Security.IUserState.IsDelegationResolved`** — boolean default-implemented property distinguishing "delegation processed but none applied" from "never processed." Default impl returns `false`; `UserStateBase` flips it once `SetActor` runs.
- **`Cirreum.Security.UserStateBase.SetActor(IActorContext?)`** — protected virtual setter following the existing `SetApplicationUser` / `SetAuthenticationBoundary` pattern. Nullable parameter records "delegation was processed, none applied." Single-call (throws `InvalidOperationException` on second invocation — delegation chains are not supported in v1). Called by per-host derived `IUserState` implementations after upstream M2M auth handlers populate the actor info in invocation items.
- **`Cirreum.Security.AuthenticationContextKeys.Actor`** — well-known string constant (`"__Cirreum_Actor"`) for the cross-package transport contract. Upstream M2M auth handlers stamp `IActorContext` under this key in `IInvocationContext.Items` / `IInvocationConnection.Items`; the server's `UserStateAccessor` reads it and calls `ServerUser.SetActor(...)`. Sibling packages that cannot take a direct reference on Cirreum.Core mirror this literal following the established convention.
- **`Cirreum.Authorization.DelegationCheckAttribute`** — abstract base for declarative delegation rules. Each derived attribute carries both its declarative metadata and its `Check(IUserState)` enforcement logic (DataAnnotations-style). A single framework-registered `DelegationConstraint` enumerates all derivatives applied to an operation in one per-request pass.
- **`Cirreum.Authorization.RequiresDelegationCheckAttribute`** — intermediate abstract base for delegation facet attributes. Self-enforces the "delegation is mandatory" precondition (returns `DELEGATION_REQUIRED` for direct callers) before invoking the derivative's `CheckDelegated(IUserState, IActorContext)` template method. Closes the fail-open hole where a facet attribute applied without an explicit `[RequiresDelegation]` companion would silently pass for direct callers.
- **Eight new declarative attributes** in `Cirreum.Authorization`: `[RequiresDirectCaller]`, `[RequiresDelegation]`, `[RequiresDelegationActor(params string[])]`, `[RequiresDelegationWithin(Seconds/Minutes/Hours)]`, `[RequiresDelegationEvidence(params string[])]`, `[RequiresDelegationScope(feature, operation)]`, `[RequiresAnyDelegationScope(params string[])]`, `[RequiresAllDelegationScopes(params string[])]`. Decorate `IAuthorizableOperation` types directly to express delegation constraints in the type system. The six facet attributes derive from `RequiresDelegationCheckAttribute` and fail-closed for direct callers; the two channel-gate attributes (`RequiresDirectCaller` / `RequiresDelegation`) derive directly from `DelegationCheckAttribute`. Auto-registered via `AssemblyScanner`.
- **`Cirreum.Authorization.Constraints.DelegationConstraint`** — single Stage 1 Step 1 `IAuthorizationConstraint` that discovers and invokes every `DelegationCheckAttribute` on an operation. Caches per-operation-type attribute arrays in a `ConcurrentDictionary` (mirrors the existing `RequiredGrantCache` strategy); zero-allocation hot path for operations with no delegation attributes. Deduplicates `DELEGATION_REQUIRED` failures by error code so a direct caller sees a single denial regardless of how many facet attributes are on the operation.
- **Eight new `PropertyValidator<T, IUserState>` implementations** in `Cirreum.Authorization.Validators`: `NotDelegatedValidator`, `DelegatedValidator`, `HasDelegationActorValidator`, `HasDelegationWithinValidator`, `HasDelegationEvidenceValidator`, `HasDelegationScopeValidator`, `HasAnyDelegationScopeValidator`, `HasAllDelegationScopesValidator`. Mirror the existing `HasRoleValidator` / `HasClaimValidator` shape. The six facet validators short-circuit to pass for direct (non-delegated) callers; `Delegated` / `NotDelegated` are the explicit channel gates.
- **Eight new extension methods** on `IRuleBuilder<T, IUserState>` in `AuthorizationValidatorExtensions`: `NotDelegated()`, `Delegated()`, `HasDelegationActor(params string[])`, `HasDelegationWithin(TimeSpan)`, `HasDelegationEvidence(params string[])`, `HasDelegationScope(Permission)`, `HasAnyDelegationScope(params Permission[])`, `HasAllDelegationScopes(params Permission[])`.
- **Ten new protected methods on `AuthorizerBase<T>`** — eight inline rule wrappers (`NotDelegated`, `Delegated`, `HasDelegationActor`, `HasDelegationWithin`, `HasDelegationEvidence`, `HasDelegationScope`, `HasAnyDelegationScope`, `HasAllDelegationScopes`) following the existing `HasRole` / `HasClaim` convention, plus two conditional rule blocks (`WhenDelegated(Action)`, `UnlessDelegated(Action)`) returning `IConditionBuilder` for `.Otherwise(...)` chaining, mirroring the existing `WhenRequiresGrant` / `UnlessRequiresGrant` pattern.

### Changed

- **`Cirreum.Authorization.AuthorizationTelemetry.RecordDecision(...)`** — extended with `bool? isDelegated`, `string? actorScheme`, `string? evidenceType` parameters. Emits three new OpenTelemetry tag dimensions: `cirreum.authz.delegation.is_delegated`, `cirreum.authz.delegation.actor_scheme`, `cirreum.authz.delegation.evidence_type`. Non-delegated requests pay zero allocation; tags are only set when delegation context is present.
- **`Cirreum.Authorization.Diagnostics.AuthorizationLogging`** — three log methods (`LogAuthorizingAllowed`, `LogAuthorizingDenied`, `LogAuthorizingException`) extended with `delegationSuffix`, `actorId`, `actorScheme`, `evidenceType` optional parameters. Templates updated to interpolate `{DelegationSuffix}` so audit lines carry actor identity, scheme, and evidence type when present.
- **`Cirreum.Authorization.Resources.ResourceAccessLogging`** — two log methods (`LogResourceAccessAllowed`, `LogResourceAccessDenied`) extended with the same delegation-context fields and template interpolation.
- **`Cirreum.Authorization.DefaultAuthorizationEvaluator`** and **`Cirreum.Authorization.Operations.Grants.OperationGrantEvaluator`** — threaded the new `DelegationLogContext` through every audit + telemetry call site. `Cirreum.Authorization.Resources.ResourceAccessEvaluator` follows the same pattern. Internal change; no public surface impact beyond the log/telemetry messages now carrying delegation fields.

These additions land the L3 (Core) contracts and app-facing authorization vocabulary for the framework's M2M-on-behalf-of-human delegation model — Cirreum's in-app analog of OAuth 2.0 Token Exchange (RFC 8693) for header-based authentication schemes that don't (or can't) get on-behalf-of from an IdP. Strictly additive; no existing behavior changes. The orchestrator, evidence-resolver / policy-provider contracts, per-credential delegation configuration, M2M auth-handler integration, and runtime wiring ship in subsequent coordinated releases across `Cirreum.AuthorizationProvider`, the per-scheme `Cirreum.Authorization.*` packages, `Cirreum.Services.Server`, and `Cirreum.Runtime.Authorization`. Apps consuming `5.3.0` can author delegation-aware authorizers against the new vocabulary today; rules begin enforcing automatically as downstream cycles land. See [`docs/RELEASE-NOTES-v5.3.0.md`](RELEASE-NOTES-v5.3.0.md) for the full architectural framing, RFC 8693 alignment, and the multi-cycle composition plan.

## [5.2.0] - 2026-05-10

### Added

- **`Cirreum.Messaging.INodeIdProvider`** — interface providing a stable identifier for the current process replica/node, distinct from `ProducerId` (which identifies the application/head and is shared across replicas). Enables broker-level and in-process self-echo prevention for inbound distributed messages where multi-replica deployments may receive their own publishes via a shared subscription.
- **`Cirreum.Messaging.DefaultNodeIdProvider`** — default parameterless implementation of `INodeIdProvider` that resolves the current process's node identity via a chain of environment-based hints (Container Apps replica name → App Service instance ID → container hostname → machine name + PID → generated GUID). Computed once at construction. Apps needing custom resolution replace via DI before the framework's `TryAddSingleton` registration runs.
- **`Cirreum.Messaging.DistributedMessageReceived<TMessage>`** — wrapper notification for inbound distributed messages dispatched via Conductor's notification pipeline. Uses a distinct wrapper type to prevent the outbound `DistributedMessageHandler<T>` interceptor from re-publishing received messages back to the bus. Carries the original typed message and the deserialized `DistributedMessageEnvelope`.
- **`Cirreum.Messaging.ReceiverOptions`** — configuration class for the distributed message receiver (consumed by `Cirreum.Runtime.Messaging` in a coordinated follow-up release). Binds from `Cirreum:Messaging:Distribution:Receiver` and specifies the messaging provider instance plus a queue source (`QueueName` for competing-consumer work distribution) and/or a topic source (`TopicName` + per-head `SubscriptionName` for broadcast). Mirrors `SenderOptions`'s queue/topic symmetry; either or both sources may be configured. Node identity is not a config concern — custom resolution registers a custom `INodeIdProvider` in DI rather than mutating options.

### Changed

- **`DistributedMessageEnvelope.PublishedAt`** — new optional `DateTimeOffset?` property stamped at envelope creation by `Create<TMessage>(...)` and `CreateWithSerializer<TMessage>(...)`. Backward-compatible: envelopes serialized prior to this release deserialize with `PublishedAt = null`. Provides handlers and consumers a portable publish timestamp independent of broker-specific properties (e.g., Service Bus `EnqueuedTimeUtc`).

### Fixed

- **`IUserStateAccessor` XML documentation** — expanded to communicate the load-bearing lifetime and usage invariants consumers must respect: dependency on an active invocation context, the prohibition on capturing the returned `IUserState` in singletons or background work (analogous to the `IHttpContextAccessor.HttpContext` capture warning), the anonymous-user contract guarantee when called outside an active invocation, and the scoped-lifetime registration expectation. No API surface or behavior change; the runtime already enforced these invariants.

These additions establish the L3 (Core) surface for cross-head distributed message dispatch coming in a follow-up `Cirreum.Runtime.Messaging` minor release. The receiver hosted service, default `INodeIdProvider` implementation, and `DefaultTransportPublisher` ApplicationProperty enrichment all live in L5 and depend on this Core surface being published first. See [`docs/RELEASE-NOTES-v5.2.0.md`](RELEASE-NOTES-v5.2.0.md) for the full architectural framing.

## [5.1.0] - 2026-05-07

### Added

- **`Cirreum.RemoteServices.IRemoteConnection`** — caller-side typed handle for long-lived bidirectional connections (SignalR Hub clients, raw WebSocket clients, gRPC streaming, …). Pairs with the existing `RemoteClient` in the same family: `RemoteClient` abstracts request/response; `IRemoteConnection` abstracts persistent bidirectional channels. Cross-host: works in WASM apps connecting to a backend, server-side microservices subscribing to events from another service, or anywhere else a long-lived outbound connection is needed.
- **`Cirreum.RemoteServices.RemoteConnectionBase`** — abstract base for `IRemoteConnection` implementations. Provides the public-surface state machine, `StateChanged` event plumbing, and a `TransitionTo(...)` helper for derived classes to call when their underlying transport state changes. Concrete impls in the `Cirreum.Runtime.Invocation.{Source}.Wasm` family derive from this and adapt SignalR, raw WebSocket, gRPC streaming, etc.
- **`Cirreum.RemoteServices.RemoteConnectionState`** — enum with `Disconnected`, `Connecting`, `Connected`, `Reconnecting`, `Disconnecting` values for reporting connection lifecycle state.
- **`Cirreum.RemoteServices.RemoteConnectionStateChangedEventArgs`** — record payload for the `IRemoteConnection.StateChanged` event with `PreviousState` and `NewState`.

These additions slot into the existing `Cirreum.RemoteServices` family alongside `RemoteClient` and friends. No changes to existing types; strictly additive. See [`docs/RELEASE-NOTES-v5.1.0.md`](RELEASE-NOTES-v5.1.0.md) for the full rationale and architectural framing.

## [5.0.1] - 2026-05-01

Doc-only follow-up to v5.0.0. No code or API changes.

### Fixed

- **`Authorization/FLOW.md` and `Authorization/README.md` overstated the
  authority model.** The line "Authority comes from the app-user layer
  (`IOwnedApplicationUser`)" was accurate for the tenant track but treated
  it as universal — operator-track (workforce) and machine-track
  (ApiKey/SignedRequest/External BYOID) callers don't have an
  `IApplicationUser` and never did. Both files now describe authority
  resolution as track-dependent, with a per-track table covering tenant,
  operator, and machine paths and how the grant evaluator accommodates each
  via documented null-fall-through. The Mermaid sequence in `FLOW.md` now
  notes the application user load step is tenant-only.
- **`IApplicationUser` XML doc.** Added remarks explicitly scoping the
  interface to the tenant track and noting that operator and machine tracks
  legitimately have a `null` `IApplicationUser` — the runtime already
  handled this correctly, but the doc previously implied the type applied
  universally.

---

## [5.0.0] - 2026-05-01

Per-scheme application user resolution and centralized authentication
context coordination keys. Sets up multi-IdP server hosts (e.g., portals
backed by workforce + customer IdP + machine credentials) to register one
`IApplicationUserResolver` per scheme and dispatch by the request's
authenticated scheme rather than gymnastics inside a single resolver.

### Added

- **`Cirreum.Security.AuthenticationContextKeys`** — new public static class
  centralizing the well-known `HttpContext.Items` keys used to coordinate
  authentication context across pipeline stages. Holds:
  - `AuthenticatedScheme` — the authentication scheme that authenticated
    the current request, written by the dynamic scheme forward selector
    and the role claims transformer, read by `IAuthenticationBoundaryResolver`
    consumers and the application user resolver dispatcher.
  - `ApplicationUserCache` — the resolved `IApplicationUser` for the current
    request, populated during role enrichment so downstream consumers
    (e.g., `UserAccessor`) avoid a redundant resolver call.
- **`IApplicationUserResolver.Scheme`** (`string?`, default `null`) — declares
  which authentication scheme the resolver handles. Default interface
  implementation returns `null`, marking the resolver as the fallback for
  schemes with no exact match. Singular by design — enforces a 1:1
  scheme→resolver→store mapping. Apps that need to share a store across
  schemes own their own discriminator.

### Removed

- **`IAuthenticationBoundaryResolver.ResolvedSchemeKey`** — replaced by
  `AuthenticationContextKeys.AuthenticatedScheme`. The key is read and
  written by multiple subsystems beyond the boundary resolver, so its home
  moved off the per-interface const.
- **`IApplicationUserResolver.CacheKey`** — replaced by
  `AuthenticationContextKeys.ApplicationUserCache`. Same reasoning: a
  cross-pipeline coordination key doesn't belong on a domain interface.

### Fixed

- **`IUserState.Id` XML doc.** The remarks called `Id` the "primary key for
  application-user resolution," conflating the IdP's stable external
  identifier with the application user's database primary key. Reworded to
  describe `Id` as the external identifier passed to
  `IApplicationUserResolver.ResolveAsync`, with grant cache keys and audit
  trails as secondary uses.

### Migration

```csharp
// Before — HttpContext.Items keys
context.Items[IAuthenticationBoundaryResolver.ResolvedSchemeKey]
context.Items[IApplicationUserResolver.CacheKey]

// After
context.Items[AuthenticationContextKeys.AuthenticatedScheme]
context.Items[AuthenticationContextKeys.ApplicationUserCache]

```

Existing `IApplicationUserResolver` implementations compile unchanged thanks
to the default interface implementation of Scheme (returns null); they
continue working as the fallback resolver. Multi-IdP server hosts that want
per-scheme dispatch override `Scheme` to declare the matching authentication
scheme name.

Downstream Cirreum packages (Cirreum.Runtime.AuthorizationProvider,
Cirreum.Runtime.Authorization, Cirreum.Services.Server,
Cirreum.Runtime.Wasm) require coordinated updates to switch to the new
constants and to allow N resolver registrations.

See `RELEASE-NOTES-v5.0.0.md` for the full architectural rationale.

---

## [4.0.2] — 2026-04-28

Cancel-and-replace for 4.0.0 / 4.0.1. Both prior 4.0 releases will be unlisted on
NuGet; 4.0.2 is the real 4.0. Closes a captured-scope leak by removing
introspection from Core entirely and re-homing it in a dedicated, opt-in package.

### Removed

- **`Introspection/**` namespace and types** (`DomainModel`, all analyzers,
  `DomainAnalyzerProvider`, `DomainDocumenter` / `IDomainDocumenter`,
  `AnalysisReport` and friends, `DomainSnapshot`, `AuthorizationConfigurationException`,
  `ValidateAuthorizationConfiguration` / `CheckAuthorizationConfiguration`
  extensions). All of these now live in the new **`Cirreum.Introspection 1.0.0`**
  package and are namespace-compatible (`Cirreum.Introspection.*`).
- **`AddDefaultDomainDocumenter`** removed from `ServiceCollectionExtensions`.
  Replacement: `services.AddIntrospection()` in `Cirreum.Introspection`, which
  registers both `IDomainModel` and `IDomainDocumenter` as singletons.

### Changed

- **`DomainFeatureResolver` (class + both `Resolve` overloads) is now `public`.**
  Promoted from `internal` so the new `Cirreum.Introspection` package can use it
  across the assembly boundary. No behavior change; still used internally by
  `RequiredGrantCache`, `AuthorizationContext`, and `OperationContext`.

### Fixed

- **Captured-scope leak (4.0.1 regression).** `DomainModel.Instance.Initialize(sp)`
  retained the scoped provider in a singleton field; later accesses (other
  consumers, second validate call) hit a disposed scope and threw
  `ObjectDisposedException`. With introspection extracted, **no Core type retains
  `IServiceProvider`** for authorization analysis. The replacement `IDomainModel`
  in `Cirreum.Introspection` holds an `IServiceScopeFactory` (singleton-safe) and
  snapshots DI-derived data on first access into immutable structures.

### Migration

```csharp
// Before
builder.Services.AddDefaultDomainDocumenter();
app.ValidateAuthorization();   // member method on DomainApplication (Server)

// After (add a PackageReference to Cirreum.Introspection)
builder.Services.AddIntrospection();
app.Services.ValidateAuthorizationConfiguration();
```

The `Cirreum.Runtime.Server.DomainApplication.ValidateAuthorization()` /
`AnalyzeAuthorization()` member methods are removed in the matching
`Cirreum.Runtime.Server` patch release.

See `RELEASE-NOTES-v4.0.2.md` for the full architectural rationale.

---

## [4.0.1] — 2026-04-27

### Fixed

- **`ValidateAuthorizationConfiguration` / `CheckAuthorizationConfiguration` now create a DI scope internally.** Both extension methods previously resolved scoped services (`IAuthorizationRoleRegistry`, `IPolicyValidator[]`, `IOperationGrantProvider`, etc.) directly from the root `IServiceProvider`, which throws under .NET's default scope-validation mode in ASP.NET Core hosts. They now wrap the resolution in `IServiceProvider.CreateScope()` so the boot-time validation works correctly in scope-validated hosts.

---

## [4.0.0] — 2026-04-25

Authorization vocabulary clarity, runtime security signals, boot-time validation,
and a documented compliance boundary. Mechanical migration (~10 minutes); the new
capabilities are additive.

### Added

- **`[RequiresGrant]` attribute** replaces `[RequiresPermission]` — names the gating
  mechanism (the grant pipeline) explicitly; ctor argument is the permission to be
  granted. Verb/data split documented in XML.
- **`AuthorizationContext<T>.RequiredGrants`** replaces `Permissions` — clearer that
  it's the *declared* requirements, not caller-held permissions or ACE permissions.
- **`ICacheableOperation<TResultValue>`** replaces `ICacheableQuery<TResultValue>` —
  aligns with Cirreum's CQRS-neutral operation vocabulary; `TResultValue` parameter
  unchanged.
- **`AuthorizerBase<T>.WhenRequiresGrant` family** replaces `WhenPermission` family
  (six methods) — names match the `[RequiresGrant]` attribute they branch on.
- **Boot-time validation:** `IServiceProvider.ValidateAuthorizationConfiguration()`
  + `CheckAuthorizationConfiguration()` extension methods. Throws
  `AuthorizationConfigurationException` on Error-severity findings; cross-platform
  (works in ASP.NET Core, Functions, WASM, console hosts).
- **`IOperationGrantAccessor` runtime signals:** `OwnerWasAutoStamped` and `WasRead`
  properties + `MarkOwnerAutoStamped()` method. Stage 2/3 evaluators and handlers
  can branch on framework-inferred owner intent and Pattern C bypass detection.
- **`GrantedLookupAudit<,>` intercept:** runtime detection of Pattern C bypass —
  emits warning log + OTel activity tag when an `IGrantableLookupBase` operation
  completes without the handler reading `IOperationGrantAccessor.Current`.
- **OTel activity tags:** `cirreum.authz.grant.owner_auto_stamped` and
  `cirreum.authz.grant.pattern_c_bypass` for forensic audit and dashboards.
- **`GrantedResourceAnalyzer` rule:** unsafe `ICacheableOperation` + grant
  interface combinations flagged at Error severity.
- **Authorization user guide** (`Authorization/README.md`) with mental model,
  pick-your-tool decision table, and Compliance Boundary section mapping to NIST
  SP 800-53 AC-3 / AC-6 / AU-2, NIST SP 800-162 ABAC, OWASP ASVS V4, OWASP Top 10
  #1, ISO/IEC 27001 A.9.4.1.
- **Consistency Model documentation** in `Operations/Grants/README.md` covering
  TTL-based eventual consistency, revocation invariants, and three escape hatches
  for domains needing strong consistency.
- **Export formatters: Runtime Signals & Compliance appendix** — `ToMarkdown`,
  `ToText`, and `ToHtml` now include a footer pointing at the runtime OTel tags
  and compliance documentation. CSV unchanged.
- **Regression tests:** stale ancestor chain over-grant prevention; role inheritance
  transitive resolution (`Manager` → `Internal` → `User`).

### Changed

- **`IOperationGrantCacheInvalidator.InvalidateDomainAsync`** renamed to
  **`InvalidateFeatureAsync`** — and the parameter renamed from `domainFeature`
  to `feature`. Aligns with the framework-wide "feature" vocabulary
  (`RequiredGrantCache.ResolveDomainFeature`, `cirreum.authz.grant.feature`
  cache tag, etc.).
- **`OperationGrantCacheDomainOverride`** renamed to
  **`OperationGrantCacheFeatureOverride`**, and
  **`OperationGrantCacheSettings.DomainOverrides`** renamed to
  **`FeatureOverrides`**. The corresponding `appsettings.json` key under
  `Cirreum:Authorization:Grants:Cache` changes from `DomainOverrides` to
  `FeatureOverrides` — adopters must update configuration as part of the v4
  migration.
- **Analyzer metric keys:** `GrantedResources.GrantDomainCount` →
  `GrantedResources.GrantFeatureCount`, and
  `GrantedResources.CrossDomainPermissionCount` →
  `GrantedResources.CrossFeaturePermissionCount`. Update dashboards or reports
  filtering on these keys.
- **Cache wire-format tags:** `grant:domain:{feature}` → `grant:feature:{feature}`;
  auto-added `tenant:{OwnerId}` tag → `owner:{OwnerId}` tag. Update external
  monitoring/invalidation scripts that key on these prefixes.
- **OTel activity tag rename:** `cirreum.authz.grant.domain` →
  `cirreum.authz.grant.feature` (`AuthorizationTelemetry.GrantDomainTag` →
  `GrantFeatureTag`). Update dashboard queries and trace filters that key on
  this tag. Aligns with the `grant:feature:{feature}` cache tag rename above
  and the framework-wide "feature" vocabulary.
- **`PermissionSet.ToSignature()`** moved to internal `OperationGrantCacheKeys.SignatureOf()`
  — was unsafe as a general-purpose hash because it omitted feature names; now
  scoped to the grant cache where the surrounding key already carries the feature.

### Removed

- **`RequiresGrantAttribute(Permission)` constructor** — was reachable only via
  reflection; `Permission` instances aren't compile-time constants and could not
  be used at attribute call sites. Use the operation-only or feature+operation
  ctor.
- **"Unused Domains" analyzer rule** — incorrectly flagged any domain namespace
  that didn't use grants as suspect. Not every domain needs grant-based access
  control; the rule was noise.
- **`UnusedDomainCount` metric** removed (correlated with the rule above).

### Security

- **Default-deny on incomplete authorization graph** (Already enforced at the
  framework boundary in `DefaultAuthorizationEvaluator`; documented in v4 in the
  Compliance Boundary section).
- **Pattern C runtime audit** turns the highest-risk bypass surface in the
  authorization model from a "trust the developer" pattern into a runtime-observable
  signal — bypasses surface as warnings + dashboard alerts before reaching production.
- **Cross-domain grant declarations** continue to be rejected at startup by
  `RequiredGrantCache`; documented as a framework-enforced AC-6 property.

### Migration

See [MIGRATION-v4.md](MIGRATION-v4.md) for the full v3 → v4 migration guide.

---

## [3.0.0] — 2026-04-09

Terminology and architectural clarity release. Runtime behavior unchanged — all
breaking changes were rename-only and resolvable with find-and-replace. v2's
mediator-style naming (`IRequest`, `TResponse`, `RequestContext`) replaced with
operation-oriented vocabulary that names what each type actually does.

### Added

- First-class `Permission`, `PermissionSet`, and `[RequiresPermission]` attribute
  (renamed to `[RequiresGrant]` in v4).
- Operation Grants — owner-scoped grant-based access control with caching,
  invalidation, and warm-up.
- Resource Access — object-level ACL evaluation via `IProtectedResource` and
  `IResourceAccessEvaluator`.
- `IAuthorizationConstraint` for global cross-cutting authorization gates
  (replaces `IScopeEvaluator`).
- `AuthenticationBoundary` enum tracks whether caller authenticated via Global
  or Tenant IdP (replaces `AccessScope`).
- `AuthorizationTelemetry` — centralized OpenTelemetry instrumentation for the
  authorization pipeline.
- `IAuthorizationContextAccessor` — read the resolved `AuthorizationContext`
  from anywhere in the pipeline.
- `IOwnedApplicationUser` — tenant/owner identity sourced from the application
  user, not JWT claims.
- Cache subsystem moved to `Cirreum.Caching` namespace; consumer-tagged via
  `cirreum.cache.consumer` metric.

### Changed

- Operation pipeline renamed: `IRequest` / `TResponse` → `IOperation` / `IOperation<T>`;
  `RequestContext<T>` → `OperationContext<T>`; `RequestHandlerDelegate` →
  `OperationHandlerDelegate`.
- Authorization base class renamed: `AuthorizationValidatorBase<T>` →
  `AuthorizerBase<TAuthorizableObject>`.
- Cache service rename: `ICacheableQueryService` → `ICacheService`.
- Introspection moved out of `Cirreum.Authorization.Analysis` namespace into
  `Cirreum.Introspection`.

### Migration

See [MIGRATION-v3.md](MIGRATION-v3.md) for the full v2 → v3 migration guide.

---

[Unreleased]: https://github.com/cirreum/Cirreum.Core/compare/v4.0.0...HEAD
[4.0.0]: https://github.com/cirreum/Cirreum.Core/releases/tag/v4.0.0
[3.0.0]: https://github.com/cirreum/Cirreum.Core/releases/tag/v3.0.0
