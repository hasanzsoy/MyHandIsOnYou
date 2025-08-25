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

    Rigidbody rb;
    Vector2 moveInput;
    bool canControl;
    bool isDashing;
    float dashEndTime;
    float nextDashTime;

    public bool IsDashing => isDashing;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Hızlı test için kontrolü hemen aç
        SetControlEnabled(true);
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
        if (!enabled) moveInput = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (!canControl) return;

        // hareket
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dir.sqrMagnitude > 1e-4f)
        {
            rb.AddForce(dir.normalized * moveForce, ForceMode.Acceleration);

            // yönüne dön
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 0.2f));
        }

        // max hız sınırı (dash değilken)
        Vector3 v = rb.linearVelocity;
        Vector2 h = new Vector2(v.x, v.z);
        if (h.magnitude > maxSpeed && !isDashing)
        {
            Vector2 clamped = h.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clamped.x, v.y, clamped.y);
        }

        if (isDashing && Time.time >= dashEndTime)
            isDashing = false;
    }

    // --- INPUT CALLBACKS (Send Messages uyumlu) ---

    // Move için iki overload: bazı sürümlerde InputValue, bazılarında direkt Vector2 gelebilir
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        // Debug
        // Debug.Log($"OnMove(InputValue): {moveInput}");
    }
    void OnMove(Vector2 v)
    {
        moveInput = v;
        // Debug.Log($"OnMove(Vector2): {moveInput}");
    }

    // Dash: hem parametreli hem parametresiz destekle
    void OnDash()
    {
        if (!canControl) return;
        if (Time.time < nextDashTime) return;

        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dir.sqrMagnitude < 1e-4f)
            dir = transform.forward;

        rb.AddForce(dir.normalized * dashImpulse, ForceMode.Impulse);

        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
    }
    void OnDash(InputValue v)
    {
        if (v.isPressed) OnDash();
    }
}
