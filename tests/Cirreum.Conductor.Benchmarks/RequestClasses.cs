namespace Cirreum.Conductor.Benchmarks;

public sealed record PingResponse(string Echo);

public sealed record MediatRPing(string Message) : MediatR.IRequest<PingResponse>; // MediatR request

public sealed record ConductorPing(string Message) : Conductor.IOperation<PingResponse>;

//public sealed class PingRequestValidator : AbstractValidator<ConductorPing> {
//	public PingRequestValidator() {
//		this.RuleFor(x => x.Message)
//			.NotEmpty()
//			.MaximumLength(100);
//	}
//}

//public sealed class PingRequestAuthorizer : AuthorizerBase<ConductorPing> {
//	public PingRequestAuthorizer() {
//		this.HasRole(ApplicationRoles.AppUserRole);
//	}
//}

/// <summary>
/// Very small handler so most of the cost you see is Conductor/DI/intercepts
/// rather than business logic.
/// </summary>
public sealed class PingHandler :
	IOperationHandler<ConductorPing, PingResponse>,
	MediatR.IRequestHandler<MediatRPing, PingResponse> {

	public async Task<Result<PingResponse>> HandleAsync(
		ConductorPing request,
		CancellationToken cancellationToken) {

		// Minimal work
		var irequest = (IOperation<PingResponse>)request;
		var ping = (ConductorPing)irequest;
		var response = new PingResponse(ping.Message);

		// Simulate async but avoid extra allocations in real bench:
		await Task.CompletedTask;

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

// ---- Pass-through intercepts for pipeline benchmarks ----

/// <summary>
/// Pass-through intercept for Conductor. Calls next() immediately with zero overhead,
/// simulating the minimal cost of pipeline traversal per interceptor level.
/// </summary>
public sealed class PassThroughIntercept1 : IIntercept<ConductorPing, PingResponse> {
	public Task<Result<PingResponse>> HandleAsync(
		OperationContext<ConductorPing> context,
		OperationHandlerDelegate<ConductorPing, PingResponse> next,
		CancellationToken cancellationToken) =>
		next(context, cancellationToken);
}

public sealed class PassThroughIntercept2 : IIntercept<ConductorPing, PingResponse> {
	public Task<Result<PingResponse>> HandleAsync(
		OperationContext<ConductorPing> context,
		OperationHandlerDelegate<ConductorPing, PingResponse> next,
		CancellationToken cancellationToken) =>
		next(context, cancellationToken);
}

public sealed class PassThroughIntercept3 : IIntercept<ConductorPing, PingResponse> {
	public Task<Result<PingResponse>> HandleAsync(
		OperationContext<ConductorPing> context,
		OperationHandlerDelegate<ConductorPing, PingResponse> next,
		CancellationToken cancellationToken) =>
		next(context, cancellationToken);
}

public sealed class PassThroughIntercept4 : IIntercept<ConductorPing, PingResponse> {
	public Task<Result<PingResponse>> HandleAsync(
		OperationContext<ConductorPing> context,
		OperationHandlerDelegate<ConductorPing, PingResponse> next,
		CancellationToken cancellationToken) =>
		next(context, cancellationToken);
}

/// <summary>
/// Pass-through pipeline behavior for MediatR. Calls next() immediately with zero overhead,
/// matching the Conductor intercepts for a fair comparison.
/// </summary>
public sealed class PassThroughBehavior1 : MediatR.IPipelineBehavior<MediatRPing, PingResponse> {
	public Task<PingResponse> Handle(
		MediatRPing request,
		MediatR.RequestHandlerDelegate<PingResponse> next,
		CancellationToken cancellationToken) =>
		next(cancellationToken);
}

public sealed class PassThroughBehavior2 : MediatR.IPipelineBehavior<MediatRPing, PingResponse> {
	public Task<PingResponse> Handle(
		MediatRPing request,
		MediatR.RequestHandlerDelegate<PingResponse> next,
		CancellationToken cancellationToken) =>
		next(cancellationToken);
}

public sealed class PassThroughBehavior3 : MediatR.IPipelineBehavior<MediatRPing, PingResponse> {
	public Task<PingResponse> Handle(
		MediatRPing request,
		MediatR.RequestHandlerDelegate<PingResponse> next,
		CancellationToken cancellationToken) =>
		next(cancellationToken);
}

public sealed class PassThroughBehavior4 : MediatR.IPipelineBehavior<MediatRPing, PingResponse> {
	public Task<PingResponse> Handle(
		MediatRPing request,
		MediatR.RequestHandlerDelegate<PingResponse> next,
		CancellationToken cancellationToken) =>
		next(cancellationToken);
}