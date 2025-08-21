using UnityEngine;

public class MouseMovement : MonoBehaviour



{

    public float mouseSensitivity = 100f;

    float xRotation = 0f;
    float yRotation = 0f;

    public float topClamp = 90f;
    public float bottomClamp = -90f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the center of the screen
        
    }

    // Update is called once per frame
    void Update()
    {

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY; // Invert the Y-axis for a more natural feel
        xRotation = Mathf.Clamp(xRotation,bottomClamp,topClamp); // Clamp
        yRotation += mouseX; // Add mouse X movement
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f); // Apply rotation to the camera
        
    }
}
