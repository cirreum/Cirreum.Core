namespace Cirreum.Authorization;

using System.Collections.Immutable;

/// <summary>
/// Provides a runtime container for registered roles and role inheritance.
/// </summary>
public interface IAuthorizationRoleRegistry {

	/// <summary>
	/// Retrieves all roles that have been registered in the registry, 
	/// including roles with direct permissions and those defined through role inheritance.
	/// </summary>
	/// <returns>An <see cref="IImmutableSet{T}"/> of <see cref="Role"/> entries representing all registered roles.</returns>
	IImmutableSet<Role> GetRegisteredRoles();

	/// <summary>
	/// Retrieves all roles from which the specified role directly inherits (i.e., its conventional parent roles).
	/// </summary>
	/// <param name="role">The role whose direct inherited (parent) roles should be retrieved.</param>
	/// <returns>
	/// An <see cref="IImmutableSet{T}"/> of <see cref="Role"/> entries representing the roles that the specified role directly inherits from.
	/// </returns>
	IImmutableSet<Role> GetInheritedRoles(Role role);

	/// <summary>
	/// Retrieves all roles that directly inherit from the specified role.
	/// </summary>
	/// <param name="role">The role whose inheriting roles should be retrieved.</param>
	/// <returns>
	/// An <see cref="IImmutableSet{T}"/> of <see cref="Role"/> entries representing the roles that directly inherit from the <paramref name="role"/>.
	/// </returns>
	IImmutableSet<Role> GetInheritingRoles(Role role);

	/// <summary>
	/// Retrieves the <see cref="Role"/> from the registry for the specified role string.
	/// </summary>
	/// <param name="roleString">The role string ({namespace}:{name}).</param>
	/// <returns>A <see cref="Role"/> record if found in the registry; otherwise <see langword="null"/>.</returns>
	Role? GetRoleFromString(string roleString);

	/// <summary>
	/// Get all roles for the specified direct roles.
	/// </summary>
	/// <param name="directRoles">One or more direct roles.</param>
	/// <returns>An <see cref="IImmutableSet{T}"/> of roles.</returns>
	IImmutableSet<Role> GetEffectiveRoles(IEnumerable<Role> directRoles);

}