namespace System;

using Cirreum;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

/// <summary>
/// Formatting Methods for Objects.
/// </summary>
public static class ObjectFormatExtensions {

	private static readonly Type StringType = typeof(string);

	/// <summary>
	/// Cache of PropertyInfo arrays keyed by Type to avoid repeated reflection.
	/// Limited to prevent unbounded memory growth in scenarios with dynamically generated types.
	/// Thread-safe for concurrent access.
	/// </summary>
	private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

	/// <summary>
	/// Tracks the insertion order of cached types for LRU eviction.
	/// </summary>
	private static readonly ConcurrentQueue<Type> CacheAccessOrder = new();

	/// <summary>
	/// The maximum number of types to cache. When exceeded, oldest entries are evicted.
	/// </summary>
	private const int MaxCacheSize = 500;

	/// <summary>
	/// Thread-safe counter for the current cache size.
	/// </summary>
	private static int _cacheSize = 0;

	/// <summary>
	/// Processes all string properties of an object, applying <see cref="Smart"/> to format their values
	/// using the object itself as the data source for replacements. Supports advanced formatting
	/// features including conditional logic, pluralization, and nested property access.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This extension method recursively traverses the object's writable string properties,
	/// treating each string value as a format template and replacing it with the formatted result.
	/// The object itself serves as the data source for the replacements.
	/// </para>
	/// <para>
	/// Example usage: If an object has a string property "Message" with value "Hello {Name}",
	/// and another property "Name" with value "World", calling FormatProperties() will update
	/// "Message" to "Hello World".
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the object being processed.</typeparam>
	/// <param name="obj">The object instance whose string properties will be formatted.</param>
	public static void FormatProperties<T>(this T obj) where T : class {
		var objVal = Check.NotNull(obj);
		var visited = new HashSet<object>();
		FormatProperties(objVal, objVal, typeof(T), Smart.Default, visited);
	}

	/// <summary>
	/// Processes all string properties of an object, applying a custom <see cref="SmartFormat.SmartFormatter"/> 
	/// to format their values using the object itself as the data source for replacements. Supports advanced 
	/// formatting features including conditional logic, pluralization, and nested property access.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This extension method recursively traverses the object's writable string properties,
	/// treating each string value as a format template and replacing it with the formatted result.
	/// The object itself serves as the data source for the replacements.
	/// </para>
	/// <para>
	/// Example usage: If an object has a string property "Message" with value "Hello {Name}",
	/// and another property "Name" with value "World", calling FormatProperties() will update
	/// "Message" to "Hello World".
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type of the object being processed.</typeparam>
	/// <param name="obj">The object instance whose string properties will be formatted.</param>
	/// <param name="formatter">The <see cref="SmartFormat.SmartFormatter"/> instance to use for formatting.</param>
	public static void FormatProperties<T>(this T obj, SmartFormat.SmartFormatter formatter) where T : class {
		var objVal = Check.NotNull(obj);
		var formatterVal = Check.NotNull(formatter);
		var visited = new HashSet<object>();
		FormatProperties(objVal, objVal, typeof(T), formatterVal, visited);
	}

	/// <summary>
	/// Processes all string properties of an object, applying <see cref="Smart"/> to format their values
	/// using the object itself as the data source for replacements. Supports advanced formatting
	/// features including conditional logic, pluralization, and nested property access.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This extension method recursively traverses the object's writable string properties,
	/// treating each string value as a format template and replacing it with the formatted result.
	/// The object itself serves as the data source for the replacements.
	/// </para>
	/// <para>
	/// Example usage: If an object has a string property "Message" with value "Hello {Name}",
	/// and another property "Name" with value "World", calling FormatProperties() will update
	/// "Message" to "Hello World".
	/// </para>
	/// </remarks>
	/// <param name="obj">The object instance whose string properties will be formatted.</param>
	/// <param name="objectType">The specific type to use for reflection (useful when working with interfaces or base classes).</param>
	public static void FormatProperties(this object obj, Type objectType) {
		var visited = new HashSet<object>();
		FormatProperties(obj, obj, objectType, Smart.Default, visited);
	}

