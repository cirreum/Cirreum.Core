# Cirreum.Core 5.0.1 — Authority model doc cleanup

**Doc-only follow-up to v5.0.0.** No code or API changes.

---

## Why this release exists

`v5.0.0` added per-scheme `IApplicationUserResolver` dispatch and centralized
the `HttpContext.Items` coordination keys. While reviewing the surrounding
documentation, two long-standing claims in the authorization docs were found
to be overstated:

> **Authority comes from the app-user layer (`IOwnedApplicationUser`), not
> from IdP claims.** *(FLOW.md and Authorization/README.md, pre-5.0.1)*

This was accurate for the **tenant track** (callers authenticated via a
customer IdP backed by an application user store) but treated as universal —
which it isn't. **Operator-track callers** (workforce IdP — typically Entra
workforce) and **machine-track callers** (`ApiKey`, `SignedRequest`,
`External` BYOID) don't have an `IApplicationUser` and never did. Authority
for those callers flows from token claims and credential policy.

The runtime has always handled this correctly — `OperationGrantEvaluator`
early-returns when `IsApplicationUserLoaded` is false, the
`IOperationGrantProvider.GetHomeOwnerId` default uses null-safe
`as IOwnedApplicationUser` casts, and `AuthorizationContext.ApplicationUser`
was already correctly typed nullable with the appropriate XML remark. The
issue was strictly in the high-level narrative documents.

This patch tightens the framing so future readers don't reverse-engineer the
wrong mental model from the docs.

---

## What changed

### `Authorization/FLOW.md`

The opening prose now describes authority resolution as track-dependent and
enumerates the three tracks (tenant / operator / machine) with their respective
authority sources. The Mermaid sequence diagram's "Load IOwnedApplicationUser"
step is annotated as tenant-track-only, with operator and machine tracks
explicitly noted as skipping the load. The *Identity vs. authority* key point
acknowledges the operator-track short-circuit path through claims
transformation's `RolesAlreadyPresent` branch.

### `Authorization/README.md`

The *Mental Model* section now has a per-track table covering authentication
mechanism, `IApplicationUser` presence, and authority source for each of the
three tracks, replacing the prior universal-authority paragraph.

### `IApplicationUser.cs` XML doc

Added `<remarks>` paragraphs explicitly scoping the interface to the tenant
track and noting that operator and machine tracks legitimately have a
permanently-`null` `ApplicationUser` by design — which the grant evaluator
already accommodates via documented null-fall-through.

---

## What didn't change

- No code, behavior, or API changes.
- No constants, types, or signatures added or removed.
- No XML doc changes that would affect consumer-facing IntelliSense beyond
  the `IApplicationUser` interface itself.

If you don't read the authorization docs or look at `IApplicationUser`'s
remarks, this release is a no-op for you. Upgrade is purely about correctness
of the framing for future readers.

---

## Migration

None. Upgrade by package reference; nothing in your code needs to change.

---

## See also

- `CHANGELOG.md` entry for `5.0.1` for the condensed change list.
- `RELEASE-NOTES-v5.0.0.md` for the underlying per-scheme resolver dispatch
  and `AuthenticationContextKeys` work that this patch documents around.
- `MIGRATION-v5.md` covers migration from v4.x to the v5 line — no patch-
  specific migration needed.
