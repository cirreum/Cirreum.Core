namespace Cirreum.RemoteServices;

using System.Net.Http.Headers;

/// <summary>
/// A record that encapsulates a response containing data of type <typeparamref name="T"/>,
/// </summary>
/// <typeparam name="T">The Type of the response content.</typeparam>
/// <param name="Data">The response content.</param>
/// <param name="Headers">The response headers.</param>
/// <param name="ContentHeaders">The response's Content headers.</param>
public record ResponseWithHeaders<T>(T Data, HttpResponseHeaders Headers, HttpContentHeaders ContentHeaders);