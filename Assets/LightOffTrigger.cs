using UnityEngine;
using UnityEngine.InputSystem;

public class LightOffTrigger : MonoBehaviour
{
    public Light roomLight;
    public AudioSource scarySound;
    public GameObject shadow;
    public GameObject trigger2;
    public float shadowSeconds = 15f;

    GameProgress progress;
    bool hasTriggered;
    bool nearPlayer;

    void Awake()
    {
        progress = FindAnyObjectByType<GameProgress>();

        if (trigger2 != null)
            trigger2.SetActive(false);

        if (shadow != null)
            shadow.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            nearPlayer = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            nearPlayer = false;
    }

    void Update()
    {
        if (!nearPlayer || hasTriggered)
            return;

        if (progress == null || progress.storyStep > 1 ||
    !progress.hasInspectedRoom ||
    progress.roomInspectedFrame == Time.frameCount)
    return;

        if (Keyboard.current != null &
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            BeginEvent();
        }
    }

    void BeginEvent()
    {
        if (roomLight == null || shadow == null || trigger2 == null)
        {
            Debug.LogError("怪異1の参照が不足しています。");
            return;
        }

        hasTriggered = true;
        progress.storyStep = 1;

        roomLight.enabled = false;

        if (scarySound != null)
            scarySound.Play();

        shadow.SetActive(true);

        Invoke(nameof(HideShadow), shadowSeconds);
    }

    void HideShadow()
    {
        shadow.SetActive(false);
        roomLight.enabled = true;

        progress.storyStep = 2;
        trigger2.SetActive(true);
    }

    void OnGUI()
{
    if (hasTriggered ||
        progress == null ||
        progress.storyStep > 1 ||
        !progress.hasInspectedRoom ||
        progress.roomInspectedFrame == Time.frameCount)
        return;

    GUIStyle style = new GUIStyle(GUI.skin.box);
    style.fontSize = 26;
    style.fontStyle = FontStyle.Bold;
    style.alignment = TextAnchor.MiddleCenter;
    style.wordWrap = true;
    style.normal.textColor = Color.white;

    float width = Mathf.Min(700f, Screen.width - 30f);

    Rect guideRect = new Rect(
        (Screen.width - width) / 2f,
        20f,
        width,
        70f
    );

    if (nearPlayer)
    {
        GUI.Box(
            guideRect,
            "E：電気を消して休む",
            style
        );
    }
    else
    {
        GUI.Box(
            guideRect,
            "目的：電気を消して休もう",
            style
        );
    }
}
}