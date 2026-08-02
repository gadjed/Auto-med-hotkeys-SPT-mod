using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using EFT.InventoryLogic;

namespace AutoMedHotkeys;

using BindOperation = GClass3431;

/// <summary>
/// Keeps quick slots 4/5/6 bound to medkits / bleed-stoppers / bandages in pockets or rig.
/// </summary>
internal static class MedHotkeyBinder
{
    private static readonly EquipmentSlot[] BindableEquipmentSlots =
    [
        EquipmentSlot.Pockets,
        EquipmentSlot.TacticalVest,
    ];

    private static int _refreshScheduled;
    private static InventoryController? _pendingController;

    public static void RequestRefresh(InventoryController? controller)
    {
        if (!AutoMedHotkeysPlugin.Enabled.Value
            || controller == null
            || !InventoryOwnerUtil.IsLocalPlayerInventory(controller))
        {
            return;
        }

        _pendingController = controller;
        if (Interlocked.Exchange(ref _refreshScheduled, 1) == 1)
        {
            return;
        }

        if (AutoMedHotkeysPlugin.Instance != null)
        {
            // Two frames: move address / UI can still be mid-update on the first frame in stash.
            AutoMedHotkeysPlugin.Instance.StartCoroutine(RefreshAfterFrames(2));
            return;
        }

        Interlocked.Exchange(ref _refreshScheduled, 0);
        Refresh(controller);
    }

    private static IEnumerator RefreshAfterFrames(int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            yield return null;
        }

        RunScheduledRefresh();
    }

    private static void RunScheduledRefresh()
    {
        Interlocked.Exchange(ref _refreshScheduled, 0);
        var controller = _pendingController;
        _pendingController = null;
        if (controller != null)
        {
            Refresh(controller);
        }
    }

    public static void Refresh(InventoryController controller)
    {
        if (!AutoMedHotkeysPlugin.Enabled.Value
            || controller == null
            || !InventoryOwnerUtil.IsLocalPlayerInventory(controller))
        {
            return;
        }

        try
        {
            Log($"Refresh on {controller.GetType().Name} (OwnerType={controller.OwnerType}).");
            EnsureSlot(controller, EBoundItem.Item4, MedItemClassifier.IsMedkit, "medkit");
            EnsureSlot(controller, EBoundItem.Item5, MedItemClassifier.IsBleedStopper, "bleed-stopper");
            EnsureSlot(controller, EBoundItem.Item6, MedItemClassifier.IsBandage, "bandage");
        }
        catch (Exception ex)
        {
            AutoMedHotkeysPlugin.Log.LogError($"[AutoMedHotkeys] Refresh failed: {ex}");
        }
    }

    private static void EnsureSlot(
        InventoryController controller,
        EBoundItem slot,
        Predicate<Item> match,
        string label
    )
    {
        var bound = controller.Inventory.FastAccess.GetBoundItem(slot);
        if (bound != null && match(bound) && controller.IsAtBindablePlace(bound))
        {
            // Data already bound — still poke the UI so the badge appears in stash.
            NotifyBindUi(controller, bound, slot);
            Log($"Slot {slot}: keep {Describe(bound)} ({label}).");
            return;
        }

        if (bound != null && !AutoMedHotkeysPlugin.OverwriteExisting.Value)
        {
            Log($"Slot {slot}: occupied by {bound.ShortName}, overwrite disabled.");
            return;
        }

        var candidate = FindBestCandidate(controller, match);
        if (candidate == null)
        {
            Log($"Slot {slot}: no bindable {label} in pockets/rig.");
            return;
        }

        if (ReferenceEquals(bound, candidate))
        {
            return;
        }

        // simulate=false applies BoundItems immediately; RaiseEvents refreshes the quickbar UI;
        // TryRunNetworkTransaction persists FastPanel to the profile.
        var result = BindOperation.Run(controller, candidate, slot, false);
        if (!result.Succeeded)
        {
            AutoMedHotkeysPlugin.Log.LogWarning(
                $"[AutoMedHotkeys] Failed to bind {candidate.ShortName} to {slot}: {result.Error}"
            );
            return;
        }

        try
        {
            result.Value.RaiseEvents(controller, CommandStatus.Begin);
            result.Value.RaiseEvents(controller, CommandStatus.Succeed);
        }
        catch (Exception ex)
        {
            AutoMedHotkeysPlugin.Log.LogWarning(
                $"[AutoMedHotkeys] RaiseEvents after bind failed: {ex.Message}"
            );
        }

        // Extra UI poke — RaiseEvents already does this, but stash views can miss it.
        NotifyBindUi(controller, candidate, slot);

        try
        {
            controller.TryRunNetworkTransaction(result, null);
        }
        catch (Exception ex)
        {
            AutoMedHotkeysPlugin.Log.LogWarning(
                $"[AutoMedHotkeys] TryRunNetworkTransaction after bind failed: {ex.Message}"
            );
        }

        AutoMedHotkeysPlugin.Log.LogInfo(
            $"[AutoMedHotkeys] Bound {Describe(candidate)} -> {slot} ({label})."
        );
    }

    private static void NotifyBindUi(InventoryController controller, Item item, EBoundItem slot)
    {
        try
        {
            controller.RaiseBindItemEvent(new GEventArgs11(item, slot, CommandStatus.Begin, controller));
            controller.RaiseBindItemEvent(new GEventArgs11(item, slot, CommandStatus.Succeed, controller));
        }
        catch (Exception ex)
        {
            Log($"NotifyBindUi failed: {ex.Message}");
        }
    }

    private static string Describe(Item item)
    {
        return $"{item.TemplateId}#{item.Id}";
    }

    private static Item? FindBestCandidate(InventoryController controller, Predicate<Item> match)
    {
        var matches = new List<Item>();
        controller.GetAcceptableItemsNonAlloc(BindableEquipmentSlots, matches, match, null);

        // Fallback: walk pocket/rig grids directly if the helper returned nothing.
        if (matches.Count == 0)
        {
            CollectFromEquipment(controller, match, matches);
        }

        Item? best = null;
        var bestScore = float.MinValue;
        foreach (var item in matches)
        {
            if (!controller.IsAtBindablePlace(item))
            {
                Log($"Skip {item.ShortName}: not at bindable place.");
                continue;
            }

            var score = MedItemClassifier.ResourceScore(item);
            if (best == null || score > bestScore)
            {
                best = item;
                bestScore = score;
            }
        }

        return best;
    }

    private static void CollectFromEquipment(
        InventoryController controller,
        Predicate<Item> match,
        List<Item> output
    )
    {
        var equipment = controller.Inventory?.Equipment;
        if (equipment == null)
        {
            return;
        }

        foreach (var slotType in BindableEquipmentSlots)
        {
            var slot = equipment.GetSlot(slotType);
            var container = slot?.ContainedItem;
            if (container == null)
            {
                continue;
            }

            foreach (var item in GClass3380.GetAllItems(container))
            {
                if (ReferenceEquals(item, container))
                {
                    continue;
                }

                if (match(item) && !output.Contains(item))
                {
                    output.Add(item);
                }
            }
        }
    }

    private static void Log(string message)
    {
        if (AutoMedHotkeysPlugin.Debug.Value)
        {
            AutoMedHotkeysPlugin.Log.LogInfo($"[AutoMedHotkeys] {message}");
        }
    }
}
