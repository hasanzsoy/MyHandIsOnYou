using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller; // Reference to the CharacterController component
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float speed = 12f; // Speed of the player movement
    public float jumpHeight = 3f; // Height of the player's jump
    public float gravity = -9.81f * 2; // Gravity applied to the player
    public Transform groundCheck; // Transform to check if the player is grounded
    public LayerMask groundMask; // Layer mask to identify ground objects
     public float groundDistance = 0.4f; // Distance to check for ground

     Vector3 velocity; // Current velocity of the player
     bool isGrounded; // Whether the player is grounded
     bool isMoving;

    private Vector3 lastPosition = new Vector3(0f, 0f, 0f); // Whether the player is currently moving

    void Start()
    {
        controller = GetComponent<CharacterController>(); // Get the CharacterController component attached to the player
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask); // Check if the player is grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Reset vertical velocity when grounded
        }

        float x = Input.GetAxis("Horizontal"); // Get horizontal input
        float z = Input.GetAxis("Vertical"); // Get vertical input

        Vector3 move = transform.right * x + transform.forward * z; // Calculate movement direction
        controller.Move(move * speed * Time.deltaTime); // Move the player

        if (Input.GetButtonDown("Jump") && isGrounded) // Check for jump input
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // Calculate jump velocity
        }

        velocity.y += gravity * Time.deltaTime; // Apply gravity to the vertical velocity
        controller.Move(velocity * Time.deltaTime); // Move the player with the updated velocity

        if (lastPosition != gameObject.transform.position && isGrounded) // Check if the player has moved
        {
            isMoving = true; // Set moving state to true
                             // Update last position
        }
        else
        {
            isMoving = false; // Set moving state to false
        }
        lastPosition = gameObject.transform.position; // Update last position to current position
    }
}
