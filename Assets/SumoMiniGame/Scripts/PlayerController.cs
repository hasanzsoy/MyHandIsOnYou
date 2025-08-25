using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveForce = 60f;    // 2.5x ölçek için
    public float maxSpeed = 12f;     // normal koşu üst sınırı

    [Header("Dash")]
    [Tooltip("Dash anında eklenecek yatay hız (kütleden bağımsız)")]
    public float dashVelocityChange = 18f;   // 16–20 aralığı iyi
    public float dashDuration = 0.25f;       // boost süresi
    public float dashCooldown = 1.25f;       // tekrar süresi
    [Tooltip("Dash ve glide başında geçici düşük drag")]
    public float dashDrag = 0.04f;           // 0.03–0.06 arası
    [Tooltip("Dash bitince kısa kayma penceresi")]
    public float dashGlideTime = 0.35f;      // 0.30–0.45 arası

    [Header("Smoothing")]
    [Tooltip("Hız maxSpeed'i aştığında saniyedeki yumuşak fren miktarı")]
    public float softCapDecel = 25f;         // ne kadar yüksek, o kadar hızlı yavaşlar

    Rigidbody rb;
    Vector2 moveInput;
    bool canControl;

    bool isDashing;
    float dashEndTime;
    float nextDashTime;

    bool isGliding;
    float glideEndTime;
    float originalDrag;

    public bool IsDashing => isDashing || isGliding;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        SetControlEnabled(true);       // hızlı test
        originalDrag = rb.drag;        // inspector'daki değeri referans al
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

        // --- Soft speed cap (ANI KESME YOK) ---
        Vector3 v = rb.velocity;
        Vector2 h = new Vector2(v.x, v.z);
        float hMag = h.magnitude;

        if (!isDashing) // dash sırasında hiç sınırlama yok
        {
            if (hMag > maxSpeed)
            {
                // hızı bir anda kesmek yerine maxSpeed'e doğru yumuşakça yaklaştır
                Vector2 target = h.normalized * maxSpeed;
                Vector2 newH = Vector2.MoveTowards(h, target, softCapDecel * Time.fixedDeltaTime);
                rb.velocity = new Vector3(newH.x, v.y, newH.y);
            }
        }

        // --- Dash & Glide zamanlaması + drag'in kademeli geri dönüşü ---
        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
            isGliding = true;
            glideEndTime = Time.time + dashGlideTime;
            // glide boyunca drag'i yavaşça yükselteceğiz
        }

        if (isGliding)
        {
            float t = 1f - Mathf.Clamp01((glideEndTime - Time.time) / dashGlideTime); // 0→1
            rb.drag = Mathf.Lerp(dashDrag, originalDrag, t);

            if (Time.time >= glideEndTime)
            {
                isGliding = false;
                rb.drag = originalDrag; // tamamen eski drag'e dön
            }
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

        // Yön yoksa ileri bak
        Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
        if (dir.sqrMagnitude < 1e-4f) dir = transform.forward;
        dir.Normalize();

        // geçici düşük drag → daha uzun ve akıcı kayış
        originalDrag = rb.drag;
        rb.drag = dashDrag;

        // kütleden bağımsız yatay hız takviyesi
        Vector3 add = dir * dashVelocityChange;
        rb.AddForce(add, ForceMode.VelocityChange);

        isDashing = true;
        dashEndTime = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;
    }
}
