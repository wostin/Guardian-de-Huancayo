using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Guardián de Huancayo - CONVERSACIONES.
///
/// El Guardián habla con la gente de la ciudad. Hay dos tipos de diálogo:
///  1) Al entrar a cada zona hay una pequeña escena de presentación
///     (policía en la Plaza, casera en el Mercado, vecino en la Ribera).
///  2) En cualquier momento se puede hablar con policías, peatones y
///     vendedores: al acercarse aparece un globo "E · Hablar".
///
/// Cuadro de diálogo estilo novela visual: RETRATO 3D de quien habla
/// (se renderiza la cara del personaje real de la escena), nombre y frases
/// cortas que aparecen letra por letra. Mientras se conversa el juego se
/// congela (no corre el tiempo ni sube la contaminación).
///
/// Controles: E / Enter / clic = siguiente · Q = saltar todo.
/// Se crea solo al dar Play (no hay que arrastrar nada a la escena).
/// </summary>
public class GuardianConversacion : MonoBehaviour
{
    public static GuardianConversacion Instance { get; private set; }
    public static bool Hablando => Instance != null && Instance.activa;

    // ---------- Datos ----------
    private struct Linea
    {
        public int quien;      // 0 = NPC, 1 = Guardián
        public string texto;
        public Linea(int q, string t) { quien = q; texto = t; }
    }

    private enum Tipo { Policia, Peaton, Vendedor }

    private class Hablante
    {
        public Transform t;
        public Tipo tipo;
        public string nombre;
        public int veces;      // cuántas veces ya habló con él
        public int semilla;    // para variar nombre y charla
    }

    private const string GUARDIAN = "Guardián";
    private const float DISTANCIA_HABLAR = 3.2f;
    private const float DISTANCIA_GLOBO = 16f;
    private const float LETRAS_POR_SEG = 48f;

    private static readonly string[] NOMBRES_PEATON =
        { "Vecina Carmen", "Don Teófilo", "Joven Kevin", "Señora Delia", "Don Eusebio", "Estudiante Lucía" };
    private static readonly string[] NOMBRES_POLICIA = { "Agente Quispe", "Agente Huamán" };
    private static readonly string[] NOMBRES_VENDEDOR = { "Casera Juana", "Don Mauro", "Casera Nely" };

    private static readonly Linea[][] CHARLA_POLICIA =
    {
        new[] { new Linea(0, "Yo recojo lo que botan a la calle..."),
                new Linea(0, "...pero segregar te toca a ti, Guardián."),
                new Linea(1, "¡Entendido, jefa!") },
        new[] { new Linea(0, "Cruza siempre por el crucero."),
                new Linea(1, "Los carros no perdonan, ya sé.") },
        new[] { new Linea(0, "Si ves a alguien botando basura, avísame."),
                new Linea(1, "Mejor le enseño a separarla.") },
    };

    private static readonly Linea[][] CHARLA_PEATON =
    {
        new[] { new Linea(0, "¿La botella de vidrio va al verde?"),
                new Linea(1, "¡Sí! Verde = vidrio.") },
        new[] { new Linea(0, "Antes el Shullcas bajaba cristalino..."),
                new Linea(1, "Y volverá a bajar así.") },
        new[] { new Linea(0, "Gracias por limpiar mi cuadra."),
                new Linea(1, "¡Ayúdame separando en casa!") },
        new[] { new Linea(0, "¿Y las cáscaras de papa?"),
                new Linea(1, "Al marrón: orgánicos."),
                new Linea(0, "¡Ah, para compost!") },
        new[] { new Linea(0, "El periódico viejo, ¿dónde lo boto?"),
                new Linea(1, "Al azul: papel y cartón.") },
        new[] { new Linea(0, "¡Uy, la lata de gaseosa!"),
                new Linea(1, "Amarillo: metales.") },
    };

