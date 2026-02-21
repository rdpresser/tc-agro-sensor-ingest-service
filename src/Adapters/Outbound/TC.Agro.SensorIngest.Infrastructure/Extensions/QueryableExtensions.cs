namespace TC.Agro.SensorIngest.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to apply sorting, filtering, and pagination dynamically.
    /// Reduces code duplication across repositories.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Applies pagination to any queryable.
        /// </summary>
        public static IQueryable<T> ApplyPagination<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize)
        {
            return query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
    }
}
