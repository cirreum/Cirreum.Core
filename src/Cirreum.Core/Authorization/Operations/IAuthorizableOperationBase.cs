namespace Cirreum.Authorization.Operations;

using Cirreum.Conductor;

/// <summary>
/// The seam between Conductor (operation routing) and Authorization (the three-stage
/// pipeline). An <see cref="IBaseOperation"/> is anonymous and routes directly to its
/// handler; an <see cref="IBaseOperation"/> that <em>also</em> implements
/// <see cref="IAuthorizableOperationBase"/> is gated by the authorization pipeline before
/// the handler runs.
/// </summary>
/// <remarks>
/// <para>
/// Implementing this interface is the single discriminator the framework checks to
/// decide whether to invoke <see cref="IAuthorizationEvaluator"/> for an operation.
/// Inheriting <see cref="IAuthorizableObject"/> means every instance is also its own
/// authorizable object — Stage 2 authorizers receive the operation directly via
/// <see cref="AuthorizationContext{TAuthorizableObject}.AuthorizableObject"/> and can
/// inspect its shape without wrapping.
/// </para>
/// <para>
/// You normally do not implement this interface directly. Pick the marker that matches
/// the operation's shape:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="IAuthorizableOperation"/> /
///     <see cref="IAuthorizableOperation{TResultValue}"/> — plain authorized operations
///     (no grant or self semantics).
///   </description></item>
///   <item><description>
///     Grant-scoped: <see cref="IOwnerMutateOperation"/>,
///     <see cref="IOwnerLookupOperation{TResultValue}"/>,
///     <see cref="IOwnerSearchOperation{TResultValue}"/> — owner-scoped writes,
///     reads, and searches that participate in Stage 1 grant resolution.
///   </description></item>
///   <item><description>
///     Self-scoped: <see cref="ISelfMutateOperation"/>,
///     <see cref="ISelfLookupOperation{TResultValue}"/> — user-owned operations
///     gated by identity match (with admin bypass).
///   </description></item>
/// </list>
/// <para>
/// All grant and self interfaces transitively implement
/// <see cref="IAuthorizableOperationBase"/>, so picking a grant interface gives you
/// authorization participation, conductor routing, and grant enforcement in one
/// declaration.
/// </para>
/// </remarks>
public interface IAuthorizableOperationBase : IBaseOperation, IAuthorizableObject;
