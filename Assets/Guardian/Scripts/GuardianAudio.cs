using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Banco de sonidos del juego.
///
/// Todo el audio se sintetiza por código al vuelo: ni una sola muestra descargada,
/// así la entrega no depende de licencias de terceros y pesa lo mismo. Lo que este
/// archivo agrega respecto de antes:
///
///  1. LOS CLIPS SE GUARDAN. La versión anterior creaba un AudioClip nuevo en cada
///     recojo de basura —miles de floats y un objeto por evento—. Ahora cada sonido
///     se arma una sola vez y se reutiliza.
///  2. VARIACIÓN DE TONO. Repetir exactamente el mismo pitido cansa en dos minutos.
///     Cada reproducción sale con el tono movido al azar, así veinte residuos
///     seguidos no suenan a metrónomo.
///  3. AUDIO 3D DE VERDAD. Las fuentes del mundo se crean con spatialBlend en 1,
///     caída lineal y un alcance definido, para que se oiga de dónde viene la cosa.
///     Los avisos de interfaz van en 2D, que no tienen posición en el mundo.
///  4. MÁS SONIDOS. Recoger, acertar, equivocarse, entregar en el acopio, perder una
///     vida, ganar el nivel, perderlo, el timbre de la bicicleta y el bonus.
/// </summary>
public static class GuardianAudio
{
    public const int SR = 44100;

    // ---------------------------------------------------------------- reproducción

    /// <summary>Suena en un punto del mundo, con tono variado y caída por distancia.</summary>
    public static void EnPunto(AudioClip clip, Vector3 pos, float volumen = 0.7f,
                               float pitchMin = 0.92f, float pitchMax = 1.08f,
                               float alcance = 25f)
    {
        if (clip == null) return;

        GameObject go = new GameObject("FX_" + clip.name);
        go.transform.position = pos;

        AudioSource a = go.AddComponent<AudioSource>();
        a.clip = clip;
        a.volume = volumen * GuardianMezcla.Ganancia(GuardianMezcla.Bus.Efectos);   // bus EFECTOS 3D
        GuardianMezcla.Marca(GuardianMezcla.Bus.Efectos);
        a.pitch = Random.Range(pitchMin, pitchMax);
        a.spatialBlend = 1f;                       // 3D puro
        a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = 2f;
        a.maxDistance = alcance;
        a.dopplerLevel = 0f;                       // sin doppler: son efectos, no motores
        a.Play();

        Object.Destroy(go, clip.length / Mathf.Max(0.05f, a.pitch) + 0.2f);
    }

    /// <summary>Suena parejo en toda la pantalla: avisos, menús, marcador.</summary>
    public static void EnPantalla(AudioClip clip, float volumen = 0.7f,
                                  float pitchMin = 0.96f, float pitchMax = 1.04f)
    {
        if (clip == null) return;

        GameObject go = new GameObject("UI_" + clip.name);
        AudioSource a = go.AddComponent<AudioSource>();
        a.clip = clip;
        a.volume = volumen * GuardianMezcla.Ganancia(GuardianMezcla.Bus.Interfaz);  // bus INTERFAZ 2D
        GuardianMezcla.Marca(GuardianMezcla.Bus.Interfaz);
        a.pitch = Random.Range(pitchMin, pitchMax);
        a.spatialBlend = 0f;                       // 2D
        a.Play();

        Object.Destroy(go, clip.length / Mathf.Max(0.05f, a.pitch) + 0.2f);
    }

    // ---------------------------------------------------------------- sintetizador

    private struct Nota
    {
        public float f, t0, dur, amp, caida;
        public Nota(float f, float t0, float dur, float amp, float caida)
        { this.f = f; this.t0 = t0; this.dur = dur; this.amp = amp; this.caida = caida; }
    }

    /// <summary>
    /// Arma un clip sumando notas con envolvente exponencial. "brillo" agrega
    /// armónicos: en 0 es un seno limpio, subiéndolo se pone metálico.
    /// </summary>
    private static AudioClip Sintetizar(string nombre, Nota[] notas, float largo, float brillo)
    {
        int len = Mathf.Max(64, Mathf.RoundToInt(SR * largo));
        float[] d = new float[len];

        for (int n = 0; n < notas.Length; n++)
        {
            Nota no = notas[n];
            int i0 = Mathf.Clamp(Mathf.RoundToInt(no.t0 * SR), 0, len - 1);
            int i1 = Mathf.Clamp(Mathf.RoundToInt((no.t0 + no.dur) * SR), 0, len);

            for (int i = i0; i < i1; i++)
            {
                float t = (i - i0) / (float)SR;
                float env = Mathf.Exp(-t * no.caida);
                float ataque = Mathf.Min(1f, t * 260f);      // sin esto se oye un "click" al arrancar
                float w = 2f * Mathf.PI * no.f * t;

                float s = Mathf.Sin(w)
                        + brillo * 0.45f * Mathf.Sin(w * 2f)
                        + brillo * 0.20f * Mathf.Sin(w * 3f);

                d[i] += s * env * ataque * no.amp;
            }
        }

        Normalizar(d);
        return Clip(nombre, d);
    }

