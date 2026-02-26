using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TC.Agro.SensorIngest.Domain.Snapshots;

namespace TC.Agro.SensorIngest.Service.Hubs
{
    [Authorize(Roles = "Admin,Producer")]
    public sealed class SensorHub : Hub<ISensorHubClient>
    {
        private static readonly string[] OwnerClaimTypes =
        [
            "sub",
            ClaimTypes.NameIdentifier,
            "oid",
        ];

        private readonly ISensorReadingRepository _readingRepository;
        private readonly ISensorSnapshotStore _snapshotStore;
        private readonly ILogger<SensorHub> _logger;

        public SensorHub(
            ISensorReadingRepository readingRepository,
            ISensorSnapshotStore snapshotStore,
            ILogger<SensorHub> logger)
        {
            _readingRepository = readingRepository ?? throw new ArgumentNullException(nameof(readingRepository));
            _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task JoinPlotGroup(string plotId)
        {
            if (!Guid.TryParse(plotId, out var parsedPlotId) || parsedPlotId == Guid.Empty)
            {
                throw new HubException("Invalid plotId. Must be a valid non-empty GUID.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"plot:{parsedPlotId}").ConfigureAwait(false);
            await SendRecentReadingsForPlotAsync(parsedPlotId, Context.ConnectionAborted).ConfigureAwait(false);
        }

        public Task LeavePlotGroup(string plotId)
        {
            if (!Guid.TryParse(plotId, out var parsedPlotId) || parsedPlotId == Guid.Empty)
            {
                throw new HubException("Invalid plotId. Must be a valid non-empty GUID.");
            }

            return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"plot:{parsedPlotId}");
        }

        public async Task JoinOwnerGroup(string? ownerId = null)
        {
            var targetOwnerId = ResolveOwnerScope(ownerId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"owner:{targetOwnerId}").ConfigureAwait(false);

            var ownerSnapshots = await _snapshotStore
                .GetByOwnerIdAsync(targetOwnerId, Context.ConnectionAborted)
                .ConfigureAwait(false);

            foreach (var plotId in ownerSnapshots.Select(snapshot => snapshot.PlotId).Distinct())
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"plot:{plotId}").ConfigureAwait(false);
            }

            await SendRecentReadingsForOwnerAsync(ownerSnapshots, Context.ConnectionAborted).ConfigureAwait(false);
        }

        public async Task LeaveOwnerGroup(string? ownerId = null)
        {
            var targetOwnerId = ResolveOwnerScope(ownerId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"owner:{targetOwnerId}").ConfigureAwait(false);

            var ownerSnapshots = await _snapshotStore
                .GetByOwnerIdAsync(targetOwnerId, Context.ConnectionAborted)
                .ConfigureAwait(false);

            foreach (var plotId in ownerSnapshots.Select(snapshot => snapshot.PlotId).Distinct())
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"plot:{plotId}").ConfigureAwait(false);
            }
        }

        private Guid ResolveOwnerScope(string? ownerId)
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                if (!Guid.TryParse(ownerId, out var adminTargetOwnerId) || adminTargetOwnerId == Guid.Empty)
                    throw new HubException("Admin must provide a valid non-empty ownerId.");

                _logger.LogDebug(
                    "Owner scope resolved for SensorHub using explicit admin ownerId parameter.");

                return adminTargetOwnerId;
            }

            var (currentOwnerId, claimTypeUsed) = ResolveOwnerScopeFromClaims();

            if (currentOwnerId == Guid.Empty)
            {
                _logger.LogWarning(
                    "Unable to resolve owner scope for SensorHub. Checked claim types: {ClaimTypes}",
                    OwnerClaimTypes);
                throw new HubException("Unable to resolve owner scope for current user.");
            }

            _logger.LogDebug(
                "Owner scope resolved for SensorHub using claim type {ClaimType}.",
                claimTypeUsed);

            return currentOwnerId;
        }

        private (Guid OwnerId, string? ClaimTypeUsed) ResolveOwnerScopeFromClaims()
        {
            foreach (var claimType in OwnerClaimTypes)
            {
                var claimValue = Context.User?.FindFirstValue(claimType);
                if (Guid.TryParse(claimValue, out var ownerId) && ownerId != Guid.Empty)
                {
                    return (ownerId, claimType);
                }
            }

            return (Guid.Empty, null);
        }

        private async Task SendRecentReadingsForPlotAsync(Guid plotId, CancellationToken cancellationToken)
        {
            var snapshots = await _snapshotStore.GetListByPlotIdAsync(plotId, cancellationToken).ConfigureAwait(false);

            if (snapshots.Count == 0)
            {
                return;
            }

            var sensorLookup = snapshots.ToDictionary(snapshot => snapshot.Id);
            var recentReadings = await _readingRepository
                .GetByPlotIdAsync(plotId, from: null, to: null, limit: 20, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            foreach (var reading in recentReadings)
            {
                if (!sensorLookup.TryGetValue(reading.SensorId, out var sensor))
                {
                    continue;
                }

                await Clients.Caller.SensorReading(new SensorReadingRequest(
                    reading.SensorId,
                    sensor.PlotId,
                    sensor.Label,
                    sensor.PlotName,
                    sensor.PropertyName,
                    reading.Temperature,
                    reading.Humidity,
                    reading.SoilMoisture,
                    reading.Time)).ConfigureAwait(false);
            }
        }

        private async Task SendRecentReadingsForOwnerAsync(
            IReadOnlyList<SensorSnapshot> ownerSnapshots,
            CancellationToken cancellationToken)
        {
            if (ownerSnapshots.Count == 0)
            {
                return;
            }

            var sensorLookup = ownerSnapshots.ToDictionary(snapshot => snapshot.Id);
            var plotIds = ownerSnapshots
                .Select(snapshot => snapshot.PlotId)
                .Distinct()
                .ToList();

            var readingsByPlotTasks = plotIds.ToDictionary(
                plotId => plotId,
                plotId => _readingRepository.GetByPlotIdAsync(
                    plotId,
                    from: null,
                    to: null,
                    limit: 20,
                    cancellationToken: cancellationToken));

            await Task.WhenAll(readingsByPlotTasks.Values).ConfigureAwait(false);

            foreach (var recentReadings in readingsByPlotTasks.Values.Select(task => task.Result))
            {
                foreach (var reading in recentReadings)
                {
                    if (!sensorLookup.TryGetValue(reading.SensorId, out var sensor))
                    {
                        continue;
                    }

                    await Clients.Caller.SensorReading(new SensorReadingRequest(
                        reading.SensorId,
                        sensor.PlotId,
                        sensor.Label,
                        sensor.PlotName,
                        sensor.PropertyName,
                        reading.Temperature,
                        reading.Humidity,
                        reading.SoilMoisture,
                        reading.Time)).ConfigureAwait(false);
                }
            }
        }
    }
}
