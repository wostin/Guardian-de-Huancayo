using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Contenedor de segregación (NTP 900.058-2019).
/// Cada contenedor ACEPTA UN SOLO TIPO de residuo. Si el Guardián llega con
/// residuos de ese tipo los deposita (puntaje + baja la contaminación); si trae
/// otra cosa, el contenedor le avisa cuál es su color y no acepta nada.
/// El error solo se cuenta al ENTRAR, no mientras se queda parado al lado.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RecycleBin : MonoBehaviour
{
    public TipoResiduo acepta = TipoResiduo.Plastico;
    public int zona = 0;                      // a qué zona/nivel pertenece

    private float siguienteIntento;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        siguienteIntento = Time.unscaledTime + 0.7f;
        Depositar(true);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;
        if (Time.unscaledTime < siguienteIntento) return;

        siguienteIntento = Time.unscaledTime + 0.7f;

        // Si no lleva nada de este tipo, no se hace nada (y NO se penaliza por
        // quedarse parado junto al contenedor).
        if (GameManager.Instance.CuantosLlevo(acepta) <= 0) return;
        Depositar(false);
    }

    private void Depositar(bool contarError)
    {
        int llevaba = GameManager.Instance.cargaActual;
        int antes = GameManager.Instance.puntaje;
        int n = GameManager.Instance.Depositar(acepta, contarError);

        if (n > 0)
        {
            GuardianFX.Efecto(transform.position + Vector3.up, Residuo.Tinte(acepta), 990f);
            GuardianEventos.AvisarDeposito(transform.position, acepta, n,
                                           GameManager.Instance.puntaje - antes);
            GuardianAnimaciones.Rebote(transform, 0.22f, 0.55f);     // squash & stretch
        }
        else if (contarError && llevaba > 0)
        {
            // Tono grave = te equivocaste de color.
            GuardianFX.Efecto(transform.position + Vector3.up, new Color(1f, 0.42f, 0.36f), 200f);
            GuardianEventos.AvisarError(transform.position, acepta);
            GuardianAnimaciones.Temblor(transform, 0.16f, 0.45f);    // "no"
        }
    }
}
