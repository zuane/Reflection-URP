using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
	#region Movement Settings
    [Header("Movement Settings")]
    private float moveSpeed = 4.5f;
    private float initialMoveSpeed;
    private float crouchSpeed = 2.0f;
    private float sprintSpeed = 8.5f;
    #endregion

    #region Height Settings
    [Header("Height Settings")]
    private float standingHeight = 1.8f;
    private float crouchTransitionSpeed = 15f;
    private float targetHeight;
    private float crouchingHeight = 1.0f;
    #endregion

    #region Mouse Settings
    [Header("Mouse Settings")]
    private float mouseSensitivity = 25f;
    #endregion

    #region Camera Settings
    [Header("Camera Settings")]
    private float defaultFOV = 65f;
    private float sprintFOV = 75f;
    private float crouchFOV = 65f;
    private float fovTransitionSpeed = 15f;
    #endregion

    #region Gravity Settings
    [Header("Gravity Settings")]
    private float jumpForce = 1.5f;
    private float gravity = 35f;
    private float maxFallSpeed = 90f;
    #endregion

    #region Slope Settings
    [Header("Slope Settings")]
    private float maxSlopeAngle = 35f;
    private float baseSlideSpeed = 12f;
    private float maxSlideSpeed = 18f;
    private bool isSliding;
    private RaycastHit slopeHit;
    #endregion

    #region Input and Player State
    private CharacterController controller;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    private Vector2 moveInput;
    private bool jumpInput;
    private Vector2 lookInput;
    private Vector3 velocity;
    private bool isSprinting;
    private bool isCrouching = false;
    private bool isGrounded;
    #endregion

    #region Camera and Rotation
    private Camera playerCamera;
    private Vector3 cameraDefaultPosition;
    private Vector3 targetCameraPosition;
    private float xRotation = 0f;
    private const float MinLookAngle = -90f;
    private const float MaxLookAngle = 90f;
    #endregion

    #region Slope Detection
    private const float SlopeCheckOffset = 0.1f;
    private const float SlopeCheckDistance = 1.5f;
    private Vector3[] slopeCheckPoints;
    #endregion

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
		moveAction = playerInput.actions["Move"];
		lookAction = playerInput.actions["Look"];
		jumpAction = playerInput.actions["Jump"];
		sprintAction = playerInput.actions["Sprint"];
		crouchAction = playerInput.actions["Crouch"];
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

		cameraDefaultPosition = playerCamera.transform.localPosition;
		playerCamera.fieldOfView = defaultFOV;

		targetHeight = standingHeight;
		targetCameraPosition = new Vector3(cameraDefaultPosition.x, standingHeight / 2f, cameraDefaultPosition.z);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

		initialMoveSpeed = moveSpeed;

		slopeCheckPoints = new Vector3[]
		{
			Vector3.zero,
			Vector3.right * SlopeCheckOffset,
			Vector3.left * SlopeCheckOffset,
			Vector3.forward * SlopeCheckOffset,
			Vector3.back * SlopeCheckOffset
		};
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
		lookInput = lookAction.ReadValue<Vector2>();
		jumpInput = jumpAction.triggered;
		isSprinting = sprintAction.ReadValue<float>() == 1;

		if (crouchAction.triggered)
		{
			ToggleCrouch();
		}

		Move();
		Look();
		AdjustFOV();
		SmoothCrouchTransition();
    }

    void Move()
    {
        isGrounded = controller.isGrounded;
		isSliding = isGrounded && OnSlope();

		float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
		Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

		if (isSliding)
		{
			float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
			float currentSlideSpeed = Mathf.Lerp(baseSlideSpeed, maxSlideSpeed, (slopeAngle - maxSlopeAngle) / (90f - maxSlopeAngle));

			Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, slopeHit.normal).normalized;
			
			Vector3 sideMovement = transform.right * moveInput.x * 0.5f;
			
			Vector3 finalSlideMovement = (slideDirection + sideMovement).normalized;
			
			Debug.DrawRay(transform.position, slideDirection * 2f, Color.yellow);
			Debug.DrawRay(transform.position, finalSlideMovement * 2f, Color.red);
			
			controller.Move(finalSlideMovement * currentSlideSpeed * Time.deltaTime);
			
			velocity.y = -2f;
		}
		else
		{
			controller.Move(move * currentSpeed * Time.deltaTime);
		}

		if (isGrounded)
		{
			if (isGrounded)
			{
				velocity.y = jumpInput && !isSliding ? Mathf.Sqrt(jumpForce * 2f * gravity) : -2f;
			}
			else
			{
				velocity.y = Mathf.Max(velocity.y - gravity * Time.deltaTime, -maxFallSpeed);
			}
		}
		else
		{
			velocity.y = Mathf.Max(velocity.y - gravity * Time.deltaTime, -maxFallSpeed);
		}

		controller.Move(velocity * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, MinLookAngle, MaxLookAngle);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

	private void SetCrouchState(bool crouching)
	{
		isCrouching = crouching;
		targetHeight = crouching ? crouchingHeight : standingHeight;
		moveSpeed = crouching ? crouchSpeed : (isSprinting ? sprintSpeed : initialMoveSpeed);
		targetCameraPosition = new Vector3(cameraDefaultPosition.x, targetHeight / 2f, cameraDefaultPosition.z);
	}

	void ToggleCrouch()
	{
		if (isCrouching && Physics.Raycast(transform.position, Vector3.up, standingHeight))
		{
			#if UNITY_EDITOR
			Debug.Log("Cannot stand up, there is an obstacle above you.");
			#endif
			return;
		}

    SetCrouchState(!isCrouching);
	}

	void SmoothCrouchTransition()
	{
		controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);

		playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, targetCameraPosition, Time.deltaTime * crouchTransitionSpeed);
	}

	private bool OnSlope()
	{
		foreach (Vector3 offset in slopeCheckPoints)
		{
			Vector3 rayOrigin = transform.position + offset;

			if (Physics.Raycast(rayOrigin, Vector3.down, out slopeHit, SlopeCheckDistance))
			{
				float angle = Vector3.Angle(Vector3.up, slopeHit.normal);

				#if UNITY_EDITOR
				Debug.DrawRay(rayOrigin, Vector3.down * (controller.height / 2 + 0.5f), Color.yellow);
				Debug.Log($"Slope angle at {offset}: {angle:F1}°");
				#endif

				if (angle > maxSlopeAngle)
				{
					return true;
				}
			}
		}

		return false;
	}

	void AdjustFOV()
	{
		float targetFOV = defaultFOV;

		if (isSliding || isSprinting)
		{
			targetFOV = sprintFOV;
		}
		else if (isCrouching)
		{
			targetFOV = crouchFOV;
		}

		playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying)
		{
			foreach (Vector3 offset in slopeCheckPoints)
			{
				Vector3 rayOrigin = transform.position + offset;

				Debug.DrawRay(rayOrigin, Vector3.down * SlopeCheckDistance, isSliding ? Color.red : Color.green);
			}
		}
	}
}