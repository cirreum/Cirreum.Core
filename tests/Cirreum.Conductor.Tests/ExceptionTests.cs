namespace Cirreum.Conductor.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[TestClass]
public class ExceptionTests {

	private IDispatcher _dispatcher = null!;
	private IPublisher _publisher = null!;

	public class Ping : IRequest<Pong> {
	}

	public class Pong {
	}

	public class VoidPing : IRequest {
	}

	public class Pinged : INotification {
	}

	public class NullPing : IRequest<Pong> {
	}

	public class VoidNullPing : IRequest {
	}

	public class NullPinged : INotification {
	}

	public class NullPingHandler : IRequestHandler<NullPing, Pong> {
		public ValueTask<Result<Pong>> HandleAsync(NullPing request, CancellationToken cancellationToken) {
			return ValueTask.FromResult(Result<Pong>.Success(new Pong()));
		}
	}

	public class VoidNullPingHandler : IRequestHandler<VoidNullPing> {
		public ValueTask<Result> HandleAsync(VoidNullPing request, CancellationToken cancellationToken) {
			return ValueTask.FromResult(Result.Success);
		}
	}

	[TestInitialize]
	public void Setup() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IDispatcher, Dispatcher>();
		services.AddSingleton<IPublisher>(sp =>
			new Publisher(sp, PublisherStrategy.Sequential, sp.GetRequiredService<ILogger<Publisher>>()));

