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
        /// Applies dynamic sorting to SensorAggregate queries.
        /// </summary>
        public static IQueryable<SensorAggregate> ApplySorting(
            this IQueryable<SensorAggregate> query,
            string? sortBy,
            string? sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderByDescending(s => s.CreatedAt);

            var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "sensorid" => isAscending
                    ? query.OrderBy(s => s.SensorId)
                    : query.OrderByDescending(s => s.SensorId),
                "plotname" => isAscending
                    ? query.OrderBy(s => s.PlotName)
                    : query.OrderByDescending(s => s.PlotName),
                "status" => isAscending
                    ? query.OrderBy(s => s.Status)
                    : query.OrderByDescending(s => s.Status),
                "battery" => isAscending
                    ? query.OrderBy(s => s.Battery)
                    : query.OrderByDescending(s => s.Battery),
                "lastreadingat" => isAscending
                    ? query.OrderBy(s => s.LastReadingAt)
                    : query.OrderByDescending(s => s.LastReadingAt),
                "createdat" => isAscending
                    ? query.OrderBy(s => s.CreatedAt)
                    : query.OrderByDescending(s => s.CreatedAt),
                _ => query.OrderByDescending(s => s.CreatedAt)
            };
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
        /// Applies text search filter to SensorAggregate queries.
        /// </summary>
        public static IQueryable<SensorAggregate> ApplyTextFilter(
            this IQueryable<SensorAggregate> query,
            string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var pattern = $"%{filter}%";
            return query.Where(s =>
                EF.Functions.ILike(s.PlotName, pattern) ||
                EF.Functions.ILike(s.Status, pattern));
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
        /// Applies status filter to SensorAggregate queries.
        /// </summary>
        public static IQueryable<SensorAggregate> ApplyStatusFilter(
            this IQueryable<SensorAggregate> query,
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return query;

            return query.Where(s => EF.Functions.ILike(s.Status, status));
        }

        /// <summary>
        /// Applies plot filter to SensorAggregate queries.
        /// </summary>
        public static IQueryable<SensorAggregate> ApplyPlotFilter(
            this IQueryable<SensorAggregate> query,
            Guid? plotId)
        {
            if (!plotId.HasValue)
                return query;

            return query.Where(s => s.PlotId == plotId.Value);
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
