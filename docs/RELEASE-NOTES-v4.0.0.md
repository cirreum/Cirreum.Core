# Cirreum.Core 4.0.0

**Authorization vocabulary clarity, runtime security signals, boot-time validation, and a documented compliance boundary.**

This release sharpens Cirreum's authorization model along two axes: (1) **vocabulary** — the grant pipeline now uses *"feature"* consistently in every public type, parameter, JSON config key, OTel tag, cache wire format, and prose surface; and (2) **security posture** — the framework now enforces, observes, and documents the authorization graph at every time horizon: compile-time analyzers, boot-time validation, request-time runtime signals, and per-stage telemetry. The mechanical migration is ~10 minutes; the new capabilities are additive.

## Highlights

- **`[RequiresGrant]` replaces `[RequiresPermission]`** — names the gating mechanism explicitly. Verb/data split documented: the attribute supplies the permission; the grant pipeline does the enforcement.
- **`ICacheableOperation<T>` replaces `ICacheableQuery<T>`** — aligns with Cirreum's CQRS-neutral operation vocabulary.
- **Boot-time validation:** `app.Services.ValidateAuthorizationConfiguration()` throws `AuthorizationConfigurationException` on Error-severity analyzer findings. Cross-platform — works in ASP.NET Core, Functions, WASM, and console hosts.
- **Pattern C runtime audit:** new `GrantedLookupAudit<,>` intercept emits a structured warning log + OTel activity tag when a Pattern C lookup completes without the handler reading `IOperationGrantAccessor.Current` — runtime detection of the highest-risk bypass surface in the model.
- **New OTel signals:** `cirreum.authz.grant.owner_auto_stamped` and `cirreum.authz.grant.pattern_c_bypass` for forensic audit and dashboards.
- **Compliance Boundary documentation:** `Authorization/README.md` now delineates what the framework guarantees vs. what remains application responsibility, with mappings to NIST SP 800-53 AC-3 / AC-6 / AU-2, NIST SP 800-162 (ABAC), OWASP ASVS V4, OWASP Top 10 #1, and ISO/IEC 27001 A.9.4.1.
- **New analyzer rule (Error severity):** flags unsafe combinations of `ICacheableOperation<T>` with grant interfaces other than `IOwnerCacheableLookupOperation<T>` — closes a per-caller cache-leak surface at startup.
- **Authorization user guide** (`Authorization/README.md`) is the new canonical map — pipeline overview, decision table for picking the right tool, core types, inline examples, permission model, namespace-derived feature resolution.

## Breaking Changes (Summary)

| Category | Find | Replace |
|---|---|---|
| Attribute | `[RequiresPermission]` | `[RequiresGrant]` |
| Property | `AuthorizationContext<T>.Permissions` | `AuthorizationContext<T>.RequiredGrants` |
| Interface | `ICacheableQuery` / `ICacheableQuery<T>` | `ICacheableOperation` / `ICacheableOperation<T>` |
| Authorizer helpers | `WhenPermission` (×6 incl. `Any`/`All`/`Unless` variants) | `WhenRequiresGrant` (×6) |
| Cache invalidator | `InvalidateDomainAsync(domainFeature, ...)` | `InvalidateFeatureAsync(feature, ...)` |
| Settings type | `OperationGrantCacheDomainOverride` | `OperationGrantCacheFeatureOverride` |
| Settings property | `OperationGrantCacheSettings.DomainOverrides` | `OperationGrantCacheSettings.FeatureOverrides` |
| `appsettings.json` | `"DomainOverrides": { ... }` | `"FeatureOverrides": { ... }` |
| Cache wire-format tag | `grant:domain:{feature}` | `grant:feature:{feature}` |
| Cache wire-format tag | `tenant:{OwnerId}` (auto-added) | `owner:{OwnerId}` |
| OTel activity tag | `cirreum.authz.grant.domain` | `cirreum.authz.grant.feature` |
| Analyzer metric key | `GrantedResources.GrantDomainCount` | `GrantedResources.GrantFeatureCount` |
| Analyzer metric key | `GrantedResources.CrossDomainPermissionCount` | `GrantedResources.CrossFeaturePermissionCount` |

**Removed:** `RequiresGrantAttribute(Permission)` constructor (was reachable only via reflection — `Permission` instances aren't compile-time constants), the "Unused Domains" analyzer rule (over-assumption — not every domain needs grants), `PermissionSet.ToSignature()` (moved internal to `OperationGrantCacheKeys.SignatureOf()`; was unsafe as a general-purpose signature).

**Adopters with dashboards or external monitoring** that key on the cache wire-format tags, OTel activity tags, or analyzer metric keys must update queries — see the migration guide for the complete list.

## Migration

For the full step-by-step migration guide with all rename rows, app-side responsibilities, and Step 1–6 walkthrough:

- **[Migration v3 → v4](MIGRATION-v4.md)** — full migration guide
- **[Changelog](CHANGELOG.md)** — release-by-release summary

## Standards Posture (v4)

| Control | v4 status |
|---|---|
| NIST SP 800-53 **AC-3** (Access Enforcement) | Enforced — runtime backstop in `DefaultAuthorizationEvaluator` denies on incomplete graph |
| NIST SP 800-53 **AC-6** (Least Privilege) | **Enforced at the framework boundary** + analyzer-driven app-side hardening; Pattern C runtime audit closes the highest-risk surface |
| NIST SP 800-53 **AU-2 / AU-12** (Audit Events) | Supported + new `owner_auto_stamped` and `pattern_c_bypass` tags |
| NIST SP 800-162 (ABAC) | Aligned |
| OWASP ASVS V4 (Access Control) | Aligned at framework boundary; Pattern C residual risk now has runtime detection |
| OWASP Top 10 #1 (Broken Access Control) | **Pattern C now monitored at runtime** — bypasses surface as warnings + dashboard signals before reaching production |
| ISO/IEC 27001 A.9.4.1 | Supported |

See [Authorization/README.md → Compliance Boundary](../src/Cirreum.Core/Authorization/README.md#compliance-boundary) for the full statement of framework guarantees vs. application responsibilities.
