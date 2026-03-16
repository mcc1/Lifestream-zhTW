
using ECommons.Automation;
using ECommons.Automation.UIInput;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lifestream.Schedulers;

namespace Lifestream.Tasks.SameWorld;
public static unsafe class TaskGoToResidentialDistrict
{
    public static void Enqueue(int ward)
    {
        if(ward < 1 || ward > 30) throw new ArgumentOutOfRangeException(nameof(ward));
        PluginLog.Information($"[Debug] Residential overlay enqueue: ward={ward}, territory={P.Territory}, activeAetheryte='{S.Data.ResidentialAethernet.ActiveAetheryte?.Name ?? "<null>"}'");
        if(C.WaitForScreenReady) P.TaskManager.Enqueue(Utils.WaitForScreen);
        P.TaskManager.Enqueue(WorldChange.TargetValidAetheryte);
        P.TaskManager.Enqueue(WorldChange.InteractWithTargetedAetheryte);
        P.TaskManager.Enqueue(SelectResidentialDistrict, $"TaskGoToResidentialDistrictSelect {Lang.ResidentialDistrict}");
        P.TaskManager.Enqueue(SelectGoToWard, $"TaskGoToResidentialDistrictSelect {Lang.GoToWard}");
        if(ward > 1) P.TaskManager.Enqueue(() => SelectWard(ward));
        P.TaskManager.Enqueue(GoToWard);
        P.TaskManager.Enqueue(ConfirmYesNoGoToWard);
        P.TaskManager.EnqueueTask(new(() => Player.Interactable && S.Data.ResidentialAethernet.IsInResidentialZone(), "Wait until player arrives"));
    }

    public static bool ConfirmYesNoGoToWard()
    {
        if(Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51]) return true;
        var x = (AddonSelectYesno*)Utils.GetSpecificYesno(true, Lang.TravelTo);
        if(x != null)
        {
            if(x->YesButton->IsEnabled && EzThrottler.Throttle("ConfirmTravelTo"))
            {
                PluginLog.Information("[Debug] Residential overlay confirm: SelectYesno matched and Yes enabled");
                new AddonMaster.SelectYesno(x).Yes();
                return true;
            }
            PluginLog.Warning("[Debug] Residential overlay confirm: SelectYesno matched but Yes is disabled");
        }
        else if(EzThrottler.Throttle("Lifestream.Debug.ResidentialOverlay.ConfirmMissing", 3000))
        {
            PluginLog.Warning($"[Debug] Residential overlay confirm: TravelTo dialog missing. Expected=[{string.Join(" | ", Lang.TravelTo)}]");
        }
        return false;
    }

    public static bool? SelectWard(int ward)
    {
        if(TryGetAddonByName<AtkUnitBase>("HousingSelectBlock", out var addon) && IsAddonReady(addon))
        {
            if(ward == 1)
            {
                PluginLog.Information("[Debug] Residential overlay ward select: ward=1, skipping explicit selection");
                return true;
            }
            else
            {
                if(EzThrottler.Throttle("HousingSelectBlockSelectWard"))
                {
                    PluginLog.Information($"[Debug] Residential overlay ward select: firing callback for ward={ward}, callbackIndex={ward - 1}");
                    Callback.Fire(addon, true, 1, ward - 1);
                    return true;
                }
            }
        }
        else if(EzThrottler.Throttle("Lifestream.Debug.ResidentialOverlay.HousingSelectBlockMissing", 3000))
        {
            PluginLog.Warning("[Debug] Residential overlay ward select: HousingSelectBlock missing or not ready");
        }
        return false;
    }

    public static bool? GoToWard()
    {
        if(TryGetAddonByName<AtkUnitBase>("HousingSelectBlock", out var addon) && IsAddonReady(addon))
        {
            var button = addon->GetButtonNodeById(34);
            if(button != null && button->IsEnabled)
            {
                if(EzThrottler.Throttle("HousingSelectBlockConfirm"))
                {
                    PluginLog.Information("[Debug] Residential overlay ward confirm: clicking HousingSelectBlock button id=34");
                    button->ClickAddonButton(addon);
                    return true;
                }
            }
            else if(EzThrottler.Throttle("Lifestream.Debug.ResidentialOverlay.ButtonState", 3000))
            {
                PluginLog.Warning($"[Debug] Residential overlay ward confirm: button id=34 missing or disabled (buttonNull={button == null})");
            }
        }
        else if(EzThrottler.Throttle("Lifestream.Debug.ResidentialOverlay.HousingSelectBlockConfirmMissing", 3000))
        {
            PluginLog.Warning("[Debug] Residential overlay ward confirm: HousingSelectBlock missing or not ready");
        }
        return false;
    }

    private static bool SelectResidentialDistrict()
    {
        return SelectOverlayEntry(Lang.ResidentialDistrict, "SelectResidentialDistrict", "residential-district");
    }

    private static bool SelectGoToWard()
    {
        return SelectOverlayEntry(Lang.GoToWard, "SelectGoToWard", "go-to-ward");
    }

    private static bool SelectOverlayEntry(IEnumerable<string> expectedEntries, string throttleKey, string stepName)
    {
        if(TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && IsAddonReady(&addon->AtkUnitBase))
        {
            var entries = Utils.GetEntries(addon);
            var entry = entries.FirstOrDefault(x => x.EqualsAny(expectedEntries));
            if(entry != null)
            {
                var index = entries.IndexOf(entry);
                if(index >= 0 && EzThrottler.Throttle(throttleKey))
                {
                    PluginLog.Information($"[Debug] Residential overlay {stepName}: selecting entry='{entry}', index={index}, entries=[{string.Join(" | ", entries)}]");
                    new AddonMaster.SelectString(addon).Entries[index].Select();
                    return true;
                }

                return false;
            }

            if(EzThrottler.Throttle($"Lifestream.Debug.ResidentialOverlay.{stepName}.NoMatch", 3000))
            {
                PluginLog.Warning($"[Debug] Residential overlay {stepName}: expected=[{string.Join(" | ", expectedEntries)}], entries=[{string.Join(" | ", entries)}]");
            }
        }
        else if(EzThrottler.Throttle($"Lifestream.Debug.ResidentialOverlay.{stepName}.MissingSelectString", 3000))
        {
            PluginLog.Warning($"[Debug] Residential overlay {stepName}: SelectString missing or not ready");
        }

        return false;
    }
}
