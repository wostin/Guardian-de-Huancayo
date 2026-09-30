using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Camión recolector de la municipalidad.
/// Circula por la zona como cualquier vehículo, pero cuando pasa al lado de un
/// contenedor lo "recoge": suena y suelta un destello del color de ese residuo.
/// Sirve para cerrar la idea: lo que el Guardián segrega no se queda ahí, se lo
/// lleva el servicio de limpieza al punto de acopio. Si estuviera todo mezclado,
/// el camión no podría separarlo.
/// </summary>
public class CamionRecolector : MonoBehaviour
{
    public float radio = 6.5f;
    public float cadaSegundos = 1.2f;

    private float proximo;
    private bool avisado;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.estado != GameManager.Estado.Jugando) return;
        if (Time.time < proximo) return;
        proximo = Time.time + cadaSegundos;

        RecycleBin[] botes = FindObjectsByType<RecycleBin>(FindObjectsSortMode.None);
        float r2 = radio * radio;

        for (int i = 0; i < botes.Length; i++)
        {
            RecycleBin b = botes[i];
            if (b == null || !b.gameObject.activeInHierarchy) continue;

            Vector3 d = b.transform.position - transform.position;
            if (d.sqrMagnitude > r2) continue;

            GuardianFX.Efecto(b.transform.position + Vector3.up * 1.7f,
                              Residuo.Tinte(b.acepta), 430f);

            if (!avisado)
            {
                avisado = true;
                gm.AvisoPublico(
                    "El camión municipal se lleva lo que segregaste al punto de acopio",
                    new Color(0.55f, 0.90f, 1f), 4.5f);
            }

            proximo = Time.time + 7f;   // no lo repite en cada contenedor de la cuadra
            return;
        }
    }
}
