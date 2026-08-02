using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using AutoMedHotkeys.Patches;
using SPT.Reflection.Patching;
using System;

namespace AutoMedHotkeys;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class AutoMedHotkeysPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "gadjed.automedhotkeys";
    public const string PluginName = "Auto Med Hotkeys";
    public const string PluginVersion = "1.0.3";

    public static AutoMedHotkeysPlugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    public static ConfigEntry<bool> Enabled { get; private set; } = null!;
    public static ConfigEntry<bool> OverwriteExisting { get; private set; } = null!;
    public static ConfigEntry<bool> Debug { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        Enabled = Config.Bind(
            "1. General",
            "Enabled",
            true,
            "Automatically bind medical items to quick slots 4 / 5 / 6."
        );
        OverwriteExisting = Config.Bind(
            "1. General",
            "OverwriteExisting",
            true,
            "If a different item is already on slot 4/5/6, replace it with the matching med."
        );
        Debug = Config.Bind(
            "1. General",
            "Debug",
            true,
            "Verbose logging of bind decisions."
        );

        EnablePatch(new InventoryControllerCreatedPatch());
        EnablePatch(new StashInventoryControllerCreatedPatch());
        EnablePatch(new MainMenuInventoryReadyPatch());
        EnablePatch(new InventoryScreenShowPatch());
        EnablePatch(new MoveOperationRaiseEventsPatch());
        EnablePatch(new InventoryAddItemPatch());
        EnablePatch(new InventoryRemoveItemPatch());
        EnablePatch(new GridItemViewBindDisplayPatch());

        Log.LogInfo($"{PluginName} v{PluginVersion} loaded (SPT 4.0.13).");
    }

    private static void EnablePatch(ModulePatch patch)
    {
        try
        {
            patch.Enable();
        }
        catch (Exception ex)
        {
            Log.LogError($"[AutoMedHotkeys] Failed to enable {patch.GetType().Name}: {ex}");
        }
    }
}
