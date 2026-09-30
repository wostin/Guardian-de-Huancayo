using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Guardián de Huancayo - CONTROLES TÁCTILES (celular / tablet).
///
///  - Joystick virtual a la IZQUIERDA: mover (si lo empujas hasta el borde, corre).
///  - Arrastrar el dedo en la mitad DERECHA: girar la cámara.
///  - Botones: SALTAR (también espanta ratas), CORRER, CÁMARA (1ª/aérea/3ª),
///    MAPA y PAUSA.
/// Se activa solo en celulares (Application.isMobilePlatform) o al primer toque
/// en una pantalla táctil. En el editor se puede probar con F5 usando el mouse.
/// Funciona también en la versión Web (Unity Play) abierta desde el celular.
/// El resto del juego lee estos valores: PlayerController (mover, saltar,
/// correr) y GuardianCameraHUD (mirar, cámara, mapa, pausa).
/// </summary>
[DefaultExecutionOrder(-200)]
public class GuardianMovil : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        Activo = Application.isMobilePlatform;       // en el editor arranca apagado (F5 lo prende)
        if (FindObjectOfType<GuardianMovil>() != null) return;
        new GameObject("GuardianMovil").AddComponent<GuardianMovil>();
    }

    // ------------------------------------------------------------ estado que leen los demás
    public static bool Activo { get; private set; }
    public static Vector2 Mover { get; private set; }       // -1..1
    public static Vector2 Mirar { get; private set; }       // píxeles de arrastre este cuadro
    public static bool Correr { get; private set; }
    public static bool Salto { get; private set; }          // solo el cuadro en que se toca
    public static bool Camara { get; private set; }
    public static bool Pausa { get; private set; }
    public static bool Mapa { get; private set; }            // mantenido

    private enum Rol { Nada, Stick, Mirar, Saltar, Correr, Camara, Mapa, Pausa }

    private struct Dedo { public int id; public Vector2 pos, delta; public bool empieza, termina; }
    private readonly Dictionary<int, Rol> roles = new Dictionary<int, Rol>();
    private readonly List<Dedo> dedos = new List<Dedo>();
    private Vector2 stickCentro, stickPos;
    private bool stickVivo;
    private Vector2 mousePrev;
    private bool mouseAntes;
    private Texture2D circulo, anillo;
    private float escala = 1f;

    void Awake()
    {
        if (Application.isMobilePlatform) Activo = true;
        circulo = Circulo(false);
        anillo = Circulo(true);
    }

    void Update()
    {
        Salto = Camara = Pausa = false;
        Mirar = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame) Activo = !Activo;
#else
        if (Input.GetKeyDown(KeyCode.F5)) Activo = !Activo;
#endif
        LeerDedos();
        if (!Activo && dedos.Count > 0 && Touchscreen()) Activo = true;   // primer toque real
        if (!Activo) { Mover = Vector2.zero; Correr = false; Mapa = false; roles.Clear(); stickVivo = false; return; }

        escala = Mathf.Clamp(Mathf.Min(Screen.height / 720f, Screen.width / 1280f), 0.6f, 3f);
        bool mapa = false, correr = false;
        Vector2 mover = Vector2.zero;

        foreach (Dedo d in dedos)
        {
            Vector2 g = new Vector2(d.pos.x, Screen.height - d.pos.y);    // coordenadas de GUI
            if (d.termina && !roles.ContainsKey(d.id)) continue;           // toque viejo ya procesado
            Rol r;
            if (d.empieza || !roles.TryGetValue(d.id, out r))
            {
                r = Asignar(g);
                roles[d.id] = r;
                if (r == Rol.Stick) { stickCentro = g; stickVivo = true; }
                if (r == Rol.Saltar) Salto = true;
                if (r == Rol.Camara) Camara = true;
                if (r == Rol.Pausa) Pausa = true;
            }

            switch (r)
            {
                case Rol.Stick:
                    float radio = 80f * escala;
                    Vector2 v = (g - stickCentro) / radio;
                    if (v.magnitude > 1f) v.Normalize();
                    stickPos = stickCentro + v * radio;
                    mover = new Vector2(v.x, -v.y);
                    if (v.magnitude > 0.92f) correr = true;               // empujar hasta el borde = correr
                    break;
                case Rol.Mirar:
                    Mirar += new Vector2(d.delta.x, d.delta.y) * 0.9f;
                    break;
                case Rol.Correr: correr = true; break;
                case Rol.Mapa: mapa = true; break;
            }

            if (d.termina)
            {
                if (r == Rol.Stick) stickVivo = false;
                roles.Remove(d.id);
            }
        }
        if (!stickVivo) mover = Vector2.zero;
        Mover = mover; Correr = correr; Mapa = mapa;
    }

    private bool Touchscreen()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.press.isPressed;
