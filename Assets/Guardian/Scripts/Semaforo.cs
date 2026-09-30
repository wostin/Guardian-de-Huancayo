using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Semáforo funcional de tres luces.
/// Cicla VERDE → ÁMBAR → ROJO con focos emisivos que se ven de lejos, y lleva
/// debajo una luz peatonal: cuando los carros tienen rojo, el peatón tiene verde.
/// Los vehículos del tránsito (Contaminante) lo obedecen y frenan antes del cruce.
/// La cabeza se arma sola al empezar, así funciona sobre cualquier poste del pack
/// sin tener que tocar sus prefabs.
/// </summary>
public class Semaforo : MonoBehaviour
{
    public enum Luz { Verde, Ambar, Rojo }

    [Header("Duración de cada fase (segundos)")]
    public float tiempoVerde = 9f;
    public float tiempoAmbar = 2.2f;
    public float tiempoRojo  = 7f;

    [Header("Altura del foco verde sobre el poste")]
    public float altura = 4.3f;

    public Luz estado { get; private set; }

    /// <summary>Rojo para los carros.</summary>
    public bool EnRojo { get { return estado == Luz.Rojo; } }
    /// <summary>Ámbar: los carros ya deben ir frenando.</summary>
    public bool EnAmbar { get { return estado == Luz.Ambar; } }
    /// <summary>Verde para el peatón = rojo para los carros.</summary>
    public bool PasoPeatonal { get { return estado == Luz.Rojo; } }

    private Renderer focoRojo, focoAmbar, focoVerde, focoPeaton;
    private float t;

    private static readonly Color APAGADO = new Color(0.13f, 0.13f, 0.14f);

    void Start()
    {
        Armar();
        estado = Luz.Verde;
        // Desfasar los cruces para que no cambien todos a la vez.
        t = tiempoVerde + Random.Range(0f, tiempoVerde);
        Pintar();
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.estado != GameManager.Estado.Jugando) return;

        t -= Time.deltaTime;
        if (t > 0f) return;

        switch (estado)
        {
            case Luz.Verde: estado = Luz.Ambar; t = tiempoAmbar; break;
            case Luz.Ambar: estado = Luz.Rojo;  t = tiempoRojo;  break;
            default:        estado = Luz.Verde; t = tiempoVerde; break;
        }
        Pintar();
    }

    /// <summary>Tres focos en columna: se leen desde cualquier lado del cruce.</summary>
    private void Armar()
    {
        Transform ya = transform.Find("CabezaSemaforo");
        if (ya != null) Destroy(ya.gameObject);

        GameObject cab = new GameObject("CabezaSemaforo");
        cab.transform.SetParent(transform, false);
        cab.transform.localPosition = Vector3.zero;
        cab.transform.localRotation = Quaternion.identity;

        focoRojo   = Foco(cab, "Luz_Rojo",   altura + 1.00f, 0.21f);
        focoAmbar  = Foco(cab, "Luz_Ambar",  altura + 0.52f, 0.21f);
        focoVerde  = Foco(cab, "Luz_Verde",  altura + 0.04f, 0.21f);
        focoPeaton = Foco(cab, "Luz_Peaton", altura - 1.30f, 0.14f);
    }

    private Renderer Foco(GameObject padre, string nombre, float y, float radio)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = nombre;
        g.transform.SetParent(padre.transform, false);
        g.transform.localPosition = new Vector3(0f, y, 0f);
        g.transform.localScale = Vector3.one * (radio * 2f);

        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);

        return g.GetComponent<Renderer>();
    }

    private void Pintar()
    {
        Encender(focoRojo,   estado == Luz.Rojo,  new Color(1.00f, 0.16f, 0.12f));
        Encender(focoAmbar,  estado == Luz.Ambar, new Color(1.00f, 0.70f, 0.10f));
        Encender(focoVerde,  estado == Luz.Verde, new Color(0.18f, 1.00f, 0.30f));
        Encender(focoPeaton, PasoPeatonal,        new Color(0.40f, 1.00f, 0.60f));
    }

    private void Encender(Renderer r, bool encendido, Color c)
    {
        if (r == null) return;

        Material m = r.material;            // instancia propia, no toca el asset
        Color col = encendido ? c : APAGADO;
        m.color = col;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);

        if (m.HasProperty("_EmissionColor"))
        {
            if (encendido)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 3.2f);
            }
            else
            {
                m.SetColor("_EmissionColor", Color.black);
                m.DisableKeyword("_EMISSION");
            }
        }
    }
}
