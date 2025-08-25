using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveForce = 35f;
    public float maxSpeed = 10f;

    [Header("Dash")]
    public float dashImpulse = 18f;
    public float dashDuration = 0.20f;
    public float dashCooldown = 1.25f;

    [Header("FX")]
    public ParticleSystem dashVfx;

    Rigidbody rb;
    Vector2 moveInput;
    bool canControl;
    bool isDashing;
    float dashEndTime;
    float nextDashTime;

    // dışarıdan okunabilsin
    public bool IsDashing => isDashing;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        canControl = false; // round başlayana kadar kilitli
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
        if (!enabled) moveInput = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (!canControl) return;

        // Yatay hareket (x,z)
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dir.sqrMagnitude > 1e-4f)
        {
            // Kuvvet uygulayalım
            rb.AddForce(dir.normalized * moveForce, ForceMode.Acceleration);

            // Yönüne dönsün
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 0.2f));
        }

        // Max hız sınırı (toplam yatay hız)
        Vector3 v = rb.linearVelocity;
        Vector2 horizontal = new Vector2(v.x, v.z);
        if (horizontal.magnitude > maxSpeed && !isDashing)
        {
            Vector2 clamped = horizontal.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clamped.x, v.y, clamped.y);
        }

        // Dash bitiş zamanı
        if (isDashing && Time.time >= dashEndTime)
            isDashing = false;
    }

    // Input System callback (PlayerInput → Send Messages)
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void OnDash()
    {
        if (!canControl) return;
        if (Time.time < nextDashTime) return;

        // hangi yöne dash? duruyorsa ileri yönü al
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dir.sqrMagnitude < 1e-4f)
            dir = transform.forward;

        rb.AddForce(dir.normalized * dashImpulse, ForceMode.Impulse);

        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        if (dashVfx) dashVfx.Play();
    }
}
