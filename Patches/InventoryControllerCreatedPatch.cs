using System.Reflection;
using System.Threading.Tasks;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace AutoMedHotkeys.Patches;

/// <summary>
/// Bind once when the in-raid player inventory controller is created.
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
        if (!InventoryOwnerUtil.IsLocalPlayerInventory(__instance))
        {
            return;
        }

        MedHotkeyBinder.RequestRefresh(__instance);
    }
}

/// <summary>
/// Bind once when the main-menu / stash inventory controller is created (GClass3388).
/// </summary>
internal class StashInventoryControllerCreatedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var type = AccessTools.TypeByName("GClass3388")
            ?? AccessTools.TypeByName("GClass3387");
        if (type == null)
        {
            AutoMedHotkeysPlugin.Log.LogError(
                "[AutoMedHotkeys] Could not find stash InventoryController type (GClass3388)."
            );
            // Return a harmless method so Enable() does not throw; patch will no-op.
            return typeof(object).GetMethod(nameof(ToString))!;
        }

        var ctor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(IBackEndSession), typeof(Profile), typeof(string)],
            null
        );

        if (ctor == null)
        {
            AutoMedHotkeysPlugin.Log.LogError(
                $"[AutoMedHotkeys] Could not find ctor on {type.FullName}."
            );
            return typeof(object).GetMethod(nameof(ToString))!;
        }

        return ctor;
    }

    [PatchPostfix]
    public static void Postfix(InventoryController __instance)
    {
        if (!InventoryOwnerUtil.IsLocalPlayerInventory(__instance))
        {
            return;
        }

        MedHotkeyBinder.RequestRefresh(__instance);
    }
}

/// <summary>
/// Refresh after the main menu inventory finishes initializing (covers already-equipped meds).
/// </summary>
internal class MainMenuInventoryReadyPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(MainMenuControllerClass), "method_5")!;
    }

    [PatchPostfix]
    public static async void Postfix(MainMenuControllerClass __instance, Task __result)
    {
        if (__result != null)
        {
            await __result;
        }

        var controller = __instance?.InventoryController;
        if (controller == null)
        {
            return;
        }

        MedHotkeyBinder.RequestRefresh(controller);
    }
}
