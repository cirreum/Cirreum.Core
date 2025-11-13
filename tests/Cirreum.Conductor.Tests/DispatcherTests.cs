// Conductor.Tests/DispatcherTests.cs
namespace Cirreum.Conductor.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

[TestClass]
public sealed class DispatcherTests {

	// ===== Fakes =====
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

	[TestMethod]
	public async Task Dispatch_VoidRequest_InvokesHandler_AndReturnsOk() {
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IDispatcher>(sp =>
			new Dispatcher(sp, NullLogger<Dispatcher>.Instance));
		var pingHandler = new PingHandler();
		services.AddTransient<IRequestHandler<Ping>>(_ => pingHandler);

		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		// Act
		var result = await dispatcher.DispatchAsync(new Ping(), this.TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.IsTrue(pingHandler.Called);
	}

	[TestMethod]
	public async Task Dispatch_TResponseRequest_InvokesHandler_AndReturnsPayload() {
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IDispatcher>(sp =>
			new Dispatcher(sp, NullLogger<Dispatcher>.Instance));
		services.AddTransient<IRequestHandler<Echo, string>, EchoHandler>();

		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		// Act
		var result = await dispatcher.DispatchAsync(new Echo("hello"), this.TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual("hello", result.Value);
	}

	[TestMethod]
	public async Task Dispatch_VoidRequest_PropagatesFailure() {
		// Arrange
		var services = new ServiceCollection();
		services.AddSingleton<IDispatcher>(sp =>
			new Dispatcher(sp, NullLogger<Dispatcher>.Instance));
		services.AddTransient<IRequestHandler<Fail>, FailHandler>();

		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		// Act
		var result = await dispatcher.DispatchAsync(new Fail(), this.TestContext.CancellationToken);

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
	}

	[TestMethod]
	public async Task Dispatch_honors_cancellation_token() {
		var services = new ServiceCollection();
		services.AddSingleton<IDispatcher>(sp => new Dispatcher(sp, NullLogger<Dispatcher>.Instance));
		services.AddTransient<IRequestHandler<Echo, string>, CancelAwareEchoHandler>();

		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
		var result = await dispatcher.DispatchAsync(new Echo("hello"), cts.Token);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsInstanceOfType<TaskCanceledException>(result.Error);
	}

	public sealed class CancelAwareEchoHandler : IRequestHandler<Echo, string> {
		public async ValueTask<Result<string>> HandleAsync(Echo r, CancellationToken ct) {
			await Task.Delay(200, ct); // should cancel
			return Result<string>.Success(r.Text);
		}
	}

	public TestContext TestContext { get; set; }

}