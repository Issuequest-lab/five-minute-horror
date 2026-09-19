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

    public void BeginIncidentAfterBath()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        if (progress == null || trigger2 == null)
        {
            Debug.LogError("事件後シーケンスの参照が不足しています。");
            return;
        }

        // 新仕様では浴槽の子供を確認した直後に事件後シーケンスへ入る。
        // 旧仕様の「電気を消す」操作はここでは要求しない。
        hasTriggered = true;

        if (shadow != null)
            shadow.SetActive(false);

        if (roomLight != null)
            roomLight.enabled = true;

        progress.AdvanceTo(GameProgress.SecondEventReady);
        trigger2.SetActive(true);
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

        if (progress == null ||
            progress.storyStep > GameProgress.FirstEvent ||
            !progress.hasInspectedRoom ||
            progress.roomInspectedFrame == Time.frameCount)
            return;

        // 旧シーンを壊さないためのフォールバック。
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            BeginLegacyLightEvent();
        }
    }

    void BeginLegacyLightEvent()
    {
        if (roomLight == null || shadow == null || trigger2 == null)
        {
            Debug.LogError("怪異1の参照が不足しています。");
            return;
        }

        hasTriggered = true;
        progress.AdvanceTo(GameProgress.FirstEvent);

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

        progress.AdvanceTo(GameProgress.SecondEventReady);
        trigger2.SetActive(true);
    }

    void OnGUI()
    {
        if (hasTriggered ||
            progress == null ||
            progress.storyStep > GameProgress.FirstEvent ||
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

        GUI.Box(
            guideRect,
            nearPlayer
                ? "E：電気を消す"
                : "目的：部屋の状況を確認しよう",
            style
        );
    }
}
