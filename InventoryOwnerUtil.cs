using System;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;

namespace AutoMedHotkeys;

internal static class InventoryOwnerUtil
{
    private static readonly FieldInfo? PlayerInventoryPlayerField =
        AccessTools.Field(typeof(Player.PlayerInventoryController), "Player_0");

    private static readonly Type? MenuInventoryControllerBase =
        AccessTools.TypeByName("GClass3387");

    /// <summary>
    /// True for the local PMC inventory in raid, or the main-menu/stash inventory controller.
    /// </summary>
    public static bool IsLocalPlayerInventory(TraderControllerClass? controller)
    {
        if (controller is not InventoryController inventoryController)
        {
            return false;
        }

        if (IsObserved(inventoryController))
        {
            return false;
        }

        // In-raid local player inventory.
        if (controller is Player.PlayerInventoryController playerInventory)
        {
            var player = PlayerInventoryPlayerField?.GetValue(playerInventory) as Player;
            return player != null && player.IsYourPlayer;
        }

        // Stash / character screen: GClass3388 : GClass3387 : InventoryController
        if (MenuInventoryControllerBase != null
            && MenuInventoryControllerBase.IsInstanceOfType(controller))
        {
            return true;
        }

        return inventoryController.OwnerType == EOwnerType.Profile;
    }

    public static bool IsObserved(InventoryController controller)
    {
        return controller.GetType().FullName
            == "Fika.Core.Main.ObservedClasses.ObservedInventoryController";
    }
}
