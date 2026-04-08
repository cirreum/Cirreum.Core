namespace Cirreum.Conductor.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Cirreum.Conductor;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Measures real-world pipeline dispatch cost with 1–4 pass-through intercepts/behaviors.
/// Conductor ships 4 default intercepts (Validation, Authorization, HandlerPerformance,
/// QueryCaching), so <c>InterceptCount=4</c> is the production-equivalent scenario.
/// </summary>
[Config(typeof(StableCoreConfig))]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[OperationsPerSecond]
public class PipelineComparisonBenchmarks {
	private IServiceProvider _provider = default!;
	private IDispatcher _conductor = default!;
	private MediatR.IMediator _mediatr = default!;
	private ConductorPing _conductorRequest = default!;
	private MediatRPing _mediatrRequest = default!;

	[Params(1, 2, 4)]
	public int InterceptCount { get; set; }

	[GlobalSetup]
	public void Setup() {

		this._provider = Shared.ArrangeSimpleDispatcher(services => {

			// --- Conductor intercepts ---
			if (this.InterceptCount >= 1) {
				services.AddSingleton<IIntercept<ConductorPing, PingResponse>, PassThroughIntercept1>();
			}
			if (this.InterceptCount >= 2) {
				services.AddSingleton<IIntercept<ConductorPing, PingResponse>, PassThroughIntercept2>();
			}
			if (this.InterceptCount >= 3) {
				services.AddSingleton<IIntercept<ConductorPing, PingResponse>, PassThroughIntercept3>();
			}
			if (this.InterceptCount >= 4) {
				services.AddSingleton<IIntercept<ConductorPing, PingResponse>, PassThroughIntercept4>();
			}

			// --- MediatR with pipeline behaviors ---
			services.AddMediatR(cfg => {
				cfg.RegisterServicesFromAssembly(typeof(PipelineComparisonBenchmarks).Assembly);
			});

			if (this.InterceptCount >= 1) {
				services.AddSingleton<MediatR.IPipelineBehavior<MediatRPing, PingResponse>, PassThroughBehavior1>();
			}
			if (this.InterceptCount >= 2) {
				services.AddSingleton<MediatR.IPipelineBehavior<MediatRPing, PingResponse>, PassThroughBehavior2>();
			}
			if (this.InterceptCount >= 3) {
				services.AddSingleton<MediatR.IPipelineBehavior<MediatRPing, PingResponse>, PassThroughBehavior3>();
			}
			if (this.InterceptCount >= 4) {
				services.AddSingleton<MediatR.IPipelineBehavior<MediatRPing, PingResponse>, PassThroughBehavior4>();
			}

			// Handler that supports both systems
			services.AddSingleton<PingHandler>();
			services.AddSingleton<IOperationHandler<ConductorPing, PingResponse>>(sp => sp.GetRequiredService<PingHandler>());
			services.AddSingleton<MediatR.IRequestHandler<MediatRPing, PingResponse>>(sp => sp.GetRequiredService<PingHandler>());

			// Pipeline path requires IUserStateAccessor for OperationContext
			services.AddSingleton<Security.IUserStateAccessor, MockUserStateAccessor>();
		});

		var domainInitializer = this._provider.GetRequiredService<IDomainContextInitializer>();
		domainInitializer.Initialize();

		this._conductor = this._provider.GetRequiredService<IDispatcher>();
		this._mediatr = this._provider.GetRequiredService<MediatR.IMediator>();

		this._conductorRequest = new ConductorPing("hello");
		this._mediatrRequest = new MediatRPing("hello");
	}

	// ---- Benchmarks ----

	[Benchmark(Description = "Conductor.Pipeline")]
	public Task<Result<PingResponse>> Conductor_Pipeline() {
		return this._conductor.DispatchAsync(this._conductorRequest);
	}

	[Benchmark(Description = "MediatR.Pipeline")]
	public Task<PingResponse> Mediatr_Pipeline() {
		return this._mediatr.Send(this._mediatrRequest);
	}
}
