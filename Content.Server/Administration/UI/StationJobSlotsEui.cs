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

public sealed partial class StationJobSlotsEui : BaseEui
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IAdminLogManager _adminLogManager = default!;

    private readonly ServerStationJobsSystem _stationJobs;

    public StationJobSlotsEui()
    {
        _stationJobs = _entityManager.System<ServerStationJobsSystem>();
    }

    public override void Opened()
    {
        base.Opened();
        _adminManager.OnPermsChanged += OnPermsChanged;
        _stationJobs.JobsChanged += StateDirty;
        StateDirty();
    }

    public override void Closed()
    {
        _adminManager.OnPermsChanged -= OnPermsChanged;
        _stationJobs.JobsChanged -= StateDirty;
        base.Closed();
    }

    public override StationJobSlotsEuiState GetNewState()
    {
        var stations = new List<StationJobSlotsData>();
        var query = _entityManager.EntityQueryEnumerator<StationJobsComponent>();
        while (query.MoveNext(out var uid, out var jobs))
        {
            stations.Add(new StationJobSlotsData(
                _entityManager.GetNetEntity(uid),
                _entityManager.GetComponent<MetaDataComponent>(uid).EntityName,
                new Dictionary<ProtoId<JobPrototype>, int?>(_stationJobs.GetJobs(uid, jobs)),
                _entityManager.TryGetComponent<StationDataComponent>(uid, out var data) ? data.JobWeights : null));
        }

        return new StationJobSlotsEuiState(stations.OrderBy(station => station.StationName, StringComparer.Ordinal).ToArray());
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (msg is not StationJobSlotsChangeMessage change)
            return;

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.VarEdit))
        {
            Close();
            return;
        }

        ChangeSlots(change);
        // Refresh rejected requests too, including a locally toggled unlimited button.
        StateDirty();
    }

    private void ChangeSlots(StationJobSlotsChangeMessage change)
    {
        if (!_prototypeManager.HasIndex(change.Job) ||
            !_entityManager.TryGetEntity(change.Station, out var station) ||
            !_entityManager.TryGetComponent<StationJobsComponent>(station, out var stationJobs))
            return;

        var exists = _stationJobs.TryGetJobSlot(station.Value, change.Job, out var current, stationJobs);
        int? updated;
        switch (change.Operation)
        {
            case StationJobSlotOperation.Add when !exists:
                updated = 1;
                break;
            case StationJobSlotOperation.Increase when exists && current is >= 0 and < int.MaxValue:
                updated = current + 1;
                break;
            case StationJobSlotOperation.Decrease when exists && current is > 0:
                updated = current - 1;
                break;
            case StationJobSlotOperation.MakeUnlimited when exists && current != null:
                updated = null;
                break;
            case StationJobSlotOperation.MakeLimited when exists && current == null:
                updated = 0;
                break;
            default:
                return;
        }

        if (updated is { } slots)
        {
            var total = (long) stationJobs.TotalJobs - (current ?? 0) + slots;
            if (total > int.MaxValue)
                return;

            if (!_stationJobs.TrySetJobSlot(station.Value, change.Job, slots,
                    createSlot: !exists, stationJobs: stationJobs))
                return;
        }
        else
        {
            _stationJobs.MakeJobUnlimited(station.Value, change.Job, stationJobs);
        }

        var previous = exists ? current?.ToString() ?? "unlimited" : "absent";
        _adminLogManager.Add(LogType.AdminCommands, LogImpact.Low,
            $"{Player.Name} ({Player.UserId}) changed job {change.Job} on {_entityManager.ToPrettyString(station.Value)} " +
            $"using {change.Operation}: {previous} -> {updated?.ToString() ?? "unlimited"}");
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.VarEdit))
            Close();
    }
}
