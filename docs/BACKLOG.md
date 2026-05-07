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
