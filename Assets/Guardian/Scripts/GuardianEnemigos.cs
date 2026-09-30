using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - ENEMIGOS (bestiario).
///
///  ENEMIGO            ZONA              MECÁNICA                                       CÓMO SE VENCE
///  Rata Basurera      Todas             Si llevas residuos te persigue y te ROBA uno   Salta cerca de ella (Espacio): huye y +10
///                                       (lo tira al piso: hay que recogerlo de nuevo)   o córrele (Shift) — es más lenta que tú
///  Humo Tóxico        Mercado, Ribera   Nube de smog que te sigue: te FRENA y sube     Segrega bien cerca: el aire limpio la
///                                       la contaminación mientras estás dentro          disuelve (+15). Se va sola a los 30 s
///  Rey Basurón (JEFE) Ribera Shullcas   Lanza bolsas de basura (la sombra roja marca   Cada residuo BIEN SEGREGADO le quita vida.
///                                       dónde caen) y da pisotones que empujan.        Segregar es tu arma. Derrotarlo: +150 y
///                                       Mientras vive, la contaminación sube más.      baja la contaminación
///  Carros / mototaxis Mercado, Ribera   (Contaminante.cs) golpe = −1 vida              Cruza por el crucero, espera tu luz
///
/// Todos se arman por código con primitivas (sin assets externos) y aparecen
/// solos al empezar cada zona; al reiniciar o cambiar de nivel se regeneran.
/// </summary>
public class GuardianEnemigos : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (FindObjectOfType<GuardianEnemigos>() != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianEnemigos").AddComponent<GuardianEnemigos>();
    }

    public static GuardianEnemigos Instancia { get; private set; }
    public static float ultimoGolpe = -10f;

    public JefeBasuron Jefe { get; private set; }
    private readonly List<GameObject> vivos = new List<GameObject>();
    private string clave = "";
    private float tiempoAntes;
    private Transform jugador;
    private Texture2D blanco;

    void Awake()
    {
        Instancia = this;
        blanco = new Texture2D(1, 1); blanco.SetPixel(0, 0, Color.white); blanco.Apply();
        GuardianEventos.Deposito += AlDepositar;
    }

    void OnDestroy() { GuardianEventos.Deposito -= AlDepositar; }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (jugador == null)
        {
            GameObject pj = GameObject.FindGameObjectWithTag("Player");
            if (pj != null) jugador = pj.transform;
        }

        if (gm.estado == GameManager.Estado.Menu) { if (vivos.Count > 0) Limpiar(); clave = ""; return; }
        if (gm.estado != GameManager.Estado.Jugando) return;

        // ¿Empezó / reinició / cambió de zona? → regenerar enemigos.
        bool reinicio = gm.tiempoRestante > tiempoAntes + 1.5f;
        tiempoAntes = gm.tiempoRestante;
        string k = gm.nivel + "_" + gm.zonaActual + "_" + gm.modoRecorrido;
        if (k != clave || reinicio)
        {
            clave = k;
            if (!gm.modoRecorrido) Generar(gm);
            else Limpiar();
        }
    }

    // ---------------------------------------------------------------- generación

    private void Limpiar()
    {
        foreach (GameObject g in vivos) if (g != null) Destroy(g);
        vivos.Clear();
        Jefe = null;
    }

    private void Generar(GameManager gm)
    {
        Limpiar();
        int z = gm.zonaActual;

        List<Vector3> basura = new List<Vector3>();
        foreach (TrashItem t in FindObjectsByType<TrashItem>(FindObjectsSortMode.None))
            if (t != null && t.gameObject.activeInHierarchy && t.zona == z) basura.Add(t.transform.position);
        Vector3 centro = Vector3.zero;
        foreach (Vector3 v in basura) centro += v;
        centro = basura.Count > 0 ? centro / basura.Count : (jugador != null ? jugador.position : Vector3.zero);

        int ratas = z == 0 ? 2 : 3;
        for (int i = 0; i < ratas; i++)
        {
            Vector3 p = basura.Count > 0 ? basura[(i * 3 + 1) % basura.Count] : centro;
            p += new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f));
            GameObject r = EnemigoRata.Crear(Suelo(p));
            vivos.Add(r);
        }

        if (z >= 1)
        {
            int nubes = 2;
            for (int i = 0; i < nubes; i++)
            {
                float ang = i * 180f + Random.Range(-40f, 40f);
                Vector3 p = centro + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * 16f;
                vivos.Add(NubeToxica.Crear(Suelo(p)));
            }
        }

        if (z == 2)
        {
            Vector3 dir = jugador != null ? (centro - jugador.position) : Vector3.forward;
            dir.y = 0f; if (dir.sqrMagnitude < 1f) dir = Vector3.forward;
            Vector3 p = centro + dir.normalized * 10f;
            GameObject j = JefeBasuron.Crear(Suelo(p));
            Jefe = j.GetComponent<JefeBasuron>();
            vivos.Add(j);
            gm.AvisoPublico("¡El REY BASURÓN domina la ribera! Segrega bien: cada acierto lo debilita",
                            new Color(1f, 0.55f, 0.3f), 6f);
        }
    }

    /// <summary>Altura del piso real bajo un punto (el impacto más bajo, no el techo de un edificio).</summary>
    public static Vector3 Suelo(Vector3 p)
    {
        RaycastHit[] hits = Physics.RaycastAll(p + Vector3.up * 40f, Vector3.down, 90f, ~0, QueryTriggerInteraction.Ignore);
        float y = float.MaxValue;
        foreach (RaycastHit h in hits)
        {
            if (h.collider == null || h.collider.CompareTag("Player")) continue;
            if (h.collider.GetComponentInParent<EnemigoBase>() != null) continue;
            if (h.point.y < y) y = h.point.y;
        }
        if (y == float.MaxValue) y = p.y;
        return new Vector3(p.x, y, p.z);
    }

    // ---------------------------------------------------------------- jefe

    private void AlDepositar(Vector3 p, TipoResiduo t, int n, int pts)
    {
        // Segregar bien es el arma contra el jefe y limpia el aire.
        if (Jefe != null) Jefe.Danar(n);
        foreach (GameObject g in vivos)
        {
            if (g == null) continue;
            NubeToxica nube = g.GetComponent<NubeToxica>();
            if (nube != null && Vector3.Distance(nube.transform.position, p) < 18f) nube.Disolver(true);
        }
    }

    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.estado != GameManager.Estado.Jugando || gm.pausado) return;
        if (Jefe == null || jugador == null) return;
        if (Vector3.Distance(Jefe.transform.position, jugador.position) > 70f && !Jefe.Muerto) return;

        float esc = Mathf.Clamp(Mathf.Min(Screen.height / 800f, Screen.width / 1360f), 0.7f, 2.4f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(esc, esc, 1f));
        float sw = Screen.width / esc;

        float w = 360f;
        Rect r = new Rect(sw / 2f - w / 2f, 44f, w, 16f);
        Pintar(new Rect(r.x - 4, r.y - 22, r.width + 8, r.height + 28), new Color(0.03f, 0.04f, 0.06f, 0.75f));
        GUIStyle st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 13 };
        st.normal.textColor = new Color(1f, 0.72f, 0.35f);
        GUI.Label(new Rect(r.x, r.y - 21, r.width, 20), Jefe.Muerto ? "REY BASURÓN · DERROTADO" : "REY BASURÓN · jefe de la ribera", st);
        Pintar(r, new Color(0.25f, 0.08f, 0.08f));
        float f = Jefe.VidaNormalizada;
        Pintar(new Rect(r.x, r.y, r.width * f, r.height), Color.Lerp(new Color(0.9f, 0.2f, 0.15f), new Color(1f, 0.6f, 0.2f), f));
        st.fontSize = 11; st.normal.textColor = Color.white;
        GUI.Label(r, Jefe.Muerto ? "¡Ribera liberada!" : "Segrega bien para dañarlo  ·  " + Jefe.Vida + " / " + Jefe.VidaMax, st);
    }

    private void Pintar(Rect r, Color c) { Color a = GUI.color; GUI.color = c; GUI.DrawTexture(r, blanco); GUI.color = a; }

    // ---------------------------------------------------------------- utilidades de modelado

    private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

    public static Material Mat(Color c, bool brilla = false, float suave = 0.35f)
    {
        string k = ColorUtility.ToHtmlStringRGB(c) + brilla + suave;
        Material m;
        if (mats.TryGetValue(k, out m) && m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suave);
        if (brilla)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.3f);
        }
        mats[k] = m;
        return m;
    }

    public static Transform Pieza(Transform padre, PrimitiveType tipo, Vector3 pos, Vector3 esc, Material m, string nombre, Vector3 rot = default(Vector3))
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        Collider c = g.GetComponent<Collider>();
        if (c != null) Destroy(c);
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = esc;
        g.transform.localRotation = Quaternion.Euler(rot);
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g.transform;
    }
}

