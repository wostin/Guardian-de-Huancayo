using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Guardián de Huancayo - Carro contaminante (enemigo).
/// Recorre una RUTA FIJA y PREDECIBLE (bucle de tramos de vía) dentro de SU zona.
/// Solo circula cuando el nivel actual es su zona; en otras zonas queda oculto y
/// en silencio (así la "zona peatonal" no tiene carros). Si golpea al Guardián le
/// quita una vida y sube la contaminación.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Contaminante : MonoBehaviour
{
    public float velocidad = 4f;
    public float radio = 16f;                 // compatibilidad
    public int zona = -1;                     // zona a la que pertenece (-1 = cualquiera)
    public List<Vector3> ruta = new List<Vector3>(); // recorrido fijo (predecible)

    private int idx;
    private float cooldown;
    private float v;                          // velocidad actual (acelera y frena suave)
    private float bocinaHasta;
    private Transform jugador;
    private Semaforo[] semaforos;
    private Contaminante[] otros;
    private Renderer[] visuales;
    private Collider golpe;
    private Collider[] solidos;   // hijos "Cuerpo": impiden que el jugador lo atraviese
    /// <summary>
    /// Motor del proyecto. La herramienta del editor engancha aca el "idle" del
    /// pack de sonido de motor que trae el proyecto; si no lo encuentra, se sigue
    /// usando el motor sintetizado por codigo y el juego no se queda mudo.
    /// </summary>
    public AudioClip clipMotor;

    /// <summary>
    /// Segunda capa del motor: el mismo carro acelerando. El pack de motor trae
    /// el ralenti y las subidas de vueltas por separado, y con el ralenti solo
    /// —aunque se le suba el tono— un carro a fondo sigue sonando a carro
    /// parado. Las dos capas se cruzan segun la velocidad: abajo manda el
    /// ralenti, arriba manda la aceleracion.
    /// </summary>
    public AudioClip clipAcelera;
    /// <summary>Arranque, una sola vez, cuando el carro se pone en marcha.</summary>
    public AudioClip clipArranque;
    private AudioSource revs;
    private bool arrancado;
    public AudioClip clipBocina;
    public float volumenMotor = 0.22f;

    private AudioSource motor;
    private bool visibleAhora = true;

    void Start()
    {
        semaforos = FindObjectsOfType<Semaforo>();
        otros = FindObjectsOfType<Contaminante>();
        GameObject pj = GameObject.FindGameObjectWithTag("Player");
        if (pj != null) jugador = pj.transform;
        visuales = GetComponentsInChildren<Renderer>();
        golpe = GetComponent<Collider>();
        if (golpe != null) golpe.isTrigger = true;

        // El cuerpo sólido lo arma la herramienta del editor como hijo "Cuerpo".
        List<Collider> sol = new List<Collider>();
        foreach (Collider c in GetComponentsInChildren<Collider>())
            if (c != golpe && !c.isTrigger && c.gameObject.name == "Cuerpo") sol.Add(c);
        solidos = sol.ToArray();

        // Si no le pasaron una ruta, armar una con la red de vías (compatibilidad).
        if (ruta == null || ruta.Count < 2)
        {
            ruta = new List<Vector3>();
            RoadNetwork red = RoadNetwork.Obtener();
            if (red != null && red.HayPuntos)
            {
                Vector3 a = red.MasCercano(transform.position);
                Vector3 prev = a;
                ruta.Add(a);
                for (int i = 0; i < 8; i++) { Vector3 s = red.Siguiente(a, prev); prev = a; a = s; ruta.Add(a); }
            }
            else
            {
                ruta.Add(transform.position);
                ruta.Add(transform.position + transform.forward * 6f);
            }
        }

        // Empezar en el punto de ruta más cercano a donde nació.
        idx = PuntoMasCercano(transform.position);
        Vector3 p0 = ruta[idx];
        transform.position = new Vector3(p0.x, p0.y + 0.05f, p0.z);
        // Arranca mirando al siguiente punto de su ruta.
        Vector3 haciaSig = ruta[(idx + 1) % ruta.Count] - p0; haciaSig.y = 0f;
        if (haciaSig.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(haciaSig);
        idx = (idx + 1) % ruta.Count;
        PrepararConduccion();

        // Sonido de motor (3D) en bucle. Con caida LINEAL: la logaritmica que
        // trae Unity por defecto hace que un carro a veinte metros casi no se
        // oiga, y aca hace falta escucharlo venir antes de tenerlo encima.
        motor = gameObject.AddComponent<AudioSource>();
        motor.clip = (clipMotor != null) ? clipMotor : Motor();
        motor.loop = true;
        motor.volume = (clipMotor != null) ? volumenMotor : 0.10f;
        motor.spatialBlend = 1f;
        motor.rolloffMode = AudioRolloffMode.Linear;
        motor.minDistance = 4f;
        motor.maxDistance = 38f;
        motor.dopplerLevel = 0.6f;              // se nota cuando pasa de largo
        motor.pitch = Random.Range(0.92f, 1.08f);   // no todos suenan igual
        motor.time = Random.Range(0f, 0.5f);        // ni arrancan sincronizados
        motor.Play();

        // Capa de aceleracion, muda hasta que el carro coge velocidad.
        if (clipAcelera != null)
        {
            revs = gameObject.AddComponent<AudioSource>();
            revs.clip = clipAcelera;
            revs.loop = true;
            revs.volume = 0f;
            revs.spatialBlend = 1f;
            revs.rolloffMode = AudioRolloffMode.Linear;
            revs.minDistance = 5f;
            revs.maxDistance = 46f;
            revs.dopplerLevel = 0.6f;
            revs.pitch = motor.pitch;
            revs.time = Random.Range(0f, 0.5f);
            revs.priority = 160;
            revs.Play();
        }
    }

    private int PuntoMasCercano(Vector3 p)
    {
        int m = 0; float d = float.MaxValue;
        for (int i = 0; i < ruta.Count; i++)
        {
            float dd = (ruta[i] - p).sqrMagnitude;
            if (dd < d) { d = dd; m = i; }
        }
        return m;
    }

    private static AudioClip _motorClip;
    private static AudioClip Motor()
    {
        if (_motorClip != null) return _motorClip;
        int sr = 44100;
        int len = sr; // 1 s en bucle
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / sr;
            float s = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.6f
                    + Mathf.Sin(2f * Mathf.PI * 105f * t) * 0.3f;
            s += (Mathf.PerlinNoise(t * 40f, 0f) - 0.5f) * 0.3f; // aspereza
            data[i] = s * 0.5f;
        }
        _motorClip = AudioClip.Create("motor", len, 1, sr, false);
        _motorClip.SetData(data, 0);
        return _motorClip;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;

        // Solo aparece y circula en SU zona (la zona peatonal se queda sin carros).
        bool visible = (gm == null) || zona < 0 || gm.modoRecorrido || gm.zonaActual == zona;
        if (visible != visibleAhora)
        {
            visibleAhora = visible;
            if (visuales != null)
                for (int i = 0; i < visuales.Length; i++)
                    if (visuales[i] != null) visuales[i].enabled = visible;
            if (golpe != null) golpe.enabled = visible;
            if (solidos != null)
                for (int i = 0; i < solidos.Length; i++)
                    if (solidos[i] != null) solidos[i].enabled = visible;
            if (motor != null) { if (visible) motor.Play(); else motor.Stop(); }
            if (revs  != null) { if (visible) revs.Play();  else revs.Stop();  }
        }
        if (!visible) return;

        if (gm != null && gm.estado != GameManager.Estado.Jugando) return;
        if (ruta == null || ruta.Count < 2) return;

        bool frenar = false;
        float dFreno = 99f;               // distancia al obstáculo que obliga a frenar

        // 1) Semáforo: solo obedece al que tiene ADELANTE y cerca del cruce.
        //    En ámbar ya va frenando, como manda el reglamento de tránsito.
        if (semaforos != null)
            for (int i = 0; i < semaforos.Length; i++)
            {
                Semaforo sm = semaforos[i];
                if (sm == null || (!sm.EnRojo && !sm.EnAmbar)) continue;
                Vector3 d = sm.transform.position - transform.position; d.y = 0f;
                float dd = d.magnitude;
                if (dd < 0.4f || dd > 12f) continue;
                if (Vector3.Dot(d / dd, transform.forward) < 0.45f) continue;
                frenar = true; dFreno = Mathf.Min(dFreno, dd); break;
            }

        // 2) No amontonarse: otro carro DE SU MISMA ZONA adelante (distancia de seguimiento).
        if (otros != null)
            for (int i = 0; i < otros.Length; i++)
            {
                Contaminante o = otros[i];
                if (o == null || o == this || o.zona != zona) continue;
                Vector3 d = o.transform.position - transform.position; d.y = 0f;
                float dd = d.magnitude;
                if (dd < 0.2f || dd > 10f) continue;
                if (Vector3.Dot(d / dd, transform.forward) < 0.7f) continue;
                frenar = true; dFreno = Mathf.Min(dFreno, dd);
            }

        // 3) El Guardián cruzando: el carro frena y toca bocina. Seguridad vial:
        //    el juego te avisa antes de atropellarte, no después.
        if (jugador != null)
        {
            Vector3 d = jugador.position - transform.position; d.y = 0f;
            float dd = d.magnitude;
            if (dd > 0.3f && dd < 8f && Vector3.Dot(d / dd, transform.forward) > 0.72f)
            {
                frenar = true; dFreno = Mathf.Min(dFreno, dd);
                Bocina();
                if (gm != null) gm.CasiTeAtropellan();
            }
        }

        // --- Velocidad objetivo ---------------------------------------------
        // a) En curva baja la velocidad (como un conductor de verdad).
        Vector3 pos = transform.position;
        float mirar = 3.5f + v * 0.6f;                       // "mirada" hacia adelante
        Vector3 objetivo = PuntoAdelante(pos, mirar);
        Vector3 haciaObj = objetivo - pos; haciaObj.y = 0f;
        float curva = haciaObj.sqrMagnitude > 0.01f
            ? Mathf.Abs(Vector3.SignedAngle(Flat(transform.forward), haciaObj, Vector3.up)) : 0f;
        float curvaAdelante = CurvaProxima(pos);
        float objetivoV = velMax * Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(Mathf.Max(curva, curvaAdelante) / 70f));

        // b) Frenado PROGRESIVO según la distancia al obstáculo (no un frenazo seco).
        if (frenar) objetivoV = Mathf.Min(objetivoV, velMax * Mathf.Clamp01((dFreno - 3f) / 6f));

        float vAntes = v;
        v = Mathf.MoveTowards(v, objetivoV, (objetivoV < v ? 12f : 3.8f) * Time.deltaTime);
        float acel = (v - vAntes) / Mathf.Max(0.0001f, Time.deltaTime);

        // Arranque: la primera vez que el carro se mueve de verdad.
        if (!arrancado && clipArranque != null && v > 0.6f)
        {
            arrancado = true;
            AudioSource.PlayClipAtPoint(clipArranque, transform.position,
                0.35f * GuardianMezcla.Ganancia(GuardianMezcla.Bus.Efectos));
        }

        float bus = GuardianMezcla.Ganancia(GuardianMezcla.Bus.Efectos);

        // Cruce de las dos capas del motor segun la marcha.
        if (revs != null)
        {
            float f2 = (velMax > 0.01f) ? Mathf.Clamp01(v / velMax) : 0f;
            float sube = Mathf.Clamp01((f2 - 0.35f) / 0.55f);     // entra pasado un tercio
            revs.pitch  = Mathf.Lerp(revs.pitch, Mathf.Lerp(0.85f, 1.32f, f2), 3f * Time.deltaTime);
            revs.volume = Mathf.Lerp(revs.volume, volumenMotor * 0.85f * sube * bus, 2.5f * Time.deltaTime);
        }

        // El motor acompana a la marcha: parado ronronea, andando sube de vueltas.
        if (motor != null)
        {
            float f = (velMax > 0.01f) ? Mathf.Clamp01(v / velMax) : 0f;
            float baseVol = (clipMotor != null) ? volumenMotor : 0.10f;
            motor.pitch  = Mathf.Lerp(motor.pitch, Mathf.Lerp(0.78f, 1.55f, f), 3f * Time.deltaTime);
            motor.volume = Mathf.Lerp(motor.volume, baseVol * Mathf.Lerp(0.55f, 1f, f) * bus,
                                      3f * Time.deltaTime);
        }

        // --- Dirección: PURE PURSUIT ------------------------------------------
        // En vez de ir punto por punto (giros bruscos en cada esquina), el carro
        // persigue un punto que va unos metros ADELANTE sobre su ruta y gira con
        // una velocidad angular limitada: dibuja curvas suaves como un auto real.
        Vector3 dPunto = ruta[idx] - pos; dPunto.y = 0f;
        if (dPunto.magnitude < 2.4f) idx = (idx + 1) % ruta.Count;

        float giroDeseado = haciaObj.sqrMagnitude > 0.01f
            ? Vector3.SignedAngle(Flat(transform.forward), haciaObj, Vector3.up) : 0f;
        float maxGiro = Mathf.Lerp(160f, 90f, Mathf.Clamp01(v / Mathf.Max(1f, velMax))) * Time.deltaTime;
        float paso = Mathf.Clamp(giroDeseado * 4f * Time.deltaTime, -maxGiro, maxGiro);
        if (v < 0.15f) paso = 0f;                            // parado no gira sobre su eje
        yaw += paso;
        float velGiro = paso / Mathf.Max(0.0001f, Time.deltaTime);   // grados/s

        // Inclinación de carrocería: se ladea hacia afuera en la curva y cabecea al frenar.
        roll  = Mathf.Lerp(roll,  Mathf.Clamp(-velGiro * v * 0.012f, -5f, 5f), 6f * Time.deltaTime);
        cabeceo = Mathf.Lerp(cabeceo, Mathf.Clamp(-acel * 0.45f, -3.5f, 3.5f), 5f * Time.deltaTime);
        transform.rotation = Quaternion.Euler(cabeceo, yaw, roll);

        Vector3 nueva = pos + Flat(Quaternion.Euler(0f, yaw, 0f) * Vector3.forward) * v * Time.deltaTime;

        // Pegado a la pista: raycast al suelo cada 0.1 s (suaviza las subidas).
        if (Time.time > proxSuelo)
        {
            proxSuelo = Time.time + 0.1f;
            alturaSuelo = AlturaSuelo(nueva, ruta[idx].y);
        }
        nueva.y = Mathf.Lerp(pos.y, alturaSuelo + 0.05f, 6f * Time.deltaTime);
        transform.position = nueva;

        Ruedas(v);
    }

    // ------------------------------------------------------------ ayuda de conducción

    private float velMax;
    private float yaw, roll, cabeceo;
    private float proxSuelo, alturaSuelo;
    private Transform[] ruedas;
    private float[] radioRueda;

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward; }

    private void PrepararConduccion()
    {
        velMax = velocidad * 1.2f;                         // algo más de vida que antes
        yaw = transform.eulerAngles.y;
        alturaSuelo = transform.position.y;

        var rs = new List<Transform>(); var rad = new List<float>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (t == transform) continue;
            if (n.Contains("rueda") || n.Contains("wheel") || n.Contains("llanta") || n.Contains("tire"))
            {
                rs.Add(t);
                Renderer r = t.GetComponent<Renderer>();
                rad.Add(r != null ? Mathf.Max(0.15f, r.bounds.extents.y) : 0.35f);
            }
        }
        ruedas = rs.ToArray(); radioRueda = rad.ToArray();
    }

    /// <summary>Gira las ruedas según la velocidad (360° por cada circunferencia recorrida).</summary>
    private void Ruedas(float vel)
    {
        if (ruedas == null) return;
        for (int i = 0; i < ruedas.Length; i++)
        {
            if (ruedas[i] == null) continue;
            float grados = vel * Time.deltaTime / (2f * Mathf.PI * radioRueda[i]) * 360f;
            ruedas[i].Rotate(transform.right, grados, Space.World);
        }
    }

    /// <summary>Punto de la ruta que queda 'distancia' metros más adelante.</summary>
    private Vector3 PuntoAdelante(Vector3 desde, float distancia)
    {
        Vector3 a = desde;
        int i = idx;
        float resta = distancia;
        for (int n = 0; n < ruta.Count; n++)
        {
            Vector3 b = ruta[i];
            Vector3 seg = b - a; seg.y = 0f;
            float l = seg.magnitude;
            if (l >= resta) return a + seg / Mathf.Max(0.0001f, l) * resta;
            resta -= l; a = b; i = (i + 1) % ruta.Count;
        }
        return ruta[idx];
    }

    /// <summary>Ángulo de la próxima esquina (si está cerca): para frenar ANTES de doblar.</summary>
    private float CurvaProxima(Vector3 pos)
    {
        int i1 = idx, i2 = (idx + 1) % ruta.Count;
        Vector3 a = ruta[i1] - pos; a.y = 0f;
        if (a.magnitude > 9f) return 0f;
        Vector3 b = ruta[i2] - ruta[i1]; b.y = 0f;
        if (a.sqrMagnitude < 0.01f || b.sqrMagnitude < 0.01f) return 0f;
        return Vector3.Angle(a, b) * Mathf.InverseLerp(9f, 2f, a.magnitude);
    }

    private float AlturaSuelo(Vector3 p, float porDefecto)
    {
        RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 3f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MinValue;
        foreach (RaycastHit h in hits)
        {
            if (h.collider == null || h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.CompareTag("Player")) continue;
            if (h.point.y > mejor && h.point.y < p.y + 1.2f) mejor = h.point.y;
        }
        return mejor > float.MinValue ? mejor : porDefecto;
    }

    /// <summary>Bocina corta generada por código (dos tonos, como las de la ciudad).</summary>
    private void Bocina()
    {
        if (Time.time < bocinaHasta) return;
        bocinaHasta = Time.time + 2.6f;
        GuardianAudio.EnPunto(clipBocina != null ? clipBocina : ClipBocina(),
                              transform.position, 0.55f, 0.95f, 1.06f, 40f);
    }

    private static AudioClip _bocina;
    private static AudioClip ClipBocina()
    {
        if (_bocina != null) return _bocina;
        int sr = 44100;
        int len = Mathf.RoundToInt(sr * 0.38f);
        float[] d = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / sr;
            float env = Mathf.Min(1f, t / 0.015f) * Mathf.Min(1f, (len - i) / (0.05f * sr));
            d[i] = (Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.60f
                  + Mathf.Sin(2f * Mathf.PI * 510f * t) * 0.40f) * env * 0.70f;
        }
        _bocina = AudioClip.Create("bocina", len, 1, sr, false);
        _bocina.SetData(d, 0);
        return _bocina;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || GameManager.Instance == null) return;
        if (Time.time < cooldown) return;
        cooldown = Time.time + 1.5f;

        GameManager.Instance.PerderVida();
        GuardianEventos.AvisarGolpe(transform.position);
        GuardianFX.Efecto(transform.position + Vector3.up, new Color(0.9f, 0.5f, 0.2f), 180f);
    }
}
