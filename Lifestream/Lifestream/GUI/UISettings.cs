using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lifestream.Data;
using Lifestream.Tasks.Shortcuts;
using Lumina.Excel.Sheets;
using NightmareUI;
using NightmareUI.PrimaryUI;
using System.Globalization;
using Action = System.Action;

namespace Lifestream.GUI;

internal static unsafe class UISettings
{
    private static string AddNew = "";
    internal static void Draw()
    {
        NuiTools.ButtonTabs([[new("一般", () => Wrapper(DrawGeneral)), new("覆蓋層", () => Wrapper(DrawOverlay))], [new("進階", () => Wrapper(DrawExpert)), new("服務帳號", () => Wrapper(UIServiceAccount.Draw)), new("旅行限制", TabTravelBan.Draw)]]);
    }

    private static void Wrapper(Action action)
    {
        ImGui.Dummy(new(5f));
        action();
    }

    private static void DrawGeneral()
    {
        new NuiBuilder()
        .Section("傳送設定")
        .Widget(() =>
        {
            ImGui.SetNextItemWidth(200f.Scale());
            ImGuiEx.EnumCombo($"傳送跨伺服器閘道", ref C.WorldChangeAetheryte, Lang.WorldChangeAetherytes);
            ImGuiEx.HelpMarker($"切換伺服器時要傳送到哪裡");
            ImGui.Checkbox($"跨伺服器/資料中心後傳送到指定以太網目的地", ref C.WorldVisitTPToAethernet);
            if(C.WorldVisitTPToAethernet)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(250f.Scale());
                ImGui.InputText("如同在 \"/li\" 指令中使用的以太網目的地", ref C.WorldVisitTPTarget, 50);
                ImGui.Checkbox($"僅從指令傳送", ref C.WorldVisitTPOnlyCmd);
                ImGui.Unindent();
            }
            ImGui.Checkbox($"在基礎城以太之光中加入蒼天街位置", ref C.Firmament);
            ImGui.Checkbox($"切換伺服器時自動離開非跨伺服器隊伍", ref C.LeavePartyBeforeWorldChange);
            ImGui.Checkbox($"在聊天視窗顯示傳送目的地", ref C.DisplayChatTeleport);
            ImGui.Checkbox($"以彈出通知顯示傳送目的地", ref C.DisplayPopupNotifications);
            ImGui.Checkbox("重試同伺服器失敗的跨伺服器訪問", ref C.RetryWorldVisit);
            ImGui.Indent();
            ImGui.SetNextItemWidth(100f.Scale());
            ImGui.InputInt("重試間隔（秒）##2", ref C.RetryWorldVisitInterval.ValidateRange(1, 120));
            ImGui.SameLine();
            ImGuiEx.Text("+ up to");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f.Scale());
            ImGui.InputInt("seconds##2", ref C.RetryWorldVisitIntervalDelta.ValidateRange(0, 120));
            ImGuiEx.HelpMarker("讓行為看起來沒那麼像機器人");
            ImGui.Unindent();
            //ImGui.Checkbox("Use Return instead of Teleport when possible", ref C.UseReturn);
            //ImGuiEx.HelpMarker("This includes any IPC calls");
            ImGui.Checkbox("完成旅行後啟用系統匣通知", ref C.EnableNotifications);
            ImGuiEx.PluginAvailabilityIndicator([new("NotificationMaster")]);
        })

        .Section("捷徑")
        .Widget(() =>
        {
            ImGui.SetNextItemWidth(200f.Scale());
            ImGuiEx.EnumCombo("\"/li\" 指令行為", ref C.LiCommandBehavior);
            ImGui.Checkbox("傳送到自己的公寓時進入內部", ref C.EnterMyApartment);
            ImGui.SetNextItemWidth(150f.Scale());
            ImGuiEx.EnumCombo("傳送到自己/部隊房屋時執行此動作", ref C.HouseEnterMode);
            ImGui.SetNextItemWidth(150f.Scale());
            if(ImGui.BeginCombo("偏好旅館", Utils.GetInnNameFromTerritory(C.PreferredInn), ImGuiComboFlags.HeightLarge))
            {
                foreach(var x in (uint[])[0, .. TaskPropertyShortcut.InnData.Keys])
                {
                    if(ImGui.Selectable(Utils.GetInnNameFromTerritory(x), x == C.PreferredInn)) C.PreferredInn = x;
                }
                ImGui.EndCombo();
            }
            if(Player.CID != 0)
            {
                ImGui.SetNextItemWidth(150f.Scale());
                var pref = C.PreferredSharedEstates.SafeSelect(Player.CID);
                var name = pref switch
                {
                    (0, 0, 0) => "第一個可用",
                    (-1, 0, 0) => "停用",
                    _ => $"{ExcelTerritoryHelper.GetName((uint)pref.Territory)}, W{pref.Ward}, P{pref.Plot}"
                };
                if(ImGui.BeginCombo($"{Player.NameWithWorld} 的偏好共享房屋", name))
                {
                    foreach(var x in Svc.AetheryteList.Where(x => x.IsSharedHouse))
                    {
                        if(ImGui.RadioButton("第一個可用", pref == default))
                        {
                            C.PreferredSharedEstates.Remove(Player.CID);
                        }
                        if(ImGui.RadioButton("停用", pref == (-1, 0, 0)))
                        {
                            C.PreferredSharedEstates[Player.CID] = (-1, 0, 0);
                        }
                        if(ImGui.RadioButton($"{ExcelTerritoryHelper.GetName(x.TerritoryId)}，第 {x.Ward} 區，地號 {x.Plot}", pref == ((int)x.TerritoryId, x.Ward, x.Plot)))
                        {
                            C.PreferredSharedEstates[Player.CID] = ((int)x.TerritoryId, x.Ward, x.Plot);
                        }
                    }
                    ImGui.EndCombo();
                }
            }
            ImGui.Separator();
            ImGuiEx.Text("\"/li auto\" 指令優先順序：");
            ImGui.SameLine();
            if(ImGui.SmallButton("重設")) C.PropertyPrio.Clear();
            var dragDrop = Ref<ImGuiEx.RealtimeDragDrop<AutoPropertyData>>.Get(() => new("apddd", x => x.Type.ToString()));
            C.PropertyPrio.AddRange(Enum.GetValues<TaskPropertyShortcut.PropertyType>().Where(x => x != TaskPropertyShortcut.PropertyType.Auto && !C.PropertyPrio.Any(s => s.Type == x)).Select(x => new AutoPropertyData(false, x)));
            dragDrop.Begin();
            for(var i = 0; i < C.PropertyPrio.Count; i++)
            {
                var d = C.PropertyPrio[i];
                ImGui.PushID($"c{i}");
                dragDrop.NextRow();
                dragDrop.DrawButtonDummy(d, C.PropertyPrio, i);
                ImGui.SameLine();
                ImGui.Checkbox($"{d.Type}", ref d.Enabled);
                ImGui.PopID();
            }
            dragDrop.End();
            ImGui.Separator();
        })

        .Section("地圖整合")
        .Widget(() =>
        {
            ImGui.Checkbox("點擊地圖上的以太網碎晶快速傳送", ref C.UseMapTeleport);
            ImGui.Checkbox("僅在同一地圖且靠近以太之光時處理", ref C.DisableMapClickOtherTerritory);
        })

        .Section("指令自動完成")
        .Widget(() =>
        {
            ImGuiEx.Text($"在聊天中輸入 Lifestream 指令時建議自動完成");
            ImGui.Checkbox("啟用", ref C.EnableAutoCompletion);
            ImGui.Checkbox("在固定位置顯示彈出視窗", ref C.AutoCompletionFixedWindow);
            ImGui.Indent();
            ImGui.SetNextItemWidth(200f.Scale());
            ImGui.DragFloat2("位置", ref C.AutoCompletionWindowOffset, 1f);
            ImGuiEx.RadioButtonBool("距底部", "距頂部", ref C.AutoCompletionWindowBottom, sameLine: true, inverted: true);
            ImGuiEx.RadioButtonBool("距右側", "距左側", ref C.AutoCompletionWindowRight, sameLine: true, inverted: true);
            ImGui.Unindent();
        })

        .Section("跨資料中心")
        .Widget(() =>
        {
            ImGui.Checkbox($"允許前往其他資料中心", ref C.AllowDcTransfer);
            ImGui.Checkbox($"切換資料中心前離開隊伍", ref C.LeavePartyBeforeLogout);
            ImGui.Checkbox($"若不在休息區，切換資料中心前先傳送到閘道以太之光", ref C.TeleportToGatewayBeforeLogout);
            ImGui.Checkbox($"完成資料中心旅行後傳送到閘道以太之光", ref C.DCReturnToGateway);
            ImGui.Checkbox($"資料中心轉移時允許替代伺服器", ref C.DcvUseAlternativeWorld);
            ImGuiEx.HelpMarker("若目標伺服器不可用，但目標資料中心的其他伺服器可用，將改選其他伺服器，並在登入後排隊進行正常的跨伺服器訪問。");
            ImGui.Checkbox($"目標伺服器不可用時重試資料中心轉移", ref C.EnableDvcRetry);
            ImGui.Indent();
            ImGui.SetNextItemWidth(150f.Scale());
            ImGui.InputInt("最大重試次數", ref C.MaxDcvRetries.ValidateRange(1, int.MaxValue));
            ImGui.SetNextItemWidth(150f.Scale());
            ImGui.InputInt("重試間隔（秒）", ref C.DcvRetryInterval.ValidateRange(10, 1000));
            ImGui.Unindent();
        })

        .Section("地址簿")
        .Widget(() =>
        {
            ImGui.Checkbox($"停用自動尋路到地號", ref C.AddressNoPathing);
            ImGuiEx.HelpMarker($"您將停留在離該小區最近的以太之光");
            ImGui.Checkbox($"停用進入公寓", ref C.AddressApartmentNoEntry);
            ImGuiEx.HelpMarker($"您將停留在進入確認視窗");
        })

        .Section("移動")
        .Checkbox("自動移動時使用坐騎", () => ref C.UseMount)
        .Widget(() =>
        {
            Dictionary<int, string> mounts = [new KeyValuePair<int, string>(0, "坐騎輪盤"), .. Svc.Data.GetExcelSheet<Mount>().Where(x => x.Singular != "").ToDictionary(x => (int)x.RowId, x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x.Singular.GetText()))];
            ImGui.SetNextItemWidth(200f);
            ImGuiEx.Combo("偏好坐騎", ref C.Mount, mounts.Keys, names: mounts);
        })
        .Checkbox("自動移動時使用衝刺", () => ref C.UseSprintPeloton)
        .Checkbox("自動移動時使用行板", () => ref C.UsePeloton)

        .Section("角色選擇選單")
        .Checkbox("從角色選擇選單啟用資料中心和伺服器訪問", () => ref C.AllowDCTravelFromCharaSelect)
        .Checkbox("在訪客資料中心使用伺服器訪問而非資料中心訪問前往同一伺服器", () => ref C.UseGuestWorldTravel)

        .Section("Wotsit 整合")
        .Widget(() =>
        {
            var anyChanged = ImGui.Checkbox("啟用 Wotsit 整合以傳送到以太網目的地", ref C.WotsitIntegrationEnabled);
            ImGuiEx.PluginAvailabilityIndicator([new("Dalamud.FindAnything", "Wotsit")]);

            if(C.WotsitIntegrationEnabled)
            {
                ImGui.Indent();
                if(ImGui.Checkbox("包含伺服器選擇視窗", ref C.WotsitIntegrationIncludes.WorldSelect))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含自動傳送到房產", ref C.WotsitIntegrationIncludes.PropertyAuto))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送到私人房屋", ref C.WotsitIntegrationIncludes.PropertyPrivate))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送到部隊房屋", ref C.WotsitIntegrationIncludes.PropertyFreeCompany))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送到公寓", ref C.WotsitIntegrationIncludes.PropertyApartment))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送到旅館房間", ref C.WotsitIntegrationIncludes.PropertyInn))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送到大國防聯軍", ref C.WotsitIntegrationIncludes.GrandCompany))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送到市場板", ref C.WotsitIntegrationIncludes.MarketBoard))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含傳送到無人島", ref C.WotsitIntegrationIncludes.IslandSanctuary))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含自動傳送到以太網目的地", ref C.WotsitIntegrationIncludes.AetheryteAethernet))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含地址簿項目", ref C.WotsitIntegrationIncludes.AddressBook))
                {
                    anyChanged = true;
                }
                if(ImGui.Checkbox("包含自訂別名", ref C.WotsitIntegrationIncludes.CustomAlias))
                {
                    anyChanged = true;
                }
                ImGui.Unindent();
            }

            if(anyChanged)
            {
                PluginLog.Debug("Wotsit integration settings changed, re-initializing immediately");
                S.Ipc.WotsitManager.TryClearWotsit();
                S.Ipc.WotsitManager.MaybeTryInit(true);
            }
        })

        .Draw();
    }

    private static void DrawOverlay()
    {
        new NuiBuilder()
        .Section("一般覆蓋層設定")
        .Widget(() =>
        {
            ImGui.Checkbox("啟用覆蓋層", ref C.Enable);
            if(C.Enable)
            {
                ImGui.Indent();
                ImGui.Checkbox($"顯示以太網選單", ref C.ShowAethernet);
                ImGui.Checkbox($"顯示跨伺服器選單", ref C.ShowWorldVisit);
                ImGui.Checkbox($"顯示住宅區小區按鈕", ref C.ShowWards);

                UtilsUI.NextSection();

                ImGui.Checkbox("固定 Lifestream 覆蓋層位置", ref C.FixedPosition);
                if(C.FixedPosition)
                {
                    ImGui.Indent();
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGuiEx.EnumCombo("水平基準位置", ref C.PosHorizontal);
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGuiEx.EnumCombo("垂直基準位置", ref C.PosVertical);
                    ImGui.SetNextItemWidth(200f.Scale());
                    ImGui.DragFloat2("偏移", ref C.Offset);

                    ImGui.Unindent();
                }

                UtilsUI.NextSection();

                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.InputInt3("按鈕左右內距", ref C.ButtonWidthArray[0]);
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.InputInt("以太之光按鈕上下內距", ref C.ButtonHeightAetheryte);
                ImGui.SetNextItemWidth(100f.Scale());
                ImGui.InputInt("伺服器按鈕上下內距", ref C.ButtonHeightWorld);
                ImGui.Unindent();

                ImGui.Checkbox("按鈕文字靠左對齊", ref C.LeftAlignButtons);
                if(C.LeftAlignButtons)
                {
                    ImGui.SetNextItemWidth(100f);
                    ImGui.DragInt("左側內距（空格）", ref C.LeftAlignPadding, 0.1f, 0, 20);
                }
            }
        })

        .Section("副本切換器")
        .Checkbox("啟用", () => ref C.ShowInstanceSwitcher)
        .Checkbox("失敗時重試", () => ref C.InstanceSwitcherRepeat)
        .Checkbox("切換副本前飛行時返回地面", () => ref C.EnableFlydownInstance)
        .Widget("在伺服器資訊列顯示副本編號", (x) =>
        {
            if(ImGui.Checkbox(x, ref C.EnableDtrBar))
            {
                S.DtrManager.Refresh();
            }
        })
        .SliderInt(150f, "額外按鈕高度", () => ref C.InstanceButtonHeight, 0, 50)
        .Widget("重設副本資料", (x) =>
        {
            if(ImGuiEx.Button(x, C.PublicInstances.Count > 0))
            {
                C.PublicInstances.Clear();
                EzConfig.Save();
            }
        })

        .Section("遊戲視窗整合")
        .Checkbox($"當以下遊戲視窗開啟時隱藏 Lifestream", () => ref C.HideAddon)
        .If(() => C.HideAddon)
        .Widget(() =>
        {
            if(ImGui.BeginTable("HideAddonTable", 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("col1", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("col2");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGuiEx.SetNextItemFullWidth();
                ImGui.InputTextWithHint("##addnew", "視窗名稱... 可用 /xldata ai 查詢", ref AddNew, 100);
                ImGui.TableNextColumn();
                if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                {
                    C.HideAddonList.Add(AddNew);
                    AddNew = "";
                }

                List<string> focused = [];
                try
                {
                    foreach(var x in RaptureAtkUnitManager.Instance()->FocusedUnitsList.Entries)
                    {
                        if(x.Value == null) continue;
                        focused.Add(x.Value->NameString);
                    }
                }
                catch(Exception e) { e.Log(); }

                if(focused != null)
                {
                    foreach(var name in focused)
                    {
                        if(name == null) continue;
                        if(C.HideAddonList.Contains(name)) continue;
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGuiEx.TextV(EColor.Green, $"Focused: {name}");
                        ImGui.TableNextColumn();
                        ImGui.PushID(name);
                        if(ImGuiEx.IconButton(FontAwesomeIcon.Plus))
                        {
                            C.HideAddonList.Add(name);
                        }
                        ImGui.PopID();
                    }
                }

                ImGui.TableNextRow();
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, 0x88888888);
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, 0x88888888);
                ImGui.TableNextColumn();
                ImGui.Dummy(new Vector2(5f));

                foreach(var s in C.HideAddonList)
                {
                    ImGui.PushID(s);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiEx.TextV(focused.Contains(s) ? EColor.Green : null, s);
                    ImGui.TableNextColumn();
                    if(ImGuiEx.IconButton(FontAwesomeIcon.Trash))
                    {
                        new TickScheduler(() => C.HideAddonList.Remove(s));
                    }
                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        })
        .EndIf()
        .Draw();

        if(C.Hidden.Count > 0)
        {
            new NuiBuilder()
            .Section("隱藏的以太之光")
            .Widget(() =>
            {
                uint toRem = 0;
                foreach(var x in C.Hidden)
                {
                    ImGuiEx.Text($"{Svc.Data.GetExcelSheet<Aetheryte>().GetRowOrDefault(x)?.AethernetName.ValueNullable?.Name.ToString() ?? x.ToString()}");
                    ImGui.SameLine();
                    if(ImGui.SmallButton($"Delete##{x}"))
                    {
                        toRem = x;
                    }
                }
                if(toRem > 0)
                {
                    C.Hidden.Remove(toRem);
                }
            })
            .Draw();
        }
    }

    private static void DrawExpert()
    {
        new NuiBuilder()
        .Section("進階設定")
        .Widget(() =>
        {
            ImGui.Checkbox($"減慢以太之光傳送速度", ref C.SlowTeleport);
            ImGuiEx.HelpMarker($"將以太網傳送減慢指定的時間。");
            if(C.SlowTeleport)
            {
                ImGui.Indent();
                ImGui.SetNextItemWidth(200f.Scale());
                ImGui.DragInt("傳送延遲（毫秒）", ref C.SlowTeleportThrottle);
                ImGui.Unindent();
            }
            ImGuiEx.CheckboxInverted($"略過等待遊戲畫面就緒", ref C.WaitForScreenReady);
            ImGuiEx.HelpMarker($"啟用此選項可加快傳送速度，但請小心可能會卡住。");
            ImGui.Checkbox($"隱藏進度條", ref C.NoProgressBar);
            ImGuiEx.HelpMarker($"隱藏進度條後，將沒有任何方式停止 Lifestream 執行任務。");
            ImGuiEx.CheckboxInverted($"從較遠距離執行跨伺服器指令時不走到附近的以太之光", ref C.WalkToAetheryte);
            ImGui.Checkbox($"進度覆蓋層顯示在螢幕頂部", ref C.ProgressOverlayToTop);
            ImGui.Checkbox("允許自訂別名和房屋別名覆蓋內建指令", ref C.AllowCustomOverrides);
            ImGui.Indent();
            ImGuiEx.TextWrapped(EColor.RedBright, "警告！其他插件可能依賴內建指令。若你決定啟用此選項並覆蓋指令，請先確認不是這種情況。");
            ImGui.Unindent();
        })
        .Draw();
    }
}
