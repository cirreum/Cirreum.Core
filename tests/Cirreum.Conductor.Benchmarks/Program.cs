using BenchmarkDotNet.Running;

// Core dispatching benchmarks (no pipeline intercepts):
BenchmarkSwitcher
		   .FromAssembly(typeof(Program).Assembly)
		   .Run(["-f*mediator*"]);

// With Pipeline intercepts (1, 2, and 4):
//BenchmarkSwitcher
//		   .FromAssembly(typeof(Program).Assembly)
//		   .Run(["-f*pipeline*"]);

//using Cirreum.Conductor.Benchmarks;
//await Doit();
//static async Task Doit() {
//	var bc = new ConductorDispatcherBenchmarks();
//	await bc.Setup();
//	for (var i = 0; i < 1000; i++) {
//		await bc.Dispatch_Ping();
//	}
//	bc.Cleanup();
//}
