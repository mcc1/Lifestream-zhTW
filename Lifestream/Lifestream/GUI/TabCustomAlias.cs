using ECommons.Configuration;
using ECommons.GameHelpers;
using ECommons.MathHelpers;
using ECommons.SplatoonAPI;
using Lifestream.Data;
using Lifestream.Tasks.SameWorld;
using Newtonsoft.Json;
using NightmareUI.ImGuiElements;
using Aetheryte = Lumina.Excel.Sheets.Aetheryte;

namespace Lifestream.GUI;
public static class TabCustomAlias
{
    private static ImGuiEx.RealtimeDragDrop<CustomAliasCommand> DragDrop = new("CusACmd", x => x.ID);

    private static readonly Dictionary<CustomAliasKind, string> CustomAliasKindNames = new()
    {
        [CustomAliasKind.Teleport_to_Aetheryte] = "傳送至以太之光",
        [CustomAliasKind.Move_to_point] = "移動至目標點",
        [CustomAliasKind.Navmesh_to_point] = "Navmesh 至目標點",
        [CustomAliasKind.Change_world] = "切換伺服器",
        [CustomAliasKind.Use_Aethernet] = "使用以太網",
        [CustomAliasKind.Circular_movement] = "環形移動",
        [CustomAliasKind.Interact] = "互動",
        [CustomAliasKind.Mount_Up] = "騎乘坐騎",
        [CustomAliasKind.Select_Yes] = "選擇「是」",
        [CustomAliasKind.Select_List_Option] = "選擇清單選項",
        [CustomAliasKind.Confirm_Contents_Finder] = "確認副本配對",
    };

    public static void Draw()
    {
        var selector = S.CustomAliasFileSystemManager.FileSystem.Selector;
        selector.Draw(150f.Scale());
        ImGui.SameLine();
        if(ImGui.BeginChild("Child"))
        {
            if(selector.Selected != null)
            {
                var item = selector.Selected;
                DrawAlias(item);
            }
            else
            {
                ImGuiEx.TextWrapped($"首先，選擇一個要編輯的別名，或建立一個新的。");
            }
        }
        ImGui.EndChild();
    }

