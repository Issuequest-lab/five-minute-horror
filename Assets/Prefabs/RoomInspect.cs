using UnityEngine;
using UnityEngine.InputSystem;

public class RoomInspect : MonoBehaviour
{
    public GameProgress progress;
    public Transform inspectTarget;
    public float inspectDistance = 2f;

    Transform player;

    void Awake()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;
    }

    bool CanInspect()
    {
        if (progress == null || player == null || inspectTarget == null)
            return false;

        if (progress.hasInspectedRoom || progress.storyStep >= 2)
            return false;

        Vector3 difference = player.position - inspectTarget.position;
        difference.y = 0f;

        return difference.sqrMagnitude <= inspectDistance * inspectDistance;
    }

    void Update()
    {
        if (!CanInspect() || Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            progress.hasInspectedRoom = true;
            progress.roomInspectedFrame = Time.frameCount;
        }
    }

    void OnGUI()
    {
        if (!CanInspect())
            return;

        GUI.Box(
            new Rect(10, 10, Screen.width - 20, 45),
            "E：浴槽を調べる");
    }
}