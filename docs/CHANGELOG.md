# Cirreum.Core Changelog

All notable changes to **Cirreum.Core** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

_(no unreleased changes)_

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
