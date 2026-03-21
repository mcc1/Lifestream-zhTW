using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.SimpleGui;
using Lumina.Excel.Sheets;

namespace Lifestream.GUI.Windows;
public class SelectWorldWindow : Window
{
    private SelectWorldWindow() : base("Lifestream：選擇伺服器", ImGuiWindowFlags.AlwaysAutoResize)
    {
        EzConfigGui.WindowSystem.AddWindow(this);
    }

    public override void Draw()
    {
        var playerDc = Player.Object?.HomeWorld.ValueNullable?.DataCenter.RowId ?? 0;
        var worlds = S.Data.DataStore.DCWorlds.Concat(S.Data.DataStore.Worlds).Select(x =>
        {
            // TW fix: ExcelWorldHelper.Get(string) searches by Name and returns the first match.
            // "巴哈姆特" (and other TW worlds) have duplicate Name entries in the World sheet
            // (e.g. row 1160 Bahamute DC=0 "Unknown" comes before row 4033 TcBahamut DC=151).
            // For TW players, prefer the world row that belongs to the player's datacenter.
            if(Utils.ShouldUseTwTravelWorlds(playerDc) &&
               Svc.Data.GetExcelSheet<World>().TryGetFirst(w => w.Name.ToString().EqualsIgnoreCase(x) && w.DataCenter.RowId == playerDc, out var twWorld))
            {
                return ExcelWorldHelper.Get(twWorld.RowId);
            }
            return ExcelWorldHelper.Get(x);
        }).OrderBy(x => x?.Name.ToString());
        if(!worlds.Any())
        {
            ImGuiEx.Text($"沒有可用的目的地");
            return;
        }
        var datacenters = worlds.Select(x => x?.DataCenter).DistinctBy(x => x?.RowId).OrderBy(x => x.Value.ValueNullable?.Region).ToArray();
        if(ImGui.BeginTable("LifestreamSelectWorld", datacenters.Length, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuter))
        {
            foreach(var dc in datacenters)
            {
                var modifier = "";
                if(Player.Object?.HomeWorld.ValueNullable?.DataCenter.RowId == dc?.RowId) modifier += "";
                if(Player.Object?.CurrentWorld.ValueNullable?.DataCenter.RowId != dc?.RowId) modifier += "";
                ImGui.TableSetupColumn($"{modifier}{dc.Value.ValueNullable?.Name}");
            }
            ImGui.TableHeadersRow();
            ImGui.TableNextRow();
            var buttonSize = Vector2.Zero;
            foreach(var w in worlds)
            {
                var newSize = ImGuiHelpers.GetButtonSize("" + w?.Name.ToString());
                if(newSize.X > buttonSize.X) buttonSize = newSize;
            }
            buttonSize += new Vector2(0, C.ButtonHeightWorld);
            foreach(var dc in datacenters)
            {
                ImGui.TableNextColumn();
                foreach(var world in worlds)
                {
                    if(world?.DataCenter.RowId == dc?.RowId)
                    {
                        var modifier = "";
                        if(Player.Object?.HomeWorld.RowId == world?.RowId) modifier += "";
                        if(ImGuiEx.Button(modifier + world?.Name.ToString(), buttonSize, !Utils.IsBusy() && Player.Interactable && Player.Object?.CurrentWorld.RowId != world?.RowId))
                        {
                            P.ProcessCommand("/li", world?.Name.ToString());
                        }
                    }
                }
            }
            ImGui.EndTable();
        }
    }
}