/// <summary>Base común: referencia al jugador, piso y utilidades.</summary>
public abstract class EnemigoBase : MonoBehaviour
{
    protected Transform jugador;
    protected PlayerController pc;
    protected GameManager gm => GameManager.Instance;
    private float proxSuelo;
    protected float sueloY;

    protected virtual void Start()
    {
        GameObject pj = GameObject.FindGameObjectWithTag("Player");
        if (pj != null) { jugador = pj.transform; pc = pj.GetComponent<PlayerController>(); }
        sueloY = transform.position.y;
    }

    protected bool Activo => gm != null && gm.estado == GameManager.Estado.Jugando && !gm.pausado && jugador != null;

    protected float DistJugador
    {
        get { Vector3 d = jugador.position - transform.position; d.y = 0f; return d.magnitude; }
    }

    protected void PegarAlSuelo()
    {
        if (Time.time > proxSuelo)
        {
            proxSuelo = Time.time + 0.2f;
            RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 1.5f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore);
            float mejor = float.MinValue;
            foreach (RaycastHit h in hits)
            {
                if (h.collider == null || h.collider.transform.IsChildOf(transform) || h.collider.CompareTag("Player")) continue;
                if (h.point.y > mejor && h.point.y < transform.position.y + 1.2f) mejor = h.point.y;
            }
            if (mejor > float.MinValue) sueloY = mejor;
        }
        Vector3 p = transform.position;
        p.y = Mathf.Lerp(p.y, sueloY, 10f * Time.deltaTime);
        transform.position = p;
    }

    protected void Mover(Vector3 hacia, float vel, float giro = 540f)
    {
        Vector3 d = hacia - transform.position; d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        Quaternion obj = Quaternion.LookRotation(d);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, obj, giro * Time.deltaTime);
        transform.position += transform.forward * vel * Time.deltaTime;
    }
}

