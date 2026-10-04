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
    private readonly StationJobSlotsWindow _window;

    public StationJobSlotsEui()
    {
        _window = new StationJobSlotsWindow();
        _window.OnClose += OnWindowClosed;
        _window.OnSlotChangeRequested += OnSlotChangeRequested;
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.OnClose -= OnWindowClosed;
        _window.OnSlotChangeRequested -= OnSlotChangeRequested;
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not StationJobSlotsEuiState slotsState)
            return;

        _window.UpdateStations(slotsState.Stations);
    }

    private void OnSlotChangeRequested(
        NetEntity station,
        ProtoId<JobPrototype> job,
        StationJobSlotOperation operation)
    {
        SendMessage(new StationJobSlotsChangeMessage(station, job, operation));
    }

    private void OnWindowClosed()
    {
        SendMessage(new CloseEuiMessage());
    }
}
