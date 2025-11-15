namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Microsoft.Extensions.Logging;

internal sealed class TestAuthorizationRoleRegistry(
	ILogger<TestAuthorizationRoleRegistry> logger)
	: AuthorizationRoleRegistryBase(logger) {

	/// <inheritdoc/>
	public ValueTask InitializeAsync() {
		return this.DefaultInitializationAsync();
	}

}