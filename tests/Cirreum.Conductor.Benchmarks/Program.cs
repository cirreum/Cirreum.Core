using BenchmarkDotNet.Running;

BenchmarkSwitcher
		   .FromAssembly(typeof(Program).Assembly)
		   .Run(["-f*"]);


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
