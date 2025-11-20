namespace Cirreum.Conductor.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Cirreum.Conductor;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

//[SimpleJob(
//	RunStrategy.Throughput,
//	RuntimeMoniker.Net10_0,
//	launchCount: 1,
//	warmupCount: 5,
//	iterationCount: 100,
//	invocationCount: 32768)]
//[MinIterationTime(1000)]
//[MemoryDiagnoser]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[OperationsPerSecond]
public class MediatorComparisonBenchmarks {
	private IServiceProvider _provider = default!;
	private IDispatcher _conductor = default!;
	private IMediator _mediatr = default!;
	private ConductorPing _conductorRequest = default!;
	private MediatRPing _mediatrRequest = default!;

	private const int Ops = 200_000; // or 500_000, etc.

	[GlobalSetup]
	public async Task Setup() {

		_provider = Shared.ArrangeSimpleDispatcher(services => {

			// --- Register MediatR (bare) ---
			services.AddMediatR(cfg => {
				cfg.RegisterServicesFromAssembly(typeof(MediatorComparisonBenchmarks).Assembly);
				// no pipeline behaviors here
			});

			// Handler that supports both systems
			services.AddSingleton<PingHandler>();
			services.AddSingleton<Conductor.IRequestHandler<ConductorPing, PingResponse>>(sp => sp.GetRequiredService<PingHandler>());
			services.AddSingleton<MediatR.IRequestHandler<MediatRPing, PingResponse>>(sp => sp.GetRequiredService<PingHandler>());

		});

		var domainInitializer = _provider.GetRequiredService<IDomainContextInitializer>();
		domainInitializer.Initialize();

		_conductor = _provider.GetRequiredService<IDispatcher>();
		_mediatr = _provider.GetRequiredService<IMediator>();

		_conductorRequest = new ConductorPing("hello");
		_mediatrRequest = new MediatRPing("hello");

	}

	// ---- Benchmarks ----

	[Benchmark(Description = "Conductor.DispatchAsync")]
	public Task<Result<PingResponse>> Conductor_Dispatch() {
		//for (var i = 0; i < Ops; i++) {
		//	await _conductor.DispatchAsync(_conductorRequest, CancellationToken.None);
		//}
		return _conductor.DispatchAsync(_conductorRequest);
		//var result = await _conductor.DispatchAsync(_conductorRequest);
		//if (result.IsFailure) {
		//	throw result.Error!;
		//}
		//return result;
	}

	[Benchmark(Description = "MediatR.Send")]
	public Task<PingResponse> Mediatr_Send() {
		//for (var i = 0; i < Ops; i++) {
		//	await _mediatr.Send(_mediatrRequest);
		//}
		return _mediatr.Send(_mediatrRequest);
	}

}