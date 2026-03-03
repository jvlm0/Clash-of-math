
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class ShotgunAssetFixer : Editor
{
    [MenuItem("Tools/Shotgun/Fix Shotgun Assets")]
    static void FixAssets()
    {
        FixProjectile();
        FixMuzzleFlash();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("✅ Shotgun assets corrigidos com sucesso!");
    }

    static void FixProjectile()
    {
        string prefabPath = "Assets/src/shotgun/SimpleProjectilePrefab.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogError("Prefab do projétil não encontrado!"); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            // --- Material do projétil ---
            Renderer rend = root.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = CreateOrLoadMaterial(
                    "Assets/src/shotgun/ProjectileMat.mat",
                    new Color(1f, 0.65f, 0.1f), // laranja
                    emissive: new Color(1f, 0.4f, 0f) * 2f
                );
                rend.sharedMaterial = mat;
            }

            // --- TrailRenderer ---
            TrailRenderer trail = root.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.time = 0.25f;
                trail.startWidth = 0.06f;
                trail.endWidth = 0f;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;

                Material trailMat = CreateOrLoadMaterial(
                    "Assets/src/shotgun/TrailMat.mat",
                    new Color(1f, 0.5f, 0.0f, 1f),
                    emissive: new Color(1f, 0.3f, 0f) * 3f
                );
                trail.sharedMaterial = trailMat;

                // Gradiente do trail — some gradualmente
                Gradient g = new Gradient();
                g.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0f),
                        new GradientColorKey(new Color(1f, 0.2f, 0f), 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                trail.colorGradient = g;
            }

            // --- Light: Point, fraca, só ilumina área próxima ---
            Light light = root.GetComponent<Light>();
            if (light != null)
            {
                light.type = LightType.Point;
                light.color = new Color(1f, 0.6f, 0.1f);
                light.intensity = 1.5f;   // bem fraco
                light.range = 1.5f;       // raio pequeno
                light.shadows = LightShadows.None; // sem sombras = sem piscar
                light.renderMode = LightRenderMode.ForceVertex; // vertex = não afeta bloom/tela
            }
        }

        Debug.Log("Projétil corrigido.");
    }

    static void FixMuzzleFlash()
    {
        string prefabPath = "Assets/src/shotgun/MuzzleFlash.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogError("MuzzleFlash prefab não encontrado!"); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            ParticleSystem ps = root.GetComponent<ParticleSystem>();
            if (ps == null) return;

            // Main
            var main = ps.main;
            main.duration = 0.08f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.95f, 0.5f, 1f),
                new Color(1f, 0.5f, 0.1f, 0.8f)
            );
            main.maxParticles = 25;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Emission — burst instantâneo
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 12, 18)
            });

            // Shape — cone frontal curto
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.03f;

            // Color over lifetime
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient cg = new Gradient();
            cg.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 1f, 0.7f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(cg);

            // Size over lifetime
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sc = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

            // Material das partículas
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                Material pMat = CreateOrLoadMaterial(
                    "Assets/src/shotgun/MuzzleFlashMat.mat",
                    new Color(1f, 0.7f, 0.2f),
                    emissive: new Color(1f, 0.5f, 0f) * 2f,
                    transparent: true
                );
                renderer.sharedMaterial = pMat;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        Debug.Log("MuzzleFlash corrigido.");
    }

    static Material CreateOrLoadMaterial(string path, Color baseColor, Color emissive = default, bool transparent = false)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            // Detecta o shader correto (URP ou Built-in)
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = baseColor;

        // Emissive
        if (emissive != default)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissive);
        }

        // Transparência para partículas
        if (transparent)
        {
            if (mat.HasProperty("_Surface"))
            {
                // URP
                mat.SetFloat("_Surface", 1f); // Transparent
                mat.SetFloat("_Blend", 0f);   // Alpha
                mat.renderQueue = 3000;
            }
            else
            {
                // Built-in
                mat.SetFloat("_Mode", 2f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
            }
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }
}
#endif
