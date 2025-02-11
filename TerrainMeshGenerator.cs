using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainMeshGenerator : MonoBehaviour
{
    [Header("References")]
    // Reference to the dynamic landscape generator.
    public DynamicLandscapeGenerator landscapeGenerator;
    
    [Header("Mesh Settings")]
    public float elevationMultiplier = 10f;
    public int meshResolution = 256;
    public float planeSize = 10f;
    
    private void Start()
    {
        if (landscapeGenerator == null)
            landscapeGenerator = FindObjectOfType<DynamicLandscapeGenerator>();
        
        Texture2D elevTexture = landscapeGenerator.GetElevationTexture();
        if (elevTexture == null)
        {
            Debug.LogError("Elevation texture is null! Ensure the landscape generator has generated the maps.");
            return;
        }
        GenerateTerrainMesh(elevTexture);
        UpdateTexture();
    }
    
    // Public method to rebuild the mesh.
    public void RegenerateMesh()
    {
        Texture2D elevTexture = landscapeGenerator.GetElevationTexture();
        if (elevTexture == null)
        {
            Debug.LogError("Elevation texture is null!");
            return;
        }
        GenerateTerrainMesh(elevTexture);
        UpdateTexture();
    }
    
    // Builds the mesh using the provided elevation texture.
    public void GenerateTerrainMesh(Texture2D elevTexture)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Procedural Terrain";

        int vertexCount = meshResolution * meshResolution;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[(meshResolution - 1) * (meshResolution - 1) * 6];

        for (int y = 0; y < meshResolution; y++)
        {
            for (int x = 0; x < meshResolution; x++)
            {
                int index = y * meshResolution + x;
                float u = (float)x / (meshResolution - 1);
                float v = (float)y / (meshResolution - 1);

                float posX = (u - 0.5f) * planeSize;
                float posZ = (v - 0.5f) * planeSize;

                float elevation = elevTexture.GetPixelBilinear(u, v).r;
                float posY = elevation * elevationMultiplier;

                vertices[index] = new Vector3(posX, posY, posZ);
                uvs[index] = new Vector2(u, v);
            }
        }

        int triIndex = 0;
        for (int y = 0; y < meshResolution - 1; y++)
        {
            for (int x = 0; x < meshResolution - 1; x++)
            {
                int current = y * meshResolution + x;
                int next = current + meshResolution;

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;

                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;

        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null)
            mc.sharedMesh = mesh;
    }
    
    // New method to update the material's main texture to the fuel classification texture.
    public void UpdateTexture()
    {
        Texture2D fuelTexture = landscapeGenerator.GetFuelClassificationTexture();
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && fuelTexture != null)
        {
            rend.material.mainTexture = fuelTexture;
        }
        else
        {
            Debug.LogWarning("Could not update texture: Renderer or fuel texture is null.");
        }
    }
}

