using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Parley.SharedKernel.Paging;

/// <summary>
/// Keyset paging shared by every list. A cursor carries the sort field next to the sort key of its
/// last row, so a cursor issued for one sort is rejected by another. Rows order by the sort key and then
/// by identifier, so rows that tie on the key still page exactly once.
/// </summary>
public static class KeysetOrdering
{
    private const string SortFieldSeparator = "\u001F";

    private const string TimestampFormat = "O";

    private static readonly MethodInfo StringCompare = typeof(string).GetMethod(nameof(string.Compare), [typeof(string), typeof(string)])!;

    /// <summary>Sort key of a text sort field: the text itself.</summary>
    public static string? ParseText(string key) => key;

    /// <summary>Text form of a text sort key: the text itself.</summary>
    public static string FormatText(string key) => key;

    /// <summary>Cursor for the row a page ends on.</summary>
    public static KeysetCursor CursorFor(string sortField, string key, Guid id) => new(sortField + SortFieldSeparator + key, id);

    /// <summary>
    /// The position a cursor encodes for <paramref name="sortField"/>. Null when the cursor was issued for
    /// another sort field or its key does not parse.
    /// </summary>
    public static KeysetPosition<TKey>? ReadPosition<TKey>(KeysetCursor cursor, string sortField, Func<string, TKey?> parseKey)
    {
        var prefix = sortField + SortFieldSeparator;
        if (!cursor.SortKey.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var key = parseKey(cursor.SortKey[prefix.Length..]);
        return key is null ? null : new KeysetPosition<TKey>(key!, cursor.Id);
    }

    /// <summary>
    /// The position a cursor encodes for a value-type sort key. The shared reader hands back the text key, which is
    /// parsed here; a key that does not parse yields null, so the caller rejects the cursor. Null when there is no cursor.
    /// </summary>
    public static KeysetPosition<TKey>? ReadValuePosition<TKey>(KeysetCursor? cursor, string sortField, Func<string, TKey?> parseKey)
        where TKey : struct
    {
        if (cursor is null)
        {
            return null;
        }

        var raw = ReadPosition(cursor, sortField, ParseText);
        var parsed = raw is null ? null : parseKey(raw.Key);
        return parsed is null ? null : new KeysetPosition<TKey>(parsed.Value, raw!.Id);
    }

    /// <summary>Round-trip text form of a timestamp sort key.</summary>
    public static string FormatTimestamp(DateTime value) => value.ToString(TimestampFormat, CultureInfo.InvariantCulture);

    /// <summary>Text form of an optional timestamp key; an absent value is written as empty text.</summary>
    public static string FormatTimestamp(DateTime? value) => value is { } present ? FormatTimestamp(present) : string.Empty;

    /// <summary>Parses a key written by <see cref="FormatTimestamp(DateTime)"/>.</summary>
    public static DateTime? ParseTimestamp(string key) =>
        DateTime.TryParse(key, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? DateTime.SpecifyKind(parsed.ToUniversalTime(), DateTimeKind.Utc)
            : null;

    /// <summary>
    /// Orders <paramref name="query"/> by <paramref name="key"/> then <paramref name="id"/>, and keeps only the
    /// rows after <paramref name="after"/> when a position is given.
    /// </summary>
    public static IQueryable<TEntity> Apply<TEntity, TKey>(
        IQueryable<TEntity> query,
        Expression<Func<TEntity, TKey>> key,
        Expression<Func<TEntity, Guid>> id,
        KeysetPosition<TKey>? after,
        bool descending)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (after is not null)
        {
            query = query.Where(RowsAfter(key, id, after, descending));
        }

        return descending
            ? query.OrderByDescending(key).ThenByDescending(id)
            : query.OrderBy(key).ThenBy(id);
    }

    private static Expression<Func<TEntity, bool>> RowsAfter<TEntity, TKey>(
        Expression<Func<TEntity, TKey>> key,
        Expression<Func<TEntity, Guid>> id,
        KeysetPosition<TKey> after,
        bool descending)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        var keyValue = ParameterRebinder.Rebind(key.Body, key.Parameters[0], entity);
        var idValue = ParameterRebinder.Rebind(id.Body, id.Parameters[0], entity);
        var keyConstant = Expression.Constant(after.Key, typeof(TKey));
        var idConstant = Expression.Constant(after.Id);

        // Ascending pages go forward (greater), descending pages go backward (less).
        var keyBeyond = Beyond(keyValue, keyConstant, descending);
        var keyTied = Expression.AndAlso(
            Expression.Equal(keyValue, keyConstant),
            Beyond(idValue, idConstant, descending));
        return Expression.Lambda<Func<TEntity, bool>>(Expression.OrElse(keyBeyond, keyTied), entity);
    }

    private static Expression Beyond(Expression value, Expression boundary, bool descending)
    {
        if (value.Type == typeof(string))
        {
            var comparison = Expression.Call(StringCompare, value, boundary);
            return descending
                ? Expression.LessThan(comparison, Expression.Constant(0))
                : Expression.GreaterThan(comparison, Expression.Constant(0));
        }

        return descending
            ? Expression.LessThan(value, boundary)
            : Expression.GreaterThan(value, boundary);
    }

    /// <summary>Points one lambda's parameter at another, so key and id expressions share the entity parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        public static Expression Rebind(Expression body, ParameterExpression from, ParameterExpression to) =>
            new ParameterRebinder(from, to).Visit(body);

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
