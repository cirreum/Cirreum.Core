namespace Cirreum.Exceptions;

using System;
using System.Net;

/// <summary>
/// Details an error when performing a batch operation for a given TEntity
/// </summary>
public class BatchOperationException : Exception {

	/// <summary>
	/// The http status code of the batch operation.
	/// </summary>
	public HttpStatusCode StatusCode { get; init; }

	/// <summary>
	/// Creates <see cref="BatchOperationException"/>
	/// </summary>
	/// <param name="statusCode"></param>
	/// <param name="entityName"></param>
	public BatchOperationException(HttpStatusCode statusCode, string entityName) : base(
		$"Failed to execute the batch operation for {entityName}") {
		StatusCode = statusCode;
	}

}