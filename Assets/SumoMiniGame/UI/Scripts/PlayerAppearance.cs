using UnityEngine;

/// <summary>
/// Oyuncu modelinin Renderer'larına instance materyal oluşturmadan renk basar.
/// (MaterialPropertyBlock kullanır; paylaşılan materyali bozmaz.)
/// </summary>
public class PlayerAppearance : MonoBehaviour
{
    [Tooltip("Renk verilecek Renderer'lar (MeshRenderer / SkinnedMeshRenderer). Boşsa runtime'da otomatik doldurulur.")]
    public Renderer[] renderersToTint;

    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor"); // URP/HDRP
    static readonly int ColorID     = Shader.PropertyToID("_Color");     // Built-in/Standard

    MaterialPropertyBlock _mpb;

    void Awake()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // Inspector'da atanmadıysa, child'lardan otomatik topla
        if (renderersToTint == null || renderersToTint.Length == 0)
            renderersToTint = GetComponentsInChildren<Renderer>(true);
    }

    /// <summary>Gövde/mesh üzerine rengi uygular.</summary>
    public void ApplyColor(Color c)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        foreach (var r in renderersToTint)
        {
            if (r == null) continue;

            var mat = r.sharedMaterial;
            if (mat == null) continue;

            r.GetPropertyBlock(_mpb);

            bool wrote = false;
            if (mat.HasProperty(BaseColorID)) { _mpb.SetColor(BaseColorID, c); wrote = true; }
            if (mat.HasProperty(ColorID))     { _mpb.SetColor(ColorID,     c); wrote = true; }

            if (wrote) r.SetPropertyBlock(_mpb);
        }
    }
}
