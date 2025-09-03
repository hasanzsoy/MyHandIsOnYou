using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Tam ekran pause paneli")]
    public GameObject panel;

    [Tooltip("Açılınca seçilecek ilk buton (örn. Resume)")]
    public GameObject firstSelected;

    [Header("Refs (opsiyonel)")]
    [Tooltip("Inspector'dan atarsan Find yapmayız; boşsa otomatik buluruz.")]
    public SumoGameManager gameManager;

    bool isOpen;
    float prevTimeScale = 1f;

    void Awake()
    {
        if (panel) panel.SetActive(false);
        ResolveGameManagerIfNeeded();
    }

    void Update()
    {
        // ESC / Start ile aç/kapa
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.JoystickButton7))   // Start
        {
            Toggle();
        }
    }

    // ----- PUBLIC UI EVENTS -----

    // Resume: oyuna devam
    public void BtnResume()
    {
        Resume();
    }

    // Restart: skorları korur, oyuncuları spawn noktalarına geri koyar
    public void BtnRestartRound()
    {
        // Menüyü kapatıp zamanı normale al
        Resume();

        ResolveGameManagerIfNeeded();
        if (gameManager != null)
        {
            gameManager.RestartRound();   // ⬅️ Round sıfırlar, oyuncuları yeniden spawn eder
        }
        else
        {
            Debug.LogError("[PauseMenu] SumoGameManager bulunamadı! (Sahnede aktif mi?)");
        }
    }

    // Quit: oyundan çık
    public void BtnQuitGame()
    {
        Time.timeScale = 1f; // her ihtimale karşı toparla

#if UNITY_EDITOR
        // Editör'de play modunu durdur
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----- TOGGLE / RESUME -----

    public void Toggle()
    {
        if (isOpen) { Resume(); }
        else        { Open();   }
    }

    void Open()
    {
        if (isOpen) return;
        isOpen = true;

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (panel) panel.SetActive(true);

        // Fare serbest (istersen proje gereğine göre kapatabilirsin)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // UI focus
        if (firstSelected != null)
            EventSystem.current?.SetSelectedGameObject(firstSelected);
    }

    void Resume()
    {
        if (!isOpen && Mathf.Approximately(Time.timeScale, 1f))
            return;

        isOpen = false;

        Time.timeScale = Mathf.Approximately(prevTimeScale, 0f) ? 1f : prevTimeScale;

        if (panel) panel.SetActive(false);

        // Oyuna dönerken fareni isteğine göre ayarla
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDisable()
    {
        // Script/Objesi devre dışı kalırsa oyunu kilitli bırakma
        if (isOpen)
        {
            Time.timeScale = 1f;
            isOpen = false;
        }
    }

    // ----- HELPERS -----

    void ResolveGameManagerIfNeeded()
    {
        if (gameManager != null) return;

#if UNITY_2023_1_OR_NEWER
        gameManager = Object.FindFirstObjectByType<SumoGameManager>();
#else
        gameManager = Object.FindObjectOfType<SumoGameManager>();
#endif
    }
}
