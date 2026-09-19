using UnityEngine;

public class BodyEvent : MonoBehaviour
{
    public GameProgress gameProgress;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            gameProgress.storyStep = 1;
        }
    }
}