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

	public async ValueTask<Result<TResponse>> HandleAsync(
		RequestContext<TRequest> context,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken) {

		if (this._validators.Any()) {

			List<ValidationFailure> failures = [];

			var validationContext = new ValidationContext<TRequest>(context.Request);

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

		}

		return await next(cancellationToken);

	}

}