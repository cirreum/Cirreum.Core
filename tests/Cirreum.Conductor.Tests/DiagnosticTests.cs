namespace Cirreum.Conductor.Tests;

[TestClass]
public class DiagnosticTests {

	[TestMethod]
	public async Task What_happens_when_no_handler_exists() {

		var dispatcher = Shared.ArrangeSimpleDispatcher();

		try {
			var result = await dispatcher.DispatchAsync(new TestRequest(), this.TestContext.CancellationToken);
			Console.WriteLine($"Result IsSuccess: {result.IsSuccess}");
			if (!result.IsSuccess) {
				Console.WriteLine($"Error: {result.Error?.GetType().Name} - {result.Error?.Message}");
			}
		} catch (Exception ex) {
			Console.WriteLine($"Exception thrown: {ex.GetType().Name} - {ex.Message}");
			throw;
		}
	}

	public class TestRequest : IRequest<string> {
	}

	public TestContext TestContext { get; set; }
}