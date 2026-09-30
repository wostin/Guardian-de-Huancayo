using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Guardián de Huancayo - FLUJO DE SEÑAL DE AUDIO (mezclador por buses).
///
///   FUENTES                     BUS (canal)              PROCESO                 SALIDA
///   ─────────────────────────   ──────────────────────   ─────────────────────   ──────────
///   Temas por zona / menú   ─►  MÚSICA    (vol)       ─► Ducking + Pasa-bajos ─┐
///   Viento, calle, capas    ─►  AMBIENTE  (vol)       ─► Pasa-bajos en pausa  ─┤
///   Pasos, motores, bocina, ─►  EFECTOS 3D (vol)      ─► Atenuación 3D lineal ─┼─► MASTER ─► AudioListener
///   recoger, contenedores                                                       │   (volumen general, M = mute)
///   Acierto, error, vida,   ─►  INTERFAZ 2D (vol)     ─► Dispara el ducking   ─┘
///   ganar/perder, tic-tac
///
/// - Cada fuente se asigna a UN bus y su volumen final = volumen propio × bus × master.
/// - DUCKING: cuando suena un aviso importante (acierto, error, golpe) la música
///   baja un momento para que la retroalimentación se entienda y vuelve sola.
/// - PASA-BAJOS: en pausa la música suena "tapada" (corte a 700 Hz): el jugador
///   siente que el mundo se detuvo sin cortar el sonido de golpe.
/// - F8 muestra este flujo EN VIVO con medidores (sirve para la exposición del GDD).
/// Unity no permite crear un AudioMixer por código, por eso los buses se
/// implementan aquí; el diagrama es el mismo que tendría un AudioMixer.
/// </summary>
public class GuardianMezcla : MonoBehaviour
{
    public enum Bus { Musica = 0, Ambiente = 1, Efectos = 2, Interfaz = 3 }
    public static readonly string[] NOMBRES = { "Música", "Ambiente", "Efectos 3D", "Interfaz" };

    private static readonly float[] vol = { 0.8f, 0.9f, 1f, 1f };
    private static readonly float[] actividad = new float[4];   // último momento en que sonó algo
    private static float duck = 1f;            // ganancia actual del ducking (1 = sin bajar)
    private static float duckHasta;
    private static float duckNivel = 1f;
    private static bool cargado;