    private static readonly Linea[][] CHARLA_VENDEDOR =
    {
        new[] { new Linea(0, "¡Papa huayro, oca, olluco! ¿Qué lleva?"),
                new Linea(1, "Hoy solo basura, casera."),
                new Linea(0, "¡Jajay! Llévate la de mi puesto.") },
        new[] { new Linea(0, "Las bolsas plásticas vuelan por todo el mercado."),
                new Linea(1, "Al blanco. ¡Y mejor usa tu canasta!") },
        new[] { new Linea(0, "Trucha fresquita del Mantaro..."),
                new Linea(1, "Si el río está limpio, ¡más trucha!") },
    };

    // Presentación al entrar a cada zona.
    private static readonly string[] INTRO_NOMBRE = { "Sub. Rosa", "Casera Juana", "Don Teodoro" };
    private static readonly Tipo[] INTRO_TIPO = { Tipo.Policia, Tipo.Vendedor, Tipo.Peaton };
    private static readonly Linea[][] INTRO =
    {
        new[] { new Linea(0, "¡Guardián! La plaza amaneció llena de basura."),
                new Linea(1, "Tranquila, yo la recojo."),
                new Linea(0, "Pero sepárala: cada residuo a su color."),
                new Linea(1, "¡Blanco, verde, azul, amarillo y marrón!") },
        new[] { new Linea(0, "¡Joven! El Mayorista está hecho un basural."),
                new Linea(1, "Para eso vine, casera."),
                new Linea(0, "Apúrate, aquí se ensucia rapidito."),
                new Linea(1, "¡Y cuidado con los carros!") },
        new[] { new Linea(0, "El Shullcas se nos está muriendo, hijo..."),
                new Linea(1, "No mientras yo esté aquí."),
                new Linea(0, "Río abajo vive el Rey Basurón."),
                new Linea(1, "Que se prepare.") },
    };

    // ---------- Estado ----------
    private readonly List<Hablante> hablantes = new List<Hablante>();
    private readonly HashSet<int> introVista = new HashSet<int>();
    private Transform jugador;
    private float proximoEscaneo;

    private bool activa;
    private Linea[] lineas;
    private int idx;
    private float inicioLinea;
    private Hablante actual;           // NPC con quien se conversa (puede ser null)
    private string nombreNPC;
    private Tipo tipoNPC;
    private float escalaAntes = 1f;

    private Hablante cercano;
    private bool estabaJugando;
    private float introEn = -1f;

    // Retratos
    private Camera camRetrato;
    private RenderTexture rtNPC, rtGuardian;

    // Texturas de UI
    private Texture2D texBlanca, texGlobo, texCirculo;

