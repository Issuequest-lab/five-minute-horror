using UnityEngine;

public class HorrorGuide : MonoBehaviour
{
    public GameProgress progress;
    public float introSeconds = 7f;

    float startTime;

    void Awake()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        startTime = Time.unscaledTime;
    }

    void OnGUI()
    {
        if (progress == null)
            return;

        string message = GetSituationMessage();
        if (message == "")
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 22;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        float width = Mathf.Min(720f, Screen.width - 24f);

        GUI.Box(
            new Rect(
                (Screen.width - width) / 2f,
                Screen.height - 110f,
                width,
                80f),
            message,
            style);
    }

    string GetSituationMessage()
    {
        switch (progress.storyStep)
        {
            case GameProgress.Start:
                if (Time.unscaledTime - startTime < introSeconds)
                {
                    return "旅館の中を確認しよう。\nWASD：移動　マウス：視点操作";
                }
                return "浴槽のある部屋を確認しよう。";

            case GameProgress.FirstEvent:
                return "浴槽で子供を発見した。警察へ連絡している。";

            case GameProgress.SecondEventReady:
                return "警察と野次馬が現場へ向かっている。";

            case GameProgress.SecondEvent:
            case GameProgress.CrowdGathering:
                return "警察官と野次馬が玄関から現場へ集まってきている。";

            case GameProgress.SceneInvestigation:
                return "20人以上が集まり、警察の現場確認が続いている。";

            case GameProgress.CrowdLeaving:
                return "現場確認が終わり、人々が一人ずつ廊下と玄関を通って旅館を出ていく。";

            case GameProgress.InnQuiet:
                return "先ほどまでの騒ぎが嘘のように静かになった。玄関から外へ出てみよう。";

            case GameProgress.GraveCreated:
                return "時間が経った。玄関の外に、さっきまで無かった墓碑がある。";

            case GameProgress.GraveInspected:
                return "墓碑に刻まれた文字を確認している。";

            default:
                return "";
        }
    }
}
