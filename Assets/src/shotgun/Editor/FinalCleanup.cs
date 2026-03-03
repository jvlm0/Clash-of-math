
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class FinalCleanup : Editor
{
    [MenuItem("Tools/Shotgun/Final Cleanup")]
    static void Fix()
    {
        string path = Application.dataPath + "/src/shotgun/ShotgunController.cs";
        string content = File.ReadAllText(path);

        // Corrige o lixo na linha do Init
        content = content.Replace(
            "behavior.Init(firePoint.position, maxDistance, projectileLifetime);ifetime;",
            "behavior.Init(firePoint.position, maxDistance, projectileLifetime);"
        );

        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset("Assets/src/shotgun/ShotgunController.cs");
        AssetDatabase.Refresh();
        Debug.Log("Limpeza final concluida!");
    }
}
#endif
