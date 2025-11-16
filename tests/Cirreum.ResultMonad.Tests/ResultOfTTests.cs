namespace Cirreum.ResultMonad.Tests;

using Cirreum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

[TestClass]
public class ResultOfTTests {

	[TestMethod]
	public void Success_ResultT_HasValue_AndNoError() {
		// Arrange
		var result = Result<string>.Success("hello");

		// Act & Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.IsFalse(result.IsFailure);
		Assert.AreEqual("hello", result.Value);
		Assert.IsNull(result.Error);
	}

	[TestMethod]
	public void Fail_ResultT_HasError_AndNoValue() {
		// Arrange
		var ex = new InvalidOperationException("Boom");
		var result = Result<string>.Fail(ex);

		// Act & Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsTrue(result.IsFailure);
		Assert.IsNull(result.Value);
		Assert.AreSame(ex, result.Error);
	}

	[TestMethod]
	public void TryGetValue_ReturnsTrue_ForSuccess_AndOutputsValue() {
		// Arrange
		var result = Result<int>.Success(42);

		// Act
		var success = result.TryGetValue(out var value);

		// Assert
		Assert.IsTrue(success);
		Assert.AreEqual(42, value);
	}

	[TestMethod]
	public void TryGetValue_ReturnsFalse_ForFailure_AndOutputsDefault() {
		// Arrange
		var result = Result<int>.Fail(new Exception("Oops"));

		// Act
		var success = result.TryGetValue(out var value);

		// Assert
		Assert.IsFalse(success);
		Assert.AreEqual(default, value);
	}

	[TestMethod]
	public void TryGetError_ReturnsTrue_ForFailure_AndOutputsError() {
		// Arrange
		var ex = new InvalidOperationException("Oops");
		var result = Result<int>.Fail(ex);

		// Act
		var hasError = result.TryGetError(out var error);

		// Assert
		Assert.IsTrue(hasError);
		Assert.AreSame(ex, error);
	}

	[TestMethod]
	public void TryGetError_ReturnsFalse_ForSuccess_AndOutputsNull() {
		// Arrange
		var result = Result<int>.Success(1);

		// Act
		var hasError = result.TryGetError(out var error);

		// Assert
		Assert.IsFalse(hasError);
		Assert.IsNull(error);
	}

	[TestMethod]
	public void IResultT_Switch_Invokes_Success_ForSuccessfulResult() {
		// Arrange
		IResult<int> result = Result<int>.Success(10);
		var successCalled = false;
		var failureCalled = false;
		var received = 0;

		// Act
		result.Switch(
			onSuccess: v => {
				successCalled = true;
				received = v;
			},
			onFailure: _ => failureCalled = true);

		// Assert
		Assert.IsTrue(successCalled);
		Assert.IsFalse(failureCalled);
		Assert.AreEqual(10, received);
	}

	[TestMethod]
	public void IResultT_Switch_Invokes_Failure_ForFailedResult() {
		// Arrange
		var ex = new InvalidOperationException("Oops");
		IResult<int> result = Result<int>.Fail(ex);
		var successCalled = false;
		var failureCalled = false;
		Exception? received = null;

		// Act
		result.Switch(
			onSuccess: _ => successCalled = true,
			onFailure: e => {
				failureCalled = true;
				received = e;
			});

		// Assert
		Assert.IsFalse(successCalled);
		Assert.IsTrue(failureCalled);
		Assert.AreSame(ex, received);
	}

	[TestMethod]
	public async Task IResultT_SwitchAsync_ValueTask_Invokes_Success_ForSuccessfulResult() {
		// Arrange
		IResult<int> result = Result<int>.Success(99);
		var successCalled = false;
		var failureCalled = false;
		var received = 0;

		// Act
		await result.SwitchAsync(
			onSuccess: v => {
				successCalled = true;
				received = v;
				return ValueTask.CompletedTask;
			},
			onFailure: _ => {
				failureCalled = true;
				return ValueTask.CompletedTask;
			});

		// Assert
		Assert.IsTrue(successCalled);
		Assert.IsFalse(failureCalled);
		Assert.AreEqual(99, received);
	}

	[TestMethod]
	public async Task IResultT_SwitchAsync_Task_Invokes_Failure_ForFailedResult() {
		// Arrange
		var ex = new InvalidOperationException("Oops");
		IResult<int> result = Result<int>.Fail(ex);
		var successCalled = false;
		var failureCalled = false;
		Exception? received = null;

		// Act
		await result.SwitchAsyncTask(
			onSuccess: _ => {
				successCalled = true;
				return Task.CompletedTask;
			},
			onFailure: e => {
				failureCalled = true;
				received = e;
				return Task.CompletedTask;
			});

		// Assert
		Assert.IsFalse(successCalled);
		Assert.IsTrue(failureCalled);
		Assert.AreSame(ex, received);
	}

	[TestMethod]
	public void Map_Propagates_Success_AndTransforms_Value() {
		// Arrange
		var result = Result<int>.Success(5);

		// Act
		var mapped = result.Map(x => x * 2);

		// Assert
		Assert.IsTrue(mapped.IsSuccess);
		Assert.AreEqual(10, mapped.Value);
	}

	[TestMethod]
	public void Map_Propagates_Failure_Without_Invoking_Selector() {
		// Arrange
		var ex = new InvalidOperationException("Oops");
		var result = Result<int>.Fail(ex);
		var selectorCalled = false;

		// Act
		var mapped = result.Map(x => {
			selectorCalled = true;
			return x * 2;
		});

		// Assert
		Assert.IsFalse(mapped.IsSuccess);
		Assert.AreSame(ex, mapped.Error);
		Assert.IsFalse(selectorCalled, "Selector should not be called on failure.");
	}

	[TestMethod]
	public void Then_Chains_Successful_Results() {
		// Arrange
		var result = Result<int>.Success(2);

		// Act
		var chained = result.Then(x => Result<string>.Success($"Value: {x}"));

		// Assert
		Assert.IsTrue(chained.IsSuccess);
		Assert.AreEqual("Value: 2", chained.Value);
	}

	[TestMethod]
	public void Then_ShortCircuits_On_Failure() {
		// Arrange
		var ex = new InvalidOperationException("Oops");
		var result = Result<int>.Fail(ex);
		var selectorCalled = false;

		// Act
		var chained = result.Then(x => {
			selectorCalled = true;
			return Result<string>.Success(x.ToString());
		});

		// Assert
		Assert.IsFalse(chained.IsSuccess);
		Assert.AreSame(ex, chained.Error);
		Assert.IsFalse(selectorCalled, "Chained selector should not run when initial result is failure.");
	}
}