    private static List<Action> PostTableActions = [];
    private static void DrawAlias(CustomAlias selected)
    {
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "新增"))
        {
            selected.Commands.Add(new());
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Paste, "貼上"))
        {
            try
            {
                var result = JsonConvert.DeserializeObject<CustomAliasCommand>(Paste());
                if(result == null) throw new NullReferenceException();
                selected.Commands.Add(result);
            }
            catch(Exception e)
            {
                Notify.Error(e.Message);
                e.Log();
            }
        }
        ImGui.SameLine();
        ImGui.Checkbox("##en", ref selected.Enabled);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f.Scale());
        if(!selected.Enabled) ImGui.BeginDisabled();
        ImGui.InputText($"##Alias", ref selected.Alias, 50);
        if(!selected.Enabled) ImGui.EndDisabled();
        ImGuiEx.Tooltip("啟用");
        ImGui.SameLine();
        ImGuiEx.HelpMarker($"可透過 \"/li {selected.Alias}\" 指令使用");
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Play, "執行", enabled: !Utils.IsBusy()))
        {
            selected.Enqueue();
        }
        ImGui.SameLine();
        ImGuiEx.Text("顯示方式：");
        ImGuiEx.PluginAvailabilityIndicator([new("Splatoon")]);
        DragDrop.Begin();
        var cursor = ImGui.GetCursorPos();
        foreach(var x in PostTableActions)
        {
            x();
        }
        ImGui.SetCursorPos(cursor);
        PostTableActions.Clear();
        if(ImGuiEx.BeginDefaultTable(["控制", "~Command"], false))
        {
            for(var i = 0; i < selected.Commands.Count; i++)
            {
                var x = selected.Commands[i];
                ImGui.TableNextRow();
                DragDrop.SetRowColor(x.ID);
                ImGui.TableNextColumn();
                DragDrop.NextRow();
                DragDrop.DrawButtonDummy(x, selected.Commands, i);
                ImGui.TableNextColumn();
                var curpos = ImGui.GetCursorPos() + ImGui.GetContentRegionAvail() with { Y = 0 };
                if(x.Kind == CustomAliasKind.Move_to_point)
                {
                    var insertIndex = i + 1;
                    PostTableActions.Add(() =>
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.SetCursorPos(curpos - ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Clone.ToIconString()) with { Y = 0 });
                        ImGui.PopFont();
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Clone, x.ID, enabled: Player.Available))
                        {
                            new TickScheduler(() =>
                            {
                                selected.Commands.Insert(insertIndex, new()
                                {
                                    Kind = CustomAliasKind.Move_to_point,
                                    UseFlight = x.UseFlight,
                                    Point = Player.Position
                                });
                            });
                        }
                        ImGuiEx.Tooltip($"複製此指令並將其座標設為玩家座標");
                    });
                }

                ImGuiEx.TreeNodeCollapsingHeader($"指令 {i + 1}：{CustomAliasKindNames.GetValueOrDefault(x.Kind, x.Kind.ToString().Replace('_', ' '))}{GetExtraText(x)}###{x.ID}", () => DrawCommand(x, selected), ImGuiTreeNodeFlags.CollapsingHeader);
                DrawSplatoon(x, i);


            }
            ImGui.EndTable();
        }
        DragDrop.End();
    }

    private static string GetExtraText(CustomAliasCommand x)
    {
        if(x.Kind == CustomAliasKind.Move_to_point)
        {
            return $" {x.Point:F1}";
        }
        return "";
    }

    private static void DrawSplatoon(CustomAliasCommand command, int index)
    {
        if(!Splatoon.IsConnected()) return;
        if(command.Kind == CustomAliasKind.Circular_movement)
        {
            {
                var point = S.Ipc.SplatoonManager.GetNextPoint($"{index + 1}：環形移動");
                point.SetRefCoord(command.CenterPoint.ToVector3());
                Splatoon.DisplayOnce(point);
            }
            {
                var point = S.Ipc.SplatoonManager.GetNextPoint($"{index + 1}：環形退出");
                point.SetRefCoord(command.CircularExitPoint);
                Splatoon.DisplayOnce(point);
            }
            {
                var point = S.Ipc.SplatoonManager.GetNextPoint();
                point.SetRefCoord(command.CenterPoint.ToVector3());
                point.Filled = false;
                point.radius = command.Clamp == null ? Math.Clamp(Player.DistanceTo(command.CenterPoint), 1f, 10f) : (command.Clamp.Value.Min + command.Clamp.Value.Max) / 2f;
                Splatoon.DisplayOnce(point);
            }
        }
        else if(command.Kind == CustomAliasKind.Move_to_point)
        {
            var point = S.Ipc.SplatoonManager.GetNextPoint($"{index + 1}：步行到");
            point.SetRefCoord(command.Point);
            point.radius = command.Scatter;
            Splatoon.DisplayOnce(point);
        }
        else if(command.Kind == CustomAliasKind.Navmesh_to_point)
        {
            var point = S.Ipc.SplatoonManager.GetNextPoint($"{index + 1}：導航到");
            point.SetRefCoord(command.Point);
            Splatoon.DisplayOnce(point);
        }
    }


    private static readonly uint[] Aetherytes = Svc.Data.GetExcelSheet<Aetheryte>().Where(x => x.PlaceName.ValueNullable?.Name.ToString().IsNullOrEmpty() == false && x.IsAetheryte).Select(x => x.RowId).ToArray();
    private static readonly Dictionary<uint, string> AetherytePlaceNames = Aetherytes.Select(Svc.Data.GetExcelSheet<Aetheryte>().GetRow).ToDictionary(x => x.RowId, x => x.PlaceName.Value.Name.ToString());

    private static void DrawCommand(CustomAliasCommand command, CustomAlias selected)
    {
        ImGui.PushID(command.ID);
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Copy, "複製"))
        {
            Copy(EzConfig.DefaultSerializationFactory.Serialize(command, false));
        }
        ImGui.SameLine();
        if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Trash, "刪除", ImGuiEx.Ctrl))
        {
            new TickScheduler(() => selected.Commands.Remove(command));
        }
        ImGuiEx.Tooltip("按住 CTRL 並點擊");

        ImGui.Separator();
        ImGui.SetNextItemWidth(150f.Scale());
        ImGuiEx.EnumCombo("別名類型", ref command.Kind, names: CustomAliasKindNames);

        if(command.Kind == CustomAliasKind.Teleport_to_Aetheryte)
        {
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.Combo("選擇要傳送的以太之光", ref command.Aetheryte, Aetherytes, names: AetherytePlaceNames);
            ImGui.SetNextItemWidth(60f.Scale());
            ImGui.DragFloat("若已在此範圍內的以太之光旁則略過傳送", ref command.SkipTeleport, 0.01f);
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Move_to_point, CustomAliasKind.Navmesh_to_point))
        {
            Utils.DrawVector3Selector($"walktopoint{command.ID}", ref command.Point);
            ImGui.SameLine();
            ImGuiEx.Text(UiBuilder.IconFont, FontAwesomeIcon.ArrowsLeftRight.ToIconString());
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50f);
            ImGui.SliderFloat($"##scatter", ref command.Scatter, 0f, 2f);
            ImGuiEx.Tooltip("散佈");
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Move_to_point))
        {
            drawFlight();
        }

        if(command.Kind.EqualsAny(CustomAliasKind.Navmesh_to_point))
        {
            ImGui.SameLine();
            ImGuiEx.ButtonCheckbox(FontAwesomeIcon.FastForward, ref command.UseTA, EColor.Green);
            ImGuiEx.Tooltip("使用 TextAdvance 進行移動。飛行設定會沿用 TextAdvance。");
            if(!command.UseTA)
            {
                drawFlight();
            }
        }

        void drawFlight()
        {
            ImGui.SameLine();
            ImGuiEx.ButtonCheckbox(FontAwesomeIcon.Plane, ref command.UseFlight, EColor.Green);
            ImGuiEx.Tooltip("Fly for movement. Don't forget to use \"Mount Up\" command before.");
        }

        if(command.Kind == CustomAliasKind.Change_world)
        {
            ImGui.SetNextItemWidth(150f.Scale());
            WorldSelector.Instance.Draw(ref command.World);
            ImGui.SameLine();
            ImGuiEx.Text("選擇伺服器");
        }

        if(command.Kind == CustomAliasKind.Use_Aethernet)
        {
            ImGui.SetNextItemWidth(150f.Scale());
            if(ImGui.BeginCombo("選擇要傳送的以太網碎晶", command.Aetheryte == 0 ? "- 未選取 -" : Utils.KnownAetherytes.SafeSelect(command.Aetheryte, command.Aetheryte.ToString()), ImGuiComboFlags.HeightLarge))
            {
                ref var filter = ref Ref<string>.Get($"Filter{command.ID}");
                ImGui.SetNextItemWidth(200f);
                ImGui.InputTextWithHint("##filter", "篩選", ref filter, 50);
                foreach(var x in Utils.KnownAetherytesByCategories)
                {
                    bool shouldHide(ref string filter, KeyValuePair<uint, string> v) => filter.Length > 0 && !v.Value.Contains(filter, StringComparison.OrdinalIgnoreCase) && !x.Key.Contains(filter, StringComparison.OrdinalIgnoreCase);
                    foreach(var v in x.Value)
                    {
                        if(!shouldHide(ref filter, v)) goto Display;
                    }
                    continue;
                Display:
                    ImGuiEx.Text(EColor.YellowBright, $"{x.Key}:");
                    ImGui.Indent();
                    foreach(var v in x.Value)
                    {
                        if(shouldHide(ref filter, v)) continue;
                        var sel = command.Aetheryte == v.Key;
                        if(sel && ImGui.IsWindowAppearing()) ImGui.SetScrollHereY();
                        if(ImGui.Selectable($"{v.Value}##{v.Key}", sel))
                        {
                            command.Aetheryte = v.Key;
                        }
                    }
                    ImGui.Unindent();
                    ImGui.Separator();
                }
                ImGui.EndCombo();
            }
        }

        if(command.Kind == CustomAliasKind.Circular_movement)
        {
            if(ImGui.BeginTable("circular", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoSavedSettings))
            {
                ImGui.TableSetupColumn("1");
                ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"中心點：");
                ImGui.TableNextColumn();
                Utils.DrawVector2Selector("center", ref command.CenterPoint);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"出口點：");
                ImGui.TableNextColumn();
                Utils.DrawVector3Selector($"exit{command.ID}", ref command.CircularExitPoint);
                ImGui.Checkbox("步行到出口點後結束", ref command.WalkToExit);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"精度：");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.DragFloat("##precision", ref command.Precision.ValidateRange(4f, 100f), 0.01f);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"容差：");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.DragInt("##tol", ref command.Tolerance.ValidateRange(1, (int)(command.Precision * 0.75f)), 0.01f);

                ImGui.TableNextColumn();
                ImGuiEx.TextV($"距離上限：");
                ImGui.TableNextColumn();
                var en = command.Clamp != null;
                if(ImGui.Checkbox($"##clamp", ref en))
                {
                    if(en)
                    {
                        command.Clamp = (0, 10);
                    }
                    else
                    {
                        command.Clamp = null;
                    }
                }
                if(command.Clamp != null)
                {
                    var v = command.Clamp.Value;
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f.Scale());
                    ImGui.DragFloat("##prec1", ref v.Min, 0.01f);
                    ImGui.SameLine();
                    ImGuiEx.Text("-");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(50f.Scale());
                    ImGui.DragFloat("##prec2", ref v.Max, 0.01f);
                    if(v.Min < v.Max)
                    {
                        command.Clamp = v;
                    }
                    if(Svc.Targets.Target != null)
                    {
                        ImGui.SameLine();
                        ImGuiEx.Text($"到目標：{Player.DistanceTo(Svc.Targets.Target):F1}");
                    }
                }

                ImGui.EndTable();
            }
        }
        if(command.Kind == CustomAliasKind.Interact)
        {
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.InputUint("資料 ID", ref command.DataID);
            ImGui.SameLine();
            if(ImGuiEx.Button("目標", Svc.Targets.Target?.DataId != 0))
            {
                command.DataID = Svc.Targets.Target.DataId;
            }
        }
        if(command.Kind.EqualsAny(CustomAliasKind.Select_Yes, CustomAliasKind.Select_List_Option))
        {
            ImGuiEx.TextWrapped($"列出你想選擇／確認的項目：");
            if(ImGuiEx.BeginDefaultTable("ItemLst", ["~1", "2"], false))
            {
                for(var i = 0; i < command.SelectOption.Count; i++)
                {
                    ImGui.PushID(i);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.SetNextItemFullWidth();
                    var str = command.SelectOption[i];
                    if(ImGui.InputText("##selectOpt", ref str, 500))
                    {
                        command.SelectOption[i] = str;
                    }
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        var idx = i;
                        new TickScheduler(() => command.SelectOption.RemoveAt(idx));
                    }
                    ImGui.PopID();
                }
                ImGui.EndTable();
                if(ImGuiEx.IconButtonWithText(FontAwesomeIcon.Plus, "新增選項"))
                {
                    command.SelectOption.Add("");
                }
            }
        }
        if(command.Kind.EqualsAny(CustomAliasKind.Select_Yes, CustomAliasKind.Select_List_Option, CustomAliasKind.Confirm_Contents_Finder))
        {
            ImGui.Checkbox("略過畫面淡出", ref command.StopOnScreenFade);
        }
        ImGui.PopID();
    }
}
