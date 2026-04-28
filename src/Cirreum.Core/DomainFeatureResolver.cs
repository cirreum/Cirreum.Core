namespace Cirreum;

using System;
using System.Collections.Concurrent;

/// <summary>
/// Resolves the domain feature name from a type's namespace convention. The first segment
/// after <c>"Domain"</c> in the namespace is the domain feature, lowercased.
/// </summary>
/// <remarks>
/// <para>
/// Convention: <c>MyApp.Domain.Issues.Commands.DeleteIssue</c> → <c>"issues"</c>.
/// Returns <see langword="null"/> when the type has no <c>*.Domain.*</c> namespace segment.
/// </para>
/// <para>
/// Results are cached per-type in a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// for zero-cost repeated lookups. Used by <c>AuthorizationContext</c>,
/// <c>OperationContext</c>, and <c>RequiredGrantCache</c> to derive the domain
/// feature structurally rather than from attributes or marker interfaces.
/// </para>
/// </remarks>
public static class DomainFeatureResolver {

	private static readonly ConcurrentDictionary<Type, string?> Cache = new();

	/// <summary>
	/// Resolves the domain feature for the given type from its namespace.
	/// </summary>
	/// <param name="type">The type whose namespace to inspect.</param>
	/// <returns>
	/// The lowercased domain feature name, or <see langword="null"/> when the type's
	/// namespace does not contain a <c>"Domain"</c> segment followed by at least one
	/// additional segment.
	/// </returns>
	public static string? Resolve(Type type) =>
		Cache.GetOrAdd(type, static t => {
			var parts = t.Namespace?.Split('.') ?? [];
			var idx = Array.IndexOf(parts, "Domain");
			return idx >= 0 && parts.Length > idx + 1
				? parts[idx + 1].ToLowerInvariant()
				: null;
		});

	/// <summary>
	/// Resolves the domain feature for <typeparamref name="T"/> from its namespace.
	/// </summary>
	public static string? Resolve<T>() => Resolve(typeof(T));
}