// ====================================================================== RATA BASURERA

/// <summary>
/// Rata Basurera: merodea cerca de la basura. Si el Guardián lleva residuos y
/// se acerca, lo PERSIGUE para robarle uno (lo tira al piso). Se la espanta
/// saltando cerca (Espacio) o simplemente corriendo: es más lenta que él.
/// Estados: Merodear → Perseguir → Robar/Huir (máquina de estados finita).
/// </summary>
public class EnemigoRata : EnemigoBase
{
    private enum Est { Merodear, Perseguir, Huir, Aturdida }
    private Est est = Est.Merodear;
    private Vector3 casa, destino;
    private float reloj, proxDestino;
    private Transform cola, cuerpo;
    private Renderer[] ojos;
    private float nacio;

    public static GameObject Crear(Vector3 pos)
    {
        GameObject g = new GameObject("Enemigo_RataBasurera");
        g.transform.position = pos;
        Transform c = new GameObject("Cuerpo").transform; c.SetParent(g.transform, false);
        Material piel = GuardianEnemigos.Mat(new Color(0.36f, 0.31f, 0.28f), false, 0.2f);
        Material rosa = GuardianEnemigos.Mat(new Color(0.93f, 0.62f, 0.66f));
        Material ojo  = GuardianEnemigos.Mat(new Color(1f, 0.15f, 0.1f), true);
        GuardianEnemigos.Pieza(c, PrimitiveType.Capsule, new Vector3(0, 0.38f, 0), new Vector3(0.55f, 0.42f, 0.55f), piel, "Torso", new Vector3(90, 0, 0));
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(0, 0.5f, 0.55f), new Vector3(0.42f, 0.38f, 0.5f), piel, "Cabeza");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(0, 0.44f, 0.84f), new Vector3(0.12f, 0.12f, 0.12f), rosa, "Nariz");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(-0.17f, 0.74f, 0.5f), new Vector3(0.2f, 0.22f, 0.06f), rosa, "OrejaI");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(0.17f, 0.74f, 0.5f), new Vector3(0.2f, 0.22f, 0.06f), rosa, "OrejaD");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(-0.11f, 0.6f, 0.76f), Vector3.one * 0.08f, ojo, "OjoI");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(0.11f, 0.6f, 0.76f), Vector3.one * 0.08f, ojo, "OjoD");
        for (int i = 0; i < 4; i++)
            GuardianEnemigos.Pieza(c, PrimitiveType.Cube, new Vector3(i < 2 ? -0.18f : 0.18f, 0.1f, i % 2 == 0 ? 0.3f : -0.3f), new Vector3(0.08f, 0.2f, 0.1f), rosa, "Pata" + i);
        Transform cola = GuardianEnemigos.Pieza(c, PrimitiveType.Cube, new Vector3(0, 0.34f, -0.9f), new Vector3(0.05f, 0.05f, 0.8f), rosa, "Cola", new Vector3(-12, 0, 0));
        // Bolsita robada en la boca (se ve cuando huye con algo).
        g.transform.localScale = Vector3.one * 1.5f;          // rata "gigante": se lee de lejos
        EnemigoRata r = g.AddComponent<EnemigoRata>();
        r.cola = cola; r.cuerpo = c;
        return g;
    }

    protected override void Start()
    {
        base.Start();
        casa = transform.position;
        destino = casa;
        nacio = Time.time;
    }

    void Update()
    {
        if (!Activo) return;
        reloj -= Time.deltaTime;
        float d = DistJugador;

        // ¡Salto cerca = espanto! (el Guardián cae sobre ella)
        if (est != Est.Aturdida && est != Est.Huir && pc != null && pc.EnElAire && d < 2.6f
            && Time.time - nacio > 3f)
        {
            est = Est.Aturdida; reloj = 1.1f;
            GuardianParticulas.Rafaga(transform.position + Vector3.up * 1.2f, new Color(1f, 0.9f, 0.3f), Color.white, 10, 2f, 0.8f, 0.4f, 0f, GuardianParticulas.Tex.Estrella, true, ParticleSystemShapeType.Circle, 0.4f);
            GuardianTextos.Mostrar(transform.position + Vector3.up * 2f, "¡Espantada! +10", new Color(1f, 0.9f, 0.4f), 22);
            GuardianAudio.EnPunto(GuardianAudio.Pitido(2100f), transform.position, 0.5f, 0.9f, 1.2f, 25f);
            GuardianAnimaciones.Rebote(transform, 0.35f, 0.5f);
            gm.SumarPuntos(10, null, Color.white);
        }

        switch (est)
        {
            case Est.Merodear:
                if (Time.time > proxDestino || (destino - transform.position).sqrMagnitude < 0.5f)
                {
                    proxDestino = Time.time + Random.Range(2f, 4f);
                    destino = casa + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
                }
                Mover(destino, 2.2f);
                if (gm.cargaActual > 0 && d < 10f)
                {
                    est = Est.Perseguir;
                    GuardianTextos.Mostrar(transform.position + Vector3.up * 2f, "!", new Color(1f, 0.3f, 0.2f), 36);
                    GuardianAudio.EnPunto(GuardianAudio.Pitido(1700f), transform.position, 0.4f, 1f, 1.3f, 25f);
                }
                break;

            case Est.Perseguir:
                Mover(jugador.position, 5.6f, 720f);                   // el Guardián corre a 7.75: se puede escapar
                if (gm.cargaActual == 0 || d > 16f || Vector3.Distance(transform.position, casa) > 25f) est = Est.Huir;
                else if (d < 1.1f)
                {
                    TipoResiduo t;
                    Vector3 donde = transform.position - transform.forward * 1.5f;
                    if (gm.RobarResiduo(GuardianEnemigos.Suelo(donde), out t))
                    {
                        GuardianTextos.Mostrar(jugador.position + Vector3.up * 2.4f, "¡Te robó " + Residuo.Nombre(t) + "!", new Color(1f, 0.55f, 0.35f), 24);
                        GuardianParticulas.Rafaga(transform.position + Vector3.up * 0.6f, Residuo.Tinte(t), Color.white, 14, 3f, 0.6f, 0.25f, 0.5f);
                        GuardianAudio.EnPunto(GuardianAudio.Pitido(2400f), transform.position, 0.55f, 1.1f, 1.3f, 25f);
                        GuardianCameraHUD.Sacudir(0.2f);
                        GuardianIluminacion.Destello(new Color(1f, 0.5f, 0.1f), 0.3f);
                    }
                    est = Est.Huir; reloj = 5f;
                }
                break;

            case Est.Huir:
                Vector3 lejos = transform.position + (transform.position - jugador.position).normalized * 5f;
                Mover(lejos, 5f);
                if (reloj <= 0f) { est = Est.Merodear; casa = transform.position; }
                break;

            case Est.Aturdida:
                transform.Rotate(0f, 720f * Time.deltaTime, 0f);
                if (reloj <= 0f) { est = Est.Huir; reloj = 4f; }
                break;
        }

        // Animación procedural: trote (rebote) y cola que se menea.
        float vel = est == Est.Perseguir ? 18f : 9f;
        if (cuerpo != null) cuerpo.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(Time.time * vel)) * 0.08f, 0f);
        if (cola != null) cola.localRotation = Quaternion.Euler(-12f, Mathf.Sin(Time.time * vel * 0.7f) * 30f, 0f);
        PegarAlSuelo();
    }
}