		var serviceProvider = services.BuildServiceProvider();
		this._dispatcher = serviceProvider.GetRequiredService<IDispatcher>();
		this._publisher = serviceProvider.GetRequiredService<IPublisher>();
	}

	[TestMethod]
	public async Task Should_return_failed_result_for_dispatch_when_no_handler_registered() {
		var result = await this._dispatcher.DispatchAsync(new Ping(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<InvalidOperationException>(result.Error);
	}

	[TestMethod]
	public async Task Should_return_failed_result_for_void_dispatch_when_no_handler_registered() {
		var result = await this._dispatcher.DispatchAsync(new VoidPing(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<InvalidOperationException>(result.Error);
	}

	[TestMethod]
	public async Task Should_not_throw_for_publish_when_no_handlers() {
		var result = await this._publisher.PublishAsync(new Pinged(), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task Should_throw_argument_null_exception_for_dispatch_when_request_is_null() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IRequestHandler<NullPing, Pong>, NullPingHandler>();
		services.AddSingleton<IDispatcher, Dispatcher>();

		var serviceProvider = services.BuildServiceProvider();
		var dispatcher = serviceProvider.GetRequiredService<IDispatcher>();

		NullPing request = null!;

		// ArgumentNullException is still thrown directly by Dispatcher (ArgumentNullException.ThrowIfNull)
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(
			async () => await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken));
	}

	[TestMethod]
	public async Task Should_throw_argument_null_exception_for_void_dispatch_when_request_is_null() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IRequestHandler<VoidNullPing>, VoidNullPingHandler>();
		services.AddSingleton<IDispatcher, Dispatcher>();

		var serviceProvider = services.BuildServiceProvider();
		var dispatcher = serviceProvider.GetRequiredService<IDispatcher>();

		VoidNullPing request = null!;

		// ArgumentNullException is still thrown directly by Dispatcher (ArgumentNullException.ThrowIfNull)
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(
			async () => await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken));
	}

	[TestMethod]
	public async Task Should_throw_argument_null_exception_for_publish_when_notification_is_null() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IPublisher>(sp =>
			new Publisher(sp, PublisherStrategy.Sequential, sp.GetRequiredService<ILogger<Publisher>>()));

		var serviceProvider = services.BuildServiceProvider();
		var publisher = serviceProvider.GetRequiredService<IPublisher>();

		NullPinged notification = null!;

		// ArgumentNullException is still thrown directly by Publisher (ArgumentNullException.ThrowIfNull)
		await Assert.ThrowsExactlyAsync<ArgumentNullException>(
			async () => await publisher.PublishAsync(notification, cancellationToken: this.TestContext.CancellationToken));
	}

	[TestMethod]
	public async Task No_handler_registered_returns_failed_for_both_void_and_typed() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IDispatcher, Dispatcher>();

		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		var a = await dispatcher.DispatchAsync(new Ping(), this.TestContext.CancellationToken);
		var b = await dispatcher.DispatchAsync(new VoidPing(), this.TestContext.CancellationToken);

		Assert.IsFalse(a.IsSuccess);
		Assert.IsInstanceOfType<InvalidOperationException>(a.Error);
		Assert.IsFalse(b.IsSuccess);
		Assert.IsInstanceOfType<InvalidOperationException>(b.Error);
	}


	public class PingException : IRequest {
	}

	public class PingExceptionHandler : IRequestHandler<PingException> {
		public ValueTask<Result> HandleAsync(PingException request, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}

	[TestMethod]
	public async Task Should_return_failed_result_when_handler_throws() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IRequestHandler<PingException>, PingExceptionHandler>();
		services.AddSingleton<IDispatcher, Dispatcher>();

		var serviceProvider = services.BuildServiceProvider();
		var dispatcher = serviceProvider.GetRequiredService<IDispatcher>();

		var result = await dispatcher.DispatchAsync(new PingException(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<NotImplementedException>(result.Error);
	}

	public class PingExceptionWithResponse : IRequest<Pong> {
	}

	public class PingExceptionWithResponseHandler : IRequestHandler<PingExceptionWithResponse, Pong> {
		public ValueTask<Result<Pong>> HandleAsync(PingExceptionWithResponse request, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}

	[TestMethod]
	public async Task Should_return_failed_result_for_generic_dispatch_when_handler_throws() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IRequestHandler<PingExceptionWithResponse, Pong>, PingExceptionWithResponseHandler>();
		services.AddSingleton<IDispatcher, Dispatcher>();

		var serviceProvider = services.BuildServiceProvider();
		var dispatcher = serviceProvider.GetRequiredService<IDispatcher>();

		var result = await dispatcher.DispatchAsync(new PingExceptionWithResponse(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<NotImplementedException>(result.Error);
	}

	public class FailingNotification : INotification {
	}

	public class FailingNotificationHandler : INotificationHandler<FailingNotification> {
		public Task HandleAsync(FailingNotification notification, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}

	[TestMethod]
	public async Task Should_capture_handler_exception_in_result_for_sequential_publish() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<INotificationHandler<FailingNotification>, FailingNotificationHandler>();
		services.AddSingleton<IPublisher>(sp =>
			new Publisher(sp, PublisherStrategy.Sequential, sp.GetRequiredService<ILogger<Publisher>>()));

		var serviceProvider = services.BuildServiceProvider();
		var publisher = serviceProvider.GetRequiredService<IPublisher>();

		var result = await publisher.PublishAsync(new FailingNotification(), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<AggregateException>(result.Error);
	}

	[TestMethod]
	public async Task Should_capture_handler_exception_in_result_for_parallel_publish() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<INotificationHandler<FailingNotification>, FailingNotificationHandler>();
		services.AddSingleton<IPublisher>(sp =>
			new Publisher(sp, PublisherStrategy.Parallel, sp.GetRequiredService<ILogger<Publisher>>()));

		var serviceProvider = services.BuildServiceProvider();
		var publisher = serviceProvider.GetRequiredService<IPublisher>();

		var result = await publisher.PublishAsync(new FailingNotification(), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<AggregateException>(result.Error);
	}

	[TestMethod]
	public async Task Should_return_success_for_fire_and_forget_even_when_handler_throws() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<INotificationHandler<FailingNotification>, FailingNotificationHandler>();
		services.AddSingleton<IPublisher>(sp =>
			new Publisher(sp, PublisherStrategy.FireAndForget, sp.GetRequiredService<ILogger<Publisher>>()));

		var serviceProvider = services.BuildServiceProvider();
		var publisher = serviceProvider.GetRequiredService<IPublisher>();

		var result = await publisher.PublishAsync(new FailingNotification(), cancellationToken: this.TestContext.CancellationToken);

		// Fire and forget always returns success immediately
		Assert.IsTrue(result.IsSuccess);
	}

	public TestContext TestContext { get; set; } = null!;

}