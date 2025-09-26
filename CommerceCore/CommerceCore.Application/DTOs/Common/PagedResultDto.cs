namespace CommerceCore.Application.DTOs.Common
{
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        // Propriedades calculadas
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public int StartItem => Page > 0 && TotalCount > 0 ? ((Page - 1) * PageSize) + 1 : 0;
        public int EndItem => Page > 0 ? Math.Min(StartItem + PageSize - 1, TotalCount) : 0;
        public bool IsFirstPage => Page == 1;
        public bool IsLastPage => Page >= TotalPages;

        // Construtores
        public PagedResultDto() { }

        public PagedResultDto(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items ?? new List<T>();
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
            HasNextPage = page < TotalPages;
            HasPreviousPage = page > 1;
        }

        // Factory method
        public static PagedResultDto<T> Create(List<T> items, int totalCount, int page, int pageSize)
        {
            return new PagedResultDto<T>(items, totalCount, page, pageSize);
        }

        // Para resultados vazios
        public static PagedResultDto<T> Empty(int page, int pageSize)
        {
            return new PagedResultDto<T>(new List<T>(), 0, page, pageSize);
        }
    }
}
