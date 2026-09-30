using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Mapa de la calzada, listo para usar en tiempo de juego.
/// La herramienta del editor ya calcula qué tramos de vía hay y hacia dónde corre
/// cada uno; aquí se guarda ese resultado para poder preguntar, mientras se juega,
/// si el Guardián está parado sobre el asfalto. Sin esto habría que adivinarlo con
/// raycasts contra la malla de la ciudad, que no distingue pista de vereda.
/// </summary>
public class RedVial : MonoBehaviour
{
    public static RedVial Instancia { get; private set; }

    /// <summary>Centro de cada tramo de vía.</summary>
    public List<Vector3> centros = new List<Vector3>();

    /// <summary>Orientación de cada tramo: bit 0 = corre en X, bit 1 = corre en Z.</summary>
    public List<int> ejes = new List<int>();

    public float medioTramo = 10f;
    public float mediaCalzada = 7f;

    void Awake() { Instancia = this; }
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    /// <summary>¿Este punto cae sobre el asfalto?</summary>
    public bool SobreLaPista(Vector3 p)
    {
        int n = Mathf.Min(centros.Count, ejes.Count);
        for (int i = 0; i < n; i++)
        {
            float dx = Mathf.Abs(p.x - centros[i].x);
            float dz = Mathf.Abs(p.z - centros[i].z);
            if (dx > medioTramo || dz > medioTramo) continue;

            bool ejeX = (ejes[i] & 1) != 0;
            bool ejeZ = (ejes[i] & 2) != 0;
            if (ejeX && dz < mediaCalzada) return true;
            if (ejeZ && dx < mediaCalzada) return true;
        }
        return false;
    }
}
