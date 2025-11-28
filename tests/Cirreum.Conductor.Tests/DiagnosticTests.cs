namespace Cirreum.Conductor.Tests;

[TestClass]
public class DiagnosticTests {

	[TestMethod]
	public async Task What_happens_when_no_handler_exists() {
		var dispatcher = Shared.ArrangeSimpleDispatcher();
		var result = await dispatcher.DispatchAsync(new TestRequest(), this.TestContext.CancellationToken);
		Assert.IsTrue(result.IsFailure, "Expected failure when no handler exists.");
	}

	public class TestRequest : IRequest<string> {
	}

	public TestContext TestContext { get; set; }
}