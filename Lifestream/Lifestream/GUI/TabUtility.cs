using ECommons.GameHelpers;
using Lifestream.Paissa;
using NightmareUI.ImGuiElements;
using NightmareUI.PrimaryUI;

namespace Lifestream.GUI;
public static class TabUtility
{
    public static int TargetWorldID = 0;
    private static WorldSelector WorldSelector = new()
    {
        DisplayCurrent = false,
        EmptyName = "Disabled",
        ShouldHideWorld = (x) => x == Player.Object?.CurrentWorld.RowId
    };
    private static PaissaImporter PaissaImporter = new();

    public static void Draw()
    {
        new NuiBuilder()
            .Section("抵達伺服器時關閉遊戲")
            .Widget(() =>
            {
                ImGuiEx.SetNextItemFullWidth();
                WorldSelector.Draw(ref TargetWorldID);
            })
            .Section("從 PaissaDB 匯入房屋列表")
            .Widget(() =>
            {
                ImGuiEx.SetNextItemFullWidth();
                PaissaImporter.Draw();
            })
            .Draw();
    }
}
