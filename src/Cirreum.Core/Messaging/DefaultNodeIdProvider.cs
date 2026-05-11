namespace Cirreum.Messaging;

/// <summary>
/// Default implementation of <see cref="INodeIdProvider"/> that resolves the current
/// process's node identity via a chain of environment-based hints, falling back to
/// a generated GUID when no hint is available.
/// </summary>
/// <remarks>
/// <para>
/// Resolution chain (first non-empty value wins):
/// </para>
/// <list type="number">
///   <item><description><c>CONTAINER_APP_REPLICA_NAME</c> — Azure Container Apps replica name.</description></item>
///   <item><description><c>WEBSITE_INSTANCE_ID</c> — Azure App Service instance ID.</description></item>
///   <item><description><c>HOSTNAME</c> — container hostname (Kubernetes, generic container runtimes).</description></item>
///   <item><description><c>{MachineName}:{ProcessId}</c> — local development and unmanaged environments.</description></item>
///   <item><description><c>Guid.NewGuid()</c> truncated — last-resort fallback ensuring uniqueness.</description></item>
/// </list>
/// <para>
/// The resolved value is computed once at construction time and remains stable for
/// the lifetime of the process. Restarts produce a different value when the chain
/// terminates at the GUID fallback (or any other non-deterministic source), which
/// is acceptable — the receiver only needs intra-process stability for echo prevention.
/// </para>
/// <para>
/// Apps that need custom resolution (e.g., reading from a bespoke infrastructure
/// metadata service) replace this implementation in DI before the framework's
/// <c>TryAddSingleton</c> registration runs:
/// </para>
/// <code>
/// services.AddSingleton&lt;INodeIdProvider&gt;(sp =&gt; new MyCustomNodeIdProvider(...));
/// // ...later, framework registration:
/// // services.TryAddSingleton&lt;INodeIdProvider, DefaultNodeIdProvider&gt;();
/// </code>
/// </remarks>
public sealed class DefaultNodeIdProvider : INodeIdProvider {

	/// <summary>
	/// Initializes a new <see cref="DefaultNodeIdProvider"/> by running the
	/// resolution chain once and caching the result.
	/// </summary>
	public DefaultNodeIdProvider() {
		this.NodeId = Resolve();
	}

	/// <inheritdoc />
	public string NodeId { get; }

	private static string Resolve() {

		var containerAppReplica = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME");
		if (!string.IsNullOrWhiteSpace(containerAppReplica)) {
			return containerAppReplica;
		}

		var appServiceInstance = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
		if (!string.IsNullOrWhiteSpace(appServiceInstance)) {
			return appServiceInstance;
		}


		var hostname = Environment.GetEnvironmentVariable("HOSTNAME");
		if (!string.IsNullOrWhiteSpace(hostname)) {
			return hostname;
		}

		var machine = Environment.MachineName;
		if (!string.IsNullOrWhiteSpace(machine)) {
			return $"{machine}:{Environment.ProcessId}";
		}

		return Guid.NewGuid().ToString("N")[..8];

	}

}