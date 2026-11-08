namespace Parley.SharedKernel.Paging;

/// <summary>The sort key and identifier of the last row of a page; the next page starts after it.</summary>
public sealed record KeysetPosition<TKey>(TKey Key, Guid Id);
