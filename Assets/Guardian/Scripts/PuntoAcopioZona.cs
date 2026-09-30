using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Zona de entrega del punto de acopio municipal.
/// Cuando la zona ya quedó limpia, el nivel pide el último paso del ciclo real:
/// llevar lo recogido al punto de acopio. Al entrar aquí se cierra la jornada.
/// Antes de eso no hace nada: el acopio es solo parte del paisaje.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PuntoAcopioZona : MonoBehaviour
{
    public int zona = 0;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.esperandoAcopio) return;

        GuardianFX.Efecto(transform.position + Vector3.up * 2.2f,
                          new Color(0.50f, 1f, 0.75f), 880f);
        gm.CerrarJornada();
    }
}
