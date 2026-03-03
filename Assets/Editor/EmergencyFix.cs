
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class EmergencyFix : Editor
{
    [MenuItem("Tools/EmergencyFix")]
    static void Fix()
    {
        string path = Application.dataPath + "/src/shotgun/ShotgunController.cs";
        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("behavior.Init") && lines[i].Length > 80)
            {
                int semi = lines[i].IndexOf(';');
                if (semi >= 0)
                {
                    lines[i] = lines[i].Substring(0, semi + 1);
                    Debug.Log("Corrigido: " + lines[i]);
                }
            }
        }

        File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset("Assets/src/shotgun/ShotgunController.cs");
        AssetDatabase.Refresh();
        Debug.Log("EmergencyFix done!");
    }
}
#endif