// ====================================================================== HUMO TÓXICO

/// <summary>
/// Humo Tóxico: nube de smog que se arrastra hacia el Guardián. Dentro de ella
/// camina más lento y la contaminación sube. Se disuelve cuando se segrega
/// bien cerca (aire limpio) o sola a los 30 s, y luego reaparece en otro lado.
/// </summary>
public class NubeToxica : EnemigoBase
{
    private const float RADIO = 3.2f;
    private float vida = 30f;
    private bool disolviendo;
    private ParticleSystem ps;
    private float proxAviso;

    public static GameObject Crear(Vector3 pos)
    {
        GameObject g = new GameObject("Enemigo_HumoToxico");
        g.transform.position = pos + Vector3.up * 1.2f;
        ParticleSystem ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.6f, 4.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.10f, 0.11f, 0.08f, 0.85f), new Color(0.24f, 0.28f, 0.12f, 0.75f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 110;
        main.prewarm = true;
        var em = ps.emission; em.rateOverTime = 22f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = RADIO * 0.7f;
        var col = ps.colorOverLifetime; col.enabled = true;
        Gradient gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                   new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        col.color = gr;
        var rol = ps.rotationOverLifetime; rol.enabled = true; rol.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        var r = g.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = GuardianParticulas.Material(GuardianParticulas.Tex.Suave, false);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();

        // Ojos verdes que brillan dentro del humo: se lee como "enemigo", no como niebla.
        Material ojo = GuardianEnemigos.Mat(new Color(0.45f, 0.95f, 0.1f), true);
        GuardianEnemigos.Pieza(g.transform, PrimitiveType.Sphere, new Vector3(-0.35f, 0.3f, 0.9f), Vector3.one * 0.22f, ojo, "OjoI");
        GuardianEnemigos.Pieza(g.transform, PrimitiveType.Sphere, new Vector3(0.35f, 0.3f, 0.9f), Vector3.one * 0.22f, ojo, "OjoD");

        NubeToxica n = g.AddComponent<NubeToxica>();
        n.ps = ps;
        return g;
    }

