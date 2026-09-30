using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Basura flotando en el río Shullcas.
/// La cantidad que se ve depende de la contaminación de la ciudad: si nadie
/// recoge, el río baja lleno de botellas y bolsas; conforme el Guardián segrega
/// bien, el agua se va limpiando. Es el vínculo visual entre lo que pasa en la
/// calle y lo que termina en el agua — el corazón del problema del ODS 11 acá.
/// </summary>
public class BasuraEnElRio : MonoBehaviour
{
    public List<Transform> flotantes = new List<Transform>();

    /// <summary>
    /// Marcado cuando lo que baja por el rio son residuos que el Guardian puede
    /// sacar del agua, y no solo decorado. Es el nivel de la Ribera: el Shullcas
    /// da nombre al juego, y hasta ahora se miraba pero no se limpiaba.
    /// </summary>
    public bool recogibles = true;
    public float largo = 140f;        // tramo que recorre antes de reaparecer
    public float velocidad = 2.2f;    // la corriente del Shullcas es rápida

    private Vector3[] inicio;
    private float[] fase;
    private int visiblesAhora = -1;

    void Start()
    {
        inicio = new Vector3[flotantes.Count];
        fase   = new float[flotantes.Count];
        for (int i = 0; i < flotantes.Count; i++)
        {
            if (flotantes[i] == null) continue;
            inicio[i] = flotantes[i].localPosition;
            fase[i] = Random.value;
        }
    }

    void Update()
    {
        if (flotantes == null || flotantes.Count == 0) return;

        GameManager gm = GameManager.Instance;
        float f = 0.3f;
        if (gm != null && gm.maxContaminacion > 0f)
            f = Mathf.Clamp01(gm.contaminacion / gm.maxContaminacion);

        // Siempre baja algo (el río nunca está impecable), pero mucho más si la
        // ciudad está sucia.
        int visibles = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, flotantes.Count, f)),
                                   1, flotantes.Count);

        // Si lo que flota son residuos RECOGIBLES, quien manda sobre su visibilidad
        // es el GameManager (nivel, reinicio, ya recogido). Que este script los
        // encendiera y apagara segun la contaminacion haria reaparecer basura que
        // el jugador ya saco del agua, que es lo contrario de lo que se quiere
        // ensenar. Solo se ocupa de moverlos con la corriente.
        if (!recogibles && visibles != visiblesAhora)
        {
            visiblesAhora = visibles;
            for (int i = 0; i < flotantes.Count; i++)
                if (flotantes[i] != null) flotantes[i].gameObject.SetActive(i < visibles);
        }

        float paso = Time.deltaTime * velocidad / Mathf.Max(1f, largo);

        for (int i = 0; i < flotantes.Count; i++)
        {
            Transform t = flotantes[i];
            if (t == null || !t.gameObject.activeSelf) continue;

            fase[i] += paso;
            if (fase[i] > 1f) fase[i] -= 1f;

            Vector3 p = inicio[i];
            p.x = (fase[i] - 0.5f) * largo;                                  // el río corre en X
            p.y = inicio[i].y + Mathf.Sin(Time.time * 1.6f + i * 1.3f) * 0.05f;
            t.localPosition = p;
            t.localRotation = Quaternion.Euler(0f, (Time.time * 24f + i * 47f) % 360f, 0f);
        }
    }
}
