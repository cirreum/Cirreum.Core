namespace Cirreum.Security;

/// <summary>
/// Default concrete implementation of <see cref="IActorContext"/>. Constructed by the
/// upstream M2M auth handler at delegation time and stamped into the invocation's
/// well-known items bag under <see cref="AuthenticationContextKeys.Actor"/>.
/// </summary>
/// <remarks>
/// <para>
/// Sealed immutable record — once captured, the actor snapshot represents the historical
/// fact of the upgrade and must not change. Producers in upstream auth libraries
/// (e.g., <c>Cirreum.Authorization.ApiKey</c>, <c>Cirreum.Authorization.SignedRequest</c>)
/// construct this directly.
/// </para>
/// <para>
/// Apps reading <see cref="IUserState.Actor"/> can pattern-match against this concrete
/// type if they want strongly-typed access; otherwise consume via the
/// <see cref="IActorContext"/> interface for forward compatibility.
/// </para>
/// </remarks>
/// <param name="Id">The actor's stable identifier. See <see cref="IActorContext.Id"/>.</param>
/// <param name="Name">The actor's display name. See <see cref="IActorContext.Name"/>.</param>
/// <param name="Scheme">The authentication scheme that authenticated the actor on the wire. See <see cref="IActorContext.Scheme"/>.</param>
/// <param name="Delegation">Metadata about the delegation event. See <see cref="IActorContext.Delegation"/>.</param>
public sealed record ActorContext(
	string Id,
	string Name,
	string Scheme,
	DelegationMetadata Delegation) : IActorContext;
