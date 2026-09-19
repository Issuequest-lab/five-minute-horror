using UnityEngine;
using UnityEngine.InputSystem;
public class MouseLook : MonoBehaviour
{
public float sensitivity = 0.1f;
public Transform playerBody;
float xRotation = 0f;    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {Cursor.lockState = CursorLockMode.Locked;}

    // Update is called once per frame
    void Update()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);    
    }
}
