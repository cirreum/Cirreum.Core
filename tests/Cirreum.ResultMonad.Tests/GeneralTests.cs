namespace Cirreum.ResultMonad.Tests;

[TestClass]
public class GeneralTests {

	#region Basic Tests

	[TestMethod]
	public void Ok_ShouldCreateSuccessfulResult() {
		// Arrange & Act
		var result = Result<int>.Success(42);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(42, result.Value);
		Assert.IsNull(result.Error);
	}

	[TestMethod]
	public void Fail_WithException_ShouldCreateFailedResult() {
		// Arrange
		var exception = new InvalidOperationException("Test error");

		// Act
		var result = Result<int>.Fail(exception);

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual(default, result.Value);
		Assert.AreEqual(exception, result.Error);
	}

	[TestMethod]
	public void Fail_WithMessage_ShouldCreateFailedResultWithException() {
		// Act
		var result = Result<string>.Fail(new("error message"));

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error); // Added null check assertion
		Assert.IsInstanceOfType<Exception>(result.Error);
		Assert.AreEqual("error message", result.Error.Message);
	}

	#endregion

	#region Factory Method Tests

	[TestMethod]
	public void Fail_WithNullException_ShouldThrow() {
		// Act & Assert
		var error = Assert.ThrowsExactly<ArgumentNullException>(() => Result<int>.Fail(null!));

		// Verify it's the correct parameter
		Assert.AreEqual("error", error.ParamName);
	}

	#endregion

	#region Implicit Operator Tests

	[TestMethod]
	public void ImplicitOperator_FromValue_ShouldCreateSuccessfulResult() {
		// Act
		Result<int> result = 100;

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual(100, result.Value);
	}

	[TestMethod]
	public void ImplicitOperator_FromException_ShouldCreateFailedResult() {
		// Arrange
		var ex = new Exception("test");

		// Act
		Result<int> result = ex;

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual(ex, result.Error);
	}

	#endregion

	#region Map Tests

	[TestMethod]
	public void Map_OnSuccess_ShouldTransformValue() {
		// Arrange
		var result = Result<int>.Success(5);

		// Act
		var mapped = result.Map(x => x * 2);

		// Assert
		Assert.IsTrue(mapped.IsSuccess);
		Assert.AreEqual(10, mapped.Value);
	}

	[TestMethod]
	public void Map_OnFailure_ShouldPreserveFailure() {
		// Arrange
		var result = Result<int>.Fail(new("error"));

		// Act
		var mapped = result.Map(x => x * 2);

		// Assert
		Assert.IsFalse(mapped.IsSuccess);
		Assert.IsNotNull(mapped.Error); // Added null check assertion
		Assert.AreEqual("error", mapped.Error.Message);
	}

	[TestMethod]
	public void Map_TypeTransformation_ShouldWork() {
		// Arrange
		var result = Result<int>.Success(5);

		// Act
		var mapped = result.Map(x => x.ToString());

		// Assert
		Assert.IsTrue(mapped.IsSuccess);
		Assert.AreEqual("5", mapped.Value);
	}

	[TestMethod]
	public void Map_WithNullSelector_ShouldThrow() {
		// Arrange
		var result = Result<int>.Success(5);

		// Act & Assert
		var exception = Assert.ThrowsExactly<ArgumentNullException>(() => result.Map<string>(null!));

		// Optional: Also verify exception properties
		Assert.AreEqual("selector", exception.ParamName);
	}

	#endregion

	#region OnSuccess/OnFailure Tests

	[TestMethod]
	public void OnSuccess_WhenSuccessful_ShouldExecuteAction() {
		// Arrange
		var result = Result<int>.Success(42);
		var executed = false;
		var capturedValue = 0;

		// Act
		var returned = result.OnSuccess(x => {
			executed = true;
			capturedValue = x;
		});

		// Assert
		Assert.IsTrue(executed);
		Assert.AreEqual(42, capturedValue);
		Assert.AreEqual(result, returned); // Use AreEqual, not AreSame
	}

	[TestMethod]
	public void OnSuccess_WhenFailed_ShouldNotExecuteAction() {
		// Arrange
		var result = Result<int>.Fail(new("error"));
		var executed = false;

		// Act
		result.OnSuccess(x => executed = true);

		// Assert
		Assert.IsFalse(executed);
	}

	[TestMethod]
	public void OnFailure_WhenFailed_ShouldExecuteAction() {
		// Arrange
		var result = Result<int>.Fail(new("test error"));
		var executed = false;
		var capturedMessage = "";

		// Act
		result.OnFailure(ex => {
			executed = true;
			capturedMessage = ex.Message;
		});

		// Assert
		Assert.IsTrue(executed);
		Assert.AreEqual("test error", capturedMessage);
	}

	[TestMethod]
	public void OnFailure_WhenSuccessful_ShouldNotExecuteAction() {
		// Arrange
		var result = Result<int>.Success(42);
		var executed = false;

		// Act
		result.OnFailure(ex => executed = true);

		// Assert
		Assert.IsFalse(executed);
	}

	[TestMethod]
	public void OnSuccessOnFailure_Chaining_ShouldWorkCorrectly() {
		// Arrange
		var result = Result<int>.Success(42);
		var successExecuted = false;
		var failureExecuted = false;

		// Act
		result
			.OnSuccess(x => successExecuted = true)
			.OnFailure(ex => failureExecuted = true);

		// Assert
		Assert.IsTrue(successExecuted);
		Assert.IsFalse(failureExecuted);
	}

	#endregion

	#region Where Tests

	[TestMethod]
	public void Where_WithTruePredicate_ShouldRemainSuccessful() {
		// Arrange
		var result = Result<int>.Success(10);

		// Act
		var filtered = result.Where(x => x > 5, new("Value too small"));

		// Assert
		Assert.IsTrue(filtered.IsSuccess);
		Assert.AreEqual(10, filtered.Value);
	}

	[TestMethod]
	public void Where_WithFalsePredicate_ShouldBecomeFailure() {
		// Arrange
		var result = Result<int>.Success(10);

		// Act
		var filtered = result.Where(x => x > 20, new("Value too small"));

		// Assert
		Assert.IsFalse(filtered.IsSuccess);
		Assert.IsNotNull(filtered.Error); // Added null check assertion
		Assert.AreEqual("Value too small", filtered.Error.Message);
	}

	[TestMethod]
	public void Where_OnAlreadyFailedResult_ShouldPreserveOriginalError() {
		// Arrange
		var result = Result<int>.Fail(new("original error"));

		// Act
		var filtered = result.Where(x => true, new("new error"));

		// Assert
		Assert.IsFalse(filtered.IsSuccess);
		Assert.IsNotNull(filtered.Error); // Added null check assertion
		Assert.AreEqual("original error", filtered.Error.Message);
	}

	[TestMethod]
	public void Where_WithNullPredicate_ShouldThrow() {
		// Arrange
		var result = Result<int>.Success(10);

		// Act & Assert
		var exception = Assert.ThrowsExactly<ArgumentNullException>(() => result.Where(null!, new("new error")));

		// Verify it's the correct parameter
		Assert.AreEqual("predicate", exception.ParamName);
	}

	#endregion

	#region Then Tests

	[TestMethod]
	public void Then_WithSuccessfulChaining_ShouldWork() {
		// Arrange
		var result = Result<int>.Success(5);

		// Act
		var chained = result.Then(x => Result<string>.Success(x.ToString()));

		// Assert
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual("5", chained.Value);
	}

	[TestMethod]
	public void Then_WithOriginalFailure_ShouldPropagateFailure() {
		// Arrange
		var result = Result<int>.Fail(new("original error"));

		// Act
		var chained = result.Then(x => Result<string>.Success(x.ToString()));

		// Assert
		Assert.IsFalse(chained.IsSuccess);
		Assert.IsNotNull(chained.Error);
		Assert.AreEqual("original error", chained.Error.Message);
	}

	[TestMethod]
	public void Then_WithChainedFailure_ShouldPropagateChainedFailure() {
		// Arrange
		var result = Result<int>.Success(5);

		// Act
		var chained = result.Then(x => Result<string>.Fail(new("chained error")));

		// Assert
		Assert.IsFalse(chained.IsSuccess);
		Assert.IsNotNull(chained.Error); // Added null check assertion
		Assert.AreEqual("chained error", chained.Error.Message);
	}

	[TestMethod]
	public void Then_WithExceptionInSelector_ShouldCatchException() {
		// Arrange
		var result = Result<int>.Success(5);

		// Act
		var chained = result.Then<string>(x => throw new InvalidOperationException("selector threw"));

		// Assert
		Assert.IsFalse(chained.IsSuccess);
		Assert.IsNotNull(chained.Error); // Added null check assertion
		Assert.IsInstanceOfType<InvalidOperationException>(chained.Error);
		Assert.AreEqual("selector threw", chained.Error.Message);
	}

	[TestMethod]
	public void Then_MultipleChaining_ShouldWork() {
		// Act
		var pipeline = Result<int>.Success(10)
			.Then(x => x > 5 ? Result<int>.Success(x * 2) : Result<int>.Fail(new("too small")))
			.Then(x => x < 50 ? Result<string>.Success($"Value: {x}") : Result<string>.Fail(new("too large")))
			.Then(s => s.Contains("Value") ? Result<int>.Success(s.Length) : Result<int>.Fail(new("invalid format")));

		// Assert
		Assert.IsTrue(pipeline.IsSuccess);
		Assert.AreEqual(9, pipeline.Value); // "Value: 20".Length = 9
	}

	[TestMethod]
	public void Then_MultipleChaining_ShouldStopAtFirstFailure() {
		// Act
		var pipeline = Result<int>.Success(3)
			.Then(x => x > 5 ? Result<int>.Success(x * 2) : Result<int>.Fail(new("too small")))
			.Then(x => Result<string>.Success($"Value: {x}"))
			.Then(s => Result<int>.Success(s.Length));

		// Assert
		Assert.IsFalse(pipeline.IsSuccess);
		Assert.IsNotNull(pipeline.Error);
		Assert.AreEqual("too small", pipeline.Error.Message);
	}

	[TestMethod]
	public void Then_WithNullSelector_ShouldThrow() {
		// Arrange
		var result = Result<int>.Success(5);

		// Act & Assert
		var exception = Assert.ThrowsExactly<ArgumentNullException>(() => result.Then<string>(null!));

		// Verify it's the correct parameter
		Assert.AreEqual("selector", exception.ParamName);
	}

	#endregion

	#region Deconstruction Tests

	[TestMethod]
	public void Deconstruct_Success_ShouldExtractComponents() {
		// Arrange
		var result = Result<string>.Success("test");

		// Act
		var (isSuccess, value, exception) = result;

		// Assert
		Assert.IsTrue(isSuccess);
		Assert.AreEqual("test", value);
		Assert.IsNull(exception);
	}

	[TestMethod]
	public void Deconstruct_Failure_ShouldExtractComponents() {
		// Arrange
		var ex = new Exception("fail");
		var result = Result<string>.Fail(ex);

		// Act
		var (isSuccess, value, exception) = result;

		// Assert
		Assert.IsFalse(isSuccess);
		Assert.IsNull(value);
		Assert.AreEqual(ex, exception);
	}

	#endregion

	#region Equality Tests

	[TestMethod]
	public void Equals_SameSuccessResults_ShouldBeEqual() {
		// Arrange
		var result1 = Result<int>.Success(42);
		var result2 = Result<int>.Success(42);

		// Act & Assert
		Assert.AreEqual(result1, result2);
		Assert.IsTrue(result1 == result2);
		Assert.IsFalse(result1 != result2);
	}

	[TestMethod]
	public void Equals_DifferentSuccessResults_ShouldNotBeEqual() {
		// Arrange
		var result1 = Result<int>.Success(42);
		var result2 = Result<int>.Success(43);

		// Act & Assert
		Assert.AreNotEqual(result1, result2);
		Assert.IsFalse(result1 == result2);
		Assert.IsTrue(result1 != result2);
	}

	[TestMethod]
	public void Equals_SameFailureResults_ShouldBeEqual() {
		// Arrange
		var ex = new Exception("test");
		var result1 = Result<int>.Fail(ex);
		var result2 = Result<int>.Fail(ex);

		// Act & Assert
		Assert.AreEqual(result1, result2);
		Assert.IsTrue(result1 == result2);
	}

	#endregion

	#region ToString Tests

	[TestMethod]
	public void ToString_Success_ShouldFormatCorrectly() {
		// Arrange
		var result = Result<int>.Success(42);

		// Act
		var str = result.ToString();

		// Assert
		Assert.AreEqual("Success(42)", str);
	}

	[TestMethod]
	public void ToString_Failure_ShouldFormatCorrectly() {
		// Arrange
		var result = Result<int>.Fail(new("test error"));

		// Act
		var str = result.ToString();

		// Assert
		Assert.Contains("Fail(", str);
		Assert.Contains("test error", str);
	}

	#endregion

	#region Business Logic Tests

	[TestMethod]
	public void BusinessFlow_SuccessfulPipeline_ShouldWork() {
		// Arrange
		static Result<int> ParseUserId(string input) =>
			int.TryParse(input, out var id) && id > 0
				? Result<int>.Success(id)
				: Result<int>.Fail(new("Invalid user ID format"));

		static Result<TestUser> LoadUser(int id) =>
			id <= 1000
				? Result<TestUser>.Success(new TestUser { Id = id, Name = $"User{id}", IsActive = id % 2 == 0 })
				: Result<TestUser>.Fail(new("User not found"));

		static Result<TestUser> ValidateUser(TestUser user) =>
			user.IsActive
				? Result<TestUser>.Success(user)
				: Result<TestUser>.Fail(new("User account is inactive"));

		static Result<string> GenerateToken(TestUser user) =>
			Result<string>.Success($"token_{user.Id}");

		// Act
		var result = Result<string>.Success("42")
			.Then(ParseUserId)
			.Then(LoadUser)
			.Then(ValidateUser)
			.Then(GenerateToken);

		// Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.AreEqual("token_42", result.Value);
	}

	[TestMethod]
	public void BusinessFlow_FailureAtParsing_ShouldPropagateError() {
		// Arrange
		static Result<int> ParseUserId(string input) =>
			int.TryParse(input, out var id) && id > 0
				? Result<int>.Success(id)
				: Result<int>.Fail(new("Invalid user ID format"));

		static Result<TestUser> LoadUser(int id) =>
			Result<TestUser>.Success(new TestUser { Id = id, Name = $"User{id}", IsActive = true });

		// Act
		var result = Result<string>.Success("invalid")
			.Then(ParseUserId)
			.Then(LoadUser);

		// Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsNotNull(result.Error);
		Assert.AreEqual("Invalid user ID format", result.Error.Message);
	}

	#endregion

	[TestMethod]
	public void TryGetValue_OnSuccess_ShouldReturnTrueAndValue() {
		var result = Result<int>.Success(42);

		Assert.IsTrue(result.TryGetValue(out var value));
		Assert.AreEqual(42, value);
	}

	[TestMethod]
	public void TryGetValue_OnFailure_ShouldReturnFalseAndDefault() {
		var result = Result<int>.Fail(new("error"));

		Assert.IsFalse(result.TryGetValue(out var value));
		Assert.AreEqual(default, value);
	}

	#region Helper Classes

	private class TestUser {
		public int Id { get; set; }
		public string Name { get; set; } = "";
		public bool IsActive { get; set; }
	}

	#endregion
}