    void Update()
    {
        if (!Activo || disolviendo) return;
        vida -= Time.deltaTime;
        if (vida <= 0f) { Disolver(false); return; }

        // Se arrastra hacia el Guardián (lento: se puede esquivar caminando).
        Vector3 obj = jugador.position + Vector3.up * 1.2f;
        Vector3 d = obj - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 3f * Time.deltaTime);
            transform.position += d.normalized * 1.6f * Time.deltaTime;
        }
        Vector3 p = transform.position; p.y = Mathf.Lerp(p.y, jugador.position.y + 1.2f + Mathf.Sin(Time.time * 1.5f) * 0.3f, 2f * Time.deltaTime);
        transform.position = p;

        if (DistJugador < RADIO)
        {
            if (pc != null) pc.multVelocidad = Mathf.Min(pc.multVelocidad, 0.5f);
            gm.SumarContaminacion(1.2f * Time.deltaTime);
            if (Time.time > proxAviso)
            {
                proxAviso = Time.time + 3f;
                GuardianTextos.Mostrar(jugador.position + Vector3.up * 2.3f, "¡Humo tóxico! Sal de la nube", new Color(0.7f, 1f, 0.3f), 22);
                GuardianIluminacion.Destello(new Color(0.4f, 0.6f, 0.1f), 0.35f);
            }
        }
    }

    /// <summary>La nube se deshace (por aire limpio = premio, o por tiempo) y reaparece lejos.</summary>
    public void Disolver(bool porAireLimpio)
    {
        if (disolviendo) return;
        disolviendo = true;
        if (porAireLimpio)
        {
            GuardianTextos.Mostrar(transform.position + Vector3.up, "¡Aire limpio! +15", new Color(0.6f, 1f, 0.8f), 24);
            GuardianParticulas.Rafaga(transform.position, new Color(0.7f, 1f, 0.9f), Color.white, 30, 5f, 1f, 0.35f, -0.2f, GuardianParticulas.Tex.Estrella, true, ParticleSystemShapeType.Sphere, 2f);
            if (gm != null) gm.SumarPuntos(15, null, Color.white);
        }
        if (ps != null) { var em = ps.emission; em.rateOverTime = 0f; }
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) if (!(r is ParticleSystemRenderer)) r.enabled = false;
        Invoke(nameof(Reaparecer), 9f);
    }

    private void Reaparecer()
    {
        if (jugador == null) return;
        float ang = Random.Range(0f, 360f);
        transform.position = GuardianEnemigos.Suelo(jugador.position + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * 22f) + Vector3.up * 1.2f;
        vida = 30f; disolviendo = false;
        if (ps != null) { var em = ps.emission; em.rateOverTime = 22f; }
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = true;
    }
}

// ====================================================================== JEFE: REY BASURÓN

/// <summary>
/// Rey Basurón (jefe de la Ribera del Shullcas). Un montón de bolsas y latas
/// que cobró vida. Telegrafía cada ataque (ojos rojos + brazo arriba) antes de:
///  - LANZAR una bolsa en arco: una sombra roja marca dónde va a caer.
///  - PISOTÓN si estás muy cerca: onda que empuja y quita vida.
/// No se le pega: se le vence SEGREGANDO BIEN (cada acierto = −1 de vida).
/// Mientras vive, la contaminación sube más rápido.
/// </summary>
public class JefeBasuron : EnemigoBase
{
    public int VidaMax = 6;
    public int Vida { get; private set; }
    public bool Muerto { get; private set; }
    public float VidaNormalizada => VidaMax > 0 ? (float)Vida / VidaMax : 0f;

