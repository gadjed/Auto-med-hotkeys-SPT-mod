using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using EFT.InventoryLogic;
using UnityEngine;

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

    public static void RequestRefresh(InventoryController controller)
    {
        if (!AutoMedHotkeysPlugin.Enabled.Value || controller == null || IsObserved(controller))
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
            AutoMedHotkeysPlugin.Instance.StartCoroutine(RefreshNextFrame());
            return;
        }

        Interlocked.Exchange(ref _refreshScheduled, 0);
        Refresh(controller);
    }

    private static IEnumerator RefreshNextFrame()
    {
        yield return null;
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
        if (!AutoMedHotkeysPlugin.Enabled.Value || controller == null || IsObserved(controller))
        {
            return;
        }

        try
        {
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
            DebugLog($"Slot {slot}: keep {bound.ShortName} ({label}).");
            return;
        }

        if (bound != null && !AutoMedHotkeysPlugin.OverwriteExisting.Value)
        {
            DebugLog($"Slot {slot}: occupied by {bound.ShortName}, overwrite disabled.");
            return;
        }

        var candidate = FindBestCandidate(controller, match);
        if (candidate == null)
        {
            DebugLog($"Slot {slot}: no bindable {label} in pockets/rig.");
            return;
        }

        if (bound == candidate)
        {
            return;
        }

        var result = BindOperation.Run(controller, candidate, slot, true);
        if (!result.Succeeded)
        {
            AutoMedHotkeysPlugin.Log.LogWarning(
                $"[AutoMedHotkeys] Failed to bind {candidate.ShortName} to {slot}: {result.Error}"
            );
            return;
        }

        controller.TryRunNetworkTransaction(result, null);
        DebugLog($"Slot {slot}: bound {candidate.ShortName} ({label}).");
    }

    private static Item? FindBestCandidate(InventoryController controller, Predicate<Item> match)
    {
        var matches = new List<Item>();
        controller.GetAcceptableItemsNonAlloc(BindableEquipmentSlots, matches, match, null);

        Item? best = null;
        var bestScore = float.MinValue;
        foreach (var item in matches)
        {
            if (!controller.IsAtBindablePlace(item))
            {
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

    private static bool IsObserved(InventoryController controller)
    {
        return controller.GetType().FullName == "Fika.Core.Main.ObservedClasses.ObservedInventoryController";
    }

    private static void DebugLog(string message)
    {
        if (AutoMedHotkeysPlugin.Debug.Value)
        {
            AutoMedHotkeysPlugin.Log.LogInfo($"[AutoMedHotkeys] {message}");
        }
    }
}
