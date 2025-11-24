using System;
using System.Collections.Generic;

namespace PhoneStoreUser.Components.ViewModels;

public class PagedResult<T>
{
    public PagedResult(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page < 1 ? 1 : page;
        PageSize = pageSize <= 0 ? 10 : pageSize;
    }

    public List<T> Items { get; }
    public int TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => TotalPages > 0 && Page < TotalPages;
}
