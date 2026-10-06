using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Roles;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.UI;

public sealed partial class StationJobSlotsEui(ServerStationJobsSystem stationJobs) : BaseEui
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IAdminLogManager _logs = default!;

    private readonly Dictionary<(EntityUid Station, ProtoId<JobPrototype> Job), int> _limitedSlots = [];

    private bool CanEdit => _admins.HasAdminFlag(Player, AdminFlags.VarEdit);

    public override void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
        stationJobs.JobsChanged += StateDirty;
        StateDirty();
    }

    public override void Closed()
    {
        _admins.OnPermsChanged -= OnPermsChanged;
        stationJobs.JobsChanged -= StateDirty;
        base.Closed();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !CanEdit)
            Close();
    }

    public override StationJobSlotsEuiState GetNewState()
    {
        var stations = new List<StationJobSlotsData>();
        var query = _entities.EntityQueryEnumerator<StationJobsComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var jobs, out var meta))
        {
            var slots = new Dictionary<ProtoId<JobPrototype>, int?>(stationJobs.GetJobs(uid, jobs));
            var weights = _entities.TryGetComponent<StationDataComponent>(uid, out var data) ? data.JobWeights : null;
            stations.Add(new StationJobSlotsData(_entities.GetNetEntity(uid), meta.EntityName, slots, weights));
        }

        var sorted = stations.OrderBy(station => station.Name, StringComparer.CurrentCulture).ToArray();
        return new StationJobSlotsEuiState(sorted);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (msg is not StationJobSlotsChangeMessage change)
            return;

        if (!CanEdit)
        {
            Close();
            return;
        }

        ChangeSlots(change);
        StateDirty();
    }

    private void ChangeSlots(StationJobSlotsChangeMessage change)
    {
        if (!_prototypes.HasIndex(change.Job) ||
            !_entities.TryGetEntity(change.Station, out var station) ||
            !_entities.TryGetComponent<StationJobsComponent>(station, out var jobs))
            return;

        var key = (station.Value, change.Job);
        var exists = stationJobs.TryGetJobSlot(station.Value, change.Job, out var current, jobs);
        int? updated;

        switch (change.Operation)
        {
            case StationJobSlotOperation.Add when !exists:
                updated = 1;
                break;
            case StationJobSlotOperation.Set when exists && change.Slots is null or >= 0:
                updated = change.Slots;
                break;
            case StationJobSlotOperation.Restore when exists && current is null:
                updated = _limitedSlots.GetValueOrDefault(key);
                break;
            case StationJobSlotOperation.Remove when exists:
                updated = null;
                break;
            default:
                return;
        }

        if (change.Operation == StationJobSlotOperation.Remove)
        {
            if (!stationJobs.TryRemoveJobSlot(station.Value, change.Job, jobs))
                return;

            _limitedSlots.Remove(key);
        }

        else if (exists && updated == current)
            return;

        else if (updated is null)
        {
            if (current is { } limited)
                _limitedSlots[key] = limited;

            stationJobs.MakeJobUnlimited(station.Value, change.Job, jobs);
        }
        else
        {
            var total = (long)jobs.TotalJobs - (current ?? 0) + updated.Value;
            if (updated < 0 || total is < 0 or > int.MaxValue)
                return;

            if (!stationJobs.TrySetJobSlot(station.Value,
                    change.Job,
                    updated.Value,
                    createSlot: !exists,
                    stationJobs: jobs))
                return;

            _limitedSlots.Remove(key);
        }

        var previous = exists ? current?.ToString() ?? "unlimited" : "absent";
        var next = change.Operation == StationJobSlotOperation.Remove
            ? "absent"
            : updated?.ToString() ?? "unlimited";

        _logs.Add(LogType.AdminCommands,
            LogImpact.Low,
            $"{Player} changed job {change.Job} on {station} " +
            $"using {change.Operation}: {previous} -> {next}");
    }
}
