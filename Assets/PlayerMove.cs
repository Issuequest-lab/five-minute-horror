using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    public float speed = 5f;
    public float lookSensitivity = 0.12f;
    public Camera playerCamera;

    Rigidbody body;
    Vector2 moveInput;
    float yaw;
    float pitch;

    void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerCamera == null)
        {
            Debug.LogError("Playerの子にCameraがありません。");
            enabled = false;
            return;
        }

        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        // Player本体は物理的に回転させない
        body.constraints = RigidbodyConstraints.FreezeRotation;

        yaw = playerCamera.transform.localEulerAngles.y;
        pitch = Mathf.DeltaAngle(
            0f, playerCamera.transform.localEulerAngles.x);
    }

    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            moveInput = Vector2.zero;

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
                LockCursor();

            return;
        }

        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed) x -= 1f;

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed) x += 1f;

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed) z -= 1f;

            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed) z += 1f;
        }

        moveInput = new Vector2(x, z).normalized;

        if (Mouse.current != null)
        {
            Vector2 mouse = Mouse.current.delta.ReadValue();

            yaw += mouse.x * lookSensitivity;
            pitch -= mouse.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -85f, 85f);

            // カメラだけを回転させる
            playerCamera.transform.localRotation =
                Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    void FixedUpdate()
    {
        // カメラの向いている方向を基準に移動
        Quaternion directionRotation = Quaternion.Euler(
            0f, transform.eulerAngles.y + yaw, 0f);

        Vector3 direction = directionRotation *
            new Vector3(moveInput.x, 0f, moveInput.y);

        Vector3 velocity = direction * speed;
        velocity.y = body.linearVelocity.y;

        body.linearVelocity = velocity;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}