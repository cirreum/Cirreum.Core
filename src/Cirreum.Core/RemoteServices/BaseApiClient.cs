namespace Cirreum.RemoteServices;

using Cirreum.Exceptions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Base class for API clients that provides common functionality for HTTP operations.
/// </summary>
public abstract class BaseApiClient {

	/// <summary>
	/// The current <see cref="HttpClient"/> that is configured for this client.
	/// </summary>
	protected HttpClient Client { get; init; }
	/// <summary>
	/// The <see cref="ILogger"/> configured for this client.
	/// </summary>
	protected ILogger Logger { get; init; }
	/// <summary>
	/// Gets the current domain environment service.
	/// </summary>
	protected IDomainEnvironment DomainEnvironment { get; init; }
	/// <summary>
	/// The configured or defaulted serialization options.
	/// </summary>
	protected JsonSerializerOptions JsonOptions { get; init; }

	/// <summary>
	/// The current UserAgent string for this client.
	/// </summary>
	protected string UserAgent { get; private set; } = string.Empty;

	/// <summary>
	/// DI Constructor.
	/// </summary>
	/// <param name="client">Injected HttpClient.</param>
	/// <param name="logger">Injected logger.</param>
	/// <param name="domainEnvironment">Injected <see cref="IDomainEnvironment"/>.</param>
	/// <param name="jsonOptions">Injected jsonOptions.</param>
	/// <remarks>
	/// <para>
	/// The default <see cref="JsonSerializerOptions"/> set the
	/// <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> to true,
	/// the <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> to 
	/// <see cref="JsonNamingPolicy.CamelCase"/> and the
	/// the <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/> to 
	/// <see cref="JsonIgnoreCondition.WhenWritingNull"/>.
	/// </para>
	/// </remarks>
	protected BaseApiClient(
		HttpClient client,
		ILogger logger,
		IDomainEnvironment domainEnvironment,
		JsonSerializerOptions? jsonOptions = null) {
		this.Client = client;
		this.Logger = logger;
		this.DomainEnvironment = domainEnvironment;
		this.JsonOptions = jsonOptions ?? new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};
		this.SetUserAgent();
	}

	/// <summary>
	/// Set's the user agent header on the <see cref="Client"/>.
	/// </summary>
	protected virtual void SetUserAgent() {
		var clientVersion = this.GetType().GetTypeInfo().Assembly.GetName().Version?.ToString() ?? "v0";
		this.UserAgent = $"{this.GetType().Name}/{clientVersion}/{this.DomainEnvironment.RuntimeType}";
		this.Client.DefaultRequestHeaders.UserAgent.TryParseAdd(this.UserAgent);
	}

	/// <summary>
	/// Executes an operation with retry logic using exponential backoff.
	/// </summary>
	/// <typeparam name="T">The return type of the operation.</typeparam>
	/// <param name="operation">The operation to execute with retry logic.</param>
	/// <param name="maxAttempts">Maximum number of retry attempts.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The result of the operation, or throws the last exception if all retries fail.</returns>
	protected async Task<T?> WithRetryAsync<T>(
		Func<CancellationToken, Task<T?>> operation,
		int maxAttempts = 3,
		CancellationToken cancellationToken = default) {
		Exception? lastException = null;

		for (var attempt = 0; attempt < maxAttempts; attempt++) {
			try {
				return await operation(cancellationToken);
			} catch (Exception ex) when (attempt < maxAttempts - 1 && this.IsTransient(ex)) {
				lastException = ex;
				if (this.Logger.IsEnabled(LogLevel.Warning)) {
					this.Logger.LogWarning(ex,
					"Attempt {Attempt} of {MaxAttempts} failed, retrying...",
					attempt + 1,
					maxAttempts);
				}

				var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
				await Task.Delay(delay, cancellationToken);
			}
		}
		if (this.Logger.IsEnabled(LogLevel.Error)) {
			this.Logger.LogError(lastException,
			"All {MaxAttempts} retry attempts failed",
			maxAttempts);
		}
		throw lastException!;
	}

	/// <summary>
	/// Determines if an exception is transient and should be retried.
	/// Can be overridden in derived classes to customize retry behavior.
	/// </summary>
	/// <param name="ex">The exception to evaluate.</param>
	/// <returns>True if the exception is transient and should be retried; otherwise, false.</returns>
	protected virtual bool IsTransient(Exception ex) =>
		ex switch {
			HttpRequestException => true,
			TimeoutException => true,
			TaskCanceledException => false,
			OperationCanceledException => false,
			_ => false
		};

	#region JSON Operations

	/// <summary>
	/// Sends a GET request and deserializes the JSON response to type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the response to.</typeparam>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The deserialized response object, or default if the request failed.</returns>
	protected async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default) {
		var response = await this.ProcessResponseAsync(this.Client.GetAsync(endpoint, cancellationToken));
		return response is not null
			? await response.Content.ReadFromJsonAsync<T>(this.JsonOptions, cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a POST request with JSON content and deserializes the JSON response to type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the response to.</typeparam>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The deserialized response object, or default if the request failed.</returns>
	protected async Task<T?> PostAsync<T>(
		string endpoint,
		object? content = null,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		if (content is not null) {
			request.Content = JsonContent.Create(content, options: this.JsonOptions);
		}

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadFromJsonAsync<T>(this.JsonOptions, cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a POST request with <see cref="HttpContent"/> and deserializes the JSON response to type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the response to.</typeparam>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The HTTP content to send, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The deserialized response object, or default if the request failed.</returns>
	protected async Task<T?> PostAsync<T>(
		string endpoint,
		HttpContent? content = null,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
			Content = content
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadFromJsonAsync<T>(this.JsonOptions, cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a PUT request with JSON content and deserializes the JSON response to type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the response to.</typeparam>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The deserialized response object, or default if the request failed.</returns>
	protected async Task<T?> PutAsync<T>(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Put, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadFromJsonAsync<T>(this.JsonOptions, cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a DELETE request and optionally deserializes the JSON response to type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the response to.</typeparam>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The deserialized response object, or default if the request failed.</returns>
	protected async Task<T?> DeleteAsync<T>(
		string endpoint,
		CancellationToken cancellationToken = default) {
		var response = await this.ProcessResponseAsync(this.Client.DeleteAsync(endpoint, cancellationToken));
		return response is not null
			? await response.Content.ReadFromJsonAsync<T>(this.JsonOptions, cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a PATCH request with JSON content and deserializes the JSON response to type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the response to.</typeparam>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The deserialized response object, or default if the request failed.</returns>
	protected async Task<T?> PatchAsync<T>(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadFromJsonAsync<T>(this.JsonOptions, cancellationToken)
			: default;
	}

	#endregion

	#region String Operations

	/// <summary>
	/// Sends a GET request and returns the response content as a string.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a string, or null if the request failed.</returns>
	protected async Task<string?> GetAndReadStringAsync(
		string endpoint,
		CancellationToken cancellationToken = default) {
		var response = await this.ProcessResponseAsync(this.Client.GetAsync(endpoint, cancellationToken));
		return response is not null
			? await response.Content.ReadAsStringAsync(cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a POST request with no content and returns the response content as a string.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a string, or null if the request failed.</returns>
	protected async Task<string?> PostAndReadStringAsync(
		string endpoint,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadAsStringAsync(cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a POST request with <see cref="HttpContent"/> and returns the response content as a string.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The HTTP content to send.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a string, or null if the request failed.</returns>
	protected async Task<string?> PostAndReadStringAsync(
		string endpoint,
		HttpContent content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
			Content = content
		};
		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadAsStringAsync(cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a POST request with JSON content and returns the response content as a string.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a string, or null if the request failed.</returns>
	protected async Task<string?> PostAndReadStringAsync(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {

		var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadAsStringAsync(cancellationToken)
			: default;

	}

	/// <summary>
	/// Sends a PATCH request with JSON content and returns the response content as a string.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a string, or null if the request failed.</returns>
	protected async Task<string?> PatchAndReadStringAsync(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {

		var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadAsStringAsync(cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a PATCH request with <see cref="HttpContent"/> and returns the response content as a string.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The HTTP content to send.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a string, or null if the request failed.</returns>
	protected async Task<string?> PatchAndReadStringAsync(
		string endpoint,
		HttpContent content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) {
			Content = content
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadAsStringAsync(cancellationToken)
			: default;
	}

	#endregion

	#region Stream Operations

	/// <summary>
	/// Sends a GET request and returns the response content as a stream.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a stream, or null if the request failed.</returns>
	protected async Task<Stream?> GetAndReadStreamAsync(
		string endpoint,
		CancellationToken cancellationToken = default) {
		var response = await this.ProcessResponseAsync(this.Client.GetAsync(endpoint, cancellationToken));
		return response?.Content.ReadAsStream(cancellationToken);
	}

	/// <summary>
	/// Sends a POST request with JSON content and returns the response content as a stream.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a stream, or null if the request failed.</returns>
	protected async Task<Stream?> PostAndReadStreamAsync(
		string endpoint,
		object? content = null,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		if (content is not null) {
			request.Content = JsonContent.Create(content, options: this.JsonOptions);
		}

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response?.Content.ReadAsStream(cancellationToken);
	}

	/// <summary>
	/// Sends a POST request with <see cref="HttpContent"/> and returns the response content as a stream.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The HTTP content to send, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a stream, or null if the request failed.</returns>
	protected async Task<Stream?> PostAndReadStreamAsync(
		string endpoint,
		HttpContent? content = null,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
			Content = content
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response?.Content.ReadAsStream(cancellationToken);
	}

	/// <summary>
	/// Sends a PATCH request with JSON content and returns the response content as a stream.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a stream, or null if the request failed.</returns>
	protected async Task<Stream?> PatchAndReadStreamAsync(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};
		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response?.Content.ReadAsStream(cancellationToken);
	}

	/// <summary>
	/// Sends a PATCH request with <see cref="HttpContent"/> and returns the response content as a stream.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The HTTP content to send.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a stream, or null if the request failed.</returns>
	protected async Task<Stream?> PatchAndReadStreamAsync(
		string endpoint,
		HttpContent content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) {
			Content = content
		};
		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response?.Content.ReadAsStream(cancellationToken);
	}

	#endregion

	#region Byte Array Operations

	/// <summary>
	/// Sends a GET request and returns the response content as a byte array.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a byte array, or null if the request failed.</returns>
	protected async Task<byte[]?> GetAndReadByteArrayAsync(
		string endpoint,
		CancellationToken cancellationToken = default) {
		var response = await this.ProcessResponseAsync(this.Client.GetAsync(endpoint, cancellationToken));
		return response is not null
			? await response.Content.ReadAsByteArrayAsync(cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a POST request with JSON content and returns the response content as a byte array.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a byte array, or null if the request failed.</returns>
	protected async Task<byte[]?> PostAndReadByteArrayAsync(
		string endpoint,
		object? content = null,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		if (content is not null) {
			request.Content = JsonContent.Create(content, options: this.JsonOptions);
		}
		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadAsByteArrayAsync(cancellationToken)
			: default;
	}

	/// <summary>
	/// Sends a POST request with <see cref="HttpContent"/> and returns the response content as a byte array.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The HTTP content to send, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The response content as a byte array, or null if the request failed.</returns>
	protected async Task<byte[]?> PostAndReadByteArrayAsync(
		string endpoint,
		HttpContent? content = null,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
			Content = content
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return response is not null
			? await response.Content.ReadAsByteArrayAsync(cancellationToken)
			: default;
	}

	#endregion

	#region File Operations

	/// <summary>
	/// Downloads a file via GET and returns the content as byte array with filename.
	/// Works in all environments including browsers.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A tuple containing the file content as a byte array and the filename, or (null, null) if the request failed.</returns>
	protected async Task<(byte[]? Content, string? FileName)> DownloadFileContentAsync(
		string endpoint,
		CancellationToken cancellationToken = default) {
		var response = await this.ProcessResponseAsync(this.Client.GetAsync(endpoint, cancellationToken));
		return await ExtractFileContentAsync(response, cancellationToken);
	}

	/// <summary>
	/// Downloads a file via POST with JSON content and returns the content as byte array with filename.
	/// Works in all environments including browsers.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A tuple containing the file content as a byte array and the filename, or (null, null) if the request failed.</returns>
	protected async Task<(byte[]? Content, string? FileName)> DownloadFileContentAsync(
		string endpoint,
		object? content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		if (content is not null) {
			request.Content = JsonContent.Create(content, options: this.JsonOptions);
		}

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return await ExtractFileContentAsync(response, cancellationToken);
	}

	/// <summary>
	/// Downloads a file via POST with HttpContent and returns the content as byte array with filename.
	/// Works in all environments including browsers.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The HTTP content to send.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A tuple containing the file content as a byte array and the filename, or (null, null) if the request failed.</returns>
	protected async Task<(byte[]? Content, string? FileName)> DownloadFileContentAsync(
		string endpoint,
		HttpContent content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
			Content = content
		};

		var response = await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
		return await ExtractFileContentAsync(response, cancellationToken);
	}

	/// <summary>
	/// Common logic for extracting file content and filename from response.
	/// </summary>
	/// <param name="response">The HTTP response message.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A tuple containing the file content as a byte array and the filename, or (null, null) if the response is null.</returns>
	private static async Task<(byte[]? Content, string? FileName)> ExtractFileContentAsync(
		HttpResponseMessage? response,
		CancellationToken cancellationToken) {

		if (response is null) {
			return (null, null);
		}

		var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
		var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ??
					   response.Content.Headers.ContentDisposition?.Name?.Trim('"');

		if (!string.IsNullOrEmpty(fileName)) {
			fileName = Uri.UnescapeDataString(fileName);
		}

		return (content, fileName);

	}

	#endregion

	#region Raw Response Operations

	/// <summary>
	/// Sends a GET request and returns the raw HttpResponseMessage.
	/// Consumer is responsible for disposing the response.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The raw HttpResponseMessage, or null if the request failed.</returns>
	protected async Task<HttpResponseMessage?> GetRawAsync(
		string endpoint,
		CancellationToken cancellationToken = default) {
		return await this.ProcessResponseAsync(this.Client.GetAsync(endpoint, cancellationToken));
	}

	/// <summary>
	/// Sends a POST request with JSON content and returns the raw HttpResponseMessage.
	/// Consumer is responsible for disposing the response.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The raw HttpResponseMessage, or null if the request failed.</returns>
	protected async Task<HttpResponseMessage?> PostRawAsync(
		string endpoint,
		object? content = null,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		if (content is not null) {
			request.Content = JsonContent.Create(content, options: this.JsonOptions);
		}
		return await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
	}

	/// <summary>
	/// Sends a POST request with HttpContent and returns the raw HttpResponseMessage.
	/// Consumer is responsible for disposing the response.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The HTTP content to send.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The raw HttpResponseMessage, or null if the request failed.</returns>
	protected async Task<HttpResponseMessage?> PostRawAsync(
		string endpoint,
		HttpContent content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
			Content = content
		};
		return await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
	}

	/// <summary>
	/// Sends a PUT request with JSON content and returns the raw HttpResponseMessage.
	/// Consumer is responsible for disposing the response.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The raw HttpResponseMessage, or null if the request failed.</returns>
	protected async Task<HttpResponseMessage?> PutRawAsync(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Put, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};
		return await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
	}

	/// <summary>
	/// Sends a PATCH request with JSON content and returns the raw HttpResponseMessage.
	/// Consumer is responsible for disposing the response.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The raw HttpResponseMessage, or null if the request failed.</returns>
	protected async Task<HttpResponseMessage?> PatchRawAsync(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};
		return await this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
	}

	/// <summary>
	/// Sends a DELETE request and returns the raw HttpResponseMessage.
	/// Consumer is responsible for disposing the response.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>The raw HttpResponseMessage, or null if the request failed.</returns>
	protected async Task<HttpResponseMessage?> DeleteRawAsync(
		string endpoint,
		CancellationToken cancellationToken = default) {
		return await this.ProcessResponseAsync(this.Client.DeleteAsync(endpoint, cancellationToken));
	}

	#endregion

	#region No Response Operations

	/// <summary>
	/// Sends a GET request with no response content processing.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	protected Task GetAsync(string endpoint, CancellationToken cancellationToken = default) =>
		this.ProcessResponseAsync(this.Client.GetAsync(endpoint, cancellationToken));

	/// <summary>
	/// Sends a POST request with JSON content and no response content processing.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content, or null for no body.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	protected Task PostAsync(
		string endpoint,
		object? content = null,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
		if (content is not null) {
			request.Content = JsonContent.Create(content, options: this.JsonOptions);
		}
		return this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
	}

	/// <summary>
	/// Sends a PUT request with JSON content and no response content processing.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	protected Task PutAsync(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Put, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};
		return this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
	}

	/// <summary>
	/// Sends a PATCH request with JSON content and no response content processing.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	protected Task PatchAsync(
		string endpoint,
		object content,
		CancellationToken cancellationToken = default) {
		var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) {
			Content = JsonContent.Create(content, options: this.JsonOptions)
		};
		return this.ProcessResponseAsync(this.Client.SendAsync(request, cancellationToken));
	}

	/// <summary>
	/// Sends a DELETE request with no response content processing.
	/// </summary>
	/// <param name="endpoint">The API endpoint to call.</param>
	/// <param name="cancellationToken">Cancellation token for the operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	protected Task DeleteAsync(
		string endpoint,
		CancellationToken cancellationToken = default) =>
		this.ProcessResponseAsync(this.Client.DeleteAsync(endpoint, cancellationToken));

	#endregion

	/// <summary>
	/// Processes the HTTP response and handles common error scenarios.
	/// </summary>
	/// <param name="action">The HTTP request task to process.</param>
	/// <returns>The HTTP response message if successful, or null if an error occurred.</returns>
	private async Task<HttpResponseMessage?> ProcessResponseAsync(Task<HttpResponseMessage> action) {
		try {
			var response = await action;
			if (response.IsSuccessStatusCode) {
				return response;
			}

			if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
				var err = await response.Content.ReadAsStringAsync();
				throw new UnauthenticatedAccessException(err);
			}

			if (response.StatusCode == System.Net.HttpStatusCode.Forbidden) {
				var err = await response.Content.ReadAsStringAsync();
				throw new ForbiddenAccessException(err);
			}

			var exceptionModel = await this.ParseErrorResponse(response);
			if (this.Logger.IsEnabled(LogLevel.Error)) {
				this.Logger.LogError("API error: {Title}. Details: {Detail}",
					exceptionModel.Title,
					exceptionModel.Detail);
			}

			throw new ApiException(exceptionModel);
		} catch (Exception ex) when (ex is not ApiException) {
			this.Logger.LogError(ex, "Unexpected error during API call");
			throw;
		}
	}

	/// <summary>
	/// Parses error response content into an ExceptionModel.
	/// </summary>
	/// <param name="response">The HTTP response containing the error.</param>
	/// <returns>An ExceptionModel representing the error details.</returns>
	private async Task<ExceptionModel> ParseErrorResponse(HttpResponseMessage response) {
		try {
			return await response.Content.ReadFromJsonAsync<ExceptionModel>(this.JsonOptions)
				?? CreateDefaultExceptionModel(response.ReasonPhrase);
		} catch {
			string? message = null;
			try {
				message = await response.Content.ReadAsStringAsync();
			} catch (Exception ex) {
				this.Logger.LogWarning(ex, "Failed to read error response content");
			}

			return CreateDefaultExceptionModel(message ?? response.ReasonPhrase);
		}
	}

	/// <summary>
	/// Creates a default exception model when error parsing fails.
	/// </summary>
	/// <param name="detail">The error detail message.</param>
	/// <returns>A default ExceptionModel with the provided detail.</returns>
	private static ExceptionModel CreateDefaultExceptionModel(string? detail) => new() {
		Detail = detail ?? "Unknown error",
		Title = "Unknown Error",
		Failures = []
	};

}