using System.Diagnostics;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
public class CUBEMOVER : MonoBehaviour
{
    public float speed = 0.005f;
    public int attackPower = 10;
    public int enemyHP = 100;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if
        (Keyboard.current.rightArrowKey.isPressed)
        {
        transform.Translate(0,0,-speed,Space.World);
        }
        if(Keyboard.current.leftArrowKey.isPressed)    
        {
        transform.Translate(0,0,speed,Space.World);
        }
        if
        (Keyboard.current.upArrowKey.isPressed)
        {
        transform.Translate(speed,0,0,Space.World);
        }
        if(Keyboard.current.downArrowKey.isPressed)    
        {
        transform.Translate(-speed,0,0,Space.World);
        }
        if
        (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
        enemyHP = enemyHP - attackPower ;
        UnityEngine.Debug.Log(enemyHP);
        }
        }
}