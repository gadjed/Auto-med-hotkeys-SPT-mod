using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace AutoMedHotkeys.Patches;

/// <summary>
/// Re-evaluate hotkeys after an item is added (stash → pockets/rig, or in-raid moves).
/// </summary>
internal class InventoryAddItemPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TraderControllerClass).GetMethod(
            nameof(TraderControllerClass.RaiseAddEvent),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        )!;
    }

    [PatchPostfix]
    public static void Postfix(TraderControllerClass __instance, GEventArgs2 args)
    {
        if (args == null || args.Status != CommandStatus.Succeed || args.Item == null)
        {
            return;
        }

        if (!InventoryOwnerUtil.IsLocalPlayerInventory(__instance))
        {
            return;
        }

        if (__instance is not InventoryController controller)
        {
            return;
        }

        if (!IsRelevant(args.Item))
        {
            return;
        }

        MedHotkeyBinder.RequestRefresh(controller);
    }

    private static bool IsRelevant(Item item)
    {
        return MedItemClassifier.IsMedkit(item)
            || MedItemClassifier.IsBleedStopper(item)
            || MedItemClassifier.IsBandage(item);
    }
}
