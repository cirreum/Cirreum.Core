namespace Cirreum.Conductor.Tests;

using Cirreum.Auditing;
using Cirreum.Authorization;
using Cirreum.Conductor.Intercepts;
using Cirreum.Exceptions;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class DispatcherTests {

	// ===== Fakes =====

	public sealed class CancelAwareEchoHandler : IRequestHandler<Echo, string> {
		public async ValueTask<Result<string>> HandleAsync(Echo r, CancellationToken ct) {
			await Task.Delay(200, ct); // should cancel
			return Result<string>.Success(r.Text);
		}
	}

	private sealed record Ping() : IRequest;

	private sealed class PingHandler : IRequestHandler<Ping> {
		public bool Called { get; private set; }

		public ValueTask<Result> HandleAsync(Ping request, CancellationToken cancellationToken = default) {
			this.Called = true;
			return ValueTask.FromResult(Result.Success);
		}
	}

	private sealed class ThrowingPingHandler : IRequestHandler<Ping> {
		public ValueTask<Result> HandleAsync(Ping request, CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("dispatch failure");
	}

	public sealed record Echo(string Text) : IRequest<string>;

	private sealed class EchoHandler : IRequestHandler<Echo, string> {
		public ValueTask<Result<string>> HandleAsync(Echo request, CancellationToken cancellationToken = default)
			=> ValueTask.FromResult(Result<string>.Success(request.Text));
	}

	private sealed record Fail() : IRequest;

	private sealed class FailHandler : IRequestHandler<Fail> {
		public ValueTask<Result> HandleAsync(Fail request, CancellationToken cancellationToken = default)
			=> ValueTask.FromResult(Result.Fail("boom"));
	}

	private sealed record AuthRequest() : IAuthorizableRequest, IAuditableRequest;

	private sealed class AuthRequestHandler : IRequestHandler<AuthRequest> {
		public bool Called { get; private set; }
		public ValueTask<Result> HandleAsync(AuthRequest request, CancellationToken cancellationToken = default) {
			this.Called = true;
			return ValueTask.FromResult(Result.Success);
		}
	}

	private sealed class AuthRequestAuthorizor : AuthorizationValidatorBase<AuthRequest> {
		public AuthRequestAuthorizor() {
			this.HasRole(ApplicationRoles.AppUserRole);
		}
	}
	private sealed class AuthAdminRequestAuthorizor : AuthorizationValidatorBase<AuthRequest> {
		public AuthAdminRequestAuthorizor() {
			this.HasRole(ApplicationRoles.AppAdminRole);
		}
	}

	[TestMethod]
	public async Task Dispatch_VoidRequest_InvokesHandler_AndReturnsOk() {
		// Arrange
		var pingHandler = new PingHandler();
		var dispatcher = Shared.ArrangeSimpleDispatcher(builder => {
			builder.AddSingleton<IRequestHandler<Ping>>(pingHandler);
		});

		// Act
		var result = await dispatcher.DispatchAsync(new Ping(), this.TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.IsTrue(pingHandler.Called);
	}

	[TestMethod]
	public async Task Dispatch_TResponseRequest_InvokesHandler_AndReturnsPayload() {
		// Arrange
		var dispatcher = Shared.ArrangeSimpleDispatcher(builder => {
			builder.AddTransient<IRequestHandler<Echo, string>, EchoHandler>();
		});

		// Act
		var result = await dispatcher.DispatchAsync(new Echo("hello"), this.TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual("hello", result.Value);
	}

	[TestMethod]
	public async Task Dispatch_VoidRequest_PropagatesFailure() {
		// Arrange
		var dispatcher = Shared.ArrangeSimpleDispatcher();

		// Act
		var result = await dispatcher.DispatchAsync(new Fail(), this.TestContext.CancellationToken);

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
	}

	[TestMethod]
	public async Task Dispatch_honors_cancellation_token() {
		var dispatcher = Shared.ArrangeSimpleDispatcher(sp => {
			sp.AddTransient<IRequestHandler<Echo, string>, CancelAwareEchoHandler>();
		});

		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
		var result = await dispatcher.DispatchAsync(new Echo("hello"), cts.Token);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsInstanceOfType<TaskCanceledException>(result.Error);
	}

	[TestMethod]
	public async Task Dispatch_abac_authorizes_appuser_role() {

		var authHandler = new AuthRequestHandler();
		var services = Shared.ArrangeServices(sp => {
			sp.AddTransient<IAuthorizationResourceValidator<AuthRequest>, AuthRequestAuthorizor>();
			sp.AddTransient<IRequestHandler<AuthRequest>>(sp => authHandler);
			sp.AddSingleton<IAuthorizationRoleRegistry, TestAuthorizationRoleRegistry>();
			sp.AddConductor(options => {
				options.AddOpenIntercept(typeof(Authorization<,>));
			}, Shared.SequentialSettings);
		});
		var sp = services.BuildServiceProvider();

		var authRegistry = sp.GetRequiredService<IAuthorizationRoleRegistry>();
		await ((TestAuthorizationRoleRegistry)authRegistry).InitializeAsync();

		var dispatcher = sp.GetRequiredService<IDispatcher>();

		var result = await dispatcher.DispatchAsync(new AuthRequest(), this.TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.IsTrue(authHandler.Called);

	}

	[TestMethod]
	public async Task Dispatch_abac_forbiddenaccess_for_appuser() {

		var authHandler = new AuthRequestHandler();
		var services = Shared.ArrangeServices(sp => {
			sp.AddTransient<IAuthorizationResourceValidator<AuthRequest>, AuthAdminRequestAuthorizor>();
			sp.AddTransient<IRequestHandler<AuthRequest>>(sp => authHandler);
			sp.AddSingleton<IAuthorizationRoleRegistry, TestAuthorizationRoleRegistry>();
			sp.AddConductor(options => {
				options.AddOpenIntercept(typeof(Authorization<,>));
			}, Shared.SequentialSettings);
		});
		var sp = services.BuildServiceProvider();

		var authRegistry = sp.GetRequiredService<IAuthorizationRoleRegistry>();
		await ((TestAuthorizationRoleRegistry)authRegistry).InitializeAsync();

		var dispatcher = sp.GetRequiredService<IDispatcher>();

		var result = await dispatcher.DispatchAsync(new AuthRequest(), this.TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(result.IsFailure);
		Assert.IsFalse(authHandler.Called);
		Assert.IsTrue(result.Error is ForbiddenAccessException, "Should be 'ForbiddenAccessException' exception");
		this.TestContext.WriteLine(result.Error.Message);
	}

	public TestContext TestContext { get; set; }

}