using UnityEngine;

public class BodyEvent : MonoBehaviour
{
    public GameProgress gameProgress;

    void Awake()
    {
        if (gameProgress == null)
            gameProgress = FindAnyObjectByType<GameProgress>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || gameProgress == null)
            return;

        gameProgress.AdvanceTo(GameProgress.FirstEvent);
    }
}
