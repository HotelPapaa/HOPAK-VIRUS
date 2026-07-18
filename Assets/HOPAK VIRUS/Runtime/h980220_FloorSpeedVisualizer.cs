using UnityEngine;
using UnityEngine.Rendering;

public static class h980220_FloorSpeedVisualizer
{
    private const float TileSize = 4f;

    private static readonly Color BaseColor = new Color(0.082f, 0.094f, 0.125f, 1f);
    private static readonly Color TileColor = new Color(0.145f, 0.173f, 0.22f, 1f);

    public static void Build()
    {
        if (GameObject.Find("h980220_SpeedFloorVisuals") != null)
            return;

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var root = new GameObject("h980220_SpeedFloorVisuals").transform;

        foreach (Renderer floorRenderer in renderers)
        {
            if (floorRenderer == null || floorRenderer.gameObject.name != "Floor")
                continue;

            ApplyFloorColor(floorRenderer, BaseColor);
            BuildFloorPattern(root, floorRenderer);
        }
    }

    private static void BuildFloorPattern(Transform root, Renderer floorRenderer)
    {
        Bounds bounds = floorRenderer.bounds;
        bool circular = floorRenderer.transform.parent != null &&
                        floorRenderer.transform.parent.name.Contains("Arena");
        float surfaceY = bounds.max.y + 0.008f;
        Material tileMaterial = CreateMaterial(floorRenderer, TileColor);

        int xCount = Mathf.CeilToInt(bounds.size.x / TileSize);
        int zCount = Mathf.CeilToInt(bounds.size.z / TileSize);
        Vector2 circleCenter = new Vector2(bounds.center.x, bounds.center.z);
        float circleRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);

        for (int z = 0; z < zCount; z++)
        {
            for (int x = 0; x < xCount; x++)
            {
                if ((x + z) % 2 == 0)
                    continue;

                float minX = bounds.min.x + x * TileSize;
                float minZ = bounds.min.z + z * TileSize;
                float width = Mathf.Min(TileSize, bounds.max.x - minX);
                float depth = Mathf.Min(TileSize, bounds.max.z - minZ);
                Vector3 center = new Vector3(minX + width * 0.5f, surfaceY, minZ + depth * 0.5f);

                if (circular)
                {
                    float halfDiagonal = Mathf.Sqrt(width * width + depth * depth) * 0.5f;
                    Vector2 cellCenter = new Vector2(center.x, center.z);
                    if (Vector2.Distance(cellCenter, circleCenter) + halfDiagonal > circleRadius)
                        continue;
                }

                CreateFlatCube(root, "Dark Tile", center,
                    new Vector3(width, 0.012f, depth), tileMaterial);
            }
        }

    }

    private static Material CreateMaterial(Renderer source, Color color)
    {
        Shader shader = source.sharedMaterial != null
            ? source.sharedMaterial.shader
            : Shader.Find("Universal Render Pipeline/Lit");
        var material = new Material(shader);
        SetMaterialColor(material, color);
        return material;
    }

    private static void CreateFlatCube(
        Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, true);
        cube.transform.position = position;
        cube.transform.localScale = scale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);

        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        cubeRenderer.sharedMaterial = material;
        cubeRenderer.shadowCastingMode = ShadowCastingMode.Off;
        cubeRenderer.receiveShadows = false;
    }

    private static void ApplyFloorColor(Renderer renderer, Color color)
    {
        var properties = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        renderer.SetPropertyBlock(properties);
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
    }
}
