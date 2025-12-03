#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CrackMaskGenerator : EditorWindow
{
    private int textureSize = 512;
    private int crackCount = 5;
    private float crackThickness = 0.05f;
    private float branchProbability = 0.3f;
    
    [MenuItem("Tools/Generate Crack Mask")]
    static void Init()
    {
        CrackMaskGenerator window = (CrackMaskGenerator)EditorWindow.GetWindow(typeof(CrackMaskGenerator));
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Crack Mask Generator", EditorStyles.boldLabel);
        
        textureSize = EditorGUILayout.IntField("Texture Size", textureSize);
        crackCount = EditorGUILayout.IntSlider("Crack Count", crackCount, 1, 20);
        crackThickness = EditorGUILayout.Slider("Thickness", crackThickness, 0.01f, 0.2f);
        branchProbability = EditorGUILayout.Slider("Branch Probability", branchProbability, 0f, 1f);
        
        if (GUILayout.Button("Generate Crack Mask"))
        {
            GenerateCrackTexture();
        }
    }
    
    void GenerateCrackTexture()
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[textureSize * textureSize];
        
        // Inicializa com preto
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.black;
        
        // Gera fendas
        for (int i = 0; i < crackCount; i++)
        {
            Vector2 start = new Vector2(Random.Range(0, textureSize), Random.Range(0, textureSize));
            Vector2 direction = Random.insideUnitCircle.normalized;
            
            DrawCrack(pixels, start, direction, Random.Range(50, 200), 0);
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Salva como asset
        string path = EditorUtility.SaveFilePanelInProject("Save Crack Mask", "CrackMask", "png", "Save crack mask texture");
        if (!string.IsNullOrEmpty(path))
        {
            byte[] pngData = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
            AssetDatabase.Refresh();
            Debug.Log("Crack mask saved to: " + path);
        }
    }
    
    void DrawCrack(Color[] pixels, Vector2 pos, Vector2 dir, int length, int depth)
    {
        if (depth > 3 || length <= 0) return;
        
        for (int i = 0; i < length; i++)
        {
            // Adiciona variação aleatória
            dir += Random.insideUnitCircle * 0.1f;
            dir.Normalize();
            
            pos += dir * 2f;
            
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);
            
            if (x >= 0 && x < textureSize && y >= 0 && y < textureSize)
            {
                int radius = Mathf.RoundToInt(crackThickness * textureSize);
                
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int px = x + dx;
                        int py = y + dy;
                        
                        if (px >= 0 && px < textureSize && py >= 0 && py < textureSize)
                        {
                            float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                            if (dist <= 1f)
                            {
                                int idx = py * textureSize + px;
                                float intensity = 1f - dist;
                                pixels[idx].r = Mathf.Max(pixels[idx].r, intensity); // Depth
                                pixels[idx].g = Mathf.Max(pixels[idx].g, intensity * 0.5f); // Edge
                            }
                        }
                    }
                }
            }
            
            // Chance de criar ramificação
            if (Random.value < branchProbability * 0.1f)
            {
                Vector2 branchDir = Quaternion.Euler(0, 0, Random.Range(-60f, 60f)) * dir;
                DrawCrack(pixels, pos, branchDir, length / 2, depth + 1);
            }
        }
    }
}
#endif