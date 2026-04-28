# Cirreum.Core 4.0.2 — Introspection extraction & architectural cleanup

**Cancel-and-replace for 4.0.0 / 4.0.1.** Both prior 4.0 releases are being
unlisted on NuGet. `4.0.2` is the real `4.0`.

---

## Why this release exists

`4.0.0` shipped boot-time authorization validation
(`ValidateAuthorizationConfiguration`) that resolved scoped services directly
from the caller-supplied root `IServiceProvider`. Under .NET's default
scope-validation mode, this threw "*cannot resolve scoped service from root
provider*" the first time anyone called it from a typical ASP.NET Core
`builder.Build()` context.

`4.0.1` wrapped the resolution in `using var scope = services.CreateScope()` to
fix that. The fix was correct in isolation, but the introspection singleton
(`DomainModel.Instance.Initialize(sp)`) **retained the scoped provider** in a
field, so the next consumer that touched the model — including a second
`Validate*` call later in the same process — dereferenced a disposed scope and
threw `ObjectDisposedException`.

Both bugs share a root cause: **introspection types holding onto an
`IServiceProvider`**. Patching the symptom kept producing new failure modes.
`4.0.2` removes the disease: introspection moves into its own package
(`Cirreum.Introspection`) and the new design makes the bug class structurally
impossible.

---

## What's removed from `Cirreum.Core`

The entire `Cirreum.Core/Introspection/**` namespace is gone — moved to the
new `Cirreum.Introspection 1.0.0` package, namespace-compatible
(`Cirreum.Introspection.*`):

- `DomainModel` and all analyzers (`AuthorizationRuleAnalyzer`,
  `RoleHierarchyAnalyzer`, `AnonymousResourceAnalyzer`,
  `AuthorizableResourceAnalyzer`, `GrantedResourceAnalyzer`,
  `AuthorizationConstraintAnalyzer`, `PolicyValidatorAnalyzer`,
  `ProtectedResourceAnalyzer`)
- `DomainAnalyzerProvider`, `CompositeAnalyzer`, `IDomainAnalyzer`
- `DomainDocumenter`, `IDomainDocumenter`
- `AnalysisReport`, `AnalysisIssue`, `AnalysisOptions`, `AnalysisSummary`,
  `AuthorizationConfigurationException`, `IssueDefinition`, `IssueSeverity`,
  `MetricCategories`
- `DomainSnapshot` and the export types
- `ValidateAuthorizationConfiguration` / `CheckAuthorizationConfiguration`
  extension methods
- `AddDefaultDomainDocumenter` extension on `IServiceCollection`

## What's promoted in `Cirreum.Core`

- `Cirreum.DomainFeatureResolver` (and both `Resolve` overloads) is now
  `public`. It was `internal` before; the new package needs to call it across
  the assembly boundary. Used internally by `RequiredGrantCache`,
  `AuthorizationContext`, and `OperationContext` — no behavior change.

---

## Architectural principle

> **No introspection type retains `IServiceProvider`.**

The replacement `IDomainModel` in `Cirreum.Introspection` is a true singleton
that holds an `IServiceScopeFactory` (singleton-safe). DI-derived data
(`IPolicyValidator` registrations, `IAuthorizationConstraint` types, access
provider registrations) is resolved on first access through a transient scope,
**snapshotted into immutable structures, and the scope is disposed**.
Reflection-derived data (resources, rules, catalog, protected types) is cached
via `Lazy<T>`. Repeated calls are pointer reads.

There is no `Initialize` method, no `DomainModel.Instance` static accessor, and
no public refresh path. The captured-scope failure mode is gone by
construction.

---

## Migration

### 1. Add a package reference

```diff
+ <PackageReference Include="Cirreum.Introspection" Version="1.0.0" />
```

### 2. Update registration

```diff
- builder.Services.AddDefaultDomainDocumenter();
+ builder.Services.AddIntrospection();   // registers IDomainModel + IDomainDocumenter
```

### 3. Update validation call sites

```diff
- app.ValidateAuthorization();                         // Server member method (removed)
+ app.Services.ValidateAuthorizationConfiguration();   // IServiceProvider extension
```

The `Cirreum.Runtime.Server.DomainApplication.ValidateAuthorization()` and
`AnalyzeAuthorization()` member methods are removed in the corresponding
`Cirreum.Runtime.Server` patch release. The replacements live on
`IServiceProvider` and work identically across Server, Serverless, and WASM.

### 4. Optional — compose your own startup policy

`Cirreum.Introspection` deliberately ships **no** `IAutoInitialize` /
`IStartupTask`. If every consumer got auto-validation just by referencing the
package, that would be the wrong default. Compose your own:

```csharp
internal sealed class ValidateAuthOnStart(IServiceProvider sp) : IAutoInitialize {
    public ValueTask InitializeAsync() {
        sp.ValidateAuthorizationConfiguration();   // throws on Error severity
        return ValueTask.CompletedTask;
    }
}
```

Or wire it to an admin endpoint, an integration test, or a debug-only init.
Library provides the model and the runners; consumer chooses the policy.

---

## Why a separate package (and not a Runtime Extensions package)

Each runtime has its own concrete `DomainApplication` (Server, Serverless,
WASM) — there is no shared base type for the built app. Sugar packages per
runtime were considered and rejected as overhead. The chosen surface is
extensions on `IServiceCollection` (`AddIntrospection`) and `IServiceProvider`
(`Validate*` / `Check*` / `Analyze*`). One package covers all three runtimes;
consumers always call `app.Services.X()`.

---

## Compatibility

- Nothing in `Cirreum.Core` 4.0.2 type-forwards to `Cirreum.Introspection`.
  Old `4.0.0` / `4.0.1` are being unlisted, so we accept the source break in
  exchange for a clean public surface.
- `DomainModel.Instance` static accessor is removed outright. Any external
  consumer that was reaching for it migrates to
  `services.GetRequiredService<IDomainModel>()`.

## See also

- `Cirreum.Introspection` package README for the full consumer API.
- `CHANGELOG.md` entry for `4.0.2` for the condensed change list.
