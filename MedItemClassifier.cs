using System.Collections.Generic;
using EFT.InventoryLogic;

namespace AutoMedHotkeys;

/// <summary>
/// Classifies medical items for quick-slot auto-bind rules.
/// </summary>
internal static class MedItemClassifier
{
    /// <summary>Esmarch, CAT, CALOK-B, Zagustin.</summary>
    private static readonly HashSet<string> BleedStopperTemplateIds = new()
    {
        "5e831507ea0a7c419c2f9bd9", // Esmarch tourniquet
        "60098af40accd37ef2175f27", // CAT hemostatic tourniquet
        "5e8488fa988a8701445df1e4", // CALOK-B hemostatic applicator
        "5c0e533786f7747fa23f4d47", // Zagustin hemostatic drug injector
    };

    public static bool IsMedkit(Item item) => item is MedKitItemClass;

    public static bool IsBleedStopper(Item item)
    {
        if (item == null)
        {
            return false;
        }

        return BleedStopperTemplateIds.Contains(item.TemplateId.ToString());
    }

    public static bool IsBandage(Item item)
    {
        if (item is not MedicalItemClass medical)
        {
            return false;
        }

        // Exclude tourniquets / CALOK / surgical kits that share the Medical node.
        if (IsBleedStopper(medical))
        {
            return false;
        }

        var effects = medical.HealthEffectsComponent;
        if (effects == null)
        {
            return false;
        }

        // Bandages remove light bleeds; they do not treat heavy bleeds / fractures / lost limbs.
        return effects.AffectsAny(EDamageEffectType.LightBleeding)
            && !effects.AffectsAny(EDamageEffectType.HeavyBleeding)
            && !effects.AffectsAny(EDamageEffectType.Fracture)
            && !effects.AffectsAny(EDamageEffectType.DestroyedPart);
    }

    public static float ResourceScore(Item item)
    {
        if (item is MedsItemClass meds && meds.MedKitComponent != null)
        {
            return meds.MedKitComponent.HpResource;
        }

        return item.StackObjectsCount;
    }
}
