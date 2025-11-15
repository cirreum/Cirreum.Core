namespace Cirreum.Conductor.Tests;

using Cirreum.Conductor.Intercepts;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Integration tests for exception handling using the AddConductor extension method.
/// These tests verify the full DI registration and handler discovery pipeline.
/// </summary>
[TestClass]
public class ExceptionIntegrationTests {

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

	public class PingException : IRequest {
	}

	public class PingExceptionHandler : IRequestHandler<PingException> {
		public ValueTask<Result> HandleAsync(PingException request, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}

	public class PingExceptionWithResponse : IRequest<Pong> {
	}

	public class PingExceptionWithResponseHandler : IRequestHandler<PingExceptionWithResponse, Pong> {
		public ValueTask<Result<Pong>> HandleAsync(PingExceptionWithResponse request, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}

	public class FailingNotification : INotification {
	}

	public class FailingNotificationHandler : INotificationHandler<FailingNotification> {
		public Task HandleAsync(FailingNotification notification, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}

	[TestMethod]
	public async Task Should_return_failed_result_for_dispatch_when_no_handler_registered() {

		var services = Shared.ArrangeServices()
			.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(ExceptionIntegrationTests).Assembly)
					.AddOpenIntercept(typeof(Validation<,>))
					.AddOpenIntercept(typeof(Authorization<,>))
					.AddOpenIntercept(typeof(QueryCaching<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.SequentialSettings);
		var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		var result = await dispatcher.DispatchAsync(new Ping(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<InvalidOperationException>(result.Error);
	}

	[TestMethod]
	public async Task Should_return_failed_result_for_void_dispatch_when_no_handler_registered() {

		var dispatcher = Shared.ArrangeSimpleDispatcher();

		var result = await dispatcher.DispatchAsync(new VoidPing(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<InvalidOperationException>(result.Error);
	}

	[TestMethod]
	public async Task Should_not_throw_for_publish_when_no_handlers() {
		var services = Shared.ArrangeServices();
		var sp = services.BuildServiceProvider();

		var publisher = sp.GetRequiredService<IPublisher>();

		var result = await publisher.PublishAsync(new Pinged(), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task Should_throw_argument_null_exception_for_dispatch_when_request_is_null() {
		var services = Shared.ArrangeServices()
			.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(ExceptionIntegrationTests).Assembly)
					.AddOpenIntercept(typeof(Validation<,>))
					.AddOpenIntercept(typeof(Authorization<,>))
					.AddOpenIntercept(typeof(QueryCaching<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.SequentialSettings);
		var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		NullPing request = null!;

		await Assert.ThrowsExactlyAsync<ArgumentNullException>(
			async () => await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken));
	}

	[TestMethod]
	public async Task Should_throw_argument_null_exception_for_void_dispatch_when_request_is_null() {
		var services = Shared.ArrangeServices()
			.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(ExceptionIntegrationTests).Assembly)
					.AddOpenIntercept(typeof(Validation<,>))
					.AddOpenIntercept(typeof(Authorization<,>))
					.AddOpenIntercept(typeof(QueryCaching<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.SequentialSettings);
		var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		VoidNullPing request = null!;

		await Assert.ThrowsExactlyAsync<ArgumentNullException>(
			async () => await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken));
	}

	[TestMethod]
	public async Task Should_throw_argument_null_exception_for_publish_when_notification_is_null() {
		var services = Shared.ArrangeServices();
		var sp = services.BuildServiceProvider();

		var publisher = sp.GetRequiredService<IPublisher>();

		NullPinged notification = null!;

		await Assert.ThrowsExactlyAsync<ArgumentNullException>(
			async () => await publisher.PublishAsync(notification, cancellationToken: this.TestContext.CancellationToken));
	}

	[TestMethod]
	public async Task Should_return_failed_result_when_handler_throws() {
		var services = Shared.ArrangeServices()
			.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(ExceptionIntegrationTests).Assembly)
					.AddOpenIntercept(typeof(Validation<,>))
					.AddOpenIntercept(typeof(Authorization<,>))
					.AddOpenIntercept(typeof(QueryCaching<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.SequentialSettings);
		var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		var result = await dispatcher.DispatchAsync(new PingException(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<NotImplementedException>(result.Error);
	}

	[TestMethod]
	public async Task Should_return_failed_result_for_generic_dispatch_when_handler_throws() {
		var services = Shared.ArrangeServices()
			.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(ExceptionIntegrationTests).Assembly)
					.AddOpenIntercept(typeof(Validation<,>))
					.AddOpenIntercept(typeof(Authorization<,>))
					.AddOpenIntercept(typeof(QueryCaching<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.SequentialSettings);
		var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();
		var result = await dispatcher.DispatchAsync(new PingExceptionWithResponse(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<NotImplementedException>(result.Error);
	}

	[TestMethod]
	public async Task Should_capture_handler_exception_in_result_for_sequential_publish() {
		var services = Shared.ArrangeServices()
			.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(ExceptionIntegrationTests).Assembly)
					.AddOpenIntercept(typeof(Validation<,>))
					.AddOpenIntercept(typeof(Authorization<,>))
					.AddOpenIntercept(typeof(QueryCaching<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.SequentialSettings);
		var sp = services.BuildServiceProvider();

		var publisher = sp.GetRequiredService<IPublisher>();

		var result = await publisher.PublishAsync(new FailingNotification(), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<AggregateException>(result.Error);
		Assert.IsInstanceOfType<NotImplementedException>(result.Error.InnerException);
	}

	[TestMethod]
	public async Task Should_capture_handler_exception_in_result_for_parallel_publish() {
		var services = Shared.ArrangeServices()
			.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(ExceptionIntegrationTests).Assembly)
					.AddOpenIntercept(typeof(Validation<,>))
					.AddOpenIntercept(typeof(Authorization<,>))
					.AddOpenIntercept(typeof(QueryCaching<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.ParallelSettings);
		var sp = services.BuildServiceProvider();

		var publisher = sp.GetRequiredService<IPublisher>();

		var result = await publisher.PublishAsync(
			new FailingNotification(),
			cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.IsInstanceOfType<AggregateException>(result.Error);
		Assert.IsInstanceOfType<NotImplementedException>(result.Error.InnerException);
	}

	[TestMethod]
	public async Task Should_return_success_for_fire_and_forget_even_when_handler_throws() {
		var services = Shared.ArrangeServices();
		var sp = services.BuildServiceProvider();

		var publisher = sp.GetRequiredService<IPublisher>();

		var result = await publisher.PublishAsync(new FailingNotification(), cancellationToken: this.TestContext.CancellationToken);

		// Fire and forget always returns success immediately
		Assert.IsTrue(result.IsSuccess);
	}

	public TestContext TestContext { get; set; } = null!;
}