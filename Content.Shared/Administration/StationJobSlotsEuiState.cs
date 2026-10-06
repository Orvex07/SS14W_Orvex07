using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class StationJobSlotsEuiState(StationJobSlotsData[] stations) : EuiStateBase
{
    public readonly StationJobSlotsData[] Stations = stations;
}

[Serializable, NetSerializable]
public sealed class StationJobSlotsData(
    NetEntity station,
    string name,
    Dictionary<ProtoId<JobPrototype>, int?> slots,
    ProtoId<JobWeightPrototype>? weights = null)
{
    public readonly NetEntity Station = station;
    public readonly string Name = name;
    public readonly Dictionary<ProtoId<JobPrototype>, int?> Slots = slots;
    public readonly ProtoId<JobWeightPrototype>? Weights = weights;
}

[Serializable, NetSerializable]
public enum StationJobSlotOperation : byte
{
    Set,
    Restore,
    Add,
}

[Serializable, NetSerializable]
public sealed class StationJobSlotsChangeMessage(
    NetEntity station,
    ProtoId<JobPrototype> job,
    StationJobSlotOperation operation,
    int? slots = null) : EuiMessageBase
{
    public readonly NetEntity Station = station;
    public readonly ProtoId<JobPrototype> Job = job;
    public readonly StationJobSlotOperation Operation = operation;
    public readonly int? Slots = slots;
}
