using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ECommons;
using ECommons.ImGuiMethods;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NightmareUI.PrimaryUI.Components;
internal unsafe class Section
{
    internal string Name = "";
    internal Vector4? Color;
    internal List<IWidget> Widgets = [];
    internal bool PrevSeparator = false;
    internal Func<bool>? Cond = null;
    internal bool CondComp;
    internal bool Collapsible;

    public bool ShouldHighlight => Widgets.OfType<ImGuiWidget>().Any(z => z.ShouldHighlight);

    private static Vector2 CellPadding = new(7f);


    internal void Draw(NuiBuilder builder)
    {
        var oldPadding = ImGui.GetStyle().CellPadding;
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, CellPadding);
        if(ImGui.BeginTable(Name, 1, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn(Name, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var isOpen = ImGui.GetStateStorage().GetBoolRef(ImGui.GetID(Name + "NuiSection"));
            Color ??= ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
            if(Collapsible && ImGui.IsWindowFocused() && ImGui.IsMouseHoveringRect(ImGui.GetCursorScreenPos() - ImGui.GetStyle().CellPadding,
                ImGui.GetCursorScreenPos() + ImGui.GetStyle().CellPadding + new Vector2(ImGui.GetContentRegionAvail().X, ImGui.CalcTextSize(Name).Y)
                ))
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                Color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered];
                if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    *isOpen = (byte)(*isOpen == 0 ? 1 : 0);
                }
            }
            if(Collapsible)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGuiEx.Text((*isOpen != 0? FontAwesomeIcon.Minus : FontAwesomeIcon.Plus).ToIconString());
                ImGui.PopFont();
                ImGui.SameLine();
            }
            ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.ColorConvertFloat4ToU32(Color.Value));
            ImGuiEx.Text(Name);
            if(!Collapsible || *isOpen != 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                foreach(var x in Widgets)
                {
                    if(x is CondIf condIf)
                    {
                        CondComp = true;
                        Cond = condIf.Predicate;
                    }
                    else if(x is CondElse)
                    {
                        CondComp = false;
                    }
                    else if(x is CondEndIf)
                    {
                        Cond = null;
                    }
                    if(Cond != null && Cond.Invoke() != CondComp) continue;
                    if(x is ImGuiWidget imGuiWidget)
                    {
                        Vector4? col = (builder.Filter != "") ? (imGuiWidget.ShouldHighlight ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudGrey3) : null;
                        PrevSeparator = false;
                        if(col != null) ImGui.PushStyleColor(ImGuiCol.Text, col.Value);
                        try
                        {
                            if(imGuiWidget.Width != null)
                            {
                                ImGui.SetNextItemWidth(imGuiWidget.Width.Value);
                            }
                            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, oldPadding);
                            try
                            {
                                imGuiWidget.DrawAction(imGuiWidget.Label);
                            }
                            catch(Exception iex)
                            {
                                iex.Log();
                            }
                            ImGui.PopStyleVar();
                            if(imGuiWidget.Help != null)
                            {
                                ImGuiEx.HelpMarker(imGuiWidget.Help);
                            }
                        }
                        catch(Exception e)
                        {
                            e.Log();
                        }
                        if(col != null) ImGui.PopStyleColor();
                    }
                    else if(x is SeparatorWidget separatorWidget)
                    {
                        if(!PrevSeparator)
                        {
                            separatorWidget.DrawAction();
                            PrevSeparator = true;
                        }
                    }
                }
            }
            ImGui.EndTable();
        }
        ImGui.Dummy(new(5f));
        ImGui.PopStyleVar();
    }
}