    // ---------- Arranque automático ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("GuardianConversacion");
        go.AddComponent<GuardianConversacion>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        texBlanca = Texture2D.whiteTexture;
        texGlobo = CrearGlobo(64);
        texCirculo = CrearCirculo(64);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        LiberarRetratos();
        if (camRetrato != null) Destroy(camRetrato.gameObject);
    }

    // ---------- Bucle ----------
    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        if (activa)
        {
            // La pausa (ESC) tiene prioridad; al volver de la pausa se congela otra vez.
            if (!gm.pausado) Time.timeScale = 0f;
            if (gm.estado != GameManager.Estado.Jugando) { Terminar(); return; }
            if (!gm.pausado) LeerAvance();
            return;
        }

        bool jugando = gm.estado == GameManager.Estado.Jugando && !gm.pausado;

        // Presentación de zona: una vez por zona y por sesión, al empezar a jugarla.
        if (jugando && !estabaJugando) introEn = Time.unscaledTime + 0.7f;
        estabaJugando = jugando;
        if (jugando && introEn > 0f && Time.unscaledTime >= introEn)
        {
            introEn = -1f;
            int z = Mathf.Clamp(gm.zonaActual, 0, INTRO.Length - 1);
            if (!gm.modoRecorrido && !introVista.Contains(z))
            {
                introVista.Add(z);
                Hablante h = MasCercano(INTRO_TIPO[z], 40f);
                Empezar(INTRO[z], h, INTRO_NOMBRE[z], INTRO_TIPO[z]);
                return;
            }
        }

        if (!jugando || jugador == null) { cercano = null; return; }

        if (Time.unscaledTime >= proximoEscaneo)
        {
            proximoEscaneo = Time.unscaledTime + 2f;
            Escanear();
        }

        cercano = MasCercano(null, DISTANCIA_HABLAR);
        if (cercano != null && TeclaHablar()) Hablar(cercano);
    }

    // ---------- Buscar personajes ----------
    private void Escanear()
    {
        hablantes.RemoveAll(h => h.t == null);
        HashSet<Transform> ya = new HashSet<Transform>();
        foreach (Hablante h in hablantes) ya.Add(h.t);

        foreach (Aliado a in FindObjectsByType<Aliado>(FindObjectsSortMode.None))
            Registrar(a.transform, Tipo.Policia, ya);
        foreach (Peaton p in FindObjectsByType<Peaton>(FindObjectsSortMode.None))
            Registrar(p.transform, Tipo.Peaton, ya);

        // Vendedores del mercado: los arma el editor con "Vendedor" en el nombre.
        foreach (Animator an in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            Transform raiz = BuscarVendedor(an.transform);
            if (raiz != null) Registrar(raiz, Tipo.Vendedor, ya);
        }
        foreach (SkinnedMeshRenderer sk in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
        {
            Transform raiz = BuscarVendedor(sk.transform);
            if (raiz != null) Registrar(raiz, Tipo.Vendedor, ya);
        }
    }

    private static Transform BuscarVendedor(Transform t)
    {
        Transform encontrado = null;
        for (Transform x = t; x != null; x = x.parent)
            if (x.name.ToLower().Contains("vendedor")) encontrado = x;   // el más alto con ese nombre
        return encontrado;
    }

    private void Registrar(Transform t, Tipo tipo, HashSet<Transform> ya)
    {
        if (t == null || ya.Contains(t)) return;
        if (jugador != null && (t == jugador || t.IsChildOf(jugador))) return;
        if (!EsPersona(t)) return;     // nada de perros, palomas, carros ni triciclos
        ya.Add(t);
        string[] nombres = tipo == Tipo.Policia ? NOMBRES_POLICIA
                         : tipo == Tipo.Vendedor ? NOMBRES_VENDEDOR : NOMBRES_PEATON;
        int semilla = hablantes.Count;
        hablantes.Add(new Hablante { t = t, tipo = tipo, nombre = nombres[semilla % nombres.Length], semilla = semilla });
    }


    // Solo conversan PERSONAS: modelo con esqueleto (SkinnedMeshRenderer) y que
    // no sea un animal ni vaya montado en un vehículo.
    private static readonly string[] NO_PERSONA =
        { "perro", "dog", "paloma", "pigeon", "bird", "gato", "rata",
          "carro", "auto", "moto", "mototaxi", "bici", "bike", "triciclo",
          "carretilla", "camion", "truck", "combi", "vehic", "taxi" };

    private static bool EsPersona(Transform t)
    {
        for (Transform x = t; x != null; x = x.parent)
        {
            string n = x.name.ToLower();
            foreach (string malo in NO_PERSONA)
                if (n.Contains(malo)) return false;
            if (x.GetComponent<Contaminante>() != null || x.GetComponent<Bicicleta>() != null
                || x.GetComponent<CamionRecolector>() != null || x.GetComponent<Paloma>() != null)
                return false;
        }
        Animator an = t.GetComponentInChildren<Animator>();
        if (an != null && an.isHuman) return true;
        return t.GetComponentInChildren<SkinnedMeshRenderer>() != null;
    }

    private Hablante MasCercano(Tipo? tipo, float maxDist)
    {
        if (jugador == null) return null;
        if (hablantes.Count == 0) Escanear();
        Hablante mejor = null;
        float mejorD = maxDist * maxDist;
        foreach (Hablante h in hablantes)
        {
            if (h.t == null || !h.t.gameObject.activeInHierarchy) continue;
            if (tipo.HasValue && h.tipo != tipo.Value) continue;
            float d = (h.t.position - jugador.position).sqrMagnitude;
            if (d < mejorD) { mejorD = d; mejor = h; }
        }
        return mejor;
    }

    // ---------- Conversación ----------
    private void Hablar(Hablante h)
    {
        Linea[][] banco = h.tipo == Tipo.Policia ? CHARLA_POLICIA
                        : h.tipo == Tipo.Vendedor ? CHARLA_VENDEDOR : CHARLA_PEATON;
        int i = (h.semilla + h.veces) % banco.Length;
        h.veces++;
        Empezar(banco[i], h, h.nombre, h.tipo);
    }

    private void Empezar(Linea[] ls, Hablante h, string nombre, Tipo tipo)
    {
        lineas = ls; idx = 0; actual = h; nombreNPC = nombre; tipoNPC = tipo;
        inicioLinea = Time.unscaledTime;
        activa = true;
        cercano = null;

        // Se miran de frente.
        if (h != null && h.t != null && jugador != null)
        {
            Vector3 a = jugador.position - h.t.position; a.y = 0f;
            if (a.sqrMagnitude > 0.01f && a.magnitude < 6f)
            {
                h.t.rotation = Quaternion.LookRotation(a);
                jugador.rotation = Quaternion.LookRotation(-a);
            }
        }

        RenderRetratos();
        escalaAntes = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
    }

    private void Terminar()
    {
        activa = false;
        lineas = null;
        actual = null;
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.pausado) Time.timeScale = escalaAntes;
        LiberarRetratos();
    }

    private void Siguiente()
    {
        if (lineas == null) { Terminar(); return; }
        int largo = lineas[idx].texto.Length;
        float visibles = (Time.unscaledTime - inicioLinea) * LETRAS_POR_SEG;
        if (visibles < largo) { inicioLinea = -999f; return; }   // completa la frase primero
        idx++;
        inicioLinea = Time.unscaledTime;
        if (idx >= lineas.Length) Terminar();
    }

    private void LeerAvance()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        if (kb != null && kb.qKey.wasPressedThisFrame) { Terminar(); return; }
        bool sig = (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame
                                   || kb.numpadEnterKey.wasPressedThisFrame))
                || (gp != null && gp.buttonSouth.wasPressedThisFrame);
