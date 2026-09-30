using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Guardián de Huancayo - Peatón / ciudadano.
/// Camina POR LA VEREDA siguiendo un recorrido de puntos que le deja la
/// herramienta del editor (ida y vuelta por su cuadra). Antes deambulaba en
/// círculo al azar y atravesaba casas y la pista.
/// Se para de rato en rato, como cualquiera esperando o mirando una vitrina.
///
/// CORTESÍA: si el Guardián se le cruza por delante, el peatón se detiene y lo
/// deja pasar. Antes lo empujaba: como el jugador usa CharacterController, un
/// peatón caminando contra él lo arrastraba metros por la vereda (y hasta a la
/// pista), lo cual se sentía como un error del juego. Ahora esperan su turno.
/// </summary>
public class Peaton : MonoBehaviour
{
    public float velocidad = 1.2f;
    public float radio = 8f;                              // solo para el modo antiguo
    public List<Vector3> ruta = new List<Vector3>();      // recorrido por la vereda

    /// <summary>A qué distancia del Guardián se detiene para no empujarlo.</summary>
    public float cortesia = 1.25f;

    private int idx;
    private int paso = 1;                                 // 1 = va, -1 = vuelve
    private Vector3 centro;
    private Vector3 objetivo;
    private float espera;

    private Transform jugador;
    private float proximaBusqueda;
    private float cediendoDesde = -1f;

    /// <summary>
    /// Cuerpo con colisiones. Antes el peaton avanzaba con "transform.position +="
    /// a secas, que no consulta la fisica para nada: por eso los NPC cruzaban
    /// paredes, autos y edificios como si no existieran. Un CharacterController
    /// resuelve el choque y el deslizamiento por la pared, que es justo lo que
    /// hace falta para alguien que camina.
    /// </summary>
    protected CharacterController cuerpo;

    void Start()
    {
        centro = transform.position;

        // Cuerpo fisico. Si el objeto ya trae capsula, se apaga: el
        // CharacterController hace de collider y tener los dos encimados provoca
        // empujones raros entre el NPC y su propia capsula.
        cuerpo = GetComponent<CharacterController>();

        // El triciclo tambien usa este script y lleva Rigidbody propio: mezclar
        // Rigidbody y CharacterController en el mismo objeto pelea consigo mismo,
        // asi que ahi se deja el movimiento directo de siempre.
        bool tieneRigidbody = GetComponent<Rigidbody>() != null;

        if (cuerpo == null && !tieneRigidbody)
        {
            foreach (Collider c in GetComponents<Collider>())
                if (c != null && !c.isTrigger) c.enabled = false;

            // La medida sale del modelo: sirve igual para una persona que para el
            // perro callejero, que es mucho mas bajo.
            Bounds caja = new Bounds(transform.position, Vector3.zero);
            bool hay = false;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (r == null) continue;
                if (!hay) { caja = r.bounds; hay = true; } else caja.Encapsulate(r.bounds);
            }

            float alto  = hay ? Mathf.Clamp(caja.size.y, 0.5f, 2.2f) : 1.7f;
            float ancho = hay ? Mathf.Clamp(Mathf.Max(caja.size.x, caja.size.z) * 0.45f, 0.18f, 0.45f)
                              : 0.28f;

            cuerpo = gameObject.AddComponent<CharacterController>();
            cuerpo.radius = ancho;
            cuerpo.height = alto;
            cuerpo.center = new Vector3(0f, alto * 0.5f, 0f);
            cuerpo.slopeLimit = 50f;
            cuerpo.stepOffset = 0.35f;
            cuerpo.skinWidth = 0.04f;
        }


        if (ruta != null && ruta.Count >= 2)
        {
            idx = MasCercano(transform.position);
            objetivo = Altura(ruta[idx]);
        }
        else Nuevo();

        espera = Random.Range(0f, 1.5f);
    }

    /// <summary>
    /// Avanza consultando la fisica. La gravedad es constante y modesta: no hace
    /// falta simular una caida real, solo que el NPC quede pegado al piso y no
    /// flote al subir un sardinel.
    /// </summary>
    protected void Avanzar(Vector3 direccion, float vel)
    {
        Vector3 paso = direccion * vel * Time.deltaTime + Vector3.down * 6f * Time.deltaTime;
        if (cuerpo != null && cuerpo.enabled) cuerpo.Move(paso);
        else transform.position += direccion * vel * Time.deltaTime;
    }

    private int MasCercano(Vector3 p)
    {
        int m = 0; float d = float.MaxValue;
        for (int i = 0; i < ruta.Count; i++)
        {
            float dd = (ruta[i] - p).sqrMagnitude;
            if (dd < d) { d = dd; m = i; }
        }
        return m;
    }

    private Vector3 Altura(Vector3 p)
    {
        return new Vector3(p.x, transform.position.y, p.z);
    }

    /// <summary>Modo antiguo (sin ruta): pasear cerca de donde nació.</summary>
    private void Nuevo()
    {
        Vector2 r = Random.insideUnitCircle * radio;
        objetivo = new Vector3(centro.x + r.x, transform.position.y, centro.z + r.y);
        espera = Random.Range(0.5f, 2.5f);
    }

    private void Siguiente()
    {
        if (ruta == null || ruta.Count < 2) { Nuevo(); return; }

        idx += paso;
        if (idx >= ruta.Count) { idx = ruta.Count - 2; paso = -1; }   // se da la vuelta
        else if (idx < 0)      { idx = 1;             paso =  1; }

        objetivo = Altura(ruta[idx]);
        if (Random.value < 0.25f) espera = Random.Range(0.8f, 2.6f);  // se queda parado un rato
    }

    /// <summary>
    /// ¿El Guardián está justo delante y tan cerca que avanzar lo empujaría?
    /// Solo cuenta si el peatón va HACIA él; si el jugador lo sigue por detrás,
    /// el peatón sigue su camino tranquilo.
    /// </summary>
    protected bool JugadorEnElPaso(Vector3 direccion)
    {
        if (jugador == null)
        {
            if (Time.time < proximaBusqueda) return false;
            proximaBusqueda = Time.time + 1f;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return false;
            jugador = p.transform;
        }

        Vector3 d = jugador.position - transform.position; d.y = 0f;
        float m = d.magnitude;
        if (m > cortesia || m < 0.0001f) return false;
        return Vector3.Dot(d / m, direccion) > 0.1f;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.estado != GameManager.Estado.Jugando)
            return;

        if (espera > 0f) { espera -= Time.deltaTime; return; }

        Vector3 dir = objetivo - transform.position; dir.y = 0f;
        if (dir.magnitude < 0.5f) { Siguiente(); return; }

        Vector3 avance = dir.normalized;
        if (JugadorEnElPaso(avance))
        {
            // Cede el paso... pero no para siempre: si el Guardián se queda
            // plantado delante suyo, al rato lo rodea y sigue su camino.
            if (cediendoDesde < 0f) cediendoDesde = Time.time;
            if (Time.time - cediendoDesde < 3f) return;
        }
        else cediendoDesde = -1f;

        Avanzar(avance, velocidad);
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), 4f * Time.deltaTime);
    }
}
