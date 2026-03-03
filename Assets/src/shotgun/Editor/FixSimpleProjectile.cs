
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class FixSimpleProjectile : Editor
{
    [MenuItem("Tools/Shotgun/Fix SimpleProjectile Script")]
    static void Fix()
    {
        string path = Application.dataPath + "/src/shotgun/SimpleProjectile.cs";

        string content = @"using UnityEngine;

/// <summary>
/// Script do prefab do projetil da shotgun.
/// Pega TrailRenderer e Light automaticamente via GetComponent.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimpleProjectile : MonoBehaviour
{
    [Header(""Configuracoes Visuais"")]
    public TrailRenderer trail;
    public Light projectileLight;

    void Awake()
    {
        // Auto-pega os componentes se nao foram vinculados no Inspector
        if (trail == null)
            trail = GetComponent<TrailRenderer>();
        if (projectileLight == null)
            projectileLight = GetComponent<Light>();
    }

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (trail != null)
        {
            trail.time = 0.25f;
            trail.startWidth = 0.06f;
            trail.endWidth = 0f;
        }
    }
}
";
        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset("Assets/src/shotgun/SimpleProjectile.cs");
        AssetDatabase.Refresh();
        Debug.Log("SimpleProjectile.cs reescrito com sucesso!");
    }
}
#endif
