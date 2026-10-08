using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Client.Administration.UI.StationJobSlots;

[UsedImplicitly]
public sealed class StationJobSlotsEui : BaseEui
{
    private readonly StationJobSlotsWindow _window = new();

    public override void Opened()
    {
        base.Opened();
        _window.OnClose += OnWindowClosed;
        _window.OnSlotChange += OnSlotChange;
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.OnClose -= OnWindowClosed;
        _window.OnSlotChange -= OnSlotChange;
        _window.Close();
    }

    private void OnSlotChange(NetEntity station, ProtoId<JobPrototype> job, StationJobSlotOperation operation, int? slots) =>
        SendMessage(new StationJobSlotsChangeMessage(station, job, operation, slots));

    private void OnWindowClosed() =>
        SendMessage(new CloseEuiMessage());

    public override void HandleState(EuiStateBase state)
    {
        if (state is StationJobSlotsEuiState slots)
            _window.UpdateStations(slots.Stations);
    }
}
