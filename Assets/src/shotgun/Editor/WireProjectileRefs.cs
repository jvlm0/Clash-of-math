
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class WireProjectileRefs : Editor
{
    [MenuItem("Tools/Shotgun/Wire Projectile References")]
    static void Wire()
    {
        string path = "Assets/src/shotgun/SimpleProjectilePrefab.prefab";
        using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject root = scope.prefabContentsRoot;

            SimpleProjectile sp = root.GetComponent<SimpleProjectile>();
            TrailRenderer trail = root.GetComponent<TrailRenderer>();
            Light light = root.GetComponent<Light>();

            if (sp != null)
            {
                sp.trail = trail;
                sp.projectileLight = light;
                EditorUtility.SetDirty(root);
                Debug.Log("✅ SimpleProjectile: trail e light vinculados.");
            }
            else
            {
                Debug.LogError("SimpleProjectile não encontrado no prefab!");
            }
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
