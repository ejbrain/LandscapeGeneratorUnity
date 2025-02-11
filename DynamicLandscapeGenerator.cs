using UnityEngine;
using UnityEngine.UI;

public class DynamicLandscapeGenerator : MonoBehaviour
{
    public int mapWidth = 256;
    public int mapHeight = 256;
    public float noiseScale = 20f;
    public int seed = 42;

    [Header("Generation Options")]
    public bool generateUrbanAreas = false;
    public bool generateBurnedAreas = false;

    [Header("Percentage Control")]
    public bool manualPercentageControl = true;

    [Range(0f, 100f)] public float waterPercentage = 20f;
    [Range(0f, 100f)] public float urbanPercentage = 10f;
    [Range(0f, 100f)] public float sparsePercentage = 10f;
    [Range(0f, 100f)] public float dryForestPercentage = 15f;
    [Range(0f, 100f)] public float wetForestPercentage = 15f;
    [Range(0f, 100f)] public float mixedForestPercentage = 10f;
    [Range(0f, 100f)] public float shrublandPercentage = 10f;
    [Range(0f, 100f)] public float grasslandPercentage = 5f;
    [Range(0f, 100f)] public float burnedPercentage = 3f;
    [Range(0f, 100f)] public float floodplainPercentage = 2f;

    public float waterElevationThreshold = 0.3f;

    public bool showElevation = true;
    public bool showFuelClassification = false;
    public bool showFuelDensity = false;
    public bool showTopography = false;

    public RawImage displayImage;

    private Texture2D elevationTexture;
    private Texture2D fuelClassificationTexture;
    private Texture2D fuelDensityTexture;
    private Texture2D topographyTexture;

    private int[,] fuelClassificationArray;

    private void Awake()
    {
        GenerateLandscape();
    }

    public Texture2D GetElevationTexture() => elevationTexture;
    public Texture2D GetFuelClassificationTexture() => fuelClassificationTexture;
    public int[,] GetFuelClassificationArray() => fuelClassificationArray;
    public Texture2D GetFuelDensityTexture() => fuelDensityTexture;

    public void GenerateLandscape()
    {
        Random.InitState(seed);

        if (!manualPercentageControl)
        {
            RandomizePercentages();
        }

        GenerateMaps();
        UpdateDisplay();
    }

    private void RandomizePercentages()
    {
        float[] randomValues = new float[9];
        float total = 0f;
        for (int i = 0; i < randomValues.Length; i++)
        {
            randomValues[i] = Random.Range(5f, 20f);
            total += randomValues[i];
        }
        urbanPercentage = randomValues[0] / total * 100f;
        sparsePercentage = randomValues[1] / total * 100f;
        dryForestPercentage = randomValues[2] / total * 100f;
        wetForestPercentage = randomValues[3] / total * 100f;
        mixedForestPercentage = randomValues[4] / total * 100f;
        shrublandPercentage = randomValues[5] / total * 100f;
        grasslandPercentage = randomValues[6] / total * 100f;
        burnedPercentage = randomValues[7] / total * 100f;
        floodplainPercentage = randomValues[8] / total * 100f;
    }

    private void GenerateMaps()
    {
        elevationTexture = GenerateElevationMap();
        fuelClassificationTexture = GenerateFuelClassificationMap();
        fuelDensityTexture = GenerateFuelDensityMap();
        topographyTexture = GenerateTopographyMap();
    }

