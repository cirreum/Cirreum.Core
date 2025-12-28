namespace Cirreum.Conductor.Intercepts;

using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Threading;

sealed class Validation<TRequest, TResponse>
	: IIntercept<TRequest, TResponse>
	where TRequest : notnull {

	private readonly IReadOnlyList<IValidator<TRequest>> _validators;
	private readonly bool _hasValidators;

	public Validation(IEnumerable<IValidator<TRequest>> validators) {

		// Materialize once, no matter how DI gives it to us
		this._validators = validators as IReadOnlyList<IValidator<TRequest>>
					  ?? [.. validators];

		this._hasValidators = this._validators.Count > 0;
	}

	public async Task<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TRequest, TResponse> next,
		CancellationToken cancellationToken) {

		// FAST PATH: no validators → just forward, no async state machine
		if (!this._hasValidators) {
			return await next(context, cancellationToken).ConfigureAwait(false);
		}

		var validationContext = new ValidationContext<TRequest>(context.Request);

		List<ValidationFailure> failures = [];

		foreach (var validator in this._validators) {
			var result = await validator.ValidateAsync(validationContext, cancellationToken);
			if (result == null) {
				continue;
			}
			if (result.Errors?.Count > 0) {
				foreach (var error in result.Errors) {
					if (error != null) {
						failures.Add(error);
					}
				}
			}
		}

		if (failures.Count > 0) {
			return Result<TResponse>.Fail(new ValidationException(failures));
		}

		return await next(context, cancellationToken).ConfigureAwait(false);

	}

}