#else
        return Input.touchCount > 0;
#endif
    }

    /// <summary>Junta toques reales y (para probar en PC) el mouse como un dedo más.</summary>
    private void LeerDedos()
    {
        dedos.Clear();
#if ENABLE_INPUT_SYSTEM
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null)
        {
            foreach (var t in ts.touches)
            {
                var fase = t.phase.ReadValue();
                if (fase == UnityEngine.InputSystem.TouchPhase.None) continue;
                bool fin = fase == UnityEngine.InputSystem.TouchPhase.Ended || fase == UnityEngine.InputSystem.TouchPhase.Canceled;
                if (!t.isInProgress && !fin) continue;
                dedos.Add(new Dedo { id = t.touchId.ReadValue(), pos = t.position.ReadValue(), delta = t.delta.ReadValue(),
                                     empieza = fase == UnityEngine.InputSystem.TouchPhase.Began, termina = fin });
            }
        }
        if (dedos.Count == 0 && Activo && Mouse.current != null)
        {
            bool p = Mouse.current.leftButton.isPressed;
            Vector2 pos = Mouse.current.position.ReadValue();
            if (p || mouseAntes)
                dedos.Add(new Dedo { id = 999, pos = pos, delta = pos - mousePrev, empieza = p && !mouseAntes, termina = !p });
            mouseAntes = p; mousePrev = pos;
        }
#else
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            dedos.Add(new Dedo { id = t.fingerId, pos = t.position, delta = t.deltaPosition,
                                 empieza = t.phase == TouchPhase.Began, termina = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled });
        }
