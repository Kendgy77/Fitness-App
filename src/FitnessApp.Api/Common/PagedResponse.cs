using System.ComponentModel.DataAnnotations;

namespace FitnessApp.Api.Common;

/// <summary>Query-string paging for every list endpoint: <c>?page=2&amp;pageSize=20</c>.</summary>
public record PageQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue, ErrorMessage = "Numer strony musi być większy od zera.")]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "Rozmiar strony musi wynosić od 1 do 100.")]
    public int PageSize { get; init; } = 20;

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>One page of a list plus what the UI needs to draw the pager.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
