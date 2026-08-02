using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace AutoMedHotkeys.Patches;

/// <summary>
/// Refresh when the stash / character inventory screen opens.
/// </summary>
internal class InventoryScreenShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Show(IHealthController, InventoryController, AbstractQuestControllerClass, ...)
        foreach (var method in AccessTools.GetDeclaredMethods(typeof(InventoryScreen)))
        {
            if (method.Name != "Show")
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length >= 2 && typeof(InventoryController).IsAssignableFrom(parameters[1].ParameterType))
            {
                return method;
            }
        }

        AutoMedHotkeysPlugin.Log.LogError("[AutoMedHotkeys] InventoryScreen.Show(InventoryController) not found.");
        return typeof(object).GetMethod(nameof(ToString))!;
    }

    [PatchPostfix]
    public static void Postfix(InventoryController inventoryController)
    {
        if (inventoryController == null)
        {
            return;
        }

        MedHotkeyBinder.RequestRefresh(inventoryController);
    }
}

/// <summary>
/// Refresh after any successful move involving the local inventory (stash ↔ pockets/rig).
/// </summary>
internal class MoveOperationRaiseEventsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass3411), nameof(GClass3411.RaiseEvents))!;
    }

    [PatchPostfix]
    public static void Postfix(GClass3411 __instance, IItemOwner owner, CommandStatus status)
    {
        if (status != CommandStatus.Succeed || owner is not InventoryController controller)
        {
            return;
        }

        if (!InventoryOwnerUtil.IsLocalPlayerInventory(controller))
        {
            return;
        }

        var item = __instance.Item;
        if (item == null)
        {
            return;
        }

        if (!MedItemClassifier.IsMedkit(item)
            && !MedItemClassifier.IsBleedStopper(item)
            && !MedItemClassifier.IsBandage(item))
        {
            return;
        }

        MedHotkeyBinder.RequestRefresh(controller);
    }
}
