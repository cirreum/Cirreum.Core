namespace Cirreum.Conductor.Tests;

using Cirreum.Authorization;
using Cirreum.Conductor;
using Cirreum.Conductor.Configuration;
using Cirreum.Conductor.Intercepts;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

/// <summary>
/// Tests for intercepts including open generic intercepts.
/// Validates that the pipeline correctly executes intercepts before/after handlers.
/// </summary>
[TestClass]
[DoNotParallelize]
public class InterceptTests {

	// Test data
	public static readonly List<string> ExecutionLog = [];


	// Simple request/handler for testing
	public class TestRequest : IRequest<string> {
		public string Value { get; set; } = "";
	}
	public class TestRequestHandler : IRequestHandler<TestRequest, string> {
		public ValueTask<Result<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken) {
			ExecutionLog.Add("Handler executed");
			return ValueTask.FromResult(Result<string>.Success($"Handled: {request.Value}"));
		}
	}


	// Test with a different request type to prove open generics work for any type
	public class AnotherRequest : IRequest<int> {
		public int Value { get; set; }
	}
	public class AnotherRequestHandler : IRequestHandler<AnotherRequest, int> {
		public ValueTask<Result<int>> HandleAsync(AnotherRequest request, CancellationToken cancellationToken) {
			ExecutionLog.Add("AnotherHandler executed");
			return ValueTask.FromResult(Result<int>.Success(request.Value * 2));
		}
	}


	// Test void request with intercept
	public class VoidTestRequest : IRequest {
		public string Value { get; set; } = "";
	}
	public class VoidTestRequestHandler : IRequestHandler<VoidTestRequest> {
		public ValueTask<Result> HandleAsync(VoidTestRequest request, CancellationToken cancellationToken) {
			ExecutionLog.Add("VoidHandler executed");
			return ValueTask.FromResult(Result.Success);
		}
	}

	// Another request type for open generic intercept testing
	public sealed class DupRequest : IRequest<string> {
		public string Value { get; init; } = "";
	}
	public sealed class DupHandler : IRequestHandler<DupRequest, string> {
		public ValueTask<Result<string>> HandleAsync(DupRequest r, CancellationToken ct)
			=> ValueTask.FromResult(Result<string>.Success($"Handled: {r.Value}"));
	}

	// Request/handler that throws to test error handling intercept
	public sealed class ErrRequest : IRequest<string> { }
	public sealed class ThrowingErrHandler : IRequestHandler<ErrRequest, string> {
		public ValueTask<Result<string>> HandleAsync(ErrRequest r, CancellationToken ct)
			=> throw new InvalidOperationException("boom");
	}

	// For Authorization tests
	public record AuthorizableTestRequest(string UserId)
		: IAuthorizableRequest<string>;
	public class AuthorizableTestHandler : IRequestHandler<AuthorizableTestRequest, string> {
		public ValueTask<Result<string>> HandleAsync(
			AuthorizableTestRequest request,
			CancellationToken cancellationToken) {
			return ValueTask.FromResult(Result<string>.Success($"Authorized: {request.UserId}"));
		}
	}

	// For QueryCaching tests
	public record CacheableTestQuery(int Id)
		: ICacheableQuery<string> {
		public string CacheKey => $"test-{this.Id}";
		public QueryCacheSettings Cache => new() {
			Expiration = TimeSpan.FromMinutes(10)
		};
		public string[]? CacheTags { get; } = ["test-queries"];
		public string CacheCategory => "TestQueries";
	}
	public class CacheableTestHandler : IRequestHandler<CacheableTestQuery, string> {
		public ValueTask<Result<string>> HandleAsync(
			CacheableTestQuery request,
			CancellationToken cancellationToken) {
			return ValueTask.FromResult(Result<string>.Success($"Data for {request.Id}"));
		}
	}



	// Open generic intercept - logs all requests
	public class LoggingIntercept<TRequest, TResponse> : IIntercept<TRequest, TResponse>
		where TRequest : notnull {

		public async ValueTask<Result<TResponse>> HandleAsync(RequestContext<TRequest> context, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
			ExecutionLog.Add($"LoggingIntercept: Before {typeof(TRequest).Name}");
			var result = await next(cancellationToken);
			ExecutionLog.Add($"LoggingIntercept: After {typeof(TRequest).Name}");
			return result;
		}
	}

	// Another open generic intercept - adds timing
	public class TimingIntercept<TRequest, TResponse> : IIntercept<TRequest, TResponse>
		where TRequest : notnull {

		public async ValueTask<Result<TResponse>> HandleAsync(RequestContext<TRequest> context, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
			ExecutionLog.Add($"TimingIntercept: Start {typeof(TRequest).Name}");
			var result = await next(cancellationToken);
			ExecutionLog.Add($"TimingIntercept: End {typeof(TRequest).Name}");
			return result;
		}
	}

	// Specific intercept for TestRequest only
	public class SpecificIntercept : IIntercept<TestRequest, string> {
		public async ValueTask<Result<string>> HandleAsync(RequestContext<TestRequest> context, RequestHandlerDelegate<string> next, CancellationToken cancellationToken) {
			ExecutionLog.Add("SpecificIntercept: Before");
			var result = await next(cancellationToken);
			ExecutionLog.Add("SpecificIntercept: After");
			return result;
		}
	}

	// Intercept that modifies the result
	public class ResultModifyingIntercept<TRequest, TResponse> : IIntercept<TRequest, TResponse>
		where TRequest : notnull {
		public async ValueTask<Result<TResponse>> HandleAsync(RequestContext<TRequest> context, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
			var result = await next(cancellationToken);

			if (result.IsSuccess && result.Value is string str) {
				// Modify the result
				var modified = str + " [Modified by intercept]";
				return Result<TResponse>.Success((TResponse)(object)modified);
			}

			return result;
		}
	}

	// Intercept that handles errors
	public class ErrorHandlingIntercept<TRequest, TResponse> : IIntercept<TRequest, TResponse>
		where TRequest : notnull {

		public async ValueTask<Result<TResponse>> HandleAsync(RequestContext<TRequest> context, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
			try {
				return await next(cancellationToken);
			} catch (Exception ex) {
				ExecutionLog.Add($"ErrorHandlingIntercept: Caught {ex.GetType().Name}");
				return Result<TResponse>.Fail(ex);
			}
		}

	}

	public sealed class ShortCircuitIntercept : IIntercept<TestRequest, string> {
		public ValueTask<Result<string>> HandleAsync(
			RequestContext<TestRequest> context,
			RequestHandlerDelegate<string> next,
			CancellationToken cancellationToken) => ValueTask.FromResult(Result<string>.Fail(new("blocked")));
	}

	public sealed class VoidShortCircuit : IIntercept<VoidTestRequest, Unit> {
		public ValueTask<Result<Unit>> HandleAsync(
			RequestContext<VoidTestRequest> context,
			RequestHandlerDelegate<Unit> next,
			CancellationToken cancellationToken) => ValueTask.FromResult(Result<Unit>.Fail(new("nope")));
	}


	[TestInitialize]
	public void Setup() {
		ExecutionLog.Clear();
	}

	[TestMethod]
	public async Task Should_register_and_execute_open_generic_intercept() {
		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddOpenIntercept(typeof(LoggingIntercept<,>));
			}, Shared.SequentialSettings);
		});

		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var request = new TestRequest { Value = "test" };
		var result = await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);

		// Verify intercept executed
		Assert.Contains("LoggingIntercept: Before TestRequest",
			ExecutionLog, "Open generic intercept should execute before handler");
		Assert.Contains("Handler executed",
			ExecutionLog, "Handler should execute");
		Assert.Contains("LoggingIntercept: After TestRequest",
			ExecutionLog, "Open generic intercept should execute after handler");
	}

	[TestMethod]
	public async Task Should_execute_multiple_intercepts_in_order() {
		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddOpenIntercept(typeof(LoggingIntercept<,>))
					.AddOpenIntercept(typeof(TimingIntercept<,>));
			}, Shared.SequentialSettings);
		});

		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var request = new TestRequest { Value = "test" };
		var result = await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);

		// Verify execution order (intercepts wrap in reverse order of registration)
		var beforeHandler = ExecutionLog.TakeWhile(log => !log.Contains("Handler executed")).ToList();
		var afterHandler = ExecutionLog.SkipWhile(log => !log.Contains("Handler executed")).Skip(1).ToList();

		Assert.IsTrue(beforeHandler.Any(log => log.Contains("Intercept: Before") || log.Contains("Intercept: Start")),
			"Intercepts should execute before handler");
		Assert.IsTrue(afterHandler.Any(log => log.Contains("Intercept: After") || log.Contains("Intercept: End")),
			"Intercepts should execute after handler");
	}

	[TestMethod]
	public async Task Should_allow_intercept_to_modify_result() {
		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddOpenIntercept(typeof(ResultModifyingIntercept<,>))
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.SequentialSettings);
		});

		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var resolved = provider.GetServices<IIntercept<TestRequest, string>>()
			   .Select(t => t.GetType().Name)
			   .ToList();
		CollectionAssert.Contains(resolved, typeof(ResultModifyingIntercept<,>).Name);

		var request = new TestRequest { Value = "test" };
		var result = await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
		Assert.Contains("[Modified by intercept]",
			result.Value, "Intercept should be able to modify the result");
	}

	[TestMethod]
	public void Should_register_both_open_and_closed_intercepts() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
				.AddIntercept<SpecificIntercept>()
				.AddOpenIntercept(typeof(LoggingIntercept<,>));
		}, Shared.SequentialSettings);

		var provider = services.BuildServiceProvider();

		// Verify open generic intercepts are registered
		var loggingIntercepts = provider.GetServices<IIntercept<TestRequest, string>>()
			.Where(i => i.GetType().IsGenericType &&
				i.GetType().GetGenericTypeDefinition() == typeof(LoggingIntercept<,>))
			.ToList();

		Assert.IsNotEmpty(loggingIntercepts,
			"Open generic logging intercept should be registered");

		// Verify specific intercept is registered
		var specificIntercepts = provider.GetServices<IIntercept<TestRequest, string>>()
			.Where(i => i.GetType() == typeof(SpecificIntercept))
			.ToList();

		Assert.IsNotEmpty(specificIntercepts,
			"Specific intercept should be registered");
	}

	[TestMethod]
	public async Task Should_apply_open_generic_intercept_to_different_request_types() {
		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddOpenIntercept(typeof(LoggingIntercept<,>));
			}, Shared.SequentialSettings);
		});
		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var request = new AnotherRequest { Value = 5 };
		var result = await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(10, result.Value);

		// Verify the SAME open generic intercept worked for a different request type
		Assert.Contains("LoggingIntercept: Before AnotherRequest",
			ExecutionLog, "Open generic intercept should work for any request type");
		Assert.Contains("LoggingIntercept: After AnotherRequest",
			ExecutionLog, "Open generic intercept should work for any request type");
	}

	[TestMethod]
	public async Task Should_apply_intercepts_to_void_requests() {
		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddOpenIntercept(typeof(LoggingIntercept<,>));
			}, Shared.SequentialSettings);
		});

		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var request = new VoidTestRequest { Value = "test" };
		var result = await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
		Assert.Contains("VoidHandler executed",
			ExecutionLog, "Handler should execute for void request");
		Assert.IsTrue(
			ExecutionLog.Exists(entry => entry.Contains("LoggingIntercept")),
			"LoggingIntercept should execute for void request");

	}

	[TestMethod]
	public async Task Should_allow_intercept_to_short_circuit_handler() {
		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddIntercept<ShortCircuitIntercept>()
					.AddOpenIntercept(typeof(HandlerPerformance<,>));
			}, Shared.SequentialSettings);
		});


		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		var result = await dispatcher.DispatchAsync(new TestRequest { Value = "x" }, this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.IsTrue(ExecutionLog.All(l => l != "Handler executed"),
			"Handler must not run when short-circuited");
	}

	[TestMethod]
	public async Task Open_generic_then_Specific_order_is_preserved() {
		// Arrange
		ExecutionLog.Clear();

		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddOpenIntercept(typeof(LoggingIntercept<,>))
					.AddIntercept<SpecificIntercept>();
			}, Shared.SequentialSettings);
		});

		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		// Act
		await dispatcher.DispatchAsync(new TestRequest { Value = "x" }, this.TestContext.CancellationToken);

		// Assert: expected sequence based on registration order (outer to inner)
		// Open generic (Logging) wraps Specific; unwinds after the handler.
		var log = ExecutionLog.ToList(); // snapshot defensively in case of async interleaving

		// Helpful failure dump
		string Dump() => string.Join(Environment.NewLine, log);

		// Find key anchors
		var iLogBefore = log.FindIndex(s => s == "LoggingIntercept: Before TestRequest");
		var iSpecBefore = log.FindIndex(s => s == "SpecificIntercept: Before");
		var iHandler = log.FindIndex(s => s == "Handler executed");
		var iSpecAfter = log.FindIndex(s => s == "SpecificIntercept: After");
		var iLogAfter = log.FindIndex(s => s == "LoggingIntercept: After TestRequest");

		// Basic presence checks
		Assert.IsGreaterThanOrEqualTo(0, iLogBefore, $"Missing 'LoggingIntercept: Before'. Log:\n{Dump()}");
		Assert.IsGreaterThanOrEqualTo(0, iSpecBefore, $"Missing 'SpecificIntercept: Before'. Log:\n{Dump()}");
		Assert.IsGreaterThanOrEqualTo(0, iHandler, $"Missing 'Handler executed'. Log:\n{Dump()}");
		Assert.IsGreaterThanOrEqualTo(0, iSpecAfter, $"Missing 'SpecificIntercept: After'. Log:\n{Dump()}");
		Assert.IsGreaterThanOrEqualTo(0, iLogAfter, $"Missing 'LoggingIntercept: After'. Log:\n{Dump()}");

		// Relative order checks (strict)
		Assert.IsLessThan(iSpecBefore, iLogBefore, $"Expected Logging BEFORE Specific. Log:\n{Dump()}");
		Assert.IsLessThan(iHandler, iSpecBefore, $"Expected Specific BEFORE Handler. Log:\n{Dump()}");
		Assert.IsLessThan(iSpecAfter, iHandler, $"Expected Handler BEFORE Specific AFTER. Log:\n{Dump()}");
		Assert.IsLessThan(iLogAfter, iSpecAfter, $"Expected Specific AFTER BEFORE Logging AFTER. Log:\n{Dump()}");

	}


	[TestMethod]
	public async Task Void_request_intercept_can_short_circuit_with_failure() {
		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddIntercept<VoidShortCircuit>();
			}, Shared.SequentialSettings);
		});
		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();
		var result = await dispatcher.DispatchAsync(new VoidTestRequest(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);

	}

	[TestMethod]
	public async Task ErrorHandlingIntercept_should_convert_handler_throw_to_failed_result() {
		var services = Shared.ArrangeServices(services => {
			services.AddConductor(builder => {
				builder
					.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
					.AddOpenIntercept(typeof(ErrorHandlingIntercept<,>));
			}, Shared.SequentialSettings);
		});

		using var sp = services.BuildServiceProvider();
		var dispatcher = sp.GetRequiredService<IDispatcher>();

		var result = await dispatcher.DispatchAsync(new ErrRequest(), this.TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);

		// ErrorHandlingIntercept<,> in InterceptTests catches and logs
		Assert.IsTrue(ExecutionLog.Any(l => l.StartsWith("ErrorHandlingIntercept: Caught")),
			"ErrorHandlingIntercept should log caught exception");

	}

	[TestMethod]
	public void AddConductor_registers_all_intercepts() {
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();

		// Act
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddOpenIntercept(typeof(Authorization<,>))
				.AddOpenIntercept(typeof(QueryCaching<,>))
				.AddOpenIntercept(typeof(ErrorHandlingIntercept<,>))
				.AddIntercept<ShortCircuitIntercept>()
				.AddOpenIntercept(typeof(HandlerPerformance<,>));
		}, Shared.SequentialSettings);

		var sp = services.BuildServiceProvider();

		// Assert - Get all registered intercept services
		var interceptDescriptors = services
			.Where(sd => sd.ServiceType.IsGenericType &&
						 sd.ServiceType.GetGenericTypeDefinition() == typeof(IIntercept<,>))
			.ToList();

		// Should have 6 registrations
		Assert.HasCount(6, interceptDescriptors,
			"Should register 6 intercepts");

		// Verify each specific intercept type
		var implementationTypes = interceptDescriptors
			.Where(sd => sd.ImplementationType?.IsGenericType ?? false)
			.Select(sd => sd.ImplementationType?.GetGenericTypeDefinition())
			.ToList();

		// Should have 5 open-generic registrations
		Assert.HasCount(5, implementationTypes,
			"Should register 5 open-generic intercepts");

		CollectionAssert.Contains(implementationTypes, typeof(Validation<,>));
		CollectionAssert.Contains(implementationTypes, typeof(Authorization<,>));
		CollectionAssert.Contains(implementationTypes, typeof(QueryCaching<,>));
		CollectionAssert.Contains(implementationTypes, typeof(ErrorHandlingIntercept<,>));
		CollectionAssert.Contains(implementationTypes, typeof(HandlerPerformance<,>));

	}

	[TestMethod]
	public void AddConductor_intercepts_resolve_for_specific_request() {
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();

		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddOpenIntercept(typeof(Authorization<,>))
				.AddOpenIntercept(typeof(QueryCaching<,>))
				.AddOpenIntercept(typeof(ErrorHandlingIntercept<,>))
				.AddOpenIntercept(typeof(HandlerPerformance<,>));
		}, Shared.SequentialSettings);

		var sp = services.BuildServiceProvider();

		// Act - Resolve intercepts for a specific request type
		var intercepts = sp.GetServices<IIntercept<ErrRequest, Unit>>().ToList();

		// Assert
		Assert.HasCount(3, intercepts,
			"Should resolve 3 intercepts for ErrRequest");

		Assert.IsInstanceOfType<Validation<ErrRequest, Unit>>(intercepts[0]);
		Assert.IsInstanceOfType<ErrorHandlingIntercept<ErrRequest, Unit>>(intercepts[1]);
		Assert.IsInstanceOfType<HandlerPerformance<ErrRequest, Unit>>(intercepts[2]);

	}

	[TestMethod]
	public void AddConductor_registers_closed_generic_intercept() {
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();

		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
				.AddIntercept<ShortCircuitIntercept>(); // Closed generic
		}, Shared.SequentialSettings);

		var sp = services.BuildServiceProvider();

		// Assert - Check service descriptors
		var interceptDescriptor = services
			.FirstOrDefault(sd =>
				sd.ServiceType == typeof(IIntercept<TestRequest, string>) &&
				sd.ImplementationType == typeof(ShortCircuitIntercept));

		Assert.IsNotNull(interceptDescriptor,
			"ShortCircuitIntercept should be registered");

		// Verify it resolves
		var intercept = sp.GetService<IIntercept<TestRequest, string>>();
		Assert.IsNotNull(intercept);
		Assert.IsInstanceOfType<ShortCircuitIntercept>(intercept);

	}

	[TestMethod]
	public void AddConductor_registers_intercepts_in_order() {
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();

		// Act
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))      // 1st
				.AddOpenIntercept(typeof(Authorization<,>))   // 2nd
				.AddOpenIntercept(typeof(QueryCaching<,>))    // 3rd
				.AddOpenIntercept(typeof(HandlerPerformance<,>));    // 4th
		}, Shared.SequentialSettings);

		var sp = services.BuildServiceProvider();

		// Assert - Order matters for intercepts
		var intercepts = sp.GetServices<IIntercept<ErrRequest, Unit>>().ToList();

		Assert.HasCount(2, intercepts);
		Assert.IsInstanceOfType<Validation<ErrRequest, Unit>>(intercepts[0], "First should be Validation");
		Assert.IsInstanceOfType<HandlerPerformance<ErrRequest, Unit>>(intercepts[1], "Second should be Performance");

	}

	[TestMethod]
	public void AddConductor_registers_mixed_open_and_closed_intercepts() {
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();

		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddIntercept<ShortCircuitIntercept>()  // Closed
				.AddOpenIntercept(typeof(HandlerPerformance<,>));
		}, Shared.SequentialSettings);

		var sp = services.BuildServiceProvider();

		// Act
		var intercepts = sp.GetServices<IIntercept<TestRequest, string>>().ToList();

		// Assert
		Assert.HasCount(3, intercepts,
			"Should have 2 open + 1 closed = 3 intercepts");

		Assert.IsInstanceOfType<Validation<TestRequest, string>>(intercepts[0]);
		Assert.IsInstanceOfType<ShortCircuitIntercept>(intercepts[1]);
		Assert.IsInstanceOfType<HandlerPerformance<TestRequest, string>>(intercepts[2]);

	}

	[TestMethod]
	public void AddConductor_registers_Authorization_intercept() {
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IAuthorizationEvaluator>(_ => new AlwaysAllowAuthorizationEvaluator());

		// Act
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
				.AddOpenIntercept(typeof(Authorization<,>));
		}, Shared.SequentialSettings);

		var sp = services.BuildServiceProvider();

		// Assert - Check it's registered
		var interceptDescriptor = services
			.FirstOrDefault(sd =>
				sd.ServiceType.IsGenericType &&
				sd.ServiceType.GetGenericTypeDefinition() == typeof(IIntercept<,>) &&
				sd.ImplementationType?.GetGenericTypeDefinition() == typeof(Authorization<,>));

		Assert.IsNotNull(interceptDescriptor,
			"Authorization<,> should be registered");

		// Verify it resolves for authorizable requests
		var intercept = sp.GetService<IIntercept<AuthorizableTestRequest, string>>();
		Assert.IsNotNull(intercept, "Authorization should resolve for IAuthorizableRequestBase");
		Assert.IsInstanceOfType<Authorization<AuthorizableTestRequest, string>>(intercept);

	}

	[TestMethod]
	public void AddConductor_registers_QueryCaching_intercept() {
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();
		var conductorSettings = new ConductorSettings();
		services.AddSingleton(conductorSettings);

		// Act
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(InterceptTests).Assembly)
				.AddOpenIntercept(typeof(QueryCaching<,>));
		}, Shared.SequentialSettings);

		var sp = services.BuildServiceProvider();

		// Assert
		var interceptDescriptor = services
			.FirstOrDefault(sd =>
				sd.ServiceType.IsGenericType &&
				sd.ServiceType.GetGenericTypeDefinition() == typeof(IIntercept<,>) &&
				sd.ImplementationType?.GetGenericTypeDefinition() == typeof(QueryCaching<,>));

		Assert.IsNotNull(interceptDescriptor,
			"QueryCaching<,> should be registered");

		// Verify it resolves for cacheable queries
		var intercept = sp.GetService<IIntercept<CacheableTestQuery, string>>();
		Assert.IsNotNull(intercept,
			"QueryCaching should resolve for ICacheableQuery");
		Assert.IsInstanceOfType<QueryCaching<CacheableTestQuery, string>>(intercept);

	}


	public TestContext TestContext { get; set; } = null!;

}