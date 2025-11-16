namespace Cirreum.ResultMonad.Tests;


[TestClass]
public sealed class ResultAsyncExtensionTests {

	#region OnSuccessAsync Tests

	[TestMethod]
	public async Task OnSuccessAsync_WithValueTask_WhenSuccessful_ShouldExecuteAction() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Success(42));
		var executed = false;
		var capturedValue = 0;

		// Act
		var result = await resultTask.OnSuccessAsync(x => {
			executed = true;
			capturedValue = x;
		});

		// Assert
		Assert.IsTrue(executed);
		Assert.AreEqual(42, capturedValue);
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(42, result.Value);
	}

	[TestMethod]
	public async Task OnSuccessAsync_WithValueTask_WhenFailed_ShouldNotExecuteAction() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Fail(new("error")));
		var executed = false;

		// Act
		var result = await resultTask.OnSuccessAsync(x => executed = true);

		// Assert
		Assert.IsFalse(executed);
		Assert.IsFalse(result.IsSuccess);
	}

	[TestMethod]
	public async Task OnSuccessAsync_WithTask_WhenSuccessful_ShouldExecuteAction() {
		// Arrange
		var resultTask = Task.FromResult(Result<int>.Success(42));
		var executed = false;

		// Act
		var result = await resultTask.OnSuccessAsyncTask(x => executed = true);

		// Assert
		Assert.IsTrue(executed);
		Assert.IsTrue(result.IsSuccess);
	}

	#endregion

	#region OnFailureAsync Tests

	[TestMethod]
	public async Task OnFailureAsync_WithValueTask_WhenFailed_ShouldExecuteAction() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Fail(new("async error")));
		var executed = false;
		var capturedMessage = "";

		// Act
		var result = await resultTask.OnFailureAsync(ex => {
			executed = true;
			capturedMessage = ex.Message;
		});

		// Assert
		Assert.IsTrue(executed);
		Assert.AreEqual("async error", capturedMessage);
		Assert.IsFalse(result.IsSuccess);
	}

	[TestMethod]
	public async Task OnFailureAsync_WithTask_WhenFailed_ShouldExecuteAction() {
		// Arrange
		var resultTask = Task.FromResult(Result<int>.Fail(new("async error")));
		var executed = false;

		// Act
		await resultTask.OnFailureAsyncTask(ex => executed = true);

		// Assert
		Assert.IsTrue(executed);
	}

	#endregion

	#region MapAsync Tests

	[TestMethod]
	public async Task MapAsync_WhenSuccessful_ShouldTransform() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Success(42));

		// Act
		var result = await resultTask.MapAsync(x => x.ToString());

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual("42", result.Value);
	}

	[TestMethod]
	public async Task MapAsync_WhenFailed_ShouldPreserveFailure() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Fail(new("error")));

		// Act
		var result = await resultTask.MapAsync(x => x.ToString());

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.AreEqual("error", result.Error.Message);
	}

	#endregion

	#region ThenAsync Tests

	[TestMethod]
	public async Task ThenAsync_WhenSuccessful_ShouldChain() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Success(42));

		// Act
		var result = await resultTask.ThenAsync(async x => {
			await Task.Delay(1, this.TestContext.CancellationToken);
			return Result<string>.Success($"Value: {x}");
		});

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual("Value: 42", result.Value);
	}

	[TestMethod]
	public async Task ThenAsync_WhenOriginalFailed_ShouldPropagateFailure() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Fail(new("async error")));

		// Act
		var result = await resultTask.ThenAsync(async x => {
			await Task.Delay(1, this.TestContext.CancellationToken);
			return Result<string>.Success("shouldn't reach");
		});

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.AreEqual("async error", result.Error.Message);
	}

	[TestMethod]
	public async Task ThenAsync_WhenChainedOperationFails_ShouldPropagateChainedFailure() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Success(42));

		// Act
		var result = await resultTask.ThenAsync(async x => {
			await Task.Delay(1, this.TestContext.CancellationToken);
			return Result<string>.Fail(new("chained error"));
		});

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.AreEqual("chained error", result.Error.Message);
	}

	#endregion

	#region WhereAsync Tests

	[TestMethod]
	public async Task WhereAsync_WithTruePredicate_ShouldRemainSuccessful() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Success(10));

		// Act
		var result = await resultTask.WhereAsync(x => x > 5, new("Value too small"));

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(10, result.Value);
	}

	[TestMethod]
	public async Task WhereAsync_WithFalsePredicate_ShouldBecomeFailure() {
		// Arrange
		var resultTask = new ValueTask<Result<int>>(Result<int>.Success(3));

		// Act
		var result = await resultTask.WhereAsync(x => x > 5, new("Value too small"));

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.AreEqual("Value too small", result.Error.Message);
	}

	public TestContext TestContext { get; set; }

	#endregion

}