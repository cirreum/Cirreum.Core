//namespace Cirreum.Conductor.Benchmarks;

//using BenchmarkDotNet.Attributes;
//using BenchmarkDotNet.Engines;
//using BenchmarkDotNet.Jobs;
//using Cirreum.Authorization;
//using Cirreum.Conductor;
//using Cirreum.Messaging;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;

//[SimpleJob(
//	RunStrategy.Throughput,
//	RuntimeMoniker.Net10_0,
//	launchCount: 1,
//	warmupCount: 5,
//	iterationCount: 30,
//	invocationCount: 32768)]   // 32k dispatches per iteration
//[MinIterationTime(200)]         // each iteration ~200ms+ of work
//[MemoryDiagnoser]
//public class ConductorDispatcherBenchmarks {

//	private ServiceProvider _provider = default!;
//	private IDispatcher _dispatcher = default!;
//	private ConductorPing _request = default!;
//	private IRequestHandler<ConductorPing, PingResponse> _handler = default!;

//	[GlobalSetup]
//	public async Task Setup() {

//		DefaultAuthorizationEvaluatorCounter.ResetCallCount();
//		DistributedMessageHandlerCounter.ResetCallCount();
//		EmptyTransportPublisherCounter.ResetCallCount();

//		var configuration = new ConfigurationBuilder()
//			.AddJsonFile("C:\\Cirreum\\Core\\Cirreum.Core\\tests\\Cirreum.Conductor.Benchmarks\\appsettings.json")
//			.Build();

//		var services = Shared.ArrangeServices(services => {
//			services.AddDefaultAuthorizationEvaluator();
//			services.AddDomainServices(configuration);
//		});

//		_provider = services.BuildServiceProvider();
//		_dispatcher = _provider.GetRequiredService<IDispatcher>();

//		// Register sample request + handler for benchmark
//		//services.AddSingleton<IRequestHandler<PingRequest, PingResponse>, PingHandler>();
//		var authRegistry = _provider.GetRequiredService<IAuthorizationRoleRegistry>();
//		await ((TestAuthorizationRoleRegistry)authRegistry).InitializeAsync();

//		_handler = _provider.GetRequiredService<IRequestHandler<ConductorPing, PingResponse>>();

//		_request = new ConductorPing("hello world");

//	}

//	//[IterationCleanup]
//	//public void IterationCleanup() {
//	//	Console.Out.WriteLine($"DefaultAuthorizationEvaluator IterationCleanup Calls: {DefaultAuthorizationEvaluator.CallCount}{Environment.NewLine}");
//	//	Console.Out.WriteLine($"EmptyTransportPublisher IterationCleanup Calls:  {EmptyTransportPublisher.CallCount}{Environment.NewLine}");
//	//	DefaultAuthorizationEvaluator.ResetCallCount();
//	//	EmptyTransportPublisher.ResetCallCount();
//	//}

//	[GlobalCleanup]
//	public void Cleanup() {

//		if (_lastError is not null) {
//			Console.Out.WriteLine("LastError type: " + _lastError.GetType().FullName);
//			Console.Out.WriteLine("LastError message: " + _lastError.Message);
//			Console.Out.WriteLine("LastError stack: " + _lastError.StackTrace);
//		} else {
//			Console.Out.WriteLine("LastError: NONE");
//		}

//		Console.Out.WriteLine($"Dispatch_Ping GlobalCleanup called: {_counter}");
//		Console.Out.WriteLine($"DefaultAuthorizationEvaluator GlobalCleanup Calls: {DefaultAuthorizationEvaluatorCounter.CallCount}{Environment.NewLine}");
//		Console.Out.WriteLine($"DistributedMessageHandler GlobalCleanup Calls:  {DistributedMessageHandlerCounter.CallCount}{Environment.NewLine}");
//		Console.Out.WriteLine($"EmptyTransportPublisher GlobalCleanup Calls:  {EmptyTransportPublisherCounter.CallCount}{Environment.NewLine}");
//	}

//	// ---- Benchmarks ----
//	private static Exception? _lastError;
//	private static int _counter = 0;

//	[Benchmark(Description = "Dispatcher: PingRequest -> PingResponse")]
//	public async Task<Result<PingResponse>> Dispatch_Ping() {
//		var result = await _dispatcher.DispatchAsync(_request, CancellationToken.None);
//		if (result.IsFailure) {
//			_lastError = result.Error;
//		}
//		Interlocked.Increment(ref _counter);
//		return result;
//	}

//	[Benchmark(Baseline = true, Description = "Direct handler: PingHandler.HandleAsync")]
//	public async Task<Result<PingResponse>> DirectHandler_Ping() {
//		// This is the closest thing to "no infrastructure overhead"
//		return await _handler.HandleAsync(
//			request: _request,
//			cancellationToken: CancellationToken.None);
//	}

//}
