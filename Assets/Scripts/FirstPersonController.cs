using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
	[Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.75f;
	[SerializeField] private float crouchSpeed = 2.5f;
	[SerializeField] private float sprintSpeed = 6.75f;

	[Header("Height Settings")]
	[SerializeField] private float standingHeight = 1.8f;
	[SerializeField] private float crouchingHeight = 1.0f;

	[Header("Mouse Settings")]
	[SerializeField] private float mouseSensitivity = 20f;

	[Header("Gravity Settings")]
    [SerializeField] private float jumpForce = 1.5f;
    [SerializeField] private float gravity = 30f;

    private CharacterController controller;
	private PlayerInput playerInput;
    private Vector2 moveInput;
	private bool jumpInput;
    private Vector2 lookInput;
    private Vector3 velocity;
	private Camera playerCamera;
	private Vector3 cameraDefaultPosition;
	private bool isSprinting;
	private bool isCrouching = false;
	private bool isGrounded;
    private float xRotation = 0f; // Rotation around the x-axis

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

		cameraDefaultPosition = playerCamera.transform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
		lookInput = playerInput.actions["Look"].ReadValue<Vector2>();
		jumpInput = playerInput.actions["Jump"].triggered;
		isSprinting = playerInput.actions["Sprint"].ReadValue<float>() == 1;
	
		if (playerInput.actions["Crouch"].triggered)
		{
			ToggleCrouch();
		}

        Move();
        Look();
    }

    void Move()
    {
        isGrounded = controller.isGrounded;

		float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (isGrounded && jumpInput)
        {
            velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
        }

        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

	void ToggleCrouch()
	{
		if (isCrouching)
		{
			if (Physics.Raycast(transform.position, Vector3.up, standingHeight))
			{
				Debug.Log("Cannot stand up, there is an obstacle above you.");
				return;
			}
		}

		isCrouching = !isCrouching;

		if (isCrouching)
		{
			controller.height = crouchingHeight;
			moveSpeed = crouchSpeed;

			// Do not forget to adjust the camera position
			playerCamera.transform.localPosition = new Vector3
			(
				cameraDefaultPosition.x,
				crouchingHeight / 2f,
				cameraDefaultPosition.z
			);


		}
		else
		{
			controller.height = standingHeight;
			moveSpeed = isSprinting ? sprintSpeed : moveSpeed;

			playerCamera.transform.localPosition = new Vector3
			(
				cameraDefaultPosition.x,
				standingHeight / 2f,
				cameraDefaultPosition.z
			);
		}
	}
}