#else
        if (Input.GetKeyDown(KeyCode.Q)) { Terminar(); return; }
        bool sig = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
#endif
        if (sig) Siguiente();
    }

    private static bool TeclaHablar()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        return (kb != null && kb.eKey.wasPressedThisFrame)
            || (gp != null && gp.buttonWest.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    // ---------- Retratos 3D ----------
    private void RenderRetratos()
    {
        LiberarRetratos();
        if (actual != null && actual.t != null) rtNPC = Retrato(actual.t);
        if (jugador != null) rtGuardian = Retrato(jugador);
    }

    private const int CAPA_RETRATO = 31;

    private RenderTexture Retrato(Transform quien)
    {
        // Solo se dibuja el personaje: sus renderers pasan un instante a una capa
        // propia y la cámara del retrato ve únicamente esa capa (antes salía
        // también la vereda, postes y lo que hubiera delante de la cara).
        List<KeyValuePair<GameObject, int>> capas = new List<KeyValuePair<GameObject, int>>();
        try
        {
            if (camRetrato == null)
            {
                GameObject c = new GameObject("CamaraRetrato");
                c.hideFlags = HideFlags.HideAndDontSave;
                camRetrato = c.AddComponent<Camera>();
                camRetrato.enabled = false;
                camRetrato.fieldOfView = 22f;
                camRetrato.nearClipPlane = 0.05f;
                camRetrato.farClipPlane = 20f;
                camRetrato.clearFlags = CameraClearFlags.SolidColor;
                camRetrato.cullingMask = 1 << CAPA_RETRATO;
            }

            Vector3 cabeza;
            if (!BuscarCabeza(quien, out cabeza)) return null;
            Vector3 frente = quien.forward; frente.y = 0f;
            if (frente.sqrMagnitude < 0.01f) frente = Vector3.forward;
            frente.Normalize();

            foreach (Renderer r in quien.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || !(r is SkinnedMeshRenderer || r is MeshRenderer)) continue;
                capas.Add(new KeyValuePair<GameObject, int>(r.gameObject, r.gameObject.layer));
                r.gameObject.layer = CAPA_RETRATO;
            }

            camRetrato.backgroundColor = new Color(0.16f, 0.20f, 0.28f);
            camRetrato.transform.position = cabeza + frente * 1.15f + Vector3.up * 0.02f;
            camRetrato.transform.LookAt(cabeza - Vector3.up * 0.06f);

            RenderTexture rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 2;
            camRetrato.targetTexture = rt;
            camRetrato.Render();
            camRetrato.targetTexture = null;
            return rt;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Conversacion] No se pudo hacer el retrato: " + e.Message);
            return null;
        }
        finally
        {
            foreach (var kv in capas) if (kv.Key != null) kv.Key.layer = kv.Value;
        }
    }

    /// <summary>
    /// Cabeza del personaje: hueso humanoide, o un hueso llamado "head"/"cabeza"
    /// (los modelos del pack son Generic), o la parte alta SOLO de su malla con
    /// esqueleto (no de flechas, mochila u otros objetos colgados del jugador).
    /// </summary>
    private static bool BuscarCabeza(Transform quien, out Vector3 cabeza)
    {
        cabeza = Vector3.zero;
        Animator an = quien.GetComponentInChildren<Animator>();
        if (an != null && an.isHuman)
        {
            Transform h = an.GetBoneTransform(HumanBodyBones.Head);
            if (h != null) { cabeza = h.position + Vector3.up * 0.08f; return true; }
        }
        foreach (Transform t in quien.GetComponentsInChildren<Transform>())
        {
            string n = t.name.ToLower();
            if ((n.EndsWith("head") || n.Contains("head ") || n == "cabeza" || n.EndsWith(":head") || n.EndsWith("_head"))
                && !n.Contains("end") && !n.Contains("top"))
            { cabeza = t.position + Vector3.up * 0.08f; return true; }
        }
        Bounds b = new Bounds(); bool hay = false;
        foreach (SkinnedMeshRenderer r in quien.GetComponentsInChildren<SkinnedMeshRenderer>())
        { if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds); }
        if (!hay && !Limites(quien, out b)) return false;
        cabeza = new Vector3(b.center.x, b.max.y - b.size.y * 0.10f, b.center.z);
        return true;
    }

    private static bool Limites(Transform t, out Bounds b)
    {
        b = new Bounds();
        bool hay = false;
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
        }
        return hay;
    }

    private void LiberarRetratos()
    {
        if (rtNPC != null) { rtNPC.Release(); Destroy(rtNPC); rtNPC = null; }
        if (rtGuardian != null) { rtGuardian.Release(); Destroy(rtGuardian); rtGuardian = null; }
    }

    // ---------- Dibujo ----------
    private static Color ColorDe(int quien, Tipo t)
    {
        if (quien == 1) return new Color(0.20f, 0.78f, 0.42f);              // Guardián: verde
        switch (t)
        {
            case Tipo.Policia:  return new Color(0.25f, 0.55f, 0.95f);
            case Tipo.Vendedor: return new Color(0.95f, 0.55f, 0.20f);
            default:            return new Color(0.85f, 0.35f, 0.40f);
        }
    }

    void OnGUI()
    {
        GUI.depth = -50;   // por encima del HUD
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.pausado) return;

        float k = Mathf.Clamp(Screen.height / 900f, 0.7f, 2.4f);

        if (activa && lineas != null && idx < lineas.Length) { DibujarDialogo(k); return; }

        if (gm.estado != GameManager.Estado.Jugando || jugador == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        // Globitos "..." sobre quienes tienen algo que contar.
        foreach (Hablante h in hablantes)
        {
            if (h.t == null || !h.t.gameObject.activeInHierarchy || h == cercano) continue;
            float d = Vector3.Distance(h.t.position, jugador.position);
            if (d > DISTANCIA_GLOBO) continue;
            Vector3 s = cam.WorldToScreenPoint(h.t.position + Vector3.up * 2.3f);
            if (s.z <= 0f) continue;
            float tam = Mathf.Lerp(34f, 20f, d / DISTANCIA_GLOBO) * k;
            Rect r = new Rect(s.x - tam / 2f, Screen.height - s.y - tam, tam, tam);
            GUI.color = new Color(1f, 1f, 1f, h.veces == 0 ? 0.95f : 0.45f);
            GUI.DrawTexture(r, texGlobo);
            GUI.color = ColorDe(0, h.tipo);
            GUI.Label(new Rect(r.x, r.y - tam * 0.08f, r.width, r.height * 0.8f), h.veces == 0 ? "!" : "...",
                      Estilo(Mathf.RoundToInt(tam * 0.55f), FontStyle.Bold, ColorDe(0, h.tipo), TextAnchor.MiddleCenter));
            GUI.color = Color.white;
        }

        // Aviso para hablar con el más cercano (botón: también sirve en celular).
        if (cercano != null && cercano.t != null)
        {
            Vector3 s = cam.WorldToScreenPoint(cercano.t.position + Vector3.up * 2.4f);
            if (s.z > 0f)
            {
                float w = 150f * k, h = 46f * k;
                Rect r = new Rect(s.x - w / 2f, Screen.height - s.y - h, w, h);
                GUI.color = new Color(0f, 0f, 0f, 0.65f);
                GUI.DrawTexture(r, texBlanca);
                GUI.color = Color.white;
                Rect tecla = new Rect(r.x + 8f * k, r.y + 8f * k, 30f * k, 30f * k);
                GUI.DrawTexture(tecla, texBlanca);
                GUI.Label(tecla, "E", Estilo(Mathf.RoundToInt(18 * k), FontStyle.Bold, Color.black, TextAnchor.MiddleCenter));
                GUI.DrawTexture(new Rect(r.x + 46f * k, r.y + 9f * k, 28f * k, 28f * k), texGlobo);
                GUI.Label(new Rect(r.x + 80f * k, r.y, w - 84f * k, h), "Hablar",
                          Estilo(Mathf.RoundToInt(17 * k), FontStyle.Bold, Color.white, TextAnchor.MiddleLeft));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) Hablar(cercano);
            }
        }
    }

    private void DibujarDialogo(float k)
    {
        Linea l = lineas[idx];
        bool esGuardian = l.quien == 1;
        Color c = ColorDe(l.quien, tipoNPC);
        float W = Screen.width, H = Screen.height;

        // Oscurecer un poco el fondo para enfocar la charla.
        GUI.color = new Color(0f, 0f, 0f, 0.25f);
        GUI.DrawTexture(new Rect(0, 0, W, H), texBlanca);

        float margen = 30f * k;
        float alto = 190f * k;
        float foto = 170f * k;
        Rect caja = new Rect(margen, H - alto - margen, W - margen * 2f, alto);

        // Caja
        GUI.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        GUI.DrawTexture(caja, texBlanca);
        GUI.color = c;
        GUI.DrawTexture(new Rect(caja.x, caja.y, caja.width, 5f * k), texBlanca);

        // Retrato: NPC a la izquierda, Guardián a la derecha.
        RenderTexture rt = esGuardian ? rtGuardian : rtNPC;
        Rect rFoto = esGuardian
            ? new Rect(caja.xMax - foto - 12f * k, caja.y - foto * 0.45f, foto, foto)
            : new Rect(caja.x + 12f * k, caja.y - foto * 0.45f, foto, foto);
        GUI.color = c;
        GUI.DrawTexture(new Rect(rFoto.x - 5f * k, rFoto.y - 5f * k, rFoto.width + 10f * k, rFoto.height + 10f * k), texBlanca);
        GUI.color = Color.white;
        if (rt != null) GUI.DrawTexture(rFoto, rt, ScaleMode.ScaleAndCrop);
        else
        {
            GUI.color = new Color(0.16f, 0.20f, 0.28f);
            GUI.DrawTexture(rFoto, texBlanca);
            GUI.color = c;
            GUI.DrawTexture(new Rect(rFoto.x + foto * 0.15f, rFoto.y + foto * 0.15f, foto * 0.7f, foto * 0.7f), texCirculo);
            GUI.color = Color.white;
            string ini = (esGuardian ? GUARDIAN : nombreNPC).Substring(0, 1);
            GUI.Label(rFoto, ini, Estilo(Mathf.RoundToInt(70 * k), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
        }

        // Nombre
        string nombre = esGuardian ? GUARDIAN : nombreNPC;
        float xTexto = esGuardian ? caja.x + 30f * k : rFoto.xMax + 26f * k;
        float anchoTexto = caja.width - foto - 70f * k;
        Rect rNombre = new Rect(xTexto, caja.y + 14f * k, 260f * k, 34f * k);
        GUI.color = c;
        GUI.DrawTexture(rNombre, texBlanca);
        GUI.color = Color.white;
        GUI.Label(rNombre, nombre, Estilo(Mathf.RoundToInt(20 * k), FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));

        // Texto letra por letra
        int visibles = Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - inicioLinea) * LETRAS_POR_SEG), 0, l.texto.Length);
        if (inicioLinea < 0f) visibles = l.texto.Length;
        GUIStyle st = Estilo(Mathf.RoundToInt(30 * k), FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        st.wordWrap = true;
        GUI.Label(new Rect(xTexto, caja.y + 62f * k, anchoTexto, alto - 80f * k), l.texto.Substring(0, visibles), st);

        // Indicadores: puntos de progreso + "E >"
        float px = esGuardian ? caja.x + 30f * k : rFoto.xMax + 26f * k;
        for (int i = 0; i < lineas.Length; i++)
        {
            GUI.color = i == idx ? c : new Color(1f, 1f, 1f, 0.3f);
            GUI.DrawTexture(new Rect(px + i * 20f * k, caja.yMax - 24f * k, 12f * k, 12f * k), texCirculo);
        }
        GUI.color = new Color(1f, 1f, 1f, 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)));
        float xFin = esGuardian ? rFoto.x - 130f * k : caja.xMax - 130f * k;
        GUI.Label(new Rect(xFin, caja.yMax - 40f * k, 120f * k, 30f * k), "E  >",
                  Estilo(Mathf.RoundToInt(22 * k), FontStyle.Bold, Color.white, TextAnchor.MiddleRight));
        GUI.color = new Color(1f, 1f, 1f, 0.45f);
        GUI.Label(new Rect(xFin - 160f * k, caja.yMax - 40f * k, 150f * k, 30f * k), "Q = saltar",
                  Estilo(Mathf.RoundToInt(14 * k), FontStyle.Normal, Color.white, TextAnchor.MiddleRight));
        GUI.color = Color.white;

        // Clic / toque en la caja = siguiente (sirve en celular)
        if (GUI.Button(caja, GUIContent.none, GUIStyle.none)) Siguiente();
    }

    private static GUIStyle Estilo(int tam, FontStyle f, Color c, TextAnchor a)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(8, tam), fontStyle = f, alignment = a };
        s.normal.textColor = c;
        return s;
    }

    // ---------- Texturas generadas ----------
    private static Texture2D CrearCirculo(int n)
    {
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
            }
        t.Apply();
        return t;
    }

    /// <summary>Globo de diálogo blanco (círculo + colita abajo a la izquierda).</summary>
    private static Texture2D CrearGlobo(int n)
    {
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        Vector2 c = new Vector2(n * 0.5f, n * 0.58f);
        float r = n * 0.40f;
        Vector2 a = new Vector2(n * 0.22f, n * 0.30f), b = new Vector2(n * 0.42f, n * 0.25f), p = new Vector2(n * 0.12f, n * 0.04f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Vector2 q = new Vector2(x + 0.5f, y + 0.5f);
                float alfa = Mathf.Clamp01(r - Vector2.Distance(q, c));
                if (EnTriangulo(q, a, b, p)) alfa = 1f;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, alfa));
            }
        t.Apply();
        return t;
    }

    private static bool EnTriangulo(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }
}
