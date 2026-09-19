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
    public float apparitionSeconds = 0.7f;

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

        if (childCorpse != null)
            childCorpse.SetActive(false);
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
            StartCoroutine(ApparitionSequence());
    }

    IEnumerator ApparitionSequence()
    {
        isInspecting = true;

        if (childCorpse != null)
        {
            childCorpse.SetActive(true);
            yield return new WaitForSeconds(apparitionSeconds);
            childCorpse.SetActive(false);
        }

        progress.AdvanceTo(GameProgress.FirstEvent);
        showingMessage = true;

        yield return new WaitForSeconds(messageSeconds);

        showingMessage = false;
        progress.hasInspectedRoom = true;
        progress.roomInspectedFrame = Time.frameCount;

        // 浴槽の子供を確認した直後に、警察・野次馬の事件後シーケンスへ進める。
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
            70f
        );

        if (showingMessage)
        {
            GUI.Box(
                messageRect,
                "浴槽に子供がいる。\n警察を呼ばないと……。",
                style
            );
            return;
        }

        if (CanInspect())
        {
            GUI.Box(
                messageRect,
                "E：浴槽を調べる",
                style
            );
        }
    }
}
