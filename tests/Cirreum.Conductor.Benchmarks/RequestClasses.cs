namespace Cirreum.Conductor.Benchmarks;

public sealed record PingResponse(string Echo);

public sealed record MediatRPing(string Message) : MediatR.IRequest<PingResponse>; // MediatR request

public sealed record ConductorPing(string Message) : Conductor.IRequest<PingResponse>;

//public sealed class PingRequestValidator : AbstractValidator<ConductorPing> {
//	public PingRequestValidator() {
//		this.RuleFor(x => x.Message)
//			.NotEmpty()
//			.MaximumLength(100);
//	}
//}

//public sealed class PingRequestAuthorizer : AuthorizationValidatorBase<ConductorPing> {
//	public PingRequestAuthorizer() {
//		this.HasRole(ApplicationRoles.AppUserRole);
//	}
//}

/// <summary>
/// Very small handler so most of the cost you see is Conductor/DI/intercepts
/// rather than business logic.
/// </summary>
public sealed class PingHandler :
	IRequestHandler<ConductorPing, PingResponse>,
	MediatR.IRequestHandler<MediatRPing, PingResponse> {

	public async ValueTask<Result<PingResponse>> HandleAsync(
		ConductorPing request,
		CancellationToken cancellationToken) {

		// Minimal work
		var irequest = (IRequest<PingResponse>)request;
		var ping = (ConductorPing)irequest;
		var response = new PingResponse(ping.Message);

		// Simulate async but avoid extra allocations in real bench:
		await ValueTask.CompletedTask;

		return Result<PingResponse>.Success(response);

	}

	public async Task<PingResponse> Handle(
		MediatRPing request,
		CancellationToken cancellationToken) {

		// Minimal work
		var irequest = (MediatR.IRequest<PingResponse>)request;
		var ping = (MediatRPing)irequest;
		var response = new PingResponse(ping.Message);

		// Simulate async but avoid extra allocations in real bench:
		await Task.CompletedTask;

		return response;

	}

}