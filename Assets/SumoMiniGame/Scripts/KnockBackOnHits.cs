using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnockbackOnHit : MonoBehaviour
{
    public float basePushImpulse = 6f;
    public float dashPushMultiplier = 2.2f;
    public float selfRecoilFactor = 0.3f;

    Rigidbody rb;
    PlayerController me;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        me = GetComponent<PlayerController>();
    }

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody otherRb = collision.rigidbody;
        if (otherRb == null || otherRb == rb) return;

        // Sadece oyuncuysa kuvvet uygula
        var otherPc = otherRb.GetComponent<PlayerController>();
        if (otherPc == null) return;

        // Yön: bizden karşıya doğru
        Vector3 dir = (otherRb.position - rb.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        dir.Normalize();

        float impulse = basePushImpulse;
        if (me != null && me.IsDashing)
            impulse *= dashPushMultiplier;

        otherRb.AddForce(dir * impulse, ForceMode.Impulse);

        // Az da kendimize geri tepmeyi ekleyelim
        rb.AddForce(-dir * (impulse * selfRecoilFactor), ForceMode.Impulse);
    }
}
