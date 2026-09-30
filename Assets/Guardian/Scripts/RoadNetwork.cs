using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Guardián de Huancayo - Red de carreteras.
/// Recolecta las posiciones de los tramos de vía del pack SimplePoly
/// (objetos "Road Lane / Road Intersection / Road Corner / Road T...") para que
/// los carros contaminantes circulen por las calles y no por el césped.
/// </summary>
public class RoadNetwork : MonoBehaviour
{
    public static RoadNetwork Instance { get; private set; }
    private readonly List<Vector3> puntos = new List<Vector3>();

    public static RoadNetwork Obtener()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("RoadNetwork");
        return go.AddComponent<RoadNetwork>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Recolectar();
    }

    void Recolectar()
    {
        puntos.Clear();
        Transform[] todos = FindObjectsOfType<Transform>();
        foreach (Transform t in todos)
        {
            string n = t.name;
            if (n.StartsWith("Road Lane") || n.StartsWith("Road Intersection")
                || n.StartsWith("Road Corner") || n.StartsWith("Road T"))
                puntos.Add(t.position);
        }
    }

    public bool HayPuntos => puntos.Count > 0;

    public Vector3 MasCercano(Vector3 desde)
    {
        Vector3 mejor = desde;
        float d = float.MaxValue;
        for (int i = 0; i < puntos.Count; i++)
        {
            float dd = (puntos[i] - desde).sqrMagnitude;
            if (dd < d) { d = dd; mejor = puntos[i]; }
        }
        return mejor;
    }

    /// <summary>
    /// El tramo de vía más cercano al actual pero DISTINTO del actual y del anterior
    /// (para avanzar por la calle sin quedarse quieto ni volver atrás).
    /// </summary>
    public Vector3 Siguiente(Vector3 actual, Vector3 anterior)
    {
        Vector3 mejor = actual;
        float d = float.MaxValue;
        bool hay = false;

        // Preferir el más cercano que no sea el actual ni el anterior.
        for (int i = 0; i < puntos.Count; i++)
        {
            if ((puntos[i] - actual).sqrMagnitude < 1f) continue;
            if ((puntos[i] - anterior).sqrMagnitude < 1f) continue;
            float dd = (puntos[i] - actual).sqrMagnitude;
            if (dd < d) { d = dd; mejor = puntos[i]; hay = true; }
        }

        // Si solo quedaba el anterior, permitirlo (para no quedarse pegado).
        if (!hay)
        {
            for (int i = 0; i < puntos.Count; i++)
            {
                if ((puntos[i] - actual).sqrMagnitude < 1f) continue;
                float dd = (puntos[i] - actual).sqrMagnitude;
                if (dd < d) { d = dd; mejor = puntos[i]; }
            }
        }
        return mejor;
    }
}