#endif
    }

    // ------------------------------------------------------------ distribución de botones

    private Rect BSaltar  => Circ(Screen.width - 120f * escala, Screen.height - 130f * escala, 62f);
    private Rect BCorrer  => Circ(Screen.width - 255f * escala, Screen.height - 90f * escala, 48f);
    private Rect BCamara  => Circ(Screen.width - 95f * escala,  Screen.height - 265f * escala, 40f);
    private Rect BMapa    => Circ(Screen.width - 205f * escala, Screen.height - 225f * escala, 38f);
    private Rect BPausa   => Circ(Screen.width / 2f + 125f * escala, 28f * escala, 26f);

    private Rect Circ(float cx, float cy, float r)
    {
        r *= escala;
        return new Rect(cx - r, cy - r, r * 2f, r * 2f);
    }

    private static bool Dentro(Rect b, Vector2 g)
    {
        Vector2 c = b.center; float r = b.width * 0.5f * 1.15f;   // un poco de tolerancia para el dedo
        return (g - c).sqrMagnitude <= r * r;
    }

    private Rol Asignar(Vector2 g)
    {
        GameManager gm = GameManager.Instance;
        bool jugando = gm != null && gm.estado == GameManager.Estado.Jugando && !gm.pausado;
        if (Dentro(BPausa, g) && gm != null && (jugando || gm.pausado)) return Rol.Pausa;
        if (!jugando) return Rol.Nada;                       // en menús el toque va a los botones de la GUI
        if (Dentro(BSaltar, g)) return Rol.Saltar;
        if (Dentro(BCorrer, g)) return Rol.Correr;
        if (Dentro(BCamara, g)) return Rol.Camara;
        if (Dentro(BMapa, g))   return Rol.Mapa;
        if (g.x < Screen.width * 0.45f && g.y > Screen.height * 0.35f) return Rol.Stick;
        if (g.x >= Screen.width * 0.40f) return Rol.Mirar;
        return Rol.Nada;
    }

    // ------------------------------------------------------------ dibujo

    void OnGUI()
    {
        if (!Activo) return;
        GUI.depth = -30;
        GameManager gm = GameManager.Instance;

        // Celular vertical: pedir que lo gire.
        if (Screen.height > Screen.width)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUIStyle s = Estilo(Mathf.RoundToInt(Screen.width / 16f));
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), "⟳  Gira el celular\n(horizontal)", s);
            return;
        }

        bool jugando = gm != null && gm.estado == GameManager.Estado.Jugando && !gm.pausado;
        if (gm != null && (jugando || gm.pausado)) Boton(BPausa, gm.pausado ? "▶" : "II", false);
        if (!jugando) return;

        // Joystick
        float radio = 80f * escala;
        Vector2 c = stickVivo ? stickCentro : new Vector2(170f * escala, Screen.height - 170f * escala);
        Vector2 k = stickVivo ? stickPos : c;
        Pintar(anillo, new Rect(c.x - radio, c.y - radio, radio * 2f, radio * 2f), new Color(1f, 1f, 1f, 0.35f));
        Pintar(circulo, new Rect(c.x - radio, c.y - radio, radio * 2f, radio * 2f), new Color(0f, 0f, 0f, 0.18f));
        float rk = 34f * escala;
        Pintar(circulo, new Rect(k.x - rk, k.y - rk, rk * 2f, rk * 2f), new Color(1f, 1f, 1f, stickVivo ? 0.75f : 0.45f));
        if (!stickVivo)
            GUI.Label(new Rect(c.x - radio, c.y + radio + 2f, radio * 2f, 22f * escala), "MOVER", Estilo(Mathf.RoundToInt(12 * escala)));

        Boton(BSaltar, "SALTAR", false);
        Boton(BCorrer, "CORRER", Correr);
        Boton(BCamara, "CÁM", false);
        Boton(BMapa, "MAPA", Mapa);
        GUI.Label(new Rect(Screen.width * 0.55f, Screen.height - 34f * escala, Screen.width * 0.2f, 24f * escala),
                  "arrastra para mirar", Estilo(Mathf.RoundToInt(11 * escala), 0.55f));
    }

    private void Boton(Rect r, string t, bool activo)
    {
        Pintar(circulo, r, activo ? new Color(0.30f, 0.85f, 0.55f, 0.75f) : new Color(0.06f, 0.09f, 0.14f, 0.55f));
        Pintar(anillo, r, new Color(1f, 1f, 1f, 0.55f));
        GUI.Label(r, t, Estilo(Mathf.RoundToInt(Mathf.Clamp(r.width / 6.5f, 10f, 40f))));
    }

    private void Pintar(Texture2D tx, Rect r, Color c) { Color a = GUI.color; GUI.color = c; GUI.DrawTexture(r, tx); GUI.color = a; }

    private static GUIStyle Estilo(int tam, float alfa = 1f)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.Max(8, tam) };
        s.normal.textColor = new Color(1f, 1f, 1f, alfa);
        return s;
    }

    private static Texture2D Circulo(bool soloBorde)
    {
        const int n = 128;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = soloBorde ? Mathf.Clamp01(1f - Mathf.Abs(d - 0.94f) / 0.05f) : Mathf.Clamp01((1f - d) * 40f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }
}
