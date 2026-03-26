using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Disintegration/Graph")]
public class DisintegrationGraph : ScriptableObject
{
    [System.Serializable]
    public class NamedGroup
    {
        public string groupName;

        [Tooltip("Nomes exatos dos renderers que pertencem a este grupo")]
        public List<string> rendererNames = new List<string>();
    }

    [Header("Centros das partes (calculado automaticamente)")]
    public List<Vector3> partCenters = new List<Vector3>();

    [Header("Grupos anatômicos definidos manualmente")]
    public List<NamedGroup> namedGroups = new List<NamedGroup>();

    [System.Serializable]
    public class IntList
    {
        public List<int> indices = new List<int>();
    }

    public List<IntList> adjacency = new List<IntList>();
    public bool isBuilt = false;

    public bool HasNamedGroups => namedGroups != null && namedGroups.Count > 0;

    public void AddNeighbor(int from, int to)
    {
        while (adjacency.Count <= Mathf.Max(from, to))
            adjacency.Add(new IntList());
        if (!adjacency[from].indices.Contains(to))
            adjacency[from].indices.Add(to);
        if (!adjacency[to].indices.Contains(from))
            adjacency[to].indices.Add(from);
    }

    public List<int> GetNeighbors(int index)
    {
        if (index < 0 || index >= adjacency.Count)
            return new List<int>();
        return adjacency[index].indices;
    }

    public void Reset()
    {
        adjacency.Clear();
        namedGroups.Clear();
        isBuilt = false;
    }
}
