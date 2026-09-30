using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - VFX: PARTÍCULAS Y EFECTOS QUE REFUERZAN LA RETROALIMENTACIÓN.
///
/// Cada acción del jugador tiene una respuesta visual inmediata y con significado:
///
///  ACCIÓN                 VFX                                             MENSAJE
///  Recoger residuo        chispas del COLOR del residuo + onda + "+1"     "lo tienes; va al contenedor de este color"
///  Depositar bien         confeti del color + onda + "+puntos" + destello "¡bien segregado!"
///  Contenedor equivocado  humo rojo + "✖" + temblor + viñeta roja         "ese no es su color"
///  Golpe de un carro      polvo + estrellas + sacudida fuerte + viñeta    "perdiste una vida: cuidado con la pista"
///  Correr                 polvo en los pies                               sensación de velocidad
///  Ganar el nivel         fuegos artificiales con los 5 colores NTP        celebración
///  Perder                 humo gris                                        la ciudad se ensució
///
/// Se suscribe a GuardianEventos, así que la lógica del juego no sabe nada de efectos.
/// </summary>
public class GuardianVFX : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (FindObjectOfType<GuardianVFX>() != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianVFX").AddComponent<GuardianVFX>();
    }

    private Transform jugador;
    private CharacterController cc;
    private Vector3 posAnterior;
    private float relojPolvo;

    void Awake()
    {
        GuardianEventos.Recogio      += AlRecoger;
        GuardianEventos.Deposito     += AlDepositar;
        GuardianEventos.Error        += AlEquivocarse;
        GuardianEventos.Golpe        += AlGolpe;
        GuardianEventos.FinNivel     += AlTerminar;
        GuardianEventos.MochilaLlena += AlLlenar;
    }

    void OnDestroy()
    {
        GuardianEventos.Recogio      -= AlRecoger;
        GuardianEventos.Deposito     -= AlDepositar;
        GuardianEventos.Error        -= AlEquivocarse;
        GuardianEventos.Golpe        -= AlGolpe;
        GuardianEventos.FinNivel     -= AlTerminar;
        GuardianEventos.MochilaLlena -= AlLlenar;
    }

    void Update()
    {
        if (jugador == null)
        {
            GameObject pj = GameObject.FindGameObjectWithTag("Player");
            if (pj == null) return;
            jugador = pj.transform; cc = pj.GetComponent<CharacterController>();
            posAnterior = jugador.position;
        }

        // Polvo al correr: solo pisando el suelo y a velocidad de carrera.
        Vector3 d = jugador.position - posAnterior; d.y = 0f;
        float vel = Time.deltaTime > 0f ? d.magnitude / Time.deltaTime : 0f;
        posAnterior = jugador.position;
        bool enSuelo = cc == null || cc.isGrounded;
        if (enSuelo && vel > 6.3f)
        {
            relojPolvo -= Time.deltaTime;
            if (relojPolvo <= 0f)
            {
                relojPolvo = 0.07f;
                GuardianParticulas.Rafaga(jugador.position + Vector3.up * 0.1f - jugador.forward * 0.3f,
                    new Color(0.78f, 0.72f, 0.62f, 0.55f), new Color(0.65f, 0.60f, 0.52f, 0.35f),
                    3, 1.2f, 0.55f, 0.55f, -0.05f, GuardianParticulas.Tex.Suave, false,
                    ParticleSystemShapeType.Sphere, 0.15f, false, -0.8f);
            }
        }
    }

    // ---------------------------------------------------------------- reacciones

    private void AlRecoger(Vector3 p, TipoResiduo t)
    {
        Color c = Residuo.Tinte(t);
        GuardianParticulas.Rafaga(p + Vector3.up * 0.4f, c, Color.white, 26, 4.5f, 0.7f, 0.28f, -0.2f,
                                  GuardianParticulas.Tex.Estrella, true);
        GuardianParticulas.Anillo(p, c, 3.2f, 0.45f);
        GuardianTextos.Mostrar(p + Vector3.up * 1.6f, "+1 " + Residuo.Nombre(t), c, 22);
        GuardianCameraHUD.Sacudir(0.06f);
        if (jugador != null)
        {
            PlayerController pc = jugador.GetComponent<PlayerController>();
            if (pc != null) pc.Accion(0.55f, "pick", "gather", "crouch", "bend", "interact");
        }
    }

    private void AlLlenar(Vector3 p)
    {
        GuardianTextos.Mostrar(p + Vector3.up * 2.4f, "MOCHILA LLENA", new Color(1f, 0.72f, 0.3f), 22);
    }

    private void AlDepositar(Vector3 p, TipoResiduo t, int n, int pts)
    {
        Color c = Residuo.Tinte(t);
        GuardianParticulas.Confeti(p + Vector3.up * 1.8f, c, 30 + n * 12);
        GuardianParticulas.Anillo(p, c, 6f, 0.6f);
        GuardianParticulas.Anillo(p, Color.white, 3.5f, 0.4f);
        GuardianParticulas.Rafaga(p + Vector3.up * 1.5f, Color.white, c, 1, 0f, 0.22f, 4f, 0f);   // destello
        GuardianTextos.Mostrar(p + Vector3.up * 2.8f, "+" + pts, new Color(0.45f, 1f, 0.6f), 34);
        if (n > 1) GuardianTextos.Mostrar(p + Vector3.up * 2.0f, "x" + n + " " + Residuo.Nombre(t), c, 20, 0.15f);
        GuardianCameraHUD.Sacudir(0.12f);
        GuardianIluminacion.Destello(new Color(0.35f, 1f, 0.55f), 0.28f);
    }

    private void AlEquivocarse(Vector3 p, TipoResiduo t)
    {
        GuardianParticulas.Humo(p + Vector3.up * 1.4f, new Color(0.85f, 0.25f, 0.2f, 0.7f), 22, 1.1f);
        GuardianParticulas.Anillo(p, new Color(1f, 0.25f, 0.2f), 4f, 0.5f);
        GuardianTextos.Mostrar(p + Vector3.up * 2.8f, "✖ Aquí va " + Residuo.Nombre(t), new Color(1f, 0.45f, 0.4f), 26);
        GuardianCameraHUD.Sacudir(0.35f);
        GuardianIluminacion.Destello(new Color(1f, 0.15f, 0.1f), 0.4f);
    }

    private void AlGolpe(Vector3 p)
    {
        Vector3 pj = jugador != null ? jugador.position : p;
        GuardianParticulas.Rafaga(pj + Vector3.up * 0.3f, new Color(0.62f, 0.54f, 0.44f, 0.8f),
            new Color(0.45f, 0.40f, 0.34f, 0.6f), 40, 6f, 1.1f, 0.9f, 0.1f,
            GuardianParticulas.Tex.Suave, false, ParticleSystemShapeType.Circle, 0.5f, false, -0.5f);
        GuardianParticulas.Rafaga(pj + Vector3.up * 2.1f, new Color(1f, 0.9f, 0.3f), Color.white, 12, 2.5f, 0.9f, 0.45f, 0f,
            GuardianParticulas.Tex.Estrella, true, ParticleSystemShapeType.Circle, 0.6f);
        GuardianTextos.Mostrar(pj + Vector3.up * 2.6f, "−1 ♥", new Color(1f, 0.35f, 0.35f), 32);
        GuardianCameraHUD.Sacudir(0.85f);
        GuardianIluminacion.Destello(new Color(1f, 0f, 0f), 0.65f);
        if (jugador != null)
        {
            GuardianAnimaciones.Parpadeo(jugador.gameObject, 1.4f);
            PlayerController pc = jugador.GetComponent<PlayerController>();
            if (pc != null)
            {
                Vector3 dir = pj - p; dir.y = 0f;
                if (dir.sqrMagnitude < 0.01f) dir = -jugador.forward;
                pc.Empujar(dir.normalized, 9f);
                pc.Accion(1.0f, "hit", "damage", "fall", "stumble");
            }
        }
    }

    private void AlTerminar(bool gano)
    {
        if (jugador == null) return;
        if (gano) StartCoroutine(Fuegos());
        else GuardianParticulas.Humo(jugador.position + Vector3.up * 1.5f, new Color(0.35f, 0.33f, 0.3f, 0.8f), 40, 2.2f);
    }

    private IEnumerator Fuegos()
    {
        PlayerController pc = jugador.GetComponent<PlayerController>();
        if (pc != null) pc.Accion(2.5f, "cheer", "victory", "dance", "wave", "clap", "jump");
        for (int i = 0; i < 9; i++)
        {
            Vector3 p = jugador.position + new Vector3(Random.Range(-9f, 9f), Random.Range(9f, 14f), Random.Range(-9f, 9f));
            Color c = Residuo.Tinte(Residuo.Desde(i % Residuo.TIPOS));
            GuardianParticulas.FuegoArtificial(p, c);
            GuardianAudio.EnPunto(GuardianAudio.Pitido(Random.Range(700f, 1200f)), p, 0.35f, 0.8f, 1.2f, 60f);
            yield return new WaitForSecondsRealtime(0.32f);
        }
    }
}

