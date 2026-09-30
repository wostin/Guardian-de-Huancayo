using UnityEngine;
using TMPro;

/// <summary>
/// Guardián de Huancayo - HUD opcional con TextMeshPro (para armar tu propio Canvas).
/// El prototipo jugable ya usa GuardianCameraHUD; este script queda disponible
/// si prefieres una interfaz con Canvas y TextMeshPro.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Referencias UI (TextMeshPro)")]
    public TMP_Text textoPuntaje;
    public TMP_Text textoCarga;
    public TMP_Text textoRecicladas;
    public GameObject panelVictoria;

    void Start()
    {
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.OnCambio += Actualizar;
        Actualizar();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCambio -= Actualizar;
    }

    void Actualizar()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (textoPuntaje != null) textoPuntaje.text = "Puntaje: " + gm.puntaje;
        if (textoCarga != null) textoCarga.text = "Carga: " + gm.cargaActual + " / " + gm.capacidadCarga;
        if (textoRecicladas != null) textoRecicladas.text = "Recicladas: " + gm.recicladas + " / " + gm.basuraTotal;

        if (panelVictoria != null)
            panelVictoria.SetActive(gm.estado == GameManager.Estado.Ganado);
    }
}