    private Vector3 casa;
    private float proxAtaque, telegrafo;
    private bool pisoton;
    private Transform brazoI, brazoD, cuerpo;
    private Material ojos;

    public static GameObject Crear(Vector3 pos)
    {
        GameObject g = new GameObject("Jefe_ReyBasuron");
        g.transform.position = pos;
        Transform c = new GameObject("Cuerpo").transform; c.SetParent(g.transform, false);
        Material bolsa  = GuardianEnemigos.Mat(new Color(0.08f, 0.09f, 0.1f), false, 0.75f);
        Material bolsa2 = GuardianEnemigos.Mat(new Color(0.16f, 0.2f, 0.14f), false, 0.7f);
        Material lata   = GuardianEnemigos.Mat(new Color(0.85f, 0.72f, 0.2f), false, 0.8f);
        Material botella = GuardianEnemigos.Mat(new Color(0.3f, 0.75f, 0.45f), false, 0.9f);
        Material carton = GuardianEnemigos.Mat(new Color(0.55f, 0.4f, 0.25f), false, 0.1f);
        Material ojo    = new Material(GuardianEnemigos.Mat(new Color(0.7f, 1f, 0.2f), true));

        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(0, 1.3f, 0), new Vector3(2.6f, 2.2f, 2.2f), bolsa, "Panza");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(0.9f, 0.7f, 0.3f), new Vector3(1.2f, 1.1f, 1.1f), bolsa2, "BolsaD");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(-0.9f, 0.7f, -0.2f), new Vector3(1.3f, 1f, 1.1f), bolsa, "BolsaI");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(0, 2.75f, 0.2f), new Vector3(1.4f, 1.2f, 1.3f), bolsa2, "Cabeza");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(-0.3f, 2.85f, 0.8f), Vector3.one * 0.3f, ojo, "OjoI");
        GuardianEnemigos.Pieza(c, PrimitiveType.Sphere, new Vector3(0.3f, 2.85f, 0.8f), Vector3.one * 0.3f, ojo, "OjoD");
        // Corona de latas.
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            GuardianEnemigos.Pieza(c, PrimitiveType.Cylinder, new Vector3(Mathf.Sin(a) * 0.5f, 3.45f, Mathf.Cos(a) * 0.5f), new Vector3(0.22f, 0.22f, 0.22f), lata, "Lata" + i);
        }
        // Basura incrustada.
        GuardianEnemigos.Pieza(c, PrimitiveType.Capsule, new Vector3(0.7f, 1.6f, 1f), new Vector3(0.25f, 0.4f, 0.25f), botella, "Botella", new Vector3(30, 0, 60));
        GuardianEnemigos.Pieza(c, PrimitiveType.Cube, new Vector3(-0.8f, 1.8f, 0.8f), new Vector3(0.6f, 0.5f, 0.1f), carton, "Carton", new Vector3(10, 30, 15));
        GuardianEnemigos.Pieza(c, PrimitiveType.Cylinder, new Vector3(0.2f, 0.9f, 1.1f), new Vector3(0.2f, 0.18f, 0.2f), lata, "LataPanza", new Vector3(90, 0, 20));
        Transform bi = new GameObject("BrazoI").transform; bi.SetParent(c, false); bi.localPosition = new Vector3(-1.3f, 1.9f, 0);
        Transform bd = new GameObject("BrazoD").transform; bd.SetParent(c, false); bd.localPosition = new Vector3(1.3f, 1.9f, 0);
        GuardianEnemigos.Pieza(bi, PrimitiveType.Capsule, new Vector3(0, -0.7f, 0), new Vector3(0.6f, 0.8f, 0.6f), bolsa2, "Brazo");
        GuardianEnemigos.Pieza(bd, PrimitiveType.Capsule, new Vector3(0, -0.7f, 0), new Vector3(0.6f, 0.8f, 0.6f), bolsa2, "Brazo");
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            if (r.name == "OjoI" || r.name == "OjoD") r.sharedMaterial = ojo;
        g.transform.localScale = Vector3.one * 1.35f;

        JefeBasuron j = g.AddComponent<JefeBasuron>();
        j.brazoI = bi; j.brazoD = bd; j.cuerpo = c; j.ojos = ojo;
        return g;
    }

    protected override void Start()
    {
        base.Start();
        Vida = VidaMax;
        casa = transform.position;
        proxAtaque = Time.time + 4f;
    }

    /// <summary>Un residuo bien segregado = un golpe al jefe.</summary>
    public void Danar(int n)
    {
        if (Muerto) return;
        Vida = Mathf.Max(0, Vida - n);
        GuardianAnimaciones.Rebote(transform, 0.3f, 0.6f);
        GuardianParticulas.Rafaga(transform.position + Vector3.up * 3f, new Color(0.3f, 0.3f, 0.3f), new Color(0.9f, 0.8f, 0.3f), 30, 6f, 1f, 0.4f, 1f, GuardianParticulas.Tex.Cuadro, false, ParticleSystemShapeType.Sphere, 1f, true);
        GuardianTextos.Mostrar(transform.position + Vector3.up * 5f, "−" + n + "  REY BASURÓN", new Color(1f, 0.75f, 0.3f), 28);
        if (Vida <= 0) Morir();
    }

    private void Morir()
    {
        Muerto = true;
        GuardianCameraHUD.Sacudir(0.6f);
        for (int i = 0; i < 4; i++)
            GuardianParticulas.FuegoArtificial(transform.position + Vector3.up * (4f + i) + Random.insideUnitSphere * 3f, Residuo.Tinte(Residuo.Desde(i)));
        GuardianParticulas.Humo(transform.position + Vector3.up * 2f, new Color(0.3f, 0.3f, 0.3f, 0.8f), 40, 3f);
        GuardianAudio.EnPantalla(GuardianAudio.Ganaste, 0.7f);
        if (gm != null)
        {
            gm.SumarPuntos(150, "¡Derrotaste al REY BASURÓN! La ribera respira · +150", new Color(0.5f, 1f, 0.7f));
            gm.SumarContaminacion(-25f);
        }
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
    }

    void Update()
    {
        if (Muerto || !Activo) return;

        // Mientras vive, ensucia la ribera.
        gm.SumarContaminacion(0.12f * Time.deltaTime);

        float d = DistJugador;
        // Avanza lento hacia el Guardián sin alejarse mucho de su guarida.
        if (telegrafo <= 0f && d > 5f && d < 40f && Vector3.Distance(transform.position, casa) < 14f)
            Mover(jugador.position, 1.4f, 90f);
        else
        {
            Vector3 dd = jugador.position - transform.position; dd.y = 0f;
            if (dd.sqrMagnitude > 0.1f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dd), 120f * Time.deltaTime);
        }

        // Balanceo al caminar.
        if (cuerpo != null) cuerpo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.5f) * 5f);

        // --- Ataques con TELEGRAFIADO (0.9 s de aviso) ---
        if (telegrafo > 0f)
        {
            telegrafo -= Time.deltaTime;
            float k = 1f - telegrafo / 0.9f;
            if (ojos != null) { ojos.SetColor("_BaseColor", Color.red); ojos.SetColor("_EmissionColor", Color.red * (1f + k * 2.5f)); }
            Transform b = pisoton ? brazoI : brazoD;
            if (b != null) b.localRotation = Quaternion.Euler(0f, 0f, (pisoton ? -1f : 1f) * Mathf.Lerp(0f, 150f, k));
            if (pisoton && brazoD != null) brazoD.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 150f, k));
            if (telegrafo <= 0f) Atacar();
        }
        else
        {
            if (ojos != null) { ojos.SetColor("_BaseColor", new Color(0.7f, 1f, 0.2f)); ojos.SetColor("_EmissionColor", new Color(0.7f, 1f, 0.2f) * 1.3f); }
            if (brazoI != null) brazoI.localRotation = Quaternion.Euler(0f, 0f, -10f + Mathf.Sin(Time.time * 2.5f) * 12f);
            if (brazoD != null) brazoD.localRotation = Quaternion.Euler(0f, 0f, 10f - Mathf.Sin(Time.time * 2.5f) * 12f);

            if (Time.time > proxAtaque && d < 32f)
            {
                pisoton = d < 5.5f;
                telegrafo = 0.9f;
                proxAtaque = Time.time + (pisoton ? 2.5f : Mathf.Lerp(2.4f, 4f, VidaNormalizada));   // más agresivo herido
                GuardianAudio.EnPunto(GuardianAudio.Pitido(pisoton ? 110f : 160f), transform.position, 0.6f, 0.8f, 0.9f, 50f);
            }
        }
        PegarAlSuelo();
    }

    private void Atacar()
    {
        if (brazoI != null) brazoI.localRotation = Quaternion.identity;
        if (brazoD != null) brazoD.localRotation = Quaternion.identity;
        if (pisoton)
        {
            GuardianParticulas.Anillo(transform.position, new Color(0.6f, 0.45f, 0.3f), 14f, 0.6f);
            GuardianParticulas.Rafaga(transform.position + Vector3.up * 0.3f, new Color(0.55f, 0.48f, 0.38f, 0.8f), new Color(0.4f, 0.35f, 0.3f, 0.6f),
                                      40, 8f, 1f, 1.2f, 0.2f, GuardianParticulas.Tex.Suave, false, ParticleSystemShapeType.Circle, 1.5f, false, -0.5f);
            GuardianCameraHUD.Sacudir(0.5f);
            if (DistJugador < 6f) GolpearJugador(transform.position, 13f);
        }
        else
        {
            // Lanza una bolsa hacia donde VA a estar el jugador (predicción simple).
            Vector3 destino = jugador.position;
            CharacterController cc = jugador.GetComponent<CharacterController>();
            if (cc != null) destino += new Vector3(cc.velocity.x, 0f, cc.velocity.z) * 0.6f;
            ProyectilBasura.Lanzar(transform.position + Vector3.up * 3.6f + transform.right * 1.6f, GuardianEnemigos.Suelo(destino));
        }
    }

    public static void GolpearJugador(Vector3 desde, float empuje)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || Time.time - GuardianEnemigos.ultimoGolpe < 1.5f) return;
        GuardianEnemigos.ultimoGolpe = Time.time;
        gm.PerderVida();
        GuardianEventos.AvisarGolpe(desde);
        GameObject pj = GameObject.FindGameObjectWithTag("Player");
        PlayerController pc = pj != null ? pj.GetComponent<PlayerController>() : null;
        if (pc != null) { Vector3 d = pj.transform.position - desde; d.y = 0f; pc.Empujar(d.normalized, empuje); }
    }
}

