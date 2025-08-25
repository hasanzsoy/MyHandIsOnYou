using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GroupCamera : MonoBehaviour
{
    public float backOffset = 12f;
    public float minHeight = 20f;
    public float maxHeight = 38f;
    public float padding = 8f;
    public float lerpSpeed = 5f;

    Camera cam;
    readonly List<Transform> targets = new();

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        // Sahnedeki Player'ları topla (dinamik katılım varsa
        // Start sonrasında da toplanabilir; basitlik için periyodik tarayalım)
        InvokeRepeating(nameof(RefreshTargets), 0f, 1f);
    }

    void RefreshTargets()
    {
        targets.Clear();
        var pcs = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var pc in pcs)
            if (pc.gameObject.activeInHierarchy)
                targets.Add(pc.transform);
    }

    void LateUpdate()
    {
        if (targets.Count == 0) return;

        // ortalama ve yayılım
        Bounds b = new Bounds(targets[0].position, Vector3.zero);
        for (int i = 1; i < targets.Count; i++) b.Encapsulate(targets[i].position);

        Vector3 center = b.center;
        float size = Mathf.Max(b.size.x, b.size.z) + padding;

        // Yüksekliği size'a göre ayarla
        float targetHeight = Mathf.Lerp(minHeight, maxHeight, Mathf.InverseLerp(5f, 25f, size));
        Vector3 desiredPos = new Vector3(center.x, targetHeight, center.z - backOffset);

        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * lerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(50f, 0f, 0f), Time.deltaTime * lerpSpeed);
    }
}
