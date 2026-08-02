using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;

namespace AutoMedHotkeys.Patches;

/// <summary>
/// When a player inventory controller is created, bind meds once.
/// </summary>
internal class InventoryControllerCreatedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.PlayerInventoryController).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(Player), typeof(Profile), typeof(bool)],
            null
        )!;
    }

    [PatchPostfix]
    public static void Postfix(Player.PlayerInventoryController __instance)
    {
        MedHotkeyBinder.RequestRefresh(__instance);
    }
}
