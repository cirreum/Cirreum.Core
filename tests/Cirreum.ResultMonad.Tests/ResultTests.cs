namespace Cirreum.ResultMonad.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

[TestClass]
public class ResultTests {

	[TestMethod]
	public void Success_Result_HasExpectedFlags_AndNoError() {
		// Arrange
		var result = Result.Success;

		// Act & Assert
		Assert.IsTrue(result.IsSuccess);
		Assert.IsFalse(result.IsFailure);
		Assert.IsNull(result.Error);
	}

	[TestMethod]
	public void Fail_Result_HasExpectedFlags_AndError() {
		// Arrange
		var ex = new InvalidOperationException("Boom");
		var result = Result.Fail(ex);

		// Act & Assert
		Assert.IsFalse(result.IsSuccess);
		Assert.IsTrue(result.IsFailure);
		Assert.AreSame(ex, result.Error);
	}

	[TestMethod]
	public void IResult_Switch_Invokes_Success_Branch_ForSuccessfulResult() {
		// Arrange
		IResult result = Result.Success;
		var successCalled = false;
		var failureCalled = false;

		// Act
		result.Switch(
			onSuccess: () => successCalled = true,
			onFailure: _ => failureCalled = true);

		// Assert
		Assert.IsTrue(successCalled, "Success branch should have been called.");
		Assert.IsFalse(failureCalled, "Failure branch should not have been called.");
	}

	[TestMethod]
	public void IResult_Switch_Invokes_Failure_Branch_ForFailedResult() {
		// Arrange
		var ex = new InvalidOperationException("Oops");
		IResult result = Result.Fail(ex);
		var successCalled = false;
		var failureCalled = false;
		Exception? received = null;

		// Act
		result.Switch(
			onSuccess: () => successCalled = true,
			onFailure: e => {
				failureCalled = true;
				received = e;
			});

		// Assert
		Assert.IsFalse(successCalled, "Success branch should not have been called.");
		Assert.IsTrue(failureCalled, "Failure branch should have been called.");
		Assert.AreSame(ex, received);
	}

	[TestMethod]
	public async Task IResult_SwitchAsync_ValueTask_Invokes_Success_ForSuccessfulResult() {
		// Arrange
		IResult result = Result.Success;
		var successCalled = false;
		var failureCalled = false;

		// Act
		await result.SwitchAsync(
			onSuccess: () => {
				successCalled = true;
				return ValueTask.CompletedTask;
			},
			onFailure: _ => {
				failureCalled = true;
				return ValueTask.CompletedTask;
			});

		// Assert
		Assert.IsTrue(successCalled);
		Assert.IsFalse(failureCalled);
	}

	[TestMethod]
	public async Task IResult_SwitchAsync_Task_Invokes_Failure_ForFailedResult() {
		// Arrange
		var ex = new InvalidOperationException("Oops");
		IResult result = Result.Fail(ex);
		var successCalled = false;
		var failureCalled = false;
		Exception? received = null;

		// Act
		await result.SwitchAsyncTask(
			onSuccess: () => {
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
}
