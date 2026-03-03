
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MuzzleFlashSetup : Editor
{
    [MenuItem("Tools/Shotgun/Setup MuzzleFlash Prefab")]
    static void SetupMuzzleFlash()
    {
        // Encontra o GameObject MuzzleFlash na cena
        GameObject go = GameObject.Find("MuzzleFlash");
        if (go == null)
        {
            Debug.LogError("MuzzleFlash GameObject not found in scene!");
            return;
        }

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            ps = go.AddComponent<ParticleSystem>();
        }

        // Main module
        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.startLifetime = 0.12f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.9f, 0.3f, 1f),
            new Color(1f, 0.4f, 0.0f, 1f)
        );
        main.maxParticles = 20;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emission
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 15, 20)
        });

        // Shape - cone curto
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.05f;

        // Color over lifetime - fade out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.5f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Size over lifetime - shrink
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Renderer
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Salva o prefab
        string path = "Assets/src/shotgun/MuzzleFlash.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        AssetDatabase.Refresh();

        Debug.Log("MuzzleFlash prefab salvo em: " + path);
    }
}
#endif
