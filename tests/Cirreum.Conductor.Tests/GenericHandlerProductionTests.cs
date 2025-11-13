namespace Cirreum.Conductor.Tests;

using Cirreum.Conductor.Intercepts;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Tests for generic handler patterns that would actually be used in production.
/// These tests verify that Conductor works with common generic handler scenarios.
/// </summary>
[TestClass]
public class GenericHandlerProductionTests {

	// Domain entities
	public class User { public string Name { get; set; } = "TestUser"; }
	public class Product { public int Id { get; set; } = 123; }

	// Concrete generic request/handler for User
	public class GetUserRequest : IRequest<User> { }
	public class GetUserHandler : IRequestHandler<GetUserRequest, User> {
		public ValueTask<Result<User>> HandleAsync(GetUserRequest request, CancellationToken cancellationToken) {
			return ValueTask.FromResult(Result<User>.Success(new User()));
		}
	}

	// Concrete generic request/handler for Product  
	public class GetProductRequest : IRequest<Product> { }
	public class GetProductHandler : IRequestHandler<GetProductRequest, Product> {
		public ValueTask<Result<Product>> HandleAsync(GetProductRequest request, CancellationToken cancellationToken) {
			return ValueTask.FromResult(Result<Product>.Success(new Product()));
		}
	}

	// Generic base handler that can be inherited
	public abstract class EntityHandlerBase<TRequest, TEntity> : IRequestHandler<TRequest, TEntity>
		where TRequest : IRequest<TEntity>
		where TEntity : new() {

		public virtual ValueTask<Result<TEntity>> HandleAsync(TRequest request, CancellationToken cancellationToken) {
			return ValueTask.FromResult(Result<TEntity>.Success(new TEntity()));
		}
	}

	// Concrete handlers that inherit from generic base
	public class CreateUserRequest : IRequest<User> { }
	public class CreateUserHandler : EntityHandlerBase<CreateUserRequest, User> { }

	public class CreateProductRequest : IRequest<Product> { }
	public class CreateProductHandler : EntityHandlerBase<CreateProductRequest, Product> { }

	[TestMethod]
	public async Task Should_handle_concrete_typed_request() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(GenericHandlerProductionTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddOpenIntercept(typeof(Authorization<,>))
				.AddOpenIntercept(typeof(QueryCaching<,>))
				.AddOpenIntercept(typeof(Performance<,>));
		}, Shared.SequentialSettings);

		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var request = new GetUserRequest();
		var result = await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
		Assert.IsNotNull(result.Value);
		Assert.AreEqual("TestUser", result.Value.Name);
	}

	[TestMethod]
	public async Task Should_handle_multiple_concrete_types() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(GenericHandlerProductionTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddOpenIntercept(typeof(Authorization<,>))
				.AddOpenIntercept(typeof(QueryCaching<,>))
				.AddOpenIntercept(typeof(Performance<,>));
		}, Shared.SequentialSettings);


		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		// Test User
		var userRequest = new GetUserRequest();
		var userResult = await dispatcher.DispatchAsync(userRequest, this.TestContext.CancellationToken);
		Assert.IsTrue(userResult.IsSuccess);
		Assert.IsInstanceOfType<User>(userResult.Value);

		// Test Product
		var productRequest = new GetProductRequest();
		var productResult = await dispatcher.DispatchAsync(productRequest, this.TestContext.CancellationToken);
		Assert.IsTrue(productResult.IsSuccess);
		Assert.IsInstanceOfType<Product>(productResult.Value);
	}

	[TestMethod]
	public void Should_register_handlers_that_inherit_from_generic_base() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(GenericHandlerProductionTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddOpenIntercept(typeof(Authorization<,>))
				.AddOpenIntercept(typeof(QueryCaching<,>))
				.AddOpenIntercept(typeof(Performance<,>));
		}, Shared.SequentialSettings);

		var provider = services.BuildServiceProvider();

		// Verify handlers inheriting from generic base are registered
		var userHandler = provider.GetService<IRequestHandler<CreateUserRequest, User>>();
		Assert.IsNotNull(userHandler, "CreateUserHandler should be registered");
		Assert.IsInstanceOfType<CreateUserHandler>(userHandler);

		var productHandler = provider.GetService<IRequestHandler<CreateProductRequest, Product>>();
		Assert.IsNotNull(productHandler, "CreateProductHandler should be registered");
		Assert.IsInstanceOfType<CreateProductHandler>(productHandler);
	}

	[TestMethod]
	public async Task Should_execute_handlers_that_inherit_from_generic_base() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(GenericHandlerProductionTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddOpenIntercept(typeof(Authorization<,>))
				.AddOpenIntercept(typeof(QueryCaching<,>))
				.AddOpenIntercept(typeof(Performance<,>));
		}, Shared.SequentialSettings);


		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var request = new CreateUserRequest();
		var result = await dispatcher.DispatchAsync(request, this.TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
		Assert.IsNotNull(result.Value);
		Assert.IsInstanceOfType<User>(result.Value);
	}

	[TestMethod]
	public async Task Should_handle_inherited_handlers_independently() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddConductor(builder => {
			builder
				.RegisterFromAssemblies(typeof(GenericHandlerProductionTests).Assembly)
				.AddOpenIntercept(typeof(Validation<,>))
				.AddOpenIntercept(typeof(Authorization<,>))
				.AddOpenIntercept(typeof(QueryCaching<,>))
				.AddOpenIntercept(typeof(Performance<,>));
		}, Shared.SequentialSettings);

		var provider = services.BuildServiceProvider();
		var dispatcher = provider.GetRequiredService<IDispatcher>();

		var userRequest = new CreateUserRequest();
		var userResult = await dispatcher.DispatchAsync(userRequest, this.TestContext.CancellationToken);

		var productRequest = new CreateProductRequest();
		var productResult = await dispatcher.DispatchAsync(productRequest, this.TestContext.CancellationToken);

		Assert.IsTrue(userResult.IsSuccess);
		Assert.IsInstanceOfType<User>(userResult.Value);

		Assert.IsTrue(productResult.IsSuccess);
		Assert.IsInstanceOfType<Product>(productResult.Value);
	}

	public TestContext TestContext { get; set; } = null!;
}