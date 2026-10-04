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
    string stationName,
    Dictionary<ProtoId<JobPrototype>, int?> jobSlots,
    ProtoId<JobWeightPrototype>? jobWeights = null)
{
    public readonly NetEntity Station = station;
    public readonly string StationName = stationName;
    public readonly ProtoId<JobWeightPrototype>? JobWeights = jobWeights;
    public readonly Dictionary<ProtoId<JobPrototype>, int?> JobSlots = jobSlots;
}

[Serializable, NetSerializable]
public enum StationJobSlotOperation : byte
{
    Increase,
    Decrease,
    MakeUnlimited,
    // Switching back from unlimited starts with no free slots.
    MakeLimited,
    // A newly added job starts with one free slot.
    Add,
}

[Serializable, NetSerializable]
public sealed class StationJobSlotsChangeMessage(
    NetEntity station,
    ProtoId<JobPrototype> job,
    StationJobSlotOperation operation) : EuiMessageBase
{
    public readonly NetEntity Station = station;
    public readonly ProtoId<JobPrototype> Job = job;
    public readonly StationJobSlotOperation Operation = operation;
}