    /// <summary>Ruido filtrado: sirve para golpes, roces y el pedaleo.</summary>
    private static AudioClip Ruido(string nombre, float largo, float caida,
                                   float suavizado, float tono)
    {
        int len = Mathf.Max(64, Mathf.RoundToInt(SR * largo));
        float[] d = new float[len];

        float anterior = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            float r = Random.value * 2f - 1f;
            anterior = Mathf.Lerp(r, anterior, suavizado);          // pasa-bajos sencillo
            float cuerpo = (tono > 0f) ? Mathf.Sin(2f * Mathf.PI * tono * t) * 0.5f : 0f;
            d[i] = (anterior + cuerpo) * Mathf.Exp(-t * caida);
        }

        Normalizar(d);
        return Clip(nombre, d);
    }

    private static void Normalizar(float[] d)
    {
        float pico = 0.0001f;
        for (int i = 0; i < d.Length; i++) { float a = Mathf.Abs(d[i]); if (a > pico) pico = a; }
        float k = 0.85f / pico;
        for (int i = 0; i < d.Length; i++) d[i] *= k;

        int cola = Mathf.Min(d.Length, SR / 100);                   // que no corte de golpe
        for (int i = 0; i < cola; i++) d[d.Length - 1 - i] *= i / (float)cola;
    }

    private static AudioClip Clip(string nombre, float[] d)
    {
        AudioClip c = AudioClip.Create(nombre, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // ---------------------------------------------------------------- el banco

    private static AudioClip _recoger, _acierto, _error, _acopio, _vida,
                             _ganaste, _perdiste, _timbre, _bonus, _subirBici, _aviso;

    /// <summary>Levantar un residuo: dos notas cortas para arriba.</summary>
    public static AudioClip Recoger
    {
        get
        {
            if (_recoger == null) _recoger = Sintetizar("recoger", new Nota[] {
                new Nota(620f, 0f,     0.09f, 0.9f, 28f),
                new Nota(930f, 0.045f, 0.11f, 0.7f, 24f),
            }, 0.20f, 0.30f);
            return _recoger;
        }
    }

    /// <summary>Segregó bien: arpegio mayor, breve y alegre.</summary>
    public static AudioClip Acierto
    {
        get
        {
            if (_acierto == null) _acierto = Sintetizar("acierto", new Nota[] {
                new Nota(523f, 0f,    0.14f, 0.9f, 12f),   // do
                new Nota(659f, 0.07f, 0.16f, 0.9f, 11f),   // mi
                new Nota(784f, 0.14f, 0.26f, 1.0f,  8f),   // sol
            }, 0.45f, 0.35f);
            return _acierto;
        }
    }

    /// <summary>Contenedor equivocado: dos notas graves que bajan. No agresivo, avisa.</summary>
    public static AudioClip Error
    {
        get
        {
            if (_error == null) _error = Sintetizar("error", new Nota[] {
                new Nota(300f, 0f,    0.14f, 0.9f, 14f),
                new Nota(208f, 0.10f, 0.22f, 0.9f, 11f),
            }, 0.38f, 0.55f);
            return _error;
        }
    }

    /// <summary>Entrega en el punto de acopio: cierre de jornada, cuatro notas.</summary>
    public static AudioClip Acopio
    {
        get
        {
            if (_acopio == null) _acopio = Sintetizar("acopio", new Nota[] {
                new Nota(523f, 0f,    0.13f, 0.8f, 12f),
                new Nota(698f, 0.10f, 0.13f, 0.9f, 12f),
                new Nota(880f, 0.20f, 0.15f, 0.9f, 10f),
                new Nota(1046f,0.31f, 0.34f, 1.0f,  7f),
            }, 0.70f, 0.30f);
            return _acopio;
        }
    }

    /// <summary>Golpe de un carro: impacto sordo.</summary>
    public static AudioClip Vida
    {
        get
        {
            if (_vida == null) _vida = Ruido("vida", 0.40f, 11f, 0.86f, 95f);
            return _vida;
        }
    }

    /// <summary>Nivel ganado: fanfarria corta.</summary>
    public static AudioClip Ganaste
    {
        get
        {
            if (_ganaste == null) _ganaste = Sintetizar("ganaste", new Nota[] {
                new Nota(523f, 0f,    0.12f, 0.8f, 11f),
                new Nota(659f, 0.11f, 0.12f, 0.8f, 11f),
                new Nota(784f, 0.22f, 0.12f, 0.9f, 11f),
                new Nota(1046f,0.33f, 0.45f, 1.0f,  5f),
                new Nota(784f, 0.33f, 0.45f, 0.5f,  5f),
            }, 0.85f, 0.35f);
            return _ganaste;
        }
    }

    /// <summary>Nivel perdido: tres notas que bajan.</summary>
    public static AudioClip Perdiste
    {
        get
        {
            if (_perdiste == null) _perdiste = Sintetizar("perdiste", new Nota[] {
                new Nota(392f, 0f,    0.18f, 0.9f, 8f),
                new Nota(311f, 0.16f, 0.20f, 0.9f, 8f),
                new Nota(233f, 0.33f, 0.50f, 1.0f, 5f),
            }, 0.90f, 0.40f);
            return _perdiste;
        }
    }

    /// <summary>
    /// Timbre de bicicleta. Una campanita no es armónica: sus parciales no son
    /// múltiplos enteros. Por eso las frecuencias van en 1 : 2.76 : 5.40, que es
    /// lo que hace que suene a metal y no a flauta.
    /// </summary>
    public static AudioClip Timbre
    {
        get
        {
            if (_timbre == null) _timbre = Sintetizar("timbre", new Nota[] {
                new Nota(1180f, 0f,    0.42f, 1.0f, 11f),
                new Nota(3257f, 0f,    0.30f, 0.45f,16f),
                new Nota(6372f, 0f,    0.16f, 0.22f,26f),
                new Nota(1180f, 0.16f, 0.40f, 0.7f, 11f),
                new Nota(3257f, 0.16f, 0.28f, 0.32f,16f),
            }, 0.70f, 0f);
            return _timbre;
        }
    }

    /// <summary>Bonus / récord: chispa aguda.</summary>
    public static AudioClip Bonus
    {
        get
        {
            if (_bonus == null) _bonus = Sintetizar("bonus", new Nota[] {
                new Nota(1046f, 0f,    0.09f, 0.8f, 26f),
                new Nota(1318f, 0.05f, 0.09f, 0.8f, 26f),
                new Nota(1568f, 0.10f, 0.09f, 0.9f, 24f),
                new Nota(2093f, 0.15f, 0.22f, 1.0f, 14f),
            }, 0.45f, 0.25f);
            return _bonus;
        }
    }

    /// <summary>Subirse o bajarse de la bicicleta: roce de cadena.</summary>
    public static AudioClip SubirBici
    {
        get
        {
            if (_subirBici == null) _subirBici = Ruido("bici", 0.26f, 16f, 0.55f, 180f);
            return _subirBici;
        }
    }

    /// <summary>Aviso neutro de interfaz.</summary>
    public static AudioClip Aviso
    {
        get
        {
            if (_aviso == null) _aviso = Sintetizar("aviso", new Nota[] {
                new Nota(880f, 0f, 0.10f, 0.9f, 22f),
            }, 0.16f, 0.20f);
            return _aviso;
        }
    }

    // ---------------------------------------------------------------- tonos sueltos

    // GuardianFX pide sonidos por frecuencia. Se guardan por frecuencia redondeada
    // para no volver a sintetizar el mismo pitido una y otra vez.
    private static readonly Dictionary<int, AudioClip> pitidos = new Dictionary<int, AudioClip>();

    public static AudioClip Pitido(float freq)
    {
        int clave = Mathf.RoundToInt(freq / 20f) * 20;
        clave = Mathf.Clamp(clave, 80, 4000);

        AudioClip c;
        if (pitidos.TryGetValue(clave, out c) && c != null) return c;

        c = Sintetizar("pitido" + clave, new Nota[] {
            new Nota(clave, 0f, 0.13f, 1f, 11f),
        }, 0.16f, 0.25f);

        pitidos[clave] = c;
        return c;
    }
}
