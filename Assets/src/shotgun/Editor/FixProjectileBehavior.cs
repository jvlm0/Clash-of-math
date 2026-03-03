
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class FixProjectileBehavior : Editor
{
    [MenuItem("Tools/Shotgun/Fix ProjectileBehavior Distance")]
    static void Fix()
    {
        string path = Application.dataPath + "/src/shotgun/ShotgunController.cs";
        string content = File.ReadAllText(path);

        // Substitui o Update() do ProjectileBehavior para usar acumulo incremental
        string oldUpdate =
@"    void Update()
    {
        // Calcula distância percorrida
        distanceTraveled = Vector3.Distance(startPosition, transform.position);";

        string newUpdate =
@"    void Update()
    {
        // Acumula distância percorrida incrementalmente (funciona mesmo com gravidade/curvas)
        distanceTraveled += Vector3.Distance(lastPosition, transform.position);
        lastPosition = transform.position;";

        if (!content.Contains(oldUpdate))
        {
            Debug.LogError("Trecho alvo não encontrado! O arquivo pode já ter sido corrigido ou mudou.");
            return;
        }

        content = content.Replace(oldUpdate, newUpdate);

        // Adiciona lastPosition ao bloco de campos privados do ProjectileBehavior
        string oldFields = "    private float distanceTraveled = 0f;\n";
        string newFields = "    private float distanceTraveled = 0f;\n    private Vector3 lastPosition;\n";

        if (content.Contains(oldFields) && !content.Contains("private Vector3 lastPosition"))
            content = content.Replace(oldFields, newFields);

        // Adiciona Awake para capturar posição inicial com segurança
        string oldAwake =
@"    void Awake()
    {
        lastPosition = transform.position;
        startPosition = transform.position;
    }";

        if (!content.Contains("void Awake()"))
        {
            // Insere Awake antes do Update do ProjectileBehavior
            content = content.Replace(
                "    void Update()\n    {\n        // Acumula",
                oldAwake + "\n\n    void Update()\n    {\n        // Acumula"
            );
        }

        File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        AssetDatabase.ImportAsset("Assets/src/shotgun/ShotgunController.cs");
        AssetDatabase.Refresh();
        Debug.Log("✅ ProjectileBehavior corrigido: distância agora usa acúmulo incremental com Awake.");
    }
}
#endif
