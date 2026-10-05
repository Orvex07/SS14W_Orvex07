using Content.Server.Administration.UI;
using Content.Server.EUI;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;

namespace Content.Server.Administration.Commands;

[ToolshedCommand(Name = "station_job_slots_ui"), AdminCommand(AdminFlags.VarEdit)]
public sealed partial class StationJobSlotsUiCommand : ToolshedCommand
{
    [Dependency] private EuiManager _eui = default!;

    [CommandImplementation]
    public void Execute(IInvocationContext context)
    {
        if (context.Session is not { } player)
        {
            context.ReportError(new NotForServerConsoleError());
            return;
        }

        _eui.OpenEui(new StationJobSlotsEui(GetSys<ServerStationJobsSystem>()), player);
    }
}
