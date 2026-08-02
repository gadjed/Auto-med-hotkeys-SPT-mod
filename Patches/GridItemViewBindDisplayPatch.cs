using System;
using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace AutoMedHotkeys.Patches;

/// <summary>
/// Ensure the hotkey badge is drawn when a grid item view is created (stash/equipment).
/// </summary>
internal class GridItemViewBindDisplayPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var method = AccessTools.Method(typeof(GridItemView), nameof(GridItemView.NewGridItemView));
        if (method != null)
        {
            return method;
        }

        throw new InvalidOperationException("[AutoMedHotkeys] GridItemView.NewGridItemView not found.");
    }

    [PatchPostfix]
    public static void Postfix(GridItemView __instance, TraderControllerClass itemController)
    {
        try
        {
            if (__instance?.Item == null || itemController is not InventoryController controller)
            {
                return;
            }

            if (!InventoryOwnerUtil.IsLocalPlayerInventory(controller))
            {
                return;
            }

            var binding = ItemView.GetBindingForItem(controller, __instance.Item);
            __instance.SetItemBinding(binding);
        }
        catch (Exception ex)
        {
            AutoMedHotkeysPlugin.Log.LogDebug($"[AutoMedHotkeys] GridItemView bind display skipped: {ex.Message}");
        }
    }
}