    public static GuardianMezcla Instancia { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (FindObjectOfType<GuardianMezcla>() != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianMezcla").AddComponent<GuardianMezcla>();
    }

    private static void Cargar()
    {
        if (cargado) return;
        cargado = true;
        for (int i = 0; i < 4; i++) vol[i] = PlayerPrefs.GetFloat("guardian_bus_" + i, vol[i]);
    }

    /// <summary>Volumen del bus (lo que ajusta el jugador), sin ducking.</summary>
    public static float Volumen(Bus b) { Cargar(); return vol[(int)b]; }

    public static void FijarVolumen(Bus b, float v)
    {
        Cargar();
        vol[(int)b] = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat("guardian_bus_" + (int)b, vol[(int)b]);
    }

    /// <summary>Ganancia que debe multiplicar el volumen de una fuente de ese bus.</summary>
    public static float Ganancia(Bus b)
    {
        Cargar();
        float g = vol[(int)b];
        if (b == Bus.Musica || b == Bus.Ambiente) g *= duck;
        return g;
    }

    /// <summary>Marca que el bus recibió señal (para el medidor en vivo).</summary>
    public static void Marca(Bus b) { actividad[(int)b] = Time.unscaledTime; }

    /// <summary>Baja la música a 'nivel' durante 'seg' segundos.</summary>
    public static void Duck(float nivel, float seg)
    {
        duckNivel = Mathf.Min(duckNivel, Mathf.Clamp01(nivel));
        duckHasta = Mathf.Max(duckHasta, Time.unscaledTime + seg);
    }

    private bool verFlujo;

    /// <summary>Muestra u oculta el panel F8 desde código (capturas del informe).</summary>
    public static void MostrarFlujo(bool v) { if (Instancia != null) Instancia.verFlujo = v; }
    private AudioLowPassFilter lpf;
    private GuardianMusic musica;
    private AudioSource[] fuentesMusica;
    private readonly float[] nivelVu = new float[4];
    private readonly float[] muestras = new float[256];
    private Texture2D blanco;

    void Awake()
    {
        Instancia = this;
        Cargar();
        blanco = new Texture2D(1, 1); blanco.SetPixel(0, 0, Color.white); blanco.Apply();

        // Cada retroalimentación importante mete ducking a la música.
        GuardianEventos.Deposito += (p, t, n, pts) => Duck(0.55f, 0.55f);
        GuardianEventos.Error    += (p, t) => Duck(0.45f, 0.7f);
        GuardianEventos.Golpe    += p => Duck(0.35f, 0.9f);
        GuardianEventos.FinNivel += g => Duck(0.25f, 2.2f);
    }

    void Update()
    {
        // Ducking: baja rápido (ataque) y vuelve lento (liberación).
        if (Time.unscaledTime < duckHasta)
            duck = Mathf.MoveTowards(duck, duckNivel, Time.unscaledDeltaTime * 6f);
        else
        {
            duckNivel = 1f;
            duck = Mathf.MoveTowards(duck, 1f, Time.unscaledDeltaTime * 1.4f);
        }

        // Pasa-bajos sobre el objeto de la música (afecta a música + ambiente).
        if (musica == null)
        {
            musica = FindObjectOfType<GuardianMusic>();
            if (musica != null)
            {
                lpf = musica.GetComponent<AudioLowPassFilter>();
                if (lpf == null) lpf = musica.gameObject.AddComponent<AudioLowPassFilter>();
                lpf.cutoffFrequency = 22000f;
            }
        }
        GameManager gm = GameManager.Instance;
        bool tapado = gm != null && gm.pausado;
        if (lpf != null)
        {
            float objetivo = tapado ? 700f : 22000f;
            lpf.cutoffFrequency = Mathf.Lerp(lpf.cutoffFrequency, objetivo, Time.unscaledDeltaTime * 5f);
        }

        // La música y el ambiente siempre están sonando: se marcan como activos.
        if (musica != null)
        {
            fuentesMusica = musica.GetComponents<AudioSource>();
            bool suenaM = false;
            if (fuentesMusica != null)
                foreach (AudioSource a in fuentesMusica)
                    if (a != null && a.isPlaying && a.volume > 0.01f) { suenaM = true; break; }
            if (suenaM) Marca(Bus.Musica);
        }

        if (TeclaF8()) verFlujo = !verFlujo;

        // Medidores: la música se mide de verdad (RMS de la salida), los demás
        // buses por actividad reciente (cada sonido deja un pico que decae).
        for (int i = 0; i < 4; i++)
        {
            float objetivo = Mathf.Clamp01(1f - (Time.unscaledTime - actividad[i]) * 2.5f) * vol[i];
            if (i == (int)Bus.Musica && fuentesMusica != null)
            {
                float rms = 0f;
                foreach (AudioSource a in fuentesMusica)
                {
                    if (a == null || !a.isPlaying) continue;
                    a.GetOutputData(muestras, 0);
                    float s = 0f;
                    for (int k = 0; k < muestras.Length; k++) s += muestras[k] * muestras[k];
                    rms = Mathf.Max(rms, Mathf.Sqrt(s / muestras.Length));
                }
                objetivo = Mathf.Clamp01(rms * 6f);
            }
            nivelVu[i] = Mathf.Max(objetivo, nivelVu[i] - Time.unscaledDeltaTime * 1.6f);
        }
    }

    private bool TeclaF8()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F8);
#endif
    }

    // ---------------------------------------------------------------- panel F8

    void OnGUI()
    {
        if (!verFlujo) return;
        GUI.depth = -20;
        float esc = Mathf.Clamp(Mathf.Min(Screen.height / 800f, Screen.width / 1360f), 0.7f, 2.4f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(esc, esc, 1f));
        float SWm = Screen.width / esc, SHm = Screen.height / esc;

        float w = 560f, h = 262f;
        Rect r = new Rect(SWm - w - 16f, SHm - h - 48f, w, h);
        Pintar(r, new Color(0.03f, 0.05f, 0.08f, 0.92f));
        Pintar(new Rect(r.x, r.y, r.width, 4), new Color(0.30f, 0.85f, 0.55f));

        GUIStyle tit = Estilo(14, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft);
        GUIStyle txt = Estilo(11, FontStyle.Normal, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleLeft);
        GUIStyle cen = Estilo(11, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);

        GUI.Label(new Rect(r.x + 12, r.y + 8, w - 24, 20), "FLUJO DE SEÑAL DE AUDIO  (F8)", tit);

        string[] fuentes = { "Temas de zona / menú", "Viento, calle, capas", "Pasos, motores, bocina", "Acierto, error, vida" };
        string[] proceso = { "Ducking + Pasa-bajos", "Ducking + Pasa-bajos", "Atenuación 3D", "Dispara ducking" };
        Color[] col = { new Color(0.45f, 0.75f, 1f), new Color(0.55f, 0.9f, 0.7f),
                        new Color(1f, 0.72f, 0.35f), new Color(0.95f, 0.5f, 0.85f) };

        for (int i = 0; i < 4; i++)
        {
            float y = r.y + 38 + i * 44;
            GUI.Label(new Rect(r.x + 12, y, 140, 30), fuentes[i], txt);
            GUI.Label(new Rect(r.x + 148, y, 20, 30), "►", cen);

            Rect bus = new Rect(r.x + 168, y, 150, 30);
            Pintar(bus, new Color(col[i].r * 0.25f, col[i].g * 0.25f, col[i].b * 0.25f, 1f));
            Pintar(new Rect(bus.x, bus.yMax - 6, bus.width * nivelVu[i], 6), col[i]);
            bool activo = Time.unscaledTime - actividad[i] < 0.2f;
            Pintar(new Rect(bus.x + 6, bus.y + 8, 8, 8), activo ? col[i] : new Color(0.3f, 0.3f, 0.3f));
            GUI.Label(new Rect(bus.x + 18, bus.y, bus.width - 20, 24),
                NOMBRES[i] + "  " + Mathf.RoundToInt(Ganancia((Bus)i) * 100f) + "%", txt);

            GUI.Label(new Rect(r.x + 318, y, 20, 30), "►", cen);
            GUI.Label(new Rect(r.x + 338, y, 126, 30), proceso[i], txt);
            GUI.Label(new Rect(r.x + 460, y, 20, 30), "►", cen);
        }

        Rect master = new Rect(r.x + 480, r.y + 38, 68, 170);
        Pintar(master, new Color(0.12f, 0.16f, 0.22f, 1f));
        float mv = AudioListener.volume;
        Pintar(new Rect(master.x + 24, master.yMax - 10 - 120 * mv, 20, 120 * mv), new Color(0.30f, 0.85f, 0.55f));
        GUI.Label(new Rect(master.x, master.y + 2, master.width, 18), "MASTER", cen);
        GUI.Label(new Rect(master.x, master.y + 18, master.width, 18), Mathf.RoundToInt(mv * 100) + "%", cen);

        GameManager gm = GameManager.Instance;
        string estado = "Ducking música: " + Mathf.RoundToInt(duck * 100f) + "%   ·   Pasa-bajos: "
                      + (lpf != null ? Mathf.RoundToInt(lpf.cutoffFrequency) + " Hz" : "—")
                      + "   ·   Salida: AudioListener (cámara)";
        GUI.Label(new Rect(r.x + 12, r.yMax - 30, w - 24, 22), estado, txt);
    }

    private void Pintar(Rect r, Color c)
    {
        Color a = GUI.color; GUI.color = c; GUI.DrawTexture(r, blanco); GUI.color = a;
    }

    private static GUIStyle Estilo(int s, FontStyle f, Color c, TextAnchor a)
    {
        GUIStyle g = new GUIStyle(GUI.skin.label);
        g.fontSize = s; g.fontStyle = f; g.normal.textColor = c; g.alignment = a; g.wordWrap = false;
        return g;
    }
}
