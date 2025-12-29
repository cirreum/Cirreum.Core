namespace Cirreum.Exceptions;

using System;
using System.Net;

/// <summary>
/// Details an error when performing a batch operation for a given TEntity
/// </summary>
/// <remarks>
/// Creates <see cref="BatchOperationException"/>
/// </remarks>
/// <param name="statusCode"></param>
/// <param name="entityName"></param>
public class BatchOperationException(
	HttpStatusCode statusCode,
	string entityName
) : Exception($"Failed to execute the batch operation for {entityName}") {
	/// <summary>
	/// The http status code of the batch operation.
	/// </summary>
	public HttpStatusCode StatusCode { get; init; } = statusCode;
}