using UnityEngine;

public class ExteriorFallGuard : MonoBehaviour
{
    GameProgress progress;
    Transform player;
    Rigidbody playerBody;
    GameObject barrierRoot;
    Bounds exteriorBounds;
    Vector3 safePosition;
    bool boundsReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<ExteriorFallGuard>() != null)
            return;

        GameObject root = new GameObject("ExteriorFallGuard");
        root.AddComponent<ExteriorFallGuard>();
    }

    void Update()
    {
        ResolveReferences();

        if (progress == null)
            return;

        bool exteriorPhase =
            progress.currentGuest == GameProgress.GuestB &&
            progress.storyStep >= GameProgress.GraveCreated &&
            progress.storyStep < GameProgress.Bedtime;

        if (!exteriorPhase)
        {
            if (barrierRoot != null)
            {
                Destroy(barrierRoot);
                barrierRoot = null;
                boundsReady = false;
            }
            return;
        }

        if (!boundsReady)
            TryBuildSafetyBounds();

        if (!boundsReady || player == null)
            return;

        bool fellBelowGround = player.position.y < exteriorBounds.min.y - 2.0f;
        bool escapedBounds =
            player.position.x < exteriorBounds.min.x - 1.0f ||
            player.position.x > exteriorBounds.max.x + 1.0f ||
            player.position.z < exteriorBounds.min.z - 1.0f ||
            player.position.z > exteriorBounds.max.z + 1.0f;

        if (fellBelowGround || escapedBounds)
            RecoverPlayer();
    }

    void ResolveReferences()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerBody = playerObject.GetComponent<Rigidbody>();
            }
        }
    }

    void TryBuildSafetyBounds()
    {
        GameObject ground = GameObject.Find("Exterior_Ground");
        if (ground == null)
            return;

        Collider groundCollider = ground.GetComponent<Collider>();
        if (groundCollider == null)
            return;

        exteriorBounds = groundCollider.bounds;
        float wallHeight = 3.0f;
        float wallThickness = 0.35f;
        float wallY = exteriorBounds.max.y + wallHeight * 0.5f;

        barrierRoot = new GameObject("Exterior_SafetyBoundaries");

        CreateInvisibleBarrier(
            "Boundary_Left",
            new Vector3(exteriorBounds.min.x - wallThickness * 0.5f, wallY, exteriorBounds.center.z),
            new Vector3(wallThickness, wallHeight, exteriorBounds.size.z + wallThickness * 2f));

        CreateInvisibleBarrier(
            "Boundary_Right",
            new Vector3(exteriorBounds.max.x + wallThickness * 0.5f, wallY, exteriorBounds.center.z),
            new Vector3(wallThickness, wallHeight, exteriorBounds.size.z + wallThickness * 2f));

        CreateInvisibleBarrier(
            "Boundary_Back",
            new Vector3(exteriorBounds.center.x, wallY, exteriorBounds.max.z + wallThickness * 0.5f),
            new Vector3(exteriorBounds.size.x + wallThickness * 2f, wallHeight, wallThickness));

        CreateInvisibleBarrier(
            "Boundary_Front",
            new Vector3(exteriorBounds.center.x, wallY, exteriorBounds.min.z - wallThickness * 0.5f),
            new Vector3(exteriorBounds.size.x + wallThickness * 2f, wallHeight, wallThickness));

        safePosition = new Vector3(
            exteriorBounds.center.x,
            exteriorBounds.max.y + 1.05f,
            exteriorBounds.center.z - exteriorBounds.extents.z * 0.62f);

        boundsReady = true;
    }

    void CreateInvisibleBarrier(string objectName, Vector3 position, Vector3 scale)
    {
        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = objectName;
        barrier.transform.SetParent(barrierRoot.transform, true);
        barrier.transform.position = position;
        barrier.transform.localScale = scale;

        Renderer renderer = barrier.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    void RecoverPlayer()
    {
        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }

        player.position = safePosition;
        Physics.SyncTransforms();
    }
}
