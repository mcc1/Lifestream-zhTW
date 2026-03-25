using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lifestream.Data;
using Lifestream.Enums;
using Lifestream.Tasks.SameWorld;
using Lumina.Excel.Sheets;

namespace Lifestream.Tasks.Shortcuts;
public static unsafe class TaskMBShortcut
{
    public static void Enqueue()
    {
        var alias = C.WorldChangeAetheryte switch
        {
            WorldChangeAetheryte.Gridania => StaticAlias.GridaniaMarketboard,
            WorldChangeAetheryte.Limsa => StaticAlias.LimsaMarketboard,
            _ => StaticAlias.UldahMarketboard,
        };
        alias.Enqueue();
    }
}
