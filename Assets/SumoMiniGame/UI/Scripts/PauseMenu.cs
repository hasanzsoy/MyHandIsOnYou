using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Refs")]
    public GameObject panel;            // PausePanel (full screen)
    public GameObject firstSelected;     // Açılınca seçilecek buton (örn. BtnResume)

    bool isOpen;
    float _prevTimeScale = 1f;

    void Start()
    {
        if (panel) panel.SetActive(false);
    }

    void Update()
    {
        // Klavye & gamepad kısayolları
        if (Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.JoystickButton7) ||   // Start
            Input.GetKeyDown(KeyCode.JoystickButton1))     // B
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (panel) panel.SetActive(true);

            // Farenin görünürlüğü (istersen)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // UI seçim
            if (firstSelected != null)
                EventSystem.current?.SetSelectedGameObject(firstSelected);
        }
        else
        {
            // Menü kapanınca zaman ölçeğini geri getir
            Time.timeScale = _prevTimeScale;

            if (panel) panel.SetActive(false);

            // Farenin durumu (oyununa göre değiştirebilirsin)
            // Cursor.visible = false;
            // Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // UI Butonları
    public void BtnResume() { if (isOpen) Toggle(); }

    public void BtnRestartRound()
    {
        // Round’u baştan başlat
        var gm =
        #if UNITY_2023_1_OR_NEWER
            Object.FindFirstObjectByType<SumoGameManager>();
        #else
            Object.FindObjectOfType<SumoGameManager>();
        #endif

        if (gm) gm.RestartRound();
        if (isOpen) Toggle();
    }

    public void BtnQuitGame()
    {
        // Editörde çalıştırırken de takılmamak için timeScale'i toparla
        Time.timeScale = 1f;
        Application.Quit();
    }

    void OnDisable()
    {
        // Oyun objesi disable olursa timeScale 0'da kalmasın
        if (isOpen)
        {
            Time.timeScale = 1f;
            isOpen = false;
        }
    }
}
