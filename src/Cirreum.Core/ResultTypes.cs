namespace Cirreum;

using System.Text;
using System.Text.Json;

/// <summary>
/// Represents a generic cursor for single-column sorting with a unique identifier tie-breaker.
/// </summary>
/// <remarks>
/// <para>
/// This cursor type is suitable for the common pagination scenario where results are sorted by a single
/// column (such as a date or timestamp) with a <see cref="Guid"/> identifier used as a tie-breaker when
/// sort column values are equal.
/// </para>
/// <para>
/// For composite sorting scenarios involving multiple columns or non-Guid identifiers, define a custom
/// cursor record and use <see cref="Cursor.DecodeAs{T}"/> for decoding.
/// </para>
/// </remarks>
/// <typeparam name="TColumn">The type of the sort column value.</typeparam>
/// <param name="Column">The value of the sort column at the cursor position.</param>
/// <param name="Id">The unique identifier used as a tie-breaker when sort column values are equal.</param>
public sealed record Cursor<TColumn>(TColumn Column, Guid Id);

/// <summary>
/// Provides encoding and decoding utilities for cursor-based pagination tokens.
/// </summary>
/// <remarks>
/// <para>
/// Cursors are opaque tokens that encode the position within a sorted result set. They enable efficient
/// keyset pagination by allowing the database to seek directly to a position rather than scanning and
/// discarding rows.
/// </para>
/// <para>
/// Cursor values are encoded as base64 JSON strings. Clients should treat cursors as opaque and not
/// attempt to parse, construct, or modify them directly.
/// </para>
/// </remarks>
public static class Cursor {

	/// <summary>
	/// Encodes a cursor value as a base64 string.
	/// </summary>
	/// <typeparam name="T">The type of the cursor data.</typeparam>
	/// <param name="data">The cursor data to encode.</param>
	/// <returns>A base64-encoded string representing the cursor.</returns>
	public static string Encode<T>(T data) {
		var json = JsonSerializer.Serialize(data);
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
	}

	/// <summary>
	/// Encodes a sort column value and identifier as a cursor string.
	/// </summary>
	/// <remarks>
	/// This is a convenience method for the common case of single-column sorting with a <see cref="Guid"/>
	/// tie-breaker. The resulting cursor can be decoded using <see cref="Decode{TColumn}"/>.
	/// </remarks>
	/// <typeparam name="TColumn">The type of the sort column value.</typeparam>
	/// <param name="column">The value of the sort column at the cursor position.</param>
	/// <param name="id">The unique identifier used as a tie-breaker.</param>
	/// <returns>A base64-encoded string representing the cursor.</returns>
	public static string Encode<TColumn>(TColumn column, Guid id) {
		return Encode(new Cursor<TColumn>(column, id));
	}

	/// <summary>
	/// Decodes a cursor string into a <see cref="Cursor{TColumn}"/>.
	/// </summary>
	/// <remarks>
	/// Use this method to decode cursors created with <see cref="Encode{TColumn}(TColumn, Guid)"/>.
	/// For custom cursor types, use <see cref="DecodeAs{T}"/> instead.
	/// </remarks>
	/// <typeparam name="TColumn">The type of the sort column value.</typeparam>
	/// <param name="cursor">The base64-encoded cursor string, or null.</param>
	/// <returns>The decoded cursor, or null if the cursor is null, empty, or invalid.</returns>
	public static Cursor<TColumn>? Decode<TColumn>(string? cursor) {
		return DecodeAs<Cursor<TColumn>>(cursor);
	}

	/// <summary>
	/// Decodes a cursor string into a custom cursor type.
	/// </summary>
	/// <remarks>
	/// Use this method when working with custom cursor records that contain multiple sort columns
	/// or non-Guid identifiers.
	/// </remarks>
	/// <typeparam name="T">The type of the cursor data.</typeparam>
	/// <param name="cursor">The base64-encoded cursor string, or null.</param>
	/// <returns>The decoded cursor data, or null if the cursor is null, empty, or invalid.</returns>
	public static T? DecodeAs<T>(string? cursor) where T : class {
		if (string.IsNullOrEmpty(cursor)) {
			return null;
		}

		try {
			var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
			return JsonSerializer.Deserialize<T>(json);
		} catch {
			return null;
		}
	}

