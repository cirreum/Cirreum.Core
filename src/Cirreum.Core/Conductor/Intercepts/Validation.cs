namespace Cirreum.Conductor.Intercepts;

using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Threading;

sealed class Validation<TOperation, TResultValue>
	: IIntercept<TOperation, TResultValue>
	where TOperation : notnull {

	private readonly IReadOnlyList<IValidator<TOperation>> _validators;
	private readonly bool _hasValidators;

	public Validation(IEnumerable<IValidator<TOperation>> validators) {

		// Materialize once, no matter how DI gives it to us
		this._validators = validators as IReadOnlyList<IValidator<TOperation>>
					  ?? [.. validators];

		this._hasValidators = this._validators.Count > 0;
	}

	public async Task<Result<TResultValue>> HandleAsync(
		OperationContext<TOperation> context,
		OperationHandlerDelegate<TOperation, TResultValue> next,
		CancellationToken cancellationToken) {

		// FAST PATH: no validators → just forward, no async state machine
		if (!this._hasValidators) {
			return await next(context, cancellationToken).ConfigureAwait(false);
		}

		var validationContext = new ValidationContext<TOperation>(context.Operation);

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
			return Result<TResultValue>.Fail(new ValidationException(failures));
		}

		return await next(context, cancellationToken).ConfigureAwait(false);

	}

}