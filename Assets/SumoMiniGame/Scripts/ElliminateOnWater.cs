using UnityEngine;

public class EliminateOnWater : MonoBehaviour
{
    public RingRules rules; // Inspector'dan atayacaksın

    void OnTriggerEnter(Collider other)
    {
        // Oyuncuyu bul (root'ta PlayerController varsa direkt onu yakalar)
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;

        var go = pc.gameObject;

        // Oyuncuyu devre dışı bırak
        go.SetActive(false);

        // RingRules'a bildir (alive listesinden düşsün, kazananı kontrol etsin)
        if (rules != null)
            rules.ForceEliminate(go);
    }
}