    private Texture2D GenerateElevationMap()
    {
        Texture2D texture = new Texture2D(mapWidth, mapHeight);
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float xCoord = (float)x / mapWidth * noiseScale + seed * 0.01f;
                float yCoord = (float)y / mapHeight * noiseScale + seed * 0.01f;
                float elevation = Mathf.PerlinNoise(xCoord, yCoord) * 0.6f +
                                  Mathf.PerlinNoise(xCoord * 2f, yCoord * 2f) * 0.3f +
                                  Mathf.PerlinNoise(xCoord * 4f, yCoord * 4f) * 0.1f;
                elevation = Mathf.Clamp01(elevation);
                Color color = new Color(elevation, elevation, elevation);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    private Texture2D GenerateFuelClassificationMap()
    {
        Texture2D texture = new Texture2D(mapWidth, mapHeight);
        fuelClassificationArray = new int[mapWidth, mapHeight];
        float effectiveBurnedPercentage = generateBurnedAreas ? burnedPercentage : 0f;
        float totalNonWater = urbanPercentage + sparsePercentage + dryForestPercentage + wetForestPercentage +
                              mixedForestPercentage + shrublandPercentage + grasslandPercentage +
                              effectiveBurnedPercentage + floodplainPercentage;
        float urbanThreshold = urbanPercentage / totalNonWater;
        float sparseThreshold = urbanThreshold + sparsePercentage / totalNonWater;
        float dryForestThreshold = sparseThreshold + dryForestPercentage / totalNonWater;
        float wetForestThreshold = dryForestThreshold + wetForestPercentage / totalNonWater;
        float mixedForestThreshold = wetForestThreshold + mixedForestPercentage / totalNonWater;
        float shrublandThreshold = mixedForestThreshold + shrublandPercentage / totalNonWater;
        float grasslandThreshold = shrublandThreshold + grasslandPercentage / totalNonWater;
        float burnedThreshold = grasslandThreshold + (effectiveBurnedPercentage / totalNonWater);
        float floodplainThreshold = burnedThreshold + floodplainPercentage / totalNonWater;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float elevation = elevationTexture.GetPixel(x, y).r;
                float classificationNoise = Mathf.PerlinNoise(x / (float)mapWidth * noiseScale + seed * 0.01f,
                                                                y / (float)mapHeight * noiseScale + seed * 0.01f);
                int classificationType;
                Color color;
                if (elevation < waterElevationThreshold)
                {
                    classificationType = 0;
                    color = HexToColor("#0000FF");
                }
                else
                {
                    if (generateUrbanAreas && classificationNoise < urbanThreshold)
                    {
                        classificationType = 1;
                        color = HexToColor("#FF0000");
                    }
                    else if (classificationNoise < sparseThreshold)
                    {
                        classificationType = 2;
                        color = HexToColor("#808080");
                    }
                    else if (classificationNoise < dryForestThreshold)
                    {
                        classificationType = 3;
                        color = HexToColor("#88CC88");
                    }
                    else if (classificationNoise < wetForestThreshold)
                    {
                        classificationType = 4;
                        color = HexToColor("#006400");
                    }
                    else if (classificationNoise < mixedForestThreshold)
                    {
                        classificationType = 5;
                        color = HexToColor("#228B22");
                    }
                    else if (classificationNoise < shrublandThreshold)
                    {
                        classificationType = 6;
                        color = HexToColor("#C2B280");
                    }
                    else if (classificationNoise < grasslandThreshold)
                    {
                        classificationType = 7;
                        color = HexToColor("#F0E68C");
                    }
                    else if (classificationNoise < burnedThreshold)
                    {
                        classificationType = 8;
                        color = HexToColor("#800080");
                    }
                    else
                    {
                        classificationType = 9;
                        color = HexToColor("#00FFFF");
                    }
                }
                fuelClassificationArray[x, y] = classificationType;
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    private Texture2D GenerateFuelDensityMap()
    {
        Texture2D texture = new Texture2D(mapWidth, mapHeight);
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float densityNoise = Mathf.PerlinNoise((float)x / mapWidth * noiseScale * 2f + seed * 0.01f,
                                                       (float)y / mapHeight * noiseScale * 2f + seed * 0.01f);
                float density = densityNoise;
                int classificationType = fuelClassificationArray[x, y];
                if (classificationType == 0)
                    density = 0f;
                else if (classificationType == 1)
                    density *= 0.2f;
                else if (classificationType == 8)
                    density *= 0.1f;
                else if (classificationType == 9)
                    density *= 0.5f;
                Color color = new Color(density, density, density);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    private Texture2D GenerateTopographyMap()
    {
        Texture2D texture = new Texture2D(mapWidth, mapHeight);
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float elevation = elevationTexture.GetPixel(x, y).r;
                float contourInterval = 0.05f;
                float line = Mathf.Abs(elevation % contourInterval - (contourInterval / 2)) < 0.005f ? 1f : elevation;
                Color color = new Color(line, line, line);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    public void UpdateDisplay()
    {
        if (showElevation)
            displayImage.texture = elevationTexture;
        else if (showFuelClassification)
            displayImage.texture = fuelClassificationTexture;
        else if (showFuelDensity)
            displayImage.texture = fuelDensityTexture;
        else if (showTopography)
            displayImage.texture = topographyTexture;
    }

    public void ShowElevation() => SetLayerState(true, false, false, false);
    public void ShowFuelClassification() => SetLayerState(false, true, false, false);
    public void ShowFuelDensity() => SetLayerState(false, false, true, false);
    public void ShowTopography() => SetLayerState(false, false, false, true);

    private void SetLayerState(bool elevation, bool classification, bool density, bool topography)
    {
        showElevation = elevation;
        showFuelClassification = classification;
        showFuelDensity = density;
        showTopography = topography;
        UpdateDisplay();
    }

    private Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var color))
            return color;
        return Color.black;
    }

    public void SetPackingRatios(float shortGrass, float timber, float hardwood)
    {
        Debug.Log($"Packing Ratios Set: Short Grass = {shortGrass}, Timber = {timber}, Hardwood = {hardwood}");
    }
}
