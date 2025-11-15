namespace Cirreum.Conductor.Tests;

using Microsoft.Extensions.Logging;

public sealed class TestContextLoggerProvider(TestContext context) : ILoggerProvider {

	public ILogger CreateLogger(string categoryName)
		=> new TestContextLogger(context, categoryName);

	public void Dispose() { }
}

public sealed class TestContextLogger(TestContext context, string categoryName) : ILogger {

	public IDisposable? BeginScope<TState>(TState state)
		where TState : notnull => default!;

	public bool IsEnabled(LogLevel logLevel) => true;

	public void Log<TState>(
		LogLevel logLevel,
		EventId eventId,
		TState state,
		Exception? exception,
		Func<TState, Exception?, string> formatter) {
		context.WriteLine($"[{logLevel}] {categoryName}: {formatter(state, exception)}");

		if (exception is not null) {
			context.WriteLine(exception.ToString());
		}
	}
}