/// <summary>Bolsa de basura lanzada por el jefe: vuela en arco; la sombra roja avisa dónde cae.</summary>
public class ProyectilBasura : MonoBehaviour
{
    private Vector3 a, b;
    private float t, dur = 1.25f;
    private Transform marca;

    public static void Lanzar(Vector3 desde, Vector3 hasta)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = "Proyectil_Bolsa";
        Destroy(g.GetComponent<Collider>());
        g.transform.position = desde;
        g.transform.localScale = new Vector3(0.8f, 0.7f, 0.8f);
        g.GetComponent<Renderer>().sharedMaterial = GuardianEnemigos.Mat(new Color(0.07f, 0.07f, 0.08f), false, 0.8f);
        ProyectilBasura p = g.AddComponent<ProyectilBasura>();
        p.a = desde; p.b = hasta;

        // Marca de impacto en el suelo (telegrafiado): disco rojo que late.
        GameObject m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        m.name = "Marca_Impacto";
        Destroy(m.GetComponent<Collider>());
        m.transform.position = hasta + Vector3.up * 0.05f;
        m.transform.localScale = new Vector3(3.2f, 0.01f, 3.2f);
        m.GetComponent<Renderer>().sharedMaterial = GuardianEnemigos.Mat(new Color(0.95f, 0.15f, 0.1f), true);
        m.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        p.marca = m.transform;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.estado != GameManager.Estado.Jugando) { Limpiar(); return; }
        t += Time.deltaTime / dur;
        Vector3 p = Vector3.Lerp(a, b, t);
        p.y += Mathf.Sin(t * Mathf.PI) * 5f;
        transform.position = p;
        transform.Rotate(300f * Time.deltaTime, 200f * Time.deltaTime, 0f);
        if (marca != null)
        {
            float s = Mathf.Lerp(3.2f, 1.8f, t) * (1f + Mathf.Sin(Time.time * 20f) * 0.08f);
            marca.localScale = new Vector3(s, 0.01f, s);
        }
        if (t >= 1f)
        {
            GuardianParticulas.Rafaga(b + Vector3.up * 0.3f, new Color(0.15f, 0.15f, 0.15f), new Color(0.5f, 0.45f, 0.3f), 30, 6f, 0.9f, 0.35f, 1.2f,
                                      GuardianParticulas.Tex.Cuadro, false, ParticleSystemShapeType.Hemisphere, 0.3f, true);
            GuardianParticulas.Anillo(b, new Color(0.9f, 0.3f, 0.2f), 4f, 0.4f);
            GuardianAudio.EnPunto(GuardianAudio.Error, b, 0.5f, 0.7f, 0.8f, 30f);
            GameObject pj = GameObject.FindGameObjectWithTag("Player");
            if (pj != null)
            {
                Vector3 d = pj.transform.position - b; d.y = 0f;
                if (d.magnitude < 1.9f) JefeBasuron.GolpearJugador(b, 8f);
            }
            if (gm != null) gm.SumarContaminacion(1f);
            Limpiar();
        }
    }

    private void Limpiar()
    {
        if (marca != null) Destroy(marca.gameObject);
        Destroy(gameObject);
    }
}
