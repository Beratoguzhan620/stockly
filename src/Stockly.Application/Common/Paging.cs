using Microsoft.EntityFrameworkCore;

namespace Stockly.Application.Common;

internal static class Paging
{
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> source, int page, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await source.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        return new PagedList<T>
        {
            Items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }
}
