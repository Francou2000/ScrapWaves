using TMPro;
using UnityEngine;

/// <summary>
/// Asigna la fuente SDF por defecto de TextMeshPro a textos creados en runtime.
/// Requiere importar recursos TMP (menú Window → TextMeshPro → Import TMP Essential Resources) al menos una vez en el proyecto.
/// </summary>
public static class TmpUiHelper
{
    public static void ApplyDefaultFont(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return;

        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
    }

    public static TextMeshProUGUI CreateWorldSpaceTmp(GameObject go)
    {
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyDefaultFont(tmp);
        return tmp;
    }
}