	/// <summary>
	/// Processes all string properties of an object, applying a custom <see cref="SmartFormat.SmartFormatter"/> 
	/// to format their values using the object itself as the data source for replacements. Supports advanced 
	/// formatting features including conditional logic, pluralization, and nested property access.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This extension method recursively traverses the object's writable string properties,
	/// treating each string value as a format template and replacing it with the formatted result.
	/// The object itself serves as the data source for the replacements.
	/// </para>
	/// <para>
	/// Example usage: If an object has a string property "Message" with value "Hello {Name}",
	/// and another property "Name" with value "World", calling FormatProperties() will update
	/// "Message" to "Hello World".
	/// </para>
	/// </remarks>
	/// <param name="obj">The object instance whose string properties will be formatted.</param>
	/// <param name="objectType">The specific type to use for reflection (useful when working with interfaces or base classes).</param>
	/// <param name="formatter">The <see cref="SmartFormat.SmartFormatter"/> instance to use for formatting.</param>
	public static void FormatProperties(this object obj, Type objectType, SmartFormat.SmartFormatter formatter) {
		var formatterVal = Check.NotNull(formatter);
		var visited = new HashSet<object>();
		FormatProperties(obj, obj, objectType, formatterVal, visited);
	}

	private static void FormatProperties(
		object root,
		object instance,
		Type objectType,
		SmartFormat.SmartFormatter formatter,
		HashSet<object> visited) {

		if (!visited.Add(instance)) {
			return; // Silently skip circular reference
		}

		var props = GetCachedProperties(objectType);

		foreach (var p in props) {
			FormatProperty(root, instance, p, formatter, visited);
		}
	}

	/// <summary>
	/// Gets properties from cache or computes and caches them with LRU eviction.
	/// Thread-safe for concurrent access.
	/// </summary>
	/// <param name="type">The type whose properties should be retrieved.</param>
	/// <returns>Array of public instance properties for the specified type.</returns>
	private static PropertyInfo[] GetCachedProperties(Type type) {
		// Fast path: check if already cached
		if (PropertyCache.TryGetValue(type, out var props)) {
			return props;
		}

		// Compute properties using reflection
		props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		// Try to add to cache
		if (PropertyCache.TryAdd(type, props)) {
			CacheAccessOrder.Enqueue(type);
			var currentSize = Interlocked.Increment(ref _cacheSize);

			// Evict oldest entries if cache exceeds max size
			// Allow slight overflow (10 entries) to avoid constant eviction at boundary
			if (currentSize > MaxCacheSize + 10) {
				EvictOldestEntries(10);
			}
		}

		return props;
	}

	/// <summary>
	/// Evicts the specified number of oldest cache entries using LRU strategy.
	/// Thread-safe for concurrent access.
	/// </summary>
	/// <param name="count">The number of entries to evict.</param>
	private static void EvictOldestEntries(int count) {
		for (var i = 0; i < count; i++) {
			if (CacheAccessOrder.TryDequeue(out var oldestType)) {
				if (PropertyCache.TryRemove(oldestType, out _)) {
					Interlocked.Decrement(ref _cacheSize);
				}
			} else {
				break; // Queue is empty
			}
		}
	}

	private static void FormatProperty(
		object root,
		object instance,
		PropertyInfo prop,
		SmartFormat.SmartFormatter formatter,
		HashSet<object> visited) {

		if (root == null) {
			return;
		}

		if (instance == null) {
			return;
		}

		if (prop == null) {
			return;
		}

		if (!prop.CanRead) {
			return;
		}

		var pType = prop.PropertyType;
		if (!pType.IsTypeDefinition) {
			return;
		}

		object? pValue;
		try {
			pValue = prop.GetValue(instance);
		} catch {
			return;
		}

		if (pValue == null) {
			return;
		}

		// if is a String and is Writable
		if (pType == StringType && prop.CanWrite) {
			if (pValue is string strValue && !string.IsNullOrWhiteSpace(strValue)) {
				var newValue = formatter.Format(strValue, root);
				prop.SetValue(instance, newValue ?? string.Empty);
			}
			return;
		}

		// continue member hierarchy
		if (!pType.IsValueType) {
			FormatProperties(root, pValue, pType, formatter, visited);
		}
	}

}