using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Vecino que bota basura a la calle.
/// Cada cierto rato deja caer un residuo de su bolsa mientras camina por la
/// vereda. Es el punto del juego que más se parece al problema real: aunque el
/// Guardián limpie, si la gente no segrega en la fuente la calle se vuelve a
/// ensuciar. El GameManager limita cuántos residuos extra pueden aparecer por
/// nivel para que el nivel siga siendo ganable.
/// </summary>
public class Ensuciador : MonoBehaviour
{
    public List<TrashItem> bolsa = new List<TrashItem>();
    public float cadaMin = 18f;
    public float cadaMax = 32f;

    private float reloj;

    /// <summary>
    /// Ventana para alcanzarlo despues de que bota algo.
    ///
    /// Antes el vecino tiraba su basura y seguia caminando como si nada: el
    /// jugador se enteraba por un aviso en pantalla y encontraba una botella en
    /// el piso, sin nadie a quien reclamarle. Ahora, si lo alcanzas en los
    /// primeros segundos, el vecino se avergüenza y recoge lo que boto. Es la
    /// unica parte del juego donde limpiar no es recoger, sino evitar que
    /// ensucien: que es de lo que trata de verdad la segregacion en la fuente.
    /// </summary>
    public float ventanaAlcance = 11f;
    public float radioAlcance = 3.6f;

    private TrashItem ultimo;
    private float alcanzableHasta;
    private Transform jugador;
    private float proximaBusqueda;

    void Start()
    {
        reloj = Random.Range(10f, 22f);
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.estado != GameManager.Estado.Jugando) return;
        if (bolsa == null || bolsa.Count == 0) return;

        Recuperar();

        reloj -= Time.deltaTime;
        if (reloj > 0f) return;
        reloj = Random.Range(cadaMin, cadaMax);

        Tirar();
    }

    /// <summary>Si el Guardian lo alcanza a tiempo, el vecino recoge lo suyo.</summary>
    private void Recuperar()
    {
        if (ultimo == null || Time.time > alcanzableHasta) return;
        if (!ultimo.gameObject.activeSelf) { ultimo = null; return; }

        if (jugador == null)
        {
            if (Time.time < proximaBusqueda) return;
            proximaBusqueda = Time.time + 0.5f;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            jugador = p.transform;
        }

        Vector3 d = jugador.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > radioAlcance * radioAlcance) return;

        GameManager.Instance.VecinoRecogeLoSuyo(ultimo);
        GuardianFX.Efecto(ultimo.transform.position + Vector3.up * 0.6f,
                          new Color(0.45f, 1f, 0.66f), 420f);
        ultimo = null;
    }

    private void Tirar()
    {
        for (int i = 0; i < bolsa.Count; i++)
        {
            TrashItem b = bolsa[i];
            if (b == null || b.gameObject.activeSelf) continue;

            // Lo deja caer a su costado, sobre la vereda, no encima del jugador.
            Vector3 lado = Vector3.Cross(Vector3.up, transform.forward);
            Vector3 p = transform.position
                      + transform.forward * Random.Range(-0.4f, 0.6f)
                      + lado * (Random.value < 0.5f ? -0.8f : 0.8f);
            p.y = transform.position.y + 0.18f;

            if (GameManager.Instance.TirarBasura(b, p))
            {
                GuardianFX.Efecto(p + Vector3.up * 0.7f, new Color(1f, 0.64f, 0.30f), 300f);
                ultimo = b;
                alcanzableHasta = Time.time + ventanaAlcance;
            }
            return;
        }
    }
}
