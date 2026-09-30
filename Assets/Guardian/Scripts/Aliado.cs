using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Guardián de Huancayo - Policía municipal ALIADO.
/// Patrulla la vereda con animación de caminata y recoge la basura que los
/// vecinos van TIRANDO durante la partida.
///
/// Por qué solo esa basura: el trabajo del jugador es SEGREGAR los residuos del
/// nivel en el contenedor de su color (NTP 900.058-2019). Antes el policía
/// levantaba cualquier residuo que viera, sin pausa ni límite, así que entre
/// dos o tres policías limpiaban el nivel solos mientras el jugador miraba —el
/// nivel se ganaba sin haber segregado nada, que es justo lo que el juego debe
/// enseñar—. Ahora el policía cumple el papel que le toca en la vida real: es
/// el servicio de limpieza pública, que recoge lo que la gente bota a la calle
/// pero NO segrega por ti. Además espera un rato entre recojo y recojo, porque
/// una cuadrilla municipal no da abasto con una ciudad que no separa en casa.
/// </summary>
public class Aliado : MonoBehaviour
{
    public float velocidad = 2.2f;
    public float radioBusqueda = 14f;
    public List<Vector3> ruta = new List<Vector3>();   // patrullaje por la vereda

    /// <summary>Segundos que tarda en volver a recoger algo.</summary>
    public float descansoMin = 14f;
    public float descansoMax = 24f;

    /// <summary>A qué distancia del Guardián se detiene para no empujarlo.</summary>
    public float cortesia = 1.25f;

    private int idx;
    private int paso = 1;
    private Vector3 centro;
    private Vector3 objetivo;
    private bool haciaBasura;
    private TrashItem presa;
    private float tBuscar;
    private float libreDesde;

    private Transform jugador;
    private float proximaBusquedaJugador;
    private float cediendoDesde = -1f;

    /// <summary>Cuerpo con colisiones: sin esto el policia cruzaba las paredes.</summary>
    private CharacterController cuerpo;

    private Animator anim;
    private string estadoCaminar, estadoIdle;
    private bool caminando;

    void Start()
    {
        centro = transform.position;
        anim = GetComponentInChildren<Animator>();
        BuscarClips();

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


        // No arranca recogiendo: le da al jugador los primeros segundos limpios.
        libreDesde = Time.time + Random.Range(12f, 22f);

        if (ruta != null && ruta.Count >= 2)
        {
            float d = float.MaxValue;
            for (int i = 0; i < ruta.Count; i++)
            {
                float dd = (ruta[i] - transform.position).sqrMagnitude;
                if (dd < d) { d = dd; idx = i; }
            }
        }
        NuevoPaseo();
    }

    void NuevoPaseo()
    {
        haciaBasura = false;
        presa = null;

        // Patrulla la VEREDA siguiendo su recorrido (ida y vuelta), no al azar.
        if (ruta != null && ruta.Count >= 2)
        {
            idx += paso;
            if (idx >= ruta.Count) { idx = ruta.Count - 2; paso = -1; }
            else if (idx < 0)      { idx = 1;             paso =  1; }
            objetivo = new Vector3(ruta[idx].x, transform.position.y, ruta[idx].z);
            return;
        }

        Vector2 r = Random.insideUnitCircle * 9f;
        objetivo = new Vector3(centro.x + r.x, transform.position.y, centro.z + r.y);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.estado != GameManager.Estado.Jugando)
        { Animar(false); return; }

        // Buscar basura tirada, pero solo cuando le toca turno.
        if (Time.time >= libreDesde
            && (presa == null || !presa.gameObject.activeSelf)
            && Time.time > tBuscar)
        {
            tBuscar = Time.time + 0.6f;
            presa = BuscarBasura();
        }

        if (presa != null && presa.gameObject.activeSelf)
        {
            objetivo = new Vector3(presa.transform.position.x, transform.position.y, presa.transform.position.z);
            haciaBasura = true;
        }

        Vector3 dir = objetivo - transform.position; dir.y = 0f;
        float umbral = haciaBasura ? 1.6f : 0.5f;

        if (dir.magnitude < umbral)
        {
            if (haciaBasura && presa != null && presa.gameObject.activeSelf)
            {
                Vector3 donde = presa.transform.position;
                presa.gameObject.SetActive(false);
                if (GameManager.Instance != null) GameManager.Instance.LimpiezaMunicipal(presa);
                GuardianFX.Efecto(donde, new Color(0.3f, 0.6f, 1f), 800f);
                libreDesde = Time.time + Random.Range(descansoMin, descansoMax);
            }
            presa = null;
            NuevoPaseo();
            Animar(false);
            return;
        }

        Vector3 avance = dir.normalized;
        if (JugadorEnElPaso(avance))
        {
            if (cediendoDesde < 0f) cediendoDesde = Time.time;
            if (Time.time - cediendoDesde < 3f) { Animar(false); return; }   // le cede el paso
        }
        else cediendoDesde = -1f;

        Vector3 paso = avance * velocidad * Time.deltaTime + Vector3.down * 6f * Time.deltaTime;
        if (cuerpo != null && cuerpo.enabled) cuerpo.Move(paso);
        else transform.position += avance * velocidad * Time.deltaTime;

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), 5f * Time.deltaTime);
        Animar(true);
    }

    /// <summary>
    /// Solo mira los residuos DE RESERVA, o sea los que un vecino acaba de botar
    /// a la vereda durante la partida. Los residuos con los que empieza el nivel
    /// son tarea del Guardián y nadie se los recoge.
    /// </summary>
    private TrashItem BuscarBasura()
    {
        TrashItem mejor = null;
        float best = radioBusqueda * radioBusqueda;
        TrashItem[] todos = Object.FindObjectsByType<TrashItem>(FindObjectsSortMode.None);

        for (int i = 0; i < todos.Length; i++)
        {
            TrashItem t = todos[i];
            if (t == null || !t.gameObject.activeSelf) continue;
            if (!t.deReserva) continue;                       // la basura del nivel no se toca
            float d = (t.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; mejor = t; }
        }
        return mejor;
    }

    private bool JugadorEnElPaso(Vector3 direccion)
    {
        if (jugador == null)
        {
            if (Time.time < proximaBusquedaJugador) return false;
            proximaBusquedaJugador = Time.time + 1f;
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return false;
            jugador = p.transform;
        }

        Vector3 d = jugador.position - transform.position; d.y = 0f;
        float m = d.magnitude;
        if (m > cortesia || m < 0.0001f) return false;
        return Vector3.Dot(d / m, direccion) > 0.1f;
    }

    private void BuscarClips()
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;
        foreach (AnimationClip cl in anim.runtimeAnimatorController.animationClips)
        {
            string n = cl.name.ToLower();
            if (estadoCaminar == null && (n.Contains("walk") || n.Contains("jog") || n.Contains("run")))
                estadoCaminar = cl.name;
            if (estadoIdle == null && n.Contains("idle")) estadoIdle = cl.name;
        }
        if (estadoIdle != null) anim.CrossFadeInFixedTime(estadoIdle, 0.1f);
    }

    private void Animar(bool mueve)
    {
        if (anim == null) return;
        if (mueve && !caminando) { caminando = true; if (estadoCaminar != null) anim.CrossFadeInFixedTime(estadoCaminar, 0.15f); }
        else if (!mueve && caminando) { caminando = false; if (estadoIdle != null) anim.CrossFadeInFixedTime(estadoIdle, 0.2f); }
    }
}
