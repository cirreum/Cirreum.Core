namespace Cirreum.Authorization;

/// <summary>
/// Declares a <see cref="Permission"/> that must be granted to the caller for this
/// operation to proceed. The attribute supplies the <em>permission</em>; the grant
/// pipeline supplies the <em>enforcement</em> — at Stage 1 it resolves the caller's
/// <see cref="Operations.Grants.OperationGrant"/> and verifies the declared permission
/// is held on the target owner.
/// </summary>
/// <remarks>
/// <para>
/// <b>Verb / data split.</b> The attribute name describes the gating mechanism (grants);
/// the argument is the permission to be granted. <c>[RequiresGrant("delete")]</c> reads
/// as "this operation is gated by the grant pipeline, requiring the <c>delete</c>
/// permission to be granted on the target owner."
/// </para>
/// <para>
/// <b>Lifecycle.</b> Read once per authorizable-object type at pipeline setup by
/// <see cref="RequiredGrantCache"/> and hoisted onto
/// <see cref="AuthorizationContext{TAuthorizableObject}.RequiredGrants"/>. No per-request
/// reflection.
/// </para>
/// <para>
/// <b>Enforcement is Stage 1 only.</b> Stage 2 resource authorizers and Stage 3 policy
/// validators may <em>inspect</em>
/// <see cref="AuthorizationContext{TAuthorizableObject}.RequiredGrants"/> to branch their
/// own rules, but they do not enforce this attribute. Resource-level ACL permissions on
/// <see cref="Resources.AccessEntry"/> are an entirely separate concept — do not
/// conflate them.
/// </para>
/// <para>
/// <b>Stacking is AND semantics.</b> When multiple attributes are present, grant
/// resolution returns only owners where the caller holds <em>every</em> declared
/// permission.
/// </para>
/// <para>
/// <b>Two declaration forms:</b>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>[RequiresGrant("delete")]</c> — operation-only. The feature is resolved from
///     the authorizable-object type's <c>*.Domain.&lt;feature&gt;.*</c> namespace via
///     <see cref="DomainFeatureResolver"/>. Preferred for granted resources.
///   </description></item>
///   <item><description>
///     <c>[RequiresGrant("issues", "delete")]</c> — explicit feature + operation. The
///     feature is validated against the domain feature for granted resources;
///     cross-feature declarations throw at pipeline setup.
///   </description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [RequiresGrant("delete")]
/// public sealed record DeleteIssue(string Id) : IOwnerMutateOperation {
///     public string? OwnerId { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public sealed class RequiresGrantAttribute : Attribute {

	/// <summary>
	/// Declares the permission that must be granted, supplying only the operation verb.
	/// The feature is resolved from the authorizable-object type's
	/// <c>*.Domain.&lt;feature&gt;.*</c> namespace at pipeline setup.
	/// </summary>
	/// <param name="operation">The operation verb (e.g., <c>"read"</c>, <c>"write"</c>, <c>"delete"</c>).</param>
	/// <exception cref="InvalidOperationException">
	/// Thrown at pipeline setup when the type's namespace does not follow the
	/// <c>*.Domain.*</c> convention. Use the two-arg form for non-conforming types.
	/// </exception>
	public RequiresGrantAttribute(string operation) {
		ArgumentException.ThrowIfNullOrWhiteSpace(operation);
		this.UnresolvedOperation = operation.ToLowerInvariant();
	}

	/// <summary>
	/// Declares the permission that must be granted, with an explicit feature and operation.
	/// On granted resources, the feature is validated against the domain feature derived
	/// from the type's namespace — cross-feature declarations throw at pipeline setup.
	/// </summary>
	/// <param name="feature">The domain feature (the bounded context — e.g., <c>"issues"</c>).</param>
	/// <param name="operation">The operation verb (e.g., <c>"read"</c>, <c>"write"</c>, <c>"delete"</c>).</param>
	public RequiresGrantAttribute(string feature, string operation) {
		ArgumentException.ThrowIfNullOrWhiteSpace(feature);
		ArgumentException.ThrowIfNullOrWhiteSpace(operation);
		this.Permission = new Permission(feature, operation);
	}

	/// <summary>
	/// The declared <see cref="Authorization.Permission"/> that must be granted to the
	/// caller. Populated immediately when constructed with the two-arg form; populated by
	/// <see cref="RequiredGrantCache"/> after feature resolution when constructed with
	/// the operation-only form.
	/// </summary>
	public Permission? Permission { get; internal set; }

	/// <summary>
	/// The raw operation verb captured by the operation-only constructor, awaiting
	/// feature resolution. <see langword="null"/> when the two-arg form was used.
	/// </summary>
	internal string? UnresolvedOperation { get; }

	/// <summary>
	/// <see langword="true"/> when this attribute was constructed with the operation-only
	/// form and still needs its feature resolved from the type's namespace by
	/// <see cref="RequiredGrantCache"/>.
	/// </summary>
	internal bool NeedsFeatureResolution => this.UnresolvedOperation is not null;
}
