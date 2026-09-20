using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewMonoBehaviourScript1 : MonoBehaviour
{
    public GameProgress progress;
    public Transform inspectTarget;
    public float inspectDistance = 2f;
    public float messageSeconds = 2.5f;

    public GameObject childCorpse;

    Transform player;

    bool showingMessage = false;
    bool isInspecting = false;

    void Awake()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        // 少年は「調べた瞬間に出現する怪異」ではなく、
        // プレイヤーが部屋へ入った時点ですでに浴槽に浮かんでいる。
        if (childCorpse != null)
            childCorpse.SetActive(true);
    }

    bool CanInspect()
    {
        if (progress == null ||
            player == null ||
            inspectTarget == null ||
            isInspecting)
            return false;

        if (progress.hasInspectedRoom ||
            progress.storyStep >= GameProgress.SecondEventReady)
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
            StartCoroutine(InspectionSequence());
    }

    IEnumerator InspectionSequence()
    {
        isInspecting = true;

        progress.AdvanceTo(GameProgress.FirstEvent);
        showingMessage = true;

        yield return new WaitForSeconds(messageSeconds);

        showingMessage = false;
        progress.hasInspectedRoom = true;
        progress.roomInspectedFrame = Time.frameCount;

        LightOffTrigger incidentStarter = FindAnyObjectByType<LightOffTrigger>();
        if (incidentStarter != null)
            incidentStarter.BeginIncidentAfterBath();
        else
            Debug.LogError("事件後シーケンスを開始するLightOffTriggerが見つかりません。");

        isInspecting = false;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 26;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        float width = Mathf.Min(700f, Screen.width - 30f);
        Rect messageRect = new Rect(
            (Screen.width - width) / 2f,
            20f,
            width,
            70f);

        if (showingMessage)
        {
            GUI.Box(
                messageRect,
                "浴槽に少年が浮かんでいる。\n……警察を呼ばないと。",
                style);
            return;
        }

        if (CanInspect())
            GUI.Box(messageRect, "E：調べる", style);
    }
}
