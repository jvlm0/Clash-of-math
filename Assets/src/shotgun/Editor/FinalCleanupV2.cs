
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

public class FinalCleanupV2 : Editor
{
    [MenuItem("Tools/Shotgun/Final Cleanup V2")]
    static void Fix()
    {
        string path = Application.dataPath + "/src/shotgun/ShotgunController.cs";
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            // Linha que tem o lixo
            if (lines[i].Contains("behavior.Init") && lines[i].Contains(";ifetime;"))
            {
                // Pega tudo ate o primeiro ; e descarta o resto
                int idx = lines[i].IndexOf(';');
                lines[i] = lines[i].Substring(0, idx + 1);
                Debug.Log("Linha corrigida: " + lines[i]);
            }
        }

        File.WriteAllLines(path, lines);
        AssetDatabase.ImportAsset("Assets/src/shotgun/ShotgunController.cs");
        AssetDatabase.Refresh();
        Debug.Log("Cleanup V2 concluido!");
    }
}
#endif
