namespace Cirreum.Conductor.Benchmarks;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

public class StableCoreConfig : ManualConfig {
	public StableCoreConfig() {
		this.AddJob(Job.Default
			.WithId("core-thread")
			.WithEnvironmentVariable("DOTNET_PROCESSOR_COUNT", "1")
			.WithAffinity(0b0001)); // pin to core 0
	}
}