	/// <summary>
	/// Decodes a cursor string into a custom value type cursor.
	/// </summary>
	/// <remarks>
	/// Use this method when working with custom cursor value types (structs or records structs).
	/// </remarks>
	/// <typeparam name="T">The type of the cursor data.</typeparam>
	/// <param name="cursor">The base64-encoded cursor string, or null.</param>
	/// <returns>The decoded cursor data, or null if the cursor is null, empty, or invalid.</returns>
	public static T? DecodeAsValue<T>(string? cursor) where T : struct {
		if (string.IsNullOrEmpty(cursor)) {
			return null;
		}

		try {
			var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
			return JsonSerializer.Deserialize<T>(json);
		} catch {
			return null;
		}
	}
}

/// <summary>
/// Represents a paginated result set using cursor-based (keyset) pagination.
/// </summary>
/// <remarks>
/// <para>
/// Cursor pagination provides stable results when data is being inserted or deleted, and performs
/// consistently regardless of how deep into the result set the client navigates. This makes it
/// ideal for large datasets, real-time data, and infinite scroll interfaces.
/// </para>
/// <para>
/// The cursor is an opaque token encoding the sort key(s) of the boundary item. Clients should
/// treat cursors as opaque strings and not attempt to parse or construct them. Use the
/// <see cref="Cursor"/> helper class to encode and decode cursor values.
/// </para>
/// <para>
/// For scenarios requiring arbitrary page jumps or total counts, consider <see cref="PagedResult{T}"/> instead.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of items in the result set.</typeparam>
/// <param name="Items">The items for the current page.</param>
/// <param name="NextCursor">The cursor to fetch the next page, or null if there are no more items.</param>
/// <param name="HasNextPage">A value indicating whether there is a subsequent page.</param>
public sealed record CursorResult<T>(
	IReadOnlyList<T> Items,
	string? NextCursor,
	bool HasNextPage) {

	/// <summary>
	/// Gets the number of items contained in the current page.
	/// </summary>
	public int Count => this.Items.Count;

	/// <summary>
	/// Gets or sets the cursor to fetch the previous page, or null if this is the first page.
	/// </summary>
	public string? PreviousCursor { get; init; }

	/// <summary>
	/// Gets a value indicating whether there is a preceding page.
	/// </summary>
	public bool HasPreviousPage => this.PreviousCursor is not null;

	/// <summary>
	/// Gets or sets the total number of items across all pages, if known.
	/// </summary>
	/// <remarks>
	/// This value is optional and may be null if computing the total count is too expensive.
	/// </remarks>
	public int? TotalCount { get; init; }
}

/// <summary>
/// Represents a paginated result set using offset-based pagination.
/// </summary>
/// <remarks>
/// <para>
/// Use offset pagination when clients need to jump to arbitrary pages or display total counts.
/// This approach works well for smaller datasets and traditional paged interfaces with numbered pages.
/// </para>
/// <para>
/// For large datasets, real-time data, or infinite scroll interfaces where consistency matters,
/// consider <see cref="CursorResult{T}"/> instead.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of items in the result set.</typeparam>
/// <param name="Items">The items for the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="PageSize">The maximum number of items per page.</param>
/// <param name="PageNumber">The current page number (1-based).</param>
public sealed record PagedResult<T>(
	IReadOnlyList<T> Items,
	int TotalCount,
	int PageSize,
	int PageNumber) {

	/// <summary>
	/// Gets the number of items contained in the current page.
	/// </summary>
	public int Count => this.Items.Count;

	/// <summary>
	/// Gets the total number of pages available.
	/// </summary>
	public int TotalPages => (int)Math.Ceiling((double)this.TotalCount / this.PageSize);

	/// <summary>
	/// Gets a value indicating whether there is a subsequent page.
	/// </summary>
	public bool HasNextPage => this.PageNumber < this.TotalPages;

	/// <summary>
	/// Gets a value indicating whether there is a preceding page.
	/// </summary>
	public bool HasPreviousPage => this.PageNumber > 1;
}