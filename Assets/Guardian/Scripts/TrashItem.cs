using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Residuo recolectable (ODS 11).
/// Cada residuo tiene un TIPO (plástico, vidrio, papel, metal, orgánico) y solo
/// puede depositarse en el contenedor del color que le corresponde según la
/// NTP 900.058-2019. Al tocarlo el jugador se lo lleva en la mochila.
/// Se registra solo en el GameManager para el conteo, la contaminación y el
/// reinicio de nivel.
/// </summary>
public class TrashItem : MonoBehaviour
{
    public int valor = 10;
    public int zona = 0;                                  // a qué zona/nivel pertenece
    public TipoResiduo tipo = TipoResiduo.Plastico;       // a qué contenedor va

    /// <summary>
    /// Residuo de RESERVA: no aparece al empezar el nivel. Lo van botando los
    /// vecinos (Ensuciador) mientras se juega, para mostrar que la basura no se
    /// acaba sola si nadie segrega en la fuente.
    /// </summary>
    public bool deReserva = false;

    private float avisoLlena;

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegistrarBasura(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.RecogerBasura(tipo))
        {
            GuardianFX.Efecto(transform.position, Residuo.Tinte(tipo), 660f);
            GuardianEventos.AvisarRecogio(transform.position, tipo);
            // Vuela en arco a la mochila y DESPUÉS se desactiva (no se destruye:
            // así el nivel se puede reiniciar).
            GuardianAnimaciones.RecogerResiduo(gameObject);
        }
        else if (GameManager.Instance.estado == GameManager.Estado.Jugando && Time.time > avisoLlena)
        {
            avisoLlena = Time.time + 2.5f;
            GuardianEventos.AvisarMochilaLlena(other.transform.position);
        }
    }
}
