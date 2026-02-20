namespace TC.Agro.SensorIngest.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for IQueryable to apply sorting, filtering, and pagination dynamically.
    /// Reduces code duplication across repositories.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Applies dynamic sorting to AlertAggregate queries.
        /// </summary>
        public static IQueryable<AlertAggregate> ApplySorting(
            this IQueryable<AlertAggregate> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(a => a.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "id" => isAscending
                    ? query.OrderBy(a => a.Id)
                    : query.OrderByDescending(a => a.Id),
                "severity" => isAscending
                    ? query.OrderBy(a => a.Severity)
                    : query.OrderByDescending(a => a.Severity),
                "title" => isAscending
                    ? query.OrderBy(a => a.Title)
                    : query.OrderByDescending(a => a.Title),
                "plotname" => isAscending
                    ? query.OrderBy(a => a.PlotName)
                    : query.OrderByDescending(a => a.PlotName),
                "status" => isAscending
                    ? query.OrderBy(a => a.Status)
                    : query.OrderByDescending(a => a.Status),
                "createdat" => isAscending
                    ? query.OrderBy(a => a.CreatedAt)
                    : query.OrderByDescending(a => a.CreatedAt),
                "resolvedat" => isAscending
                    ? query.OrderBy(a => a.ResolvedAt)
                    : query.OrderByDescending(a => a.ResolvedAt),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };
        }

        /// <summary>
        /// Applies plot filter to SensorSnapshot queries.
        /// </summary>
        public static IQueryable<SensorSnapshot> ApplyPlotFilter(
            this IQueryable<SensorSnapshot> query,
            Guid? plotId)
        {
            if (!plotId.HasValue)
                return query;

            return query.Where(s => s.PlotId == plotId.Value);
        }

        /// <summary>
        /// Applies text search filter to SensorSnapshot queries.
        /// </summary>
        public static IQueryable<SensorSnapshot> ApplyTextFilter(
            this IQueryable<SensorSnapshot> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var pattern = $"%{filter}%";
            return query.Where(s =>
                EF.Functions.ILike(s.PlotName, pattern) ||
                (s.Label != null && EF.Functions.ILike(s.Label, pattern)) ||
                EF.Functions.ILike(s.PropertyName, pattern));
        }

        /// <summary>
        /// Applies text search filter to AlertAggregate queries.
        /// </summary>
        public static IQueryable<AlertAggregate> ApplyTextFilter(
            this IQueryable<AlertAggregate> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var pattern = $"%{filter}%";
            return query.Where(a =>
                EF.Functions.ILike(a.Title, pattern) ||
                EF.Functions.ILike(a.Message, pattern) ||
                EF.Functions.ILike(a.PlotName, pattern) ||
                EF.Functions.ILike(a.Severity, pattern));
        }

        /// <summary>
        /// Applies status filter to AlertAggregate queries.
        /// </summary>
        public static IQueryable<AlertAggregate> ApplyStatusFilter(
            this IQueryable<AlertAggregate> query,
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return query;

            return query.Where(a => EF.Functions.ILike(a.Status, status));
        }

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
