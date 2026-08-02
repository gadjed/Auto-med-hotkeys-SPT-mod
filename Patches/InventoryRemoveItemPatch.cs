using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace AutoMedHotkeys.Patches;

/// <summary>
/// Re-evaluate hotkeys after an item leaves pockets/rig (e.g. moved into backpack).
/// Vanilla clears the bind; when another matching item remains we rebind it.
/// </summary>
internal class InventoryRemoveItemPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(TraderControllerClass).GetMethod(
            nameof(TraderControllerClass.RaiseRemoveEvent),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        )!;
    }

    [PatchPostfix]
    public static void Postfix(TraderControllerClass __instance, GEventArgs3 args)
    {
        if (args == null || args.Status != CommandStatus.Succeed || args.Item == null)
        {
            return;
        }

        if (__instance is not InventoryController controller)
        {
            return;
        }

        if (__instance is not Player.PlayerInventoryController)
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
