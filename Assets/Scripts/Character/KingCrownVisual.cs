using UnityEngine;

[DisallowMultipleComponent]
public class KingCrownVisual : MonoBehaviour
{
    [Header("Optional Visual")]
    [SerializeField] private Sprite crownSprite;
    [SerializeField] private GameObject crownPrefab;
    [SerializeField] private Transform crownAnchor;
    [SerializeField] private bool faceCamera = true;

    [Header("Fallback")]
    [SerializeField] private Color fallbackColor =
        new Color(1f, 0.72f, 0.12f, 1f);

    private NetworkPlayerScore playerScore;
    private GameObject crownInstance;
    private Transform characterAnchor;
    private Vector3 fallbackOffset = new Vector3(0f, 2.8f, 0f);
    private float visualScale = 0.65f;
    private bool previousVisible;
    private bool hasVisibilityState;
    private Camera mainCamera;
    private Mesh fallbackMesh;
    private Material fallbackMaterial;

    public void Configure(CharacterData character, GameObject characterVisual)
    {
        if (character != null)
        {
            fallbackOffset = character.crownOffset;
            visualScale = character.crownScale;
        }

        characterAnchor = crownAnchor != null
            ? crownAnchor
            : FindChildByName(characterVisual, "CrownAnchor");

        EnsureCrownInstance();
        ApplyVisibility(force: true);
    }

    private void Awake()
    {
        playerScore = GetComponent<NetworkPlayerScore>();
    }

    private void LateUpdate()
    {
        ApplyVisibility(force: false);

        if (crownInstance == null || !crownInstance.activeSelf)
            return;

        Transform crownTransform = crownInstance.transform;
        crownTransform.position = characterAnchor != null
            ? characterAnchor.position
            : transform.TransformPoint(fallbackOffset);

        crownTransform.localScale = Vector3.one * visualScale;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (faceCamera && mainCamera != null)
            crownTransform.forward = mainCamera.transform.forward;
    }

    private void ApplyVisibility(bool force)
    {
        EnsureCrownInstance();

        if (playerScore == null)
            playerScore = GetComponent<NetworkPlayerScore>();

        bool visible = playerScore != null && playerScore.IsKing;

        if (!force && hasVisibilityState && previousVisible == visible)
            return;

        hasVisibilityState = true;
        previousVisible = visible;

        if (crownInstance != null && crownInstance.activeSelf != visible)
            crownInstance.SetActive(visible);
    }

    private void EnsureCrownInstance()
    {
        if (crownInstance != null)
            return;

        if (crownPrefab != null)
            crownInstance = Instantiate(crownPrefab, transform);
        else if (crownSprite != null)
            crownInstance = CreateSpriteCrown();
        else
            crownInstance = CreateFallbackCrown();

        crownInstance.name = "KingCrown";
        crownInstance.SetActive(false);
    }

    private GameObject CreateSpriteCrown()
    {
        GameObject spriteObject = new GameObject("KingCrownSprite");
        spriteObject.transform.SetParent(transform, false);

        SpriteRenderer spriteRenderer =
            spriteObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sprite = crownSprite;
        spriteRenderer.sortingOrder = 100;
        return spriteObject;
    }

    private GameObject CreateFallbackCrown()
    {
        GameObject fallback = new GameObject("KingCrownPlaceholder");
        fallback.transform.SetParent(transform, false);

        MeshFilter meshFilter = fallback.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = fallback.AddComponent<MeshRenderer>();

        fallbackMesh = BuildCrownMesh();
        meshFilter.sharedMesh = fallbackMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            fallbackMaterial = new Material(shader);
            fallbackMaterial.color = fallbackColor;
            meshRenderer.sharedMaterial = fallbackMaterial;
        }

        meshRenderer.sortingOrder = 100;
        return fallback;
    }

    private static Mesh BuildCrownMesh()
    {
        Vector3[] vertices =
        {
            new(-0.8f, -0.35f, 0f), new(0.8f, -0.35f, 0f),
            new(0.8f, -0.05f, 0f), new(-0.8f, -0.05f, 0f),
            new(-0.75f, -0.05f, 0f), new(-0.62f, 0.55f, 0f),
            new(-0.18f, -0.05f, 0f), new(-0.35f, -0.05f, 0f),
            new(0f, 0.85f, 0f), new(0.35f, -0.05f, 0f),
            new(0.18f, -0.05f, 0f), new(0.62f, 0.55f, 0f),
            new(0.75f, -0.05f, 0f)
        };

        int[] triangles =
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6,
            7, 8, 9,
            10, 11, 12
        };

        Mesh mesh = new Mesh { name = "KingCrownPlaceholderMesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Transform FindChildByName(
        GameObject characterVisual,
        string childName
    )
    {
        if (characterVisual == null)
            return null;

        Transform[] children =
            characterVisual.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private void OnDestroy()
    {
        if (fallbackMesh != null)
            Destroy(fallbackMesh);

        if (fallbackMaterial != null)
            Destroy(fallbackMaterial);
    }
}
