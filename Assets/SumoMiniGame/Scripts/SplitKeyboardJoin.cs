using UnityEngine;
using UnityEngine.InputSystem;

public class SplitKeyboardJoin : MonoBehaviour
{
    [SerializeField] private PlayerInputManager pim;
    public bool autoSpawnTwo = true;

    void Awake()
    {
        if (pim == null)
        {
            #if UNITY_2023_1_OR_NEWER
            pim = FindFirstObjectByType<PlayerInputManager>();
            #else
            pim = FindObjectOfType<PlayerInputManager>();
            #endif
        }
    }

    void Start()
    {
        if (!autoSpawnTwo || pim == null || Keyboard.current == null) return;

        // P1 (WASD + Space/LeftShift)
        var p1 = pim.JoinPlayer(controlScheme: "P1", pairWithDevice: Keyboard.current);
        ApplyGroup(p1, "P1");

        // P2 (Arrows + RightCtrl)
        var p2 = pim.JoinPlayer(controlScheme: "P2", pairWithDevice: Keyboard.current);
        ApplyGroup(p2, "P2");
    }

    void ApplyGroup(PlayerInput pi, string group)
    {
        if (pi == null) return;

        // 1) Aksiyonları geçici kapat
        var actions = pi.actions;
        actions.Disable();

        // 2) Sadece ilgili grubun binding'lerini dinle
        actions.bindingMask = InputBinding.MaskByGroup(group);

        // 3) Cihaz/şema sabitle
        pi.SwitchCurrentControlScheme(group, Keyboard.current);
        #if UNITY_INPUT_SYSTEM_1_5_OR_NEWER
        pi.neverAutoSwitchControlSchemes = true;
        #endif

        // 4) Tekrar aç
        actions.Enable();

        // isim
        pi.gameObject.name = $"Player_{group}";
    }
}
