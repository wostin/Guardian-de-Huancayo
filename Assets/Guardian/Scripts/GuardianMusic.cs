using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Música de fondo generada por código (sin assets ni derechos).
/// Huayno wanka: melodía de quena en pentatónica menor, bombo en los tiempos fuertes
/// y rasgueo de charango en los débiles. Cada zona tiene su propia versión:
///   · Centro   — tranquilo, en tono medio
///   · Mercado  — más rápido y más arriba, con el charango bien presente
///   · Ribera   — más lento y grave, casi sin charango
/// Se activa sola al presionar Play y cambia de tema al cambiar de nivel.
/// </summary>
public class GuardianMusic : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (FindObjectOfType<GuardianMusic>() != null) return;
        if (GameObject.Find("GuardianAuto") != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianMusic").AddComponent<GuardianMusic>();
    }

    public float volumen = 0.20f;

    /// <summary>Volumen de la música después de pasar por el bus MÚSICA (incluye ducking).</summary>
    private float VolBus => volumen * GuardianMezcla.Ganancia(GuardianMezcla.Bus.Musica);

    /// <summary>
    /// Musica del proyecto. La herramienta del editor engancha estos clips desde
    /// Assets/Sounds al armar el juego. Si alguno queda vacio, esa zona vuelve
    /// sola al huayno sintetizado por codigo, asi el juego nunca se queda mudo
    /// aunque falte un archivo.
    /// </summary>
    [Header("Musica del proyecto (opcional)")]
    public AudioClip temaMenu;
    public AudioClip temaZona0;
    public AudioClip temaZona1;
    public AudioClip temaZona2;
    public AudioClip fanfarria;

    /// <summary>
    /// Tema de alerta. Suena cuando la contaminacion del Shullcas pasa del umbral
    /// y vuelve al tema de la zona al bajar. Es lo que convierte a la musica en
    /// parte del mensaje y no en relleno: la ciudad empeorando SE OYE, no solo se
    /// ve en la neblina y en el rio.
    /// </summary>
    public AudioClip temaAlerta;
    /// <summary>Golpe de entrada de la alerta (Boss_Battle_Intro). Suena una vez y
    /// enseguida arranca el bucle, para que el cambio se sienta.</summary>
    public AudioClip introAlerta;
    [Range(0.4f, 0.95f)] public float umbralAlerta = 0.78f;

    [Header("Mas temas del proyecto")]
    /// <summary>Segundo tema de menu: se alterna con el primero al volver al menu.</summary>
    public AudioClip temaMenu2;
    /// <summary>Tema del agua: entra solo cuando el Guardian se acerca al Shullcas.</summary>
    public AudioClip temaRio;
    /// <summary>Ultimos segundos del cronometro.</summary>
    public AudioClip temaApuro;
    /// <summary>Bucle de la victoria, despues de la fanfarria.</summary>
    public AudioClip fanfarriaBucle;
    /// <summary>Al perder.</summary>
    public AudioClip temaDerrota;
    /// <summary>Cortina corta al entrar a una zona nueva.</summary>
    public AudioClip stingerNivel;

    [Header("Capa de ambiente (cuerdas, piano, arpa)")]
    /// <summary>Colchon suave por debajo del tema. Suma aire a la ciudad sin tapar
    /// la musica; se apaga en la alerta y en el apuro para no amontonar.</summary>
    public AudioClip[] ambientes;
    [Range(0f, 0.6f)] public float volumenAmbiente = 0.30f;

    /// <summary>Metros al rio desde los que entra el tema del agua.</summary>
    public float distanciaRio = 46f;

    [Tooltip("Segundos que tarda un tema en dar paso al siguiente.")]
    public float cruce = 1.2f;

    private AudioSource src;      // pista que suena
    private AudioSource otra;     // pista que se apaga mientras entra la nueva
    private readonly AudioClip[] temas = new AudioClip[3];
    private int zonaSonando = -1;
    private bool enMenu = true;
    private bool fanfarriaDada;
    private bool enAlerta;
    private bool enApuro;
    private bool enRio;
    private float mezcla = 1f;

    private AudioSource ambiente;       // capa de fondo, aparte de la musica
    private AudioClip colaClip;         // lo que entra cuando termine el clip de entrada
    private bool derrotaDada;
    private int menuAlterno;
    private Transform rio;
    private float proximoRio;
    private int zonaAnterior = -1;

    // Parámetros de cada zona: bpm, transposición y presencia del charango.
    private static readonly float[] BPM       = { 104f, 120f, 94f };
    private static readonly float[] TONO      = { 1.000f, 1.1225f, 0.8909f };  // ±1 tono
    private static readonly float[] CHARANGO  = { 1.00f, 1.45f, 0.35f };

    void Start()
    {
        src  = NuevaPista();
        otra = NuevaPista();
        Poner(TemaDeMenu(), true);
    }

    private AudioSource NuevaPista()
    {
        AudioSource a = gameObject.AddComponent<AudioSource>();
        a.loop = true;
        a.volume = 0f;
        a.priority = 200;
        a.playOnAwake = false;
        a.spatialBlend = 0f;
        return a;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;

        // Encadenado entrada -> bucle: el intro no lleva loop, asi que cuando
        // termina hay que soltar el bucle a mano (Boss_Battle, Victory_Fanfare).
        if (colaClip != null && src != null && !src.isPlaying && mezcla >= 1f)
        {
            AudioClip sigue = colaClip; colaClip = null;
            Poner(sigue, true);
        }

        // El menu tiene su propio tema; dentro del juego manda la zona.
        bool menuAhora = (gm == null) || gm.estado == GameManager.Estado.Menu;

        if (menuAhora != enMenu)
        {
            enMenu = menuAhora;
            if (enMenu)
            {
                zonaSonando = -1; fanfarriaDada = false; derrotaDada = false;
                enAlerta = false; enApuro = false; enRio = false; zonaAnterior = -1;
                Poner(TemaDeMenu(), false);
            }
            else Poner(TemaDeZona(gm != null ? gm.zonaActual : 0), false);
        }
        else if (!enMenu && gm != null)
        {
            // Fanfarria al ganar: entrada una sola vez, y despues el bucle.
            if (gm.estado == GameManager.Estado.Ganado && !fanfarriaDada
                && (fanfarria != null || fanfarriaBucle != null))
            {
                fanfarriaDada = true;
                ConIntro(fanfarria, fanfarriaBucle);
            }
            else if (gm.estado == GameManager.Estado.Perdido && temaDerrota != null && !derrotaDada)
            {
                derrotaDada = true;
                Poner(temaDerrota, false);
            }
            else if (gm.estado == GameManager.Estado.Jugando)
            {
                fanfarriaDada = false; derrotaDada = false;

                // La ciudad sucia cambia el tema. Con histeresis (baja al 0.68 de
                // lo que pide para entrar) para que no salte de ida y vuelta
                // cuando la cifra se queda justo en el filo.
                float suciedad = (gm.maxContaminacion > 0f)
                    ? gm.contaminacion / gm.maxContaminacion : 0f;

                if ((temaAlerta != null || introAlerta != null) && !enAlerta && suciedad > umbralAlerta)
                {
                    enAlerta = true; enApuro = false; enRio = false;
                    ConIntro(introAlerta, temaAlerta);
                }
                else if (enAlerta && suciedad < umbralAlerta * 0.68f)
                {
                    enAlerta = false;
                    zonaSonando = -1;                      // fuerza volver al tema de la zona
                }

                if (!enAlerta)
                {
                    // Ultimos segundos: la musica avisa antes que el numero.
                    bool apuroAhora = temaApuro != null && gm.tiempoRestante > 0f && gm.tiempoRestante < 32f;
                    if (apuroAhora != enApuro)
                    {
                        enApuro = apuroAhora;
                        if (enApuro) { enRio = false; Poner(temaApuro, false); }
                        else zonaSonando = -1;
                    }

                    if (!enApuro)
                    {
                        // Cerca del Shullcas manda el agua: el nivel de la ribera
                        // se trata de limpiar el rio, y se nota al acercarse.
                        bool rioAhora = temaRio != null && CercaDelRio();
                        if (rioAhora != enRio)
                        {
                            enRio = rioAhora;
                            if (enRio) Poner(temaRio, false);
                            else zonaSonando = -1;
                        }

                        if (!enRio)
                        {
                            int z = Mathf.Clamp(gm.zonaActual, 0, 2);
                            if (z != zonaSonando)
                            {
                                // Al estrenar zona entra la cortina y detras el tema.
                                bool estrena = (zonaAnterior >= 0 && z != zonaAnterior);
                                zonaAnterior = z;
                                if (estrena && stingerNivel != null) ConIntro(stingerNivel, TemaDeZona(z));
                                else Poner(TemaDeZona(z), false);
                            }
                            zonaAnterior = z;
                        }
                    }
                }
            }
        }

        Ambiente(gm);

        // Cruce suave entre la pista que sale y la que entra.
        if (mezcla < 1f)
        {
            mezcla = Mathf.Min(1f, mezcla + Time.unscaledDeltaTime / Mathf.Max(0.05f, cruce));
            if (src  != null) src.volume  = VolBus * mezcla;
            if (otra != null)
            {
                otra.volume = VolBus * (1f - mezcla);
                if (mezcla >= 1f) otra.Stop();
            }
        }
        else if (src != null) src.volume = VolBus;   // bus MÚSICA (volumen + ducking) en todo momento
    }

    private AudioClip TemaDeMenu()
    {
        // Dos temas de menu que se van turnando: volver al menu cinco veces
        // seguidas con la misma cancion cansa.
        menuAlterno++;
        if (temaMenu != null && temaMenu2 != null)
            return (menuAlterno % 2 == 0) ? temaMenu : temaMenu2;
        if (temaMenu  != null) return temaMenu;
        if (temaMenu2 != null) return temaMenu2;
        return TemaDeZona(0);
    }

    /// <summary>Suelta un clip de entrada (sin bucle) y deja el bucle en cola.</summary>
    private void ConIntro(AudioClip intro, AudioClip bucle)
    {
        if (intro != null) { Poner(intro, false, false); colaClip = bucle; }
        else if (bucle != null) { Poner(bucle, false); colaClip = null; }
    }

    /// <summary>
    /// Capa de ambiente por debajo del tema. Son las cuerdas, el piano y el arpa
    /// del pack: solas no dicen nada, pero puestas al 30% bajo la musica le dan
    /// aire a la ciudad. Se callan en la alerta y en el apuro, donde lo que hace
    /// falta es tension limpia.
    /// </summary>
    private void Ambiente(GameManager gm)
    {
        if (ambientes == null || ambientes.Length == 0) return;

        if (ambiente == null)
        {
            ambiente = gameObject.AddComponent<AudioSource>();
            ambiente.playOnAwake = false;
            ambiente.loop = false;
            ambiente.spatialBlend = 0f;
            ambiente.priority = 200;
            ambiente.volume = 0f;
        }

        bool toca = !enMenu && gm != null && gm.estado == GameManager.Estado.Jugando
                    && !enAlerta && !enApuro;

        float objetivo = toca ? volumen * volumenAmbiente * GuardianMezcla.Ganancia(GuardianMezcla.Bus.Ambiente) : 0f;
        if (toca) GuardianMezcla.Marca(GuardianMezcla.Bus.Ambiente);
        ambiente.volume = Mathf.MoveTowards(ambiente.volume, objetivo, Time.unscaledDeltaTime * 0.12f);

        if (toca && !ambiente.isPlaying)
        {
            ambiente.clip  = ambientes[Random.Range(0, ambientes.Length)];
            ambiente.pitch = Random.Range(0.97f, 1.03f);
            if (ambiente.clip != null) ambiente.Play();
        }
        else if (!toca && ambiente.isPlaying && ambiente.volume <= 0.001f) ambiente.Stop();
    }

    /// <summary>
    /// Distancia al Shullcas. El rio se busca una vez por nombre y despues solo
    /// se mide cada medio segundo: no hace falta mas para un cambio de musica.
    /// </summary>
    private bool CercaDelRio()
    {
        if (Time.time < proximoRio) return enRio;
        proximoRio = Time.time + 0.5f;

        if (rio == null)
        {
            AguaRio a = Object.FindFirstObjectByType<AguaRio>();
            if (a != null) rio = a.transform;
            if (rio == null) return false;
        }

        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j == null) return false;

        // El rio es una cinta larga: importa la distancia al EJE, no al centro.
        Vector3 d = j.transform.position - rio.position;
        return Mathf.Abs(d.z) < distanciaRio && Mathf.Abs(d.x) < 400f;
    }

    private AudioClip TemaDeZona(int z)
    {
        z = Mathf.Clamp(z, 0, 2);
        zonaSonando = z;

        AudioClip propio = (z == 0) ? temaZona0 : (z == 1 ? temaZona1 : temaZona2);
        if (propio != null) return propio;

        // Sin archivo para esta zona: huayno generado por codigo, como antes.
        if (temas[z] == null) temas[z] = Melodia(BPM[z], TONO[z], CHARANGO[z]);
        return temas[z];
    }

    /// <summary>Pone un tema cruzando con el que estaba sonando.</summary>
    private void Poner(AudioClip clip, bool inmediato, bool enBucle = true)
    {
        if (clip == null || src == null) return;
        if (src.clip == clip && src.isPlaying) return;

        AudioSource tmp = otra; otra = src; src = tmp;

        src.clip = clip;
        src.loop = enBucle;
        src.volume = inmediato ? VolBus : 0f;
        src.Play();

        mezcla = inmediato ? 1f : 0f;
        if (inmediato && otra != null) otra.Stop();
    }

    private AudioClip Melodia(float bpm, float tono, float charango)
    {
        int sr = 44100;
        float beat = 60f / bpm;
        int beatN = Mathf.RoundToInt(beat * sr);

        // Pentatónica menor de La (La-Do-Re-Mi-Sol), transpuesta según la zona.
        float[] esc = { 329.63f, 392.00f, 440.00f, 523.25f, 587.33f, 659.25f, 783.99f, 880.00f };
        for (int i = 0; i < esc.Length; i++) esc[i] *= tono;

        int[] mel = {
            7, 6, 5, 4, 3, 2,
            5, 4, 3, 2, 3,-1,
            3, 4, 5, 6, 7, 6,
            5, 3, 2, 1, 2,
            2, 3, 5, 4, 3, 2,
            1, 2, 3, 4, 5,
            6, 5, 4, 3, 4, 3,
            2, 1, 0, 2
        };
        float[] dur = {
            .75f,.25f,.5f,.5f,1f,1f,
            .75f,.25f,.5f,.5f,1f,1f,
            .75f,.25f,.5f,.5f,1f,1f,
            .75f,.25f,.5f,.5f,2f,
            .5f,.5f,.75f,.25f,1f,1f,
            .75f,.25f,.5f,.5f,2f,
            .75f,.25f,.5f,.5f,1f,1f,
            1f,.5f,.5f,2f
        };

        float totalBeats = 0f;
        for (int i = 0; i < dur.Length; i++) totalBeats += dur[i];
        int len = Mathf.CeilToInt(totalBeats * beat * sr);
        float[] data = new float[len];

        // ---- Quena (melodía) ----
        int pos = 0;
        for (int n = 0; n < mel.Length && n < dur.Length; n++)
        {
            int nd = Mathf.RoundToInt(dur[n] * beat * sr);
            if (mel[n] >= 0)
            {
                float f = esc[mel[n]];
                for (int i = 0; i < nd && pos + i < len; i++)
                {
                    float t = (float)i / sr;
                    float tt = t / Mathf.Max(0.0001f, nd / (float)sr);
                    float env = Mathf.Min(1f, t / 0.045f)
                              * Mathf.Min(1f, (1f - tt) / 0.12f);
                    float vib = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 5.2f * t);
                    float ff = f * vib;
                    float s = Mathf.Sin(2f * Mathf.PI * ff * t)
                            + 0.25f * Mathf.Sin(2f * Mathf.PI * ff * 2f * t)
                            + 0.10f * Mathf.Sin(2f * Mathf.PI * ff * 3f * t);
                    s += (Mathf.PerlinNoise(t * 900f, n) - 0.5f) * 0.10f;   // aire de la quena
                    data[pos + i] += s * env * 0.16f;
                }
            }
            pos += nd;
        }

        // ---- Bombo (tiempos 1 y 3) ----
        for (int b = 0; b * 2 * beatN < len; b++)
        {
            int ini = b * 2 * beatN;
            int dl = Mathf.RoundToInt(0.28f * sr);
            for (int i = 0; i < dl && ini + i < len; i++)
            {
                float t = (float)i / sr;
                float env = Mathf.Exp(-t * 16f);
                float f = 70f * Mathf.Exp(-t * 9f) + 45f;
                data[ini + i] += Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.30f;
            }
        }

        // ---- Charango (rasgueo en los tiempos débiles) ----
        if (charango > 0.01f)
        {
            float[] acorde = { 880f * tono, 1046.50f * tono, 1318.51f * tono };
            for (int b = 0; ; b++)
            {
                int ini = (2 * b + 1) * beatN;
                if (ini >= len) break;
                int dl = Mathf.RoundToInt(0.16f * sr);
                for (int i = 0; i < dl && ini + i < len; i++)
                {
                    float t = (float)i / sr;
                    float env = Mathf.Exp(-t * 22f);
                    float s = 0f;
                    for (int k = 0; k < acorde.Length; k++)
                        s += Mathf.Sin(2f * Mathf.PI * acorde[k] * (t + k * 0.008f));
                    data[ini + i] += s * env * 0.045f * charango;
                }
            }
        }

        for (int i = 0; i < len; i++) data[i] = Mathf.Clamp(data[i], -0.95f, 0.95f);

        AudioClip clip = AudioClip.Create("huayno_guardian", len, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}
