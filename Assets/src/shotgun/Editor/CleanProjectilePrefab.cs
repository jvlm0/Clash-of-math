
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CleanProjectilePrefab : Editor
{
    [MenuItem("Tools/Shotgun/Clean Projectile Prefab")]
    static void Clean()
    {
        string path = "Assets/src/shotgun/SimpleProjectilePrefab.prefab";

        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;

            // Remove todos os componentes invalidos (missing scripts)
            var allComponents = root.GetComponents<Component>();
            int removedCount = 0;
            foreach (var comp in allComponents)
            {
                if (comp == null)
                {
                    // Usa SerializedObject para remover missing scripts
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
                    removedCount++;
                    break;
                }
            }
            Debug.Log($"Missing scripts removidos: {removedCount > 0}");

            // Remove ProjectileBehavior do prefab — o ShotgunController adiciona em runtime
            var pb = root.GetComponent<ProjectileBehavior>();
            if (pb != null)
            {
                DestroyImmediate(pb);
                Debug.Log("ProjectileBehavior removido do prefab (adicionado em runtime pelo controller).");
            }

            // Garante SimpleProjectile e vincula trail/light
            SimpleProjectile sp = root.GetComponent<SimpleProjectile>();
            if (sp == null)
            {
                sp = root.AddComponent<SimpleProjectile>();
                Debug.Log("SimpleProjectile adicionado.");
            }

            TrailRenderer trail = root.GetComponent<TrailRenderer>();
            Light light = root.GetComponent<Light>();

            sp.trail = trail;
            sp.projectileLight = light;

            EditorUtility.SetDirty(root);
            Debug.Log($"Trail vinculado: {trail != null} | Light vinculada: {light != null}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Prefab limpo e salvo com sucesso!");
    }
}
#endif
