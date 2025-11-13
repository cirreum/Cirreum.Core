namespace Cirreum.Conductor.Intercepts;

using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public sealed class Validation<TRequest, TResponse>(
	IEnumerable<IValidator<TRequest>> validators
) : IIntercept<TRequest, TResponse>
	where TRequest : notnull {

	private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

	public ValueTask<Result<TResponse>> HandleAsync(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken) {

		if (this._validators.Any()) {

			var context = new ValidationContext<TRequest>(request);

			List<ValidationFailure>? failures = null;

			foreach (var validator in this._validators) {
				var result = validator.Validate(context);
				if (result.Errors.Count > 0) {
					failures ??= new List<ValidationFailure>(result.Errors.Count);
					foreach (var error in result.Errors) {
						if (error != null) {
							failures.Add(error);
						}
					}
				}
			}

			if (failures?.Count > 0) {
				throw new ValidationException(failures!);
			}

		}

		return next(cancellationToken);

	}

}