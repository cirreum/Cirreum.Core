namespace Cirreum.Messaging;

using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

/// <summary>
/// Scans all assemblies for any type that inherits from
/// <see cref="DistributedMessage"/> and contains a <see cref="MessageDefinitionAttribute"/>.
/// </summary>
public static class MessageScanner {

	/// <summary>
	/// Scans assemblies for role definitions
	/// </summary>
	public static List<MessageDefinition> ScanAssemblies(ILogger logger) {
		ArgumentNullException.ThrowIfNull(logger);
		using var loggingScope = logger.BeginScope("Begin scanning assemblies for {Scanner}", nameof(MessageScanner));
		var distributedMessages = new List<MessageDefinition>();
		var messageIdentifiers = new Dictionary<string, HashSet<string>>();
		var duplicates = new List<(Type Type, string Identifier, string Version)>();
		foreach (var type in AssemblyScanner.ScanExportedTypes(IsDistributedMessage)) {
			if (TryScanType(type, logger, out var message)) {
				// Check for duplicates
				if (!messageIdentifiers.TryGetValue(message.Identifier, out var versions)) {
					versions = [];
					messageIdentifiers[message.Identifier] = versions;
				}
				// If this version already exists for this identifier, it's a duplicate
				if (!versions.Add(message.Version)) {
					duplicates.Add((type, message.Identifier, message.Version));
					logger.DuplicateDetected(type.FullName ?? type.Name ?? "UnknownType", message.Identifier, message.Version);
					// log the warning and skip
					continue;
				}
				distributedMessages.Add(message);
			}
		}
		// Log summary of duplicates
		if (duplicates.Count > 0) {
			logger.DuplicateSummary(duplicates.Count);
		}
		logger.CompletedScanning(distributedMessages.Count);
		return distributedMessages;
	}

	private static bool IsDistributedMessage(Type type) =>
		type != null &&
		type.IsClass &&
		!type.IsAbstract &&
		typeof(DistributedMessage).IsAssignableFrom(type) &&
		IsRecord(type);
	private static bool IsRecord(Type type) {

		// Check if the type has the CompilerFeatureRequired attribute with the name "RecordType"
		var attr = type.GetCustomAttributes(true)
			.FirstOrDefault(a => a.GetType().FullName == "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute");
		if (attr != null) {
			var featureProperty = attr.GetType().GetProperty("FeatureName");
			return featureProperty != null &&
				   "RecordType".Equals(featureProperty.GetValue(attr) as string);
		}

		// Alternative check: records in C# have an EqualityContract property
		var equalityContractProperty = type.GetProperty("EqualityContract",
			BindingFlags.NonPublic |
			BindingFlags.Instance |
			BindingFlags.DeclaredOnly);

		return equalityContractProperty != null;
	}
	private static bool TryScanType(
		Type type,
		ILogger logger,
		[NotNullWhen(true)] out MessageDefinition? messageDefinition) {
		messageDefinition = null;
		try {
			var attr = type.GetCustomAttribute<MessageDefinitionAttribute>();
			if (attr == null) {
				return false;
			}
			// Build schema by scanning properties
			var schema = new List<MessageProperty>();
			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p =>
					p.CanRead
					&& p.Name != nameof(DistributedMessage.UseBackgroundDelivery));
			foreach (var property in properties) {
				var propertyDef = new MessageProperty(
					property.Name,
					property.PropertyType.FullName ?? property.PropertyType.Name
				);
				schema.Add(propertyDef);
			}
			messageDefinition = new MessageDefinition(
				attr.Identifier,
				attr.Version,
				attr.Target,
				type.FullName!,
				schema);
			logger.DiscoveredMessage(type.FullName ?? type.Name ?? "UnknownType");
			return true;
		} catch (MissingMemberException ex) {
			logger.MissingMember(ex.Message, ex);
			return false;
		} catch (TargetInvocationException ex) {
			logger.TargetInvocation(type.FullName ?? type.Name ?? "UnknownType", ex.InnerException?.Message, ex);
			return false;
		} catch (Exception ex) {
			logger.UnexpectedError(type.FullName ?? type.Name ?? "UnknownType", ex);
			return false;
		}
	}

}