namespace Motorsports.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int PageCount => Math.Max(1, (Total + PageSize - 1) / PageSize);
}

public static class Paging
{
    public const int DefaultPageSize = 50;

    public static int ClampPage(int? page) => page is > 0 ? page.Value : 1;

    public static string? NormalizeSearch(string? search) => string.IsNullOrWhiteSpace(search) ? null : search.Trim();
}