/// <summary>
/// Textos flotantes en el mundo ("+45", "+1 Plástico", "✖ ..."). Nacen grandes
/// (pop), suben y se desvanecen: el jugador lee el resultado DONDE ocurrió.
/// </summary>
public class GuardianTextos : MonoBehaviour
{
    private struct Texto
    {
        public Vector3 pos; public string txt; public Color col; public int tam; public float t0, dur;
    }

    private static GuardianTextos inst;
    private readonly List<Texto> lista = new List<Texto>();
    private GUIStyle estilo;

    public static void Mostrar(Vector3 pos, string txt, Color col, int tam = 24, float retraso = 0f)
    {
        if (!Application.isPlaying) return;
        if (inst == null)
        {
            inst = FindObjectOfType<GuardianTextos>();
            if (inst == null) inst = new GameObject("GuardianTextos").AddComponent<GuardianTextos>();
        }
        inst.lista.Add(new Texto { pos = pos, txt = txt, col = col, tam = tam, t0 = Time.unscaledTime + retraso, dur = 1.3f });
    }

    void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null || lista.Count == 0) return;
        GUI.depth = -5;
        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label);
            estilo.alignment = TextAnchor.MiddleCenter;
            estilo.fontStyle = FontStyle.Bold;
        }

        float ahora = Time.unscaledTime;
        for (int i = lista.Count - 1; i >= 0; i--)
        {
            Texto t = lista[i];
            float k = (ahora - t.t0) / t.dur;
            if (k >= 1f) { lista.RemoveAt(i); continue; }
            if (k < 0f) continue;

            Vector3 w = t.pos + Vector3.up * (k * 1.6f);
            Vector3 s = cam.WorldToScreenPoint(w);
            if (s.z <= 0f) continue;

            float pop = k < 0.12f ? Mathf.Lerp(1.6f, 1f, k / 0.12f) : 1f;              // aparece "de golpe"
            float alfa = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            estilo.fontSize = Mathf.RoundToInt(t.tam * pop * Mathf.Clamp(Screen.height / 900f, 0.7f, 1.6f));

            Rect r = new Rect(s.x - 200f, Screen.height - s.y - 30f, 400f, 60f);
            estilo.normal.textColor = new Color(0f, 0f, 0f, 0.75f * alfa);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t.txt, estilo);
            estilo.normal.textColor = new Color(t.col.r, t.col.g, t.col.b, alfa);
            GUI.Label(r, t.txt, estilo);
        }
    }
}
