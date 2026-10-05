using Content.Shared.Eui;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// Job slots for each station. Null counts mean unlimited slots.
/// </summary>
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
    Increase,
    Decrease,
    MakeUnlimited,

    // Restore the free-slot count saved by this EUI, or zero if none was saved....
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
