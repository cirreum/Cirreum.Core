namespace Cirreum.Conductor.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Cirreum.Authorization;
using Cirreum.Conductor;
using Cirreum.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

[SimpleJob(
	RunStrategy.Throughput,
	RuntimeMoniker.Net10_0,
	launchCount: 1,
	warmupCount: 5,
	iterationCount: 100,
	invocationCount: 32768)]
[MinIterationTime(500)]
[MemoryDiagnoser]
public class MediatorComparisonBenchmarks {
	private IServiceProvider _provider = default!;
	private IDispatcher _conductor = default!;
	private IMediator _mediatr = default!;
	private ConductorPing _conductorRequest = default!;
	private MediatRPing _mediatrRequest = default!;

	[GlobalSetup]
	public async Task Setup() {

		DefaultAuthorizationEvaluatorCounter.ResetCallCount();
		DistributedMessageHandlerCounter.ResetCallCount();
		EmptyTransportPublisherCounter.ResetCallCount();

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

		_conductor = _provider.GetRequiredService<IDispatcher>();
		_mediatr = _provider.GetRequiredService<IMediator>();

		_conductorRequest = new ConductorPing("hello");
		_mediatrRequest = new MediatRPing("hello");

	}

	// ---- Benchmarks ----

	[Benchmark(Description = "Conductor.DispatchAsync")]
	public Task<Result<PingResponse>> Conductor_Dispatch()
		=> _conductor.DispatchAsync(_conductorRequest, CancellationToken.None);

	[Benchmark(Description = "MediatR.Send")]
	public Task<PingResponse> Mediatr_Send()
		=> _mediatr.Send(_mediatrRequest);

}