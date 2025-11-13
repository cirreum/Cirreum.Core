namespace Cirreum.Conductor.Tests;

using Cirreum.Conductor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
public sealed class PublisherTests {

	private sealed record Tick(int Value) : INotification;

	[PublishingStrategy(PublisherStrategy.FailFast)]
	private sealed record Marked(int Value) : INotification;

	private sealed class CountTickHandler(Action<Tick> onHandle) : INotificationHandler<Tick> {
		public Task HandleAsync(Tick notification, CancellationToken cancellationToken = default) {
			onHandle(notification);
			return Task.CompletedTask;
		}
	}

	private sealed class CountMarkedHandler(Action<Marked> onHandle) : INotificationHandler<Marked> {
		public Task HandleAsync(Marked notification, CancellationToken cancellationToken = default) {
			onHandle(notification);
			return Task.CompletedTask;
		}
	}

	private sealed class ThrowingTickHandler : INotificationHandler<Tick> {
		public Task HandleAsync(Tick notification, CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("kaboom");
	}

	private sealed class ThrowingMarkedHandler : INotificationHandler<Marked> {
		public Task HandleAsync(Marked notification, CancellationToken cancellationToken = default)
			=> throw new InvalidOperationException("kaboom");
	}

	private static (ServiceProvider sp, IPublisher publisher) MakePublisher<TNotification>(
		IEnumerable<object> handlers,
		PublisherStrategy defaultStrategy = PublisherStrategy.Sequential)
		where TNotification : INotification {

		var services = new ServiceCollection();

		foreach (var h in handlers) {
			services.AddTransient(typeof(INotificationHandler<TNotification>), _ => h);
		}

		services.AddSingleton<IPublisher>(sp =>
			new Publisher(sp, defaultStrategy, NullLogger<Publisher>.Instance));

		var sp = services.BuildServiceProvider();
		return (sp, sp.GetRequiredService<IPublisher>());
	}

	[TestMethod]
	public async Task Publish_NoHandlers_ReturnsOk() {
		var (_, publisher) = MakePublisher<Tick>([]);

		var result = await publisher.PublishAsync(new Tick(1), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task Publish_Sequential_AllHandlersInvoked() {
		var seen = new List<int>();
		var (_, publisher) = MakePublisher<Tick>(
		[
			new CountTickHandler(t => seen.Add(t.Value)),
			new CountTickHandler(t => seen.Add(t.Value + 10)),
		], defaultStrategy: PublisherStrategy.Sequential);

		var result = await publisher.PublishAsync(new Tick(5), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
		CollectionAssert.AreEqual(expected, seen);
	}

	[TestMethod]
	public async Task Publish_FailFast_StopsAfterFirstFailure() {
		var seen = new List<int>();
		var (_, publisher) = MakePublisher<Tick>(
		[
			new ThrowingTickHandler(),
			new CountTickHandler(t => seen.Add(t.Value)), // should be skipped
		], defaultStrategy: PublisherStrategy.FailFast);

		var result = await publisher.PublishAsync(new Tick(7), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(seen);
	}

	[TestMethod]
	public async Task Publish_Parallel_InvokesAll_AndAggregatesFailures() {
		var seen = new List<int>();
		var (_, publisher) = MakePublisher<Tick>(
		[
			new CountTickHandler(t => { lock (seen) { seen.Add(t.Value); } }),
			new ThrowingTickHandler(),
			new CountTickHandler(t => { lock (seen) { seen.Add(t.Value + 100); } }),
		], defaultStrategy: PublisherStrategy.Parallel);

		var result = await publisher.PublishAsync(new Tick(3), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess); // one handler throws
		seen.Sort();
		CollectionAssert.AreEquivalent(expectedArray, seen);
	}

	[TestMethod]
	public async Task Publish_FireAndForget_ReturnsOk_AndHandlerRunsSoonAfter() {
		var seen = 0;
		var (_, publisher) = MakePublisher<Tick>(
		[
			new CountTickHandler(_ => Interlocked.Increment(ref seen))
		], defaultStrategy: PublisherStrategy.FireAndForget);

		var result = await publisher.PublishAsync(new Tick(1), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
		await Task.Delay(50, this.TestContext.CancellationToken); // tiny window for background task
		Assert.AreEqual(1, Volatile.Read(ref seen));
	}

	[TestMethod]
	public async Task Publish_Sequential_ContinuesAfterFailure() {
		// Sequential strategy should continue executing subsequent handlers even if one fails.
		var seen = new List<int>();
		var (_, publisher) = MakePublisher<Tick>(
		[
			new ThrowingTickHandler(),
			new CountTickHandler(t => seen.Add(t.Value)), // should still run under Sequential
		], defaultStrategy: PublisherStrategy.Sequential);

		var result = await publisher.PublishAsync(new Tick(9), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		var expected_array_9 = new[] { 9 };
		CollectionAssert.AreEqual(expected_array_9, seen);
	}

	[TestMethod]
	public async Task Publish_ExplicitStrategy_OverridesDefault() {
		// Default: Sequential, but explicitly request FailFast -> subsequent handlers skipped.
		var seenA = new List<int>();
		var (_, publisherA) = MakePublisher<Tick>(
		[
			new ThrowingTickHandler(),
			new CountTickHandler(t => seenA.Add(t.Value)),
		], defaultStrategy: PublisherStrategy.Sequential);

		var resultA = await publisherA.PublishAsync(new Tick(11), strategy: PublisherStrategy.FailFast, this.TestContext.CancellationToken);
		Assert.IsFalse(resultA.IsSuccess);
		Assert.IsEmpty(seenA);

		// Default: FailFast, but explicitly request Sequential -> subsequent handlers should run.
		var seenB = new List<int>();
		var (_, publisherB) = MakePublisher<Tick>(
		[
			new ThrowingTickHandler(),
			new CountTickHandler(t => seenB.Add(t.Value)),
		], defaultStrategy: PublisherStrategy.FailFast);

		var resultB = await publisherB.PublishAsync(new Tick(12), strategy: PublisherStrategy.Sequential, this.TestContext.CancellationToken);
		Assert.IsFalse(resultB.IsSuccess);
		CollectionAssert.AreEqual(expectedArray0, seenB);
	}

	[TestMethod]
	public async Task Publish_uses_attribute_when_no_explicit_strategy() {
		var seen = new List<int>();
		var (_, publisher) = MakePublisher<Marked>([
			new ThrowingMarkedHandler(),
			new CountMarkedHandler(t => seen.Add(t.Value)), // should be skipped in FailFast (from attribute)
		]);

		var result = await publisher.PublishAsync(
			new Marked(42),
			cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsEmpty(seen);
	}

	[TestMethod]
	public async Task Publish_explicit_strategy_overrides_attribute() {
		var seen = new List<int>();
		var (_, publisher) = MakePublisher<Marked>([
			new ThrowingMarkedHandler(),
			new CountMarkedHandler(t => seen.Add(t.Value)), // should run under Sequential override
		]);

		var result = await publisher.PublishAsync(new Marked(7), strategy: PublisherStrategy.Sequential,
			cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		CollectionAssert.AreEqual(expectedArray1, seen);
	}

	[TestMethod]
	public async Task Publish_parallel_aggregates_all_failures_with_inner_exceptions() {
		var (_, publisher) = MakePublisher<Tick>([
			new ThrowingTickHandler(),
		new ThrowingTickHandler(),
	], defaultStrategy: PublisherStrategy.Parallel);

		var result = await publisher.PublishAsync(new Tick(1), cancellationToken: this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		var agg = (AggregateException)result.Error!;
		Assert.HasCount(2, agg.InnerExceptions);
		Assert.IsTrue(agg.InnerExceptions.All(e => e is InvalidOperationException));
	}

	public TestContext TestContext { get; set; }

	private static readonly int[] expected = [5, 15];
	private static readonly int[] expectedArray = [3, 103];
	private static readonly int[] expectedArray0 = [12];
	private static readonly int[] expectedArray1 = [7];
}
