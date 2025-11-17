namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Cirreum.Conductor;
using Cirreum.Conductor.Intercepts;
using Cirreum.Exceptions;
using Microsoft.Extensions.DependencyInjection;

[TestClass]
public sealed class DispatcherTests {

	// ===== Fakes =====

	public sealed class CancelAwareEchoHandler : IRequestHandler<Echo, string> {
		public async Task<Result<string>> HandleAsync(Echo r, CancellationToken ct) {
			await Task.Delay(200, ct); // should cancel
			return Result<string>.Success(r.Text);
		}
	}

	private sealed record Ping() : IRequest;

	private sealed class PingHandler : IRequestHandler<Ping> {
		public bool Called { get; private set; }

		public Task<Result> HandleAsync(Ping request, CancellationToken cancellationToken = default) {
			this.Called = true;
			return Task.FromResult(Result.Success);
		}
	}

	private sealed class ThrowingPingHandler : IRequestHandler<Ping> {
		public Task<Result> HandleAsync(Ping request, CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("dispatch failure");
	}

	public sealed record Echo(string Text) : IRequest<string>;

	private sealed class EchoHandler : IRequestHandler<Echo, string> {
		public Task<Result<string>> HandleAsync(Echo request, CancellationToken cancellationToken = default)
			=> Task.FromResult(Result<string>.Success(request.Text));
	}

	private sealed record Fail() : IRequest;

	private sealed class FailHandler : IRequestHandler<Fail> {
		public Task<Result> HandleAsync(Fail request, CancellationToken cancellationToken = default)
			=> Task.FromResult(Result.Fail(new("boom")));
	}

	public sealed class BoomRequest : IRequest<string> { }

	public sealed class BoomHandler : IRequestHandler<BoomRequest, string> {
		public Task<Result<string>> HandleAsync(BoomRequest request, CancellationToken cancellationToken) {
			throw new InvalidOperationException("Boom!");
		}
	}

	public sealed class FatalRequest : IRequest<string> { }

	public sealed class FatalHandler : IRequestHandler<FatalRequest, string> {
		public Task<Result<string>> HandleAsync(FatalRequest request, CancellationToken cancellationToken) {
			throw new OutOfMemoryException("Simulated OOM");
		}
	}

	private sealed record AuthRequest() : IAuthorizableRequest, IAuditableRequest;

	private sealed class AuthRequestHandler : IRequestHandler<AuthRequest> {
		public bool Called { get; private set; }
		public Task<Result> HandleAsync(AuthRequest request, CancellationToken cancellationToken = default) {
			this.Called = true;
			return Task.FromResult(Result.Success);
		}
	}

	private sealed class AuthRequestAuthorizer : AuthorizationValidatorBase<AuthRequest> {
		public AuthRequestAuthorizer() {
			this.HasRole(ApplicationRoles.AppUserRole);
		}
	}
	private sealed class AuthAdminRequestAuthorizer : AuthorizationValidatorBase<AuthRequest> {
		public AuthAdminRequestAuthorizer() {
			this.HasRole(ApplicationRoles.AppAdminRole);
		}
	}


	[TestMethod]
	public async Task DispatchAsync_FatalException_IsNotConvertedToResult_BubblesOut() {
		// Arrange
		var services = new ServiceCollection();
		var dispatcher = Shared.ArrangeSimpleDispatcher(services => {
			services.AddSingleton<IDispatcher, Dispatcher>();
			services.AddTransient<IRequestHandler<FatalRequest, string>, FatalHandler>();
		});

		// Act & Assert
		// We expect the fatal exception to bubble, not to be wrapped in Result<string>.
		await Assert.ThrowsExactlyAsync<OutOfMemoryException>(async () => {
			await dispatcher.DispatchAsync(new FatalRequest(), this.TestContext.CancellationToken);
		});
	}

	[TestMethod]
	public async Task DispatchAsync_WhenCancelled_BubblesOperationCanceledException() {

		// Arrange
		var services = new ServiceCollection();
		var dispatcher = Shared.ArrangeSimpleDispatcher(services => {
			services.AddSingleton<IDispatcher, Dispatcher>();
			services.AddTransient<IRequestHandler<Echo, string>, CancelAwareEchoHandler>();
		});

		using var cts = new CancellationTokenSource();

		// Act
		var dispatchTask = dispatcher.DispatchAsync(new Echo("hi"), cts.Token);

		// trigger cancellation while the handler is mid-Delay
		cts.Cancel();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(async () => {
			await dispatchTask;
		});

	}


	[TestMethod]
	public async Task DispatchAsync_NonFatalException_IsConvertedToResultFailure() {

		// Arrange
		var dispatcher = Shared.ArrangeSimpleDispatcher(services => {
			// handler registration
			services.AddTransient<IRequestHandler<BoomRequest, string>, BoomHandler>();
		});

		// Act
		var result = await dispatcher.DispatchAsync(new BoomRequest(), this.TestContext.CancellationToken);

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<InvalidOperationException>(result.Error);
		Assert.AreEqual("Boom!", result.Error.Message);
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
	public async Task DispatchAsync_PropagatesCancellation_WhenHandlerHonorsToken() {

		var dispatcher = Shared.ArrangeSimpleDispatcher(sp => {
			sp.AddTransient<IRequestHandler<Echo, string>, CancelAwareEchoHandler>();
		});

		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
		var dispatchTask = dispatcher.DispatchAsync(new Echo("hello"), cts.Token);

		await Assert.ThrowsAsync<OperationCanceledException>(async () => {
			await dispatchTask;
		});

	}

	[TestMethod]
	public async Task Dispatch_abac_authorizes_appuser_role() {

		var authHandler = new AuthRequestHandler();
		var services = Shared.ArrangeServices(sp => {
			sp.AddTransient<IAuthorizationResourceValidator<AuthRequest>, AuthRequestAuthorizer>();
			sp.AddTransient<IRequestHandler<AuthRequest>>(sp => authHandler);
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
			sp.AddTransient<IAuthorizationResourceValidator<AuthRequest>, AuthAdminRequestAuthorizer>();
			sp.AddTransient<IRequestHandler<AuthRequest>>(sp => authHandler);
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