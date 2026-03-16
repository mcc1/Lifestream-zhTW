using ECommons.Funding;

namespace Lifestream.GUI;

internal static unsafe class MainGui
{
    internal static void Draw()
    {
        PatreonBanner.DrawRight();
        ImGuiEx.EzTabBar("LifestreamTabs", PatreonBanner.Text,
            ("地址簿", TabAddressBook.Draw, null, true),
            ("房屋登記", UIHouseReg.Draw, null, true),
            ("自訂別名", TabCustomAlias.Draw, null, true),
            ("工具", TabUtility.Draw, null, true),
            ("設定", UISettings.Draw, null, true),
            ("說明", DrawHelp, null, true),
            ("除錯", UIDebug.Draw, ImGuiColors.DalamudGrey3, true)
            );
    }

    private static void DrawHelp()
    {
        ImGuiEx.TextWrapped(Lang.Help);
    }
}
