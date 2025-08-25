using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveForce = 60f;    // 2.5x ölçek için
    public float maxSpeed = 12f;     // normal koşu üst sınırı

    [Header("Dash")]
    [Tooltip("Dash anında HIZA eklenecek yatay hız (kütleden bağımsız)")]
    public float dashVelocityChange = 16f;   // 14–20 aralığı ideal
    public float dashDuration = 0.25f;       // dash “boost” süresi
    public float dashCooldown = 1.25f;       // tekrar süresi
    [Tooltip("Dash ve glide sırasında geçici drag")]
    public float dashDrag = 0.05f;           // düşük olursa daha çok kayar
    [Tooltip("Dash bittikten sonra kısa bir kayma penceresi")]
    public float dashGlideTime = 0.30f;      // 0.25–0.40 arası hoş

    Rigidbody rb;
    Vector2 moveInput;
    bool canControl;

    bool isDashing;
    float dashEndTime;
    float nextDashTime;

    bool isGliding;          // dash sonrası kayma penceresi
    float glideEndTime;
    float originalDrag;

    public bool IsDashing => isDashing || isGliding;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Sahne başında kontrol açık kalsın (hızlı test için)
        SetControlEnabled(true);
        originalDrag = rb.drag;
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
        if (!enabled) moveInput = Vector2.zero;
    }

    void FixedUpdate()
    {
        if (!canControl) return;

        // --- Yürüyüş kuvveti ---
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dir.sqrMagnitude > 1e-4f)
        {
            rb.AddForce(dir.normalized * moveForce, ForceMode.Acceleration);

            // bakış yönünü yumuşak çevir
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 0.2f));
        }

        // --- Hız sınırlama (dash/glide dışında) ---
        Vector3 v = rb.velocity;
        Vector2 h = new Vector2(v.x, v.z);
        if (h.magnitude > maxSpeed && !isDashing && !isGliding)
        {
            Vector2 clamped = h.normalized * maxSpeed;
            rb.velocity = new Vector3(clamped.x, v.y, clamped.y);
        }

        // --- Dash & glide zamanlaması ---
        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
            isGliding = true;
            glideEndTime = Time.time + dashGlideTime;
            // drag'ı glide bitene kadar düşük tut
        }

        if (isGliding && Time.time >= glideEndTime)
        {
            isGliding = false;
            rb.drag = originalDrag; // drag'ı normale döndür
        }
    }

    // --- INPUT CALLBACKS ---

    void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    void OnMove(Vector2 v)        => moveInput = v;

    void OnDash()                 => TryDash();
    void OnDash(InputValue v)     { if (v.isPressed) TryDash(); }

    void TryDash()
    {
        if (!canControl) return;
        if (Time.time < nextDashTime) return;

        // Yön yoksa ileri bakış yönüne dash
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
        dir.Normalize();

        // Geçici düşük drag → daha uzun kayma
        originalDrag = rb.drag;
        rb.drag = dashDrag;

        // KÜTLEDEN BAĞIMSIZ hız takviyesi (yalnızca yatay düzlemde)
        Vector3 add = dir * dashVelocityChange;
        rb.AddForce(add, ForceMode.VelocityChange);

        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
    }
}
