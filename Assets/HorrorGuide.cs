using UnityEngine;

public class HorrorGuide : MonoBehaviour
{
    public GameProgress progress;
    public float introSeconds = 7f;

    float startTime;
    float stepChangedTime;
    int previousStep;

    void Awake()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        startTime = Time.unscaledTime;
        stepChangedTime = startTime;
        previousStep = progress != null ? progress.storyStep : 0;
    }

    void Update()
    {
        if (progress == null)
            return;

        if (progress.storyStep != previousStep)
        {
            previousStep = progress.storyStep;
            stepChangedTime = Time.unscaledTime;
        }
    }

    void OnGUI()
    {
        if (progress == null)
            return;

        string message = "";

        if (progress.storyStep == 0 &&
            Time.unscaledTime - startTime < introSeconds)
        {
            message = "供養を終え、宿に戻った。今夜はここで休もう。\nWASD：移動　マウス：視点操作";
        }
        else if (progress.storyStep == 2 &&
                 Time.unscaledTime - stepChangedTime < 3f)
        {
            message = "……何かが変わった。";
        }

        if (message == "")
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 22;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        float width = Mathf.Min(620f, Screen.width - 24f);

        GUI.Box(
            new Rect(
                (Screen.width - width) / 2f,
                Screen.height - 110f,
                width,
                80f),
            message,
            style);
    }
}