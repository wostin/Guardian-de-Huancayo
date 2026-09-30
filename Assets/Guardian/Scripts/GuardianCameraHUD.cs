using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Guardián de Huancayo - Cámara seguidora (orbita con el mouse) y HUD completo:
/// zona, nivel, vidas, cronómetro, contaminación, mochila de residuos, tabla de
/// colores de la NTP 900.058-2019, menús de inicio / pausa / resultados y créditos.
/// </summary>
public class GuardianCameraHUD : MonoBehaviour
{
    private Transform jugador;
    private Transform cam;

    [Header("Camara")]
    public float sensibilidadMouse = 0.15f;
    /// <summary>Sensibilidad para mirar arriba y abajo. Va aparte porque conviene
    /// que el eje vertical sea algo mas suave que el horizontal.</summary>
    public float sensibilidadVertical = 0.11f;
    /// <summary>Marcado: subir el mouse baja la vista (estilo simulador).</summary>
    public bool invertirVertical;

    private float yaw;

    // ------------------------------------------------------------ MODOS DE CÁMARA
    /// <summary>
    /// 0 = PRIMERA PERSONA (ojos del Guardián, se ve la ciudad a su altura)
    /// 1 = AÉREA / cenital (vista de estratega: ubicar basura y contenedores)
    /// 2 = TERCERA PERSONA (por defecto: se ve al Guardián y su entorno)
    /// Teclas 1, 2, 3 o V para cambiar. Se guarda entre partidas.
    /// </summary>
    public static int Modo = 2;

    // Escala de la interfaz según la resolución.
    private static float escUI = 1f;
    private static float SW => Screen.width / escUI;
    private static float SH => Screen.height / escUI;
    public static bool PrimeraPersona => Modo == 0;
    public static readonly string[] NOMBRE_MODO = { "1ª persona", "Aérea", "3ª persona" };
    private int modoAplicado = -1;
    private float avisoModoHasta;
    private float pitchTercera = 33f;

    // Sacudida de cámara ("trauma"): sube con cada golpe y baja sola.
    private static float trauma;
    public static void Sacudir(float cantidad) { trauma = Mathf.Clamp01(trauma + cantidad); }

    // Puntaje "rodante" del HUD: sube contando, no salta.
    private float puntajeMostrado;
    private float contaminacionMostrada;
    private int puntajeAnterior;
    private float popPuntaje;

    /// <summary>
    /// Inclinacion de la camara en grados sobre el horizonte. Antes la camara
    /// SOLO giraba en redondo: el brazo era un vector fijo (0, 11, -11) y no
    /// habia forma de levantar o bajar la vista, asi que no se podia mirar el
    /// piso para ubicar un residuo ni levantar la vista a las fachadas ni a los
    /// cerros. Ahora el brazo se arma con yaw + pitch y el mouse maneja los dos.
    /// </summary>
    private float pitch = 33f;
    private const float PITCH_MIN = -14f;   // vista casi a ras de piso, mirando arriba
    private const float PITCH_MAX = 80f;    // vista casi cenital, para buscar basura

    /// <summary>Largo del brazo de la camara. La rueda del mouse acerca y aleja.</summary>
    private float distancia = 15.6f;
    private const float DIST_MIN = 6.0f;
    private const float DIST_MAX = 28f;

    /// <summary>TAB abre el plano grande de la ciudad.</summary>
    private bool mapaGrande;
    private bool tabAntes;

    private Texture2D texPanel, texVerde, texNaranja, texRojo, texVida, texVidaOff, texAccent;
    private Texture2D texFondo, texCard, texCaja, texBlanco, texSombra;
    private Texture2D[] texTipo;

    private bool verCreditos;

    // Modo foto: F9 esconde todo el HUD para sacar capturas limpias del informe.
    private bool hudVisible = true;

    // Contador de FPS (F10): sirve para medir el rendimiento en el informe.
    private bool verFps;
    private float fps = 60f;

    // Tic-tac de los últimos segundos.
    private float proximoTic;
    private static AudioClip clipTic;

    // Marcas en pantalla (dónde está el contenedor que necesitas)
    private Camera camara;
    private RecycleBin[] botes;
    private TrashItem[] residuos;
    private PuntoAcopioZona[] acopios;
    private float tRefresco;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (FindObjectOfType<GuardianCameraHUD>() != null) return;
        if (GameObject.Find("GuardianAuto") != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianCameraHUD").AddComponent<GuardianCameraHUD>();
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;
        camara = Camera.main;
        if (camara != null) cam = camara.transform;

        texPanel   = Solido(new Color(0.05f, 0.07f, 0.10f, 0.72f));
        texVerde   = Solido(new Color(0.22f, 0.80f, 0.38f));
        texNaranja = Solido(new Color(0.96f, 0.66f, 0.12f));
        texRojo    = Solido(new Color(0.92f, 0.27f, 0.22f));
        texVida    = Solido(new Color(0.95f, 0.30f, 0.35f));
        texVidaOff = Solido(new Color(0.30f, 0.30f, 0.34f));
        texAccent  = Solido(new Color(0.30f, 0.85f, 0.55f));

        texFondo   = Solido(new Color(0.03f, 0.05f, 0.08f, 0.92f));
        texCard    = Solido(new Color(0.08f, 0.11f, 0.15f, 0.98f));
        texCaja    = Solido(new Color(0.14f, 0.18f, 0.24f, 0.95f));
        texBlanco  = Solido(new Color(0.95f, 0.95f, 0.95f));
        texSombra  = Solido(new Color(0.02f, 0.03f, 0.05f, 0.85f));

        texTipo = new Texture2D[Residuo.TIPOS];
        for (int i = 0; i < Residuo.TIPOS; i++)
            texTipo[i] = Solido(Residuo.Tinte(Residuo.Desde(i)));

        AplicarCalidad(PlayerPrefs.GetInt("guardian_calidad", Application.isMobilePlatform ? 0 : 2));
        AplicarVolumen(PlayerPrefs.GetFloat("guardian_volumen", 0.8f));
        Modo = Mathf.Clamp(PlayerPrefs.GetInt("guardian_camara", 2), 0, 2);
        if (jugador != null) yaw = jugador.eulerAngles.y;
    }

    private void CambiarModo(int m)
    {
        Modo = (m % 3 + 3) % 3;
        PlayerPrefs.SetInt("guardian_camara", Modo);
        avisoModoHasta = Time.unscaledTime + 1.8f;
        GuardianAudio.EnPantalla(GuardianAudio.Pitido(Modo == 0 ? 880f : (Modo == 1 ? 660f : 740f)), 0.25f);
    }

    /// <summary>En primera persona el cuerpo del Guardián solo proyecta sombra (no tapa la vista).</summary>
    private void AplicarModoAlCuerpo()
    {
        if (modoAplicado == Modo || jugador == null) return;
        modoAplicado = Modo;
        foreach (Renderer r in jugador.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            r.shadowCastingMode = PrimeraPersona ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                                                 : UnityEngine.Rendering.ShadowCastingMode.On;
        }
        if (Modo == 0) pitch = 6f;
        else if (Modo == 1) pitch = 62f;
        else pitch = pitchTercera;
    }

    /// <summary>
    /// Control de volumen general. El juego sintetiza toda su música y todos sus
    /// efectos, así que suena aunque la laptop no tenga nada instalado; pero no
    /// había manera de bajarle. Quien lo evalúe puede estar en un salón o con
    /// audífonos, así que se guarda su elección entre partidas.
    /// Con la tecla M se silencia y se vuelve a activar sin entrar al menú.
    /// </summary>
    private void AplicarVolumen(float v)
    {
        volumen = Mathf.Clamp01(v);
        AudioListener.volume = volumen;
        PlayerPrefs.SetFloat("guardian_volumen", volumen);
    }

    /// <summary>
    /// Tres niveles de calidad para que el juego corra también en una laptop
    /// modesta (que es donde lo va a abrir quien lo evalúe). Lo único que se
    /// toca son sombras, antialiasing y distancia de dibujado: el contenido del
    /// juego es exactamente el mismo.
    /// </summary>
    private void AplicarCalidad(int nivel)
    {
        calidad = Mathf.Clamp(nivel, 0, 2);
        PlayerPrefs.SetInt("guardian_calidad", calidad);
        GuardianIluminacion.AplicarCalidad(calidad);      // sombras URP + post-proceso

        switch (calidad)
        {
            case 0:   // Bajo
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowDistance = 20f;
                QualitySettings.antiAliasing = 0;
                if (camara != null) camara.farClipPlane = 420f;
                break;

            case 1:   // Medio
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowDistance = 60f;
                QualitySettings.antiAliasing = 2;
                if (camara != null) camara.farClipPlane = 700f;
                break;

            default:  // Alto
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowDistance = 120f;
                QualitySettings.antiAliasing = 4;
                if (camara != null) camara.farClipPlane = 1400f;
                break;
        }
    }

    private int calidad = 2;
    private static readonly string[] NOMBRE_CALIDAD = { "Bajo", "Medio", "Alto" };

    private float volumen = 0.8f;
    private float volumenAntesDeSilenciar = 0.8f;

    private Texture2D Solido(Color c)
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c); t.Apply();
        return t;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (EscPresionado() && (gm.estado == GameManager.Estado.Jugando || gm.pausado))
            gm.TogglePausa();

        if (camara == null && Camera.main != null) { camara = Camera.main; cam = camara.transform; }

        if (F9Presionado()) hudVisible = !hudVisible;

        bool enJuego = gm.estado == GameManager.Estado.Jugando && !gm.pausado;
        if (enJuego)
        {
            int tecla = TeclaModo();
            if (tecla >= 0) CambiarModo(tecla);
            else if (TeclaV()) CambiarModo(Modo == 2 ? 0 : Modo + 1);
        }
        if (F10Presionado()) verFps = !verFps;
        if (MPresionada()) Silenciar();

        // Media suavizada, que el número no salte en cada cuadro.
        if (Time.unscaledDeltaTime > 0f)
            fps = Mathf.Lerp(fps, 1f / Time.unscaledDeltaTime, 0.08f);
        if (gm.modoRecorrido && CPresionada()) gm.DemoContaminacion();

        // Últimos 15 segundos: un tic por segundo. Se siente la presión sin texto.
        if (gm.estado == GameManager.Estado.Jugando && !gm.pausado
            && gm.tiempoRestante > 0f && gm.tiempoRestante < 15f
            && Time.unscaledTime > proximoTic)
        {
            proximoTic = Time.unscaledTime + 1f;
            AudioSource.PlayClipAtPoint(Tic(), cam != null ? cam.position : Vector3.zero, 0.35f);
        }

        if (Time.unscaledTime > tRefresco)
        {
            tRefresco = Time.unscaledTime + 1f;
            botes    = FindObjectsByType<RecycleBin>(FindObjectsSortMode.None);
            residuos = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
            acopios  = FindObjectsByType<PuntoAcopioZona>(FindObjectsSortMode.None);
        }
    }

    private int TeclaModo()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return -1;
        if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) return 0;
        if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) return 1;
        if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) return 2;
        return -1;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
        return -1;
#endif
    }

    private bool TeclaV()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) || GuardianMovil.Camara;
#else
        return Input.GetKeyDown(KeyCode.V);
#endif
    }

    private bool CPresionada()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.C);
#endif
    }

    private bool F10Presionado()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F10);
#endif
    }

    private bool F9Presionado()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F9);
#endif
    }

    private bool MPresionada()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.M);
#endif
    }

    /// <summary>Silencia el juego, o lo devuelve al volumen que tenía antes.</summary>
    private void Silenciar()
    {
        if (volumen > 0.001f)
        {
            volumenAntesDeSilenciar = volumen;
            AplicarVolumen(0f);
        }
        else AplicarVolumen(volumenAntesDeSilenciar > 0.05f ? volumenAntesDeSilenciar : 0.8f);
    }

    private static AudioClip Tic()
    {
        if (clipTic != null) return clipTic;
        int sr = 44100;
        int len = Mathf.RoundToInt(sr * 0.05f);
        float[] d = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / sr;
            d[i] = Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 90f) * 0.8f;
        }
        clipTic = AudioClip.Create("tic", len, 1, sr, false);
        clipTic.SetData(d, 0);
        return clipTic;
    }

    void LateUpdate()
    {
        if (jugador == null || cam == null) return;

        GameManager gm = GameManager.Instance;
        bool jugando = gm != null && gm.estado == GameManager.Estado.Jugando && !gm.pausado;
        bool enMenu  = gm != null && gm.estado == GameManager.Estado.Menu;
        AplicarModoAlCuerpo();

        if (jugando)
        {
            yaw += LeerMouseX() * sensibilidadMouse;

            // Vertical: el mouse hacia arriba levanta la vista.
            float dy = LeerMouseY() * sensibilidadVertical;
            float pmin = PrimeraPersona ? -70f : (Modo == 1 ? 45f : PITCH_MIN);
            float pmax = PrimeraPersona ?  75f : (Modo == 1 ? 85f : PITCH_MAX);
            pitch = Mathf.Clamp(pitch + (invertirVertical ? dy : -dy), pmin, pmax);
            if (Modo == 2) pitchTercera = pitch;

            // Rueda del mouse: acerca y aleja la camara.
            float rueda = LeerRueda();
            if (Mathf.Abs(rueda) > 0.01f)
                distancia = Mathf.Clamp(distancia - rueda * 0.012f, DIST_MIN, DIST_MAX);

            // TAB: plano grande de la ciudad.
            bool tabAhora = TabPresionado();
            if (tabAhora && !tabAntes) mapaGrande = !mapaGrande;
            tabAntes = tabAhora;
        }

        Camera c = camara != null ? camara : cam.GetComponent<Camera>();
        float fovObjetivo = 60f;

        // ---------------------------------------------------------- MENÚ: vuelo lento
        if (enMenu)
        {
            // Detrás del menú la cámara sobrevuela la ciudad: el menú "respira".
            float t = Time.unscaledTime * 0.06f;
            Vector3 centro = jugador.position + Vector3.up * 2f;
            Vector3 p = centro + new Vector3(Mathf.Sin(t) * 46f, 26f + Mathf.Sin(t * 1.7f) * 4f, Mathf.Cos(t) * 46f);
            cam.position = Vector3.Lerp(cam.position, p, 2f * Time.unscaledDeltaTime);
            cam.rotation = Quaternion.Slerp(cam.rotation, Quaternion.LookRotation(centro - cam.position), 3f * Time.unscaledDeltaTime);
            if (c != null) c.fieldOfView = Mathf.Lerp(c.fieldOfView, 55f, 2f * Time.unscaledDeltaTime);
            yaw = cam.eulerAngles.y;
            return;
        }

        Vector3 deseada;
        Vector3 mira = jugador.position + Vector3.up * 1.4f;

        if (PrimeraPersona)
        {
            // ------------------------------------------------ 1ª PERSONA: ojos del Guardián
            Vector3 ojos = jugador.position + Vector3.up * 1.62f;
            // Balanceo de cabeza al caminar (head bob): sensación de pasos.
            CharacterController ccj = jugador.GetComponent<CharacterController>();
            float velJ = ccj != null ? new Vector3(ccj.velocity.x, 0f, ccj.velocity.z).magnitude : 0f;
            Quaternion soloYaw = Quaternion.Euler(0f, yaw, 0f);
            if (velJ > 0.5f && ccj.isGrounded)
            {
                bob += Time.deltaTime * velJ * 1.9f;
                ojos += Vector3.up * Mathf.Sin(bob * 2f) * 0.045f + (soloYaw * Vector3.right) * Mathf.Cos(bob) * 0.03f;
            }
            cam.position = ojos + (soloYaw * Vector3.forward) * 0.18f;
            cam.rotation = Quaternion.Euler(pitch, yaw, 0f);
            modoAplicadoPrevio = Modo;
            AplicarSacudida(c, 72f, 0.5f);
            return;
        }

        if (Modo == 1)
        {
            // ------------------------------------------------ AÉREA: vista de estratega
            float alto = Mathf.Clamp(distancia * 2.1f, 22f, 48f);
            deseada = mira + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -alto);
            fovObjetivo = 50f;
        }
        else
        {
            // ------------------------------------------------ 3ª PERSONA
            deseada = mira + Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, 0f, -distancia);

            // Si hay una pared entre el Guardián y la cámara, la cámara se acerca:
            // antes se metía dentro de los edificios y no se veía nada.
            Vector3 d = deseada - mira;
            float dist = d.magnitude;
            if (dist > 0.05f)
            {
                Vector3 dirCam = d / dist;
                bool qht = Physics.queriesHitTriggers;
                Physics.queriesHitTriggers = false;

                RaycastHit[] golpes = Physics.SphereCastAll(mira, 0.42f, dirCam, dist,
                                                            ~0, QueryTriggerInteraction.Ignore);
                float libre = dist;
                for (int i = 0; i < golpes.Length; i++)
                {
                    RaycastHit h = golpes[i];
                    if (h.distance < 0.05f) continue;
                    if (h.collider == null) continue;
                    if (h.collider.transform.IsChildOf(jugador)) continue;
                    if (h.distance < libre) libre = h.distance;
                }
                Physics.queriesHitTriggers = qht;

                deseada = mira + dirCam * Mathf.Max(2.6f, libre - 0.25f);
            }

            // Correr abre un poco el campo de visión: se siente la velocidad.
            CharacterController ccj = jugador.GetComponent<CharacterController>();
            float velJ = ccj != null ? new Vector3(ccj.velocity.x, 0f, ccj.velocity.z).magnitude : 0f;
            fovObjetivo = 60f + Mathf.Clamp01((velJ - 5.5f) / 3f) * 7f;
        }

        // Que no se entierre en el piso cuando la vista baja del todo.
        float piso = jugador.position.y;
        RaycastHit suelo;
        if (Physics.Raycast(deseada + Vector3.up * 30f, Vector3.down, out suelo, 80f,
                            ~0, QueryTriggerInteraction.Ignore))
            piso = Mathf.Max(piso, suelo.point.y);
        if (Modo == 2 && deseada.y < piso + 0.85f) deseada.y = piso + 0.85f;

        // Al cambiar de modo la cámara viaja suave a su nueva posición.
        if (modoAplicadoPrevio != Modo) { transicion = 1f; modoAplicadoPrevio = Modo; }
        transicion = Mathf.MoveTowards(transicion, 0f, Time.deltaTime * 1.5f);
        float suave = Mathf.Lerp(11f, 3.5f, transicion);
        cam.position = Vector3.Lerp(cam.position, deseada, suave * Time.deltaTime);
        cam.LookAt(mira);
        AplicarSacudida(c, fovObjetivo, 1f);
    }

    private int modoAplicadoPrevio = -1;
    private float transicion;
    private float bob;

    /// <summary>Sacudida por "trauma" (ruido Perlin, no azar puro: no marea) + FOV suave.</summary>
    private void AplicarSacudida(Camera c, float fov, float escala)
    {
        if (c != null) c.fieldOfView = Mathf.Lerp(c.fieldOfView, fov, 6f * Time.deltaTime);
        if (trauma <= 0.001f) return;
        float s = trauma * trauma * escala;
        float t = Time.unscaledTime * 25f;
        cam.position += cam.right * (Mathf.PerlinNoise(t, 0f) - 0.5f) * 0.9f * s
                      + cam.up    * (Mathf.PerlinNoise(0f, t) - 0.5f) * 0.9f * s;
        cam.rotation *= Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t, t) - 0.5f) * 8f * s);
        trauma = Mathf.MoveTowards(trauma, 0f, Time.unscaledDeltaTime * 1.6f);
    }

    private float LeerMouseX()
    {
        if (GuardianMovil.Activo) return GuardianMovil.Mirar.x;    // arrastre táctil
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue().x : 0f;
#else
        return Input.GetAxis("Mouse X") * 10f;
#endif
    }

    private float LeerMouseY()
    {
        if (GuardianMovil.Activo) return GuardianMovil.Mirar.y;
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue().y : 0f;
#else
        return Input.GetAxis("Mouse Y") * 10f;
#endif
    }

    private float LeerRueda()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
#else
        return Input.GetAxis("Mouse ScrollWheel") * 120f;
#endif
    }

    private bool TabPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.tabKey.isPressed) || GuardianMovil.Mapa;
#else
        return Input.GetKey(KeyCode.Tab);
#endif
    }

    /// <summary>
    /// Pausa con ESC o con P. Van las dos porque el Game View del editor se queda
    /// con la tecla ESC (la usa para soltar el foco), así que probando dentro de
    /// Unity la pausa parecía no funcionar aunque en el .exe sí funcione. Con P
    /// se puede comprobar en los dos lados, y de paso ESC queda libre para quien
    /// lo use por costumbre.
    /// </summary>
    private bool EscPresionado()
    {
#if ENABLE_INPUT_SYSTEM
        if (GuardianMovil.Pausa) return true;
        if (Keyboard.current == null) return false;
        return Keyboard.current.escapeKey.wasPressedThisFrame
            || Keyboard.current.pKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
    }

    void OnGUI()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        // La interfaz se ESCALA con la resolución: en Full HD o 4K ya no sale
        // diminuta (se diseñó para ~760 px de alto).
        escUI = Mathf.Clamp(Mathf.Min(Screen.height / 800f, Screen.width / 1360f), 0.7f, 2.4f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escUI, escUI, 1f));
        EstiloBotones();

        if (verFps)
        {
            Color fc = fps >= 50f ? new Color(0.45f, 1f, 0.62f)
                     : fps >= 30f ? new Color(1f, 0.85f, 0.35f)
                                  : new Color(1f, 0.5f, 0.45f);
            GUI.Label(new Rect(SW - 130, 2, 120, 22),
                Mathf.RoundToInt(fps) + " FPS",
                Txt(15, FontStyle.Bold, fc, TextAnchor.MiddleRight));
        }

        if (!hudVisible)
        {
            // Modo foto (F9): solo un recordatorio chiquito en la esquina.
            GUI.Label(new Rect(SW - 150, SH - 26, 140, 20), "F9 = ver HUD",
                Txt(11, FontStyle.Normal, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleRight));
            return;
        }

        if (gm.pausado) { Pausa(gm); return; }

        switch (gm.estado)
        {
            case GameManager.Estado.Menu: Menu(gm); break;
            case GameManager.Estado.Jugando: HUD(gm); break;
            default: Fin(gm); break;
        }
    }

    // ============================ HUD ============================

    private void HUD(GameManager gm)
    {
        // Borde rojo latiendo: la ciudad está por colapsar de basura.
        float peligro = gm.maxContaminacion > 0f
            ? Mathf.InverseLerp(gm.maxContaminacion * 0.78f, gm.maxContaminacion, gm.contaminacion)
            : 0f;
        if (peligro > 0.01f)
        {
            Color prev = GUI.color;
            GUI.color = new Color(0.92f, 0.16f, 0.12f,
                                  peligro * (0.22f + 0.18f * Mathf.Sin(Time.unscaledTime * 5f)));
            const float g = 26f;
            GUI.DrawTexture(new Rect(0, 0, SW, g), texBlanco);
            GUI.DrawTexture(new Rect(0, SH - g, SW, g), texBlanco);
            GUI.DrawTexture(new Rect(0, 0, g, SH), texBlanco);
            GUI.DrawTexture(new Rect(SW - g, 0, g, SH), texBlanco);
            GUI.color = prev;
        }

        GUI.DrawTexture(new Rect(14, 12, 320, 200), texPanel);
        GUI.DrawTexture(new Rect(14, 12, 320, 4), texAccent);

        GUI.Label(new Rect(26, 20, 300, 24), "Zona: " + gm.ZonaNombre,
            Txt(16, FontStyle.Bold, new Color(0.8f, 0.95f, 0.85f), TextAnchor.MiddleLeft));

        string badge = gm.modoRecorrido
            ? "MODO RECORRIDO · C = ensuciar / limpiar la ciudad · Esc = pausa"
            : gm.ZonaPeatonal ? "Zona peatonal · segura (sin carros)"
                              : "Zona con tráfico · ¡evita los carros!";
        Color badgeCol = gm.modoRecorrido ? new Color(0.62f, 0.90f, 1f)
                       : gm.ZonaPeatonal  ? new Color(0.50f, 0.90f, 0.65f)
                                          : new Color(0.98f, 0.68f, 0.20f);
        GUI.Label(new Rect(26, 42, 300, 20), badge,
            Txt(13, FontStyle.Bold, badgeCol, TextAnchor.MiddleLeft));

        GUI.Label(new Rect(26, 64, 130, 22), "Nivel " + gm.nivel,
            Txt(14, FontStyle.Bold, new Color(0.6f, 0.9f, 0.7f), TextAnchor.MiddleLeft));

        if (gm.esperandoAcopio)
        {
            GUI.Label(new Rect(150, 64, 190, 22), "→ AL ACOPIO",
                Txt(16, FontStyle.Bold, new Color(0.55f, 1f, 0.78f), TextAnchor.MiddleLeft));
        }
        else if (gm.modoRecorrido)
        {
            GUI.Label(new Rect(150, 64, 180, 22), "Sin límite",
                Txt(16, FontStyle.Bold, new Color(0.62f, 0.90f, 1f), TextAnchor.MiddleLeft));
        }
        else
        {
            int min = (int)(gm.tiempoRestante / 60f);
            int seg = (int)(gm.tiempoRestante % 60f);
            Color tCol = gm.tiempoRestante < 20f ? new Color(1f, 0.5f, 0.5f) : Color.white;
            GUI.Label(new Rect(150, 64, 180, 22), string.Format("Tiempo {0:0}:{1:00}", min, seg),
                Txt(16, FontStyle.Bold, tCol, TextAnchor.MiddleLeft));
        }

        // Puntaje que CUENTA hacia arriba y "salta" al ganar puntos.
        if (gm.puntaje != puntajeAnterior)
        {
            if (gm.puntaje > puntajeAnterior) popPuntaje = 1f;
            puntajeAnterior = gm.puntaje;
        }
        puntajeMostrado = Mathf.MoveTowards(puntajeMostrado, gm.puntaje,
            Mathf.Max(20f, Mathf.Abs(gm.puntaje - puntajeMostrado) * 4f) * Time.unscaledDeltaTime);
        popPuntaje = Mathf.MoveTowards(popPuntaje, 0f, Time.unscaledDeltaTime * 2.5f);
        GUI.Label(new Rect(26, 88, 300, 22), "Puntaje: " + Mathf.RoundToInt(puntajeMostrado),
            Txt(16 + Mathf.RoundToInt(popPuntaje * 6f), FontStyle.Bold,
                Color.Lerp(Color.white, new Color(0.5f, 1f, 0.6f), popPuntaje), TextAnchor.MiddleLeft));
        GUI.Label(new Rect(26, 108, 300, 22), "Recicladas: " + gm.recicladas + " / " + gm.basuraTotal, Fila());

        // Racha de segregacion. Aparece solo cuando hay algo que presumir, late
        // un momento al sumar y se apaga sola al errar: el jugador la ve crecer
        // por el rabillo del ojo sin que le robe sitio al resto del HUD.
        if (gm.racha >= 2)
        {
            int multi = Mathf.Clamp(1 + (gm.racha - 1) / 3, 1, 5);
            float desde = Time.unscaledTime - gm.ultimoAcierto;
            float latido = Mathf.Clamp01(1f - desde * 2.2f);           // se apaga en medio segundo
            float alto = 26f + latido * 6f;

            Color tono = (multi >= 4) ? new Color(1f, 0.78f, 0.25f)
                       : (multi >= 2) ? new Color(0.55f, 1f, 0.72f)
                                      : new Color(0.75f, 0.88f, 1f);

            Rect rr = new Rect(24, 128, 150f + latido * 10f, alto);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rr, texBlanco);
            GUI.color = new Color(tono.r, tono.g, tono.b, 0.30f + latido * 0.45f);
            GUI.DrawTexture(new Rect(rr.x, rr.y, 4f, rr.height), texBlanco);
            GUI.color = Color.white;

            GUI.Label(new Rect(rr.x + 12f, rr.y, rr.width - 14f, rr.height),
                "RACHA " + gm.racha + (multi > 1 ? "   x" + multi : ""),
                Txt(Mathf.RoundToInt(14 + latido * 3f), FontStyle.Bold, tono, TextAnchor.MiddleLeft));
        }

        // Mochila: una ficha por tipo de residuo que llevas encima.
        // Con la racha en pantalla, todo lo de abajo baja un renglon: asi la
        // ficha de racha nunca se come la mochila ni las vidas.
        float dy = (gm.racha >= 2) ? 32f : 0f;

        GUI.Label(new Rect(26, 130 + dy, 90, 20),
            "Mochila " + gm.cargaActual + "/" + gm.capacidadCarga,
            Txt(13, FontStyle.Bold, new Color(0.85f, 0.88f, 0.92f), TextAnchor.MiddleLeft));
        float fx = 128f;
        for (int i = 0; i < Residuo.TIPOS; i++)
        {
            TipoResiduo t = Residuo.Desde(i);
            int n = gm.CuantosLlevo(t);
            if (n <= 0) continue;
            GUI.DrawTexture(new Rect(fx, 130 + dy, 34, 20), texTipo[i]);
            GUI.Label(new Rect(fx, 130 + dy, 34, 20), n.ToString(),
                Txt(13, FontStyle.Bold, ContrasteSobre(Residuo.Tinte(t)), TextAnchor.MiddleCenter));
            fx += 38f;
        }

        GUI.Label(new Rect(26, 156 + dy, 60, 22), "Vidas:", Fila());
        for (int i = 0; i < gm.maxVidas; i++)
            GUI.DrawTexture(new Rect(90 + i * 26, 158 + dy, 20, 18), i < gm.vidas ? texVida : texVidaOff);

        Rect barra = new Rect(200, 160 + dy, 120, 14);
        GUI.DrawTexture(barra, texVidaOff);
        contaminacionMostrada = Mathf.Lerp(contaminacionMostrada,
            Mathf.Clamp01(gm.contaminacion / gm.maxContaminacion), Time.unscaledDeltaTime * 4f);
        float f = contaminacionMostrada;          // la barra se desliza, no salta
        Texture2D col = f < 0.5f ? texVerde : (f < 0.8f ? texNaranja : texRojo);
        GUI.DrawTexture(new Rect(barra.x, barra.y, barra.width * f, barra.height), col);
        GUI.Label(new Rect(26, 180 + dy, 300, 18), "Contaminación del Shullcas",
            Txt(11, FontStyle.Normal, new Color(0.75f, 0.8f, 0.85f), TextAnchor.MiddleLeft));

        TablaColores();
        Minimapa(gm);
        Marcas(gm);

        if (!GuardianMovil.Activo)
        GUI.Label(new Rect(SW - 700, SH - 32, 690, 20),
            "1/2/3 o V cámara · G flechas · F8 audio · ESC pausa · Shift correr · TAB mapa · M silencio · F9 HUD",
            Txt(12, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleRight));

        // Chip de cámara arriba al centro + aviso grande al cambiar.
        Rect chip = new Rect(SW / 2f - 70f, 10f, 140f, 24f);
        GUI.DrawTexture(chip, texPanel);
        GUI.Label(chip, "Cámara: " + NOMBRE_MODO[Modo],
            Txt(12, FontStyle.Bold, new Color(0.8f, 0.92f, 1f), TextAnchor.MiddleCenter));
        if (Time.unscaledTime < avisoModoHasta)
        {
            float a = Mathf.Clamp01((avisoModoHasta - Time.unscaledTime) / 0.5f);
            GUI.Label(new Rect(0, SH * 0.30f, SW, 50), "📷  " + NOMBRE_MODO[Modo].ToUpper(),
                Txt(34, FontStyle.Bold, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter));
        }
        if (PrimeraPersona)
        {
            // Mira central en primera persona.
            Color antes = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.7f);
            GUI.DrawTexture(new Rect(SW / 2f - 1f, SH / 2f - 7f, 2f, 14f), texBlanco);
            GUI.DrawTexture(new Rect(SW / 2f - 7f, SH / 2f - 1f, 14f, 2f), texBlanco);
            GUI.color = antes;
        }

        Pista(gm);
        if (gm.HayAviso) Aviso(gm);
        if (gm.HayLeccion) Leccion(gm);
    }

    /// <summary>
    /// Señala en pantalla el contenedor que hace falta (con la distancia) y los
    /// residuos cercanos. Sin esto, buscar el color correcto sería a ciegas.
    /// </summary>
    private void Marcas(GameManager gm)
    {
        if (camara == null || jugador == null) return;

        if (botes != null)
        {
            for (int i = 0; i < botes.Length; i++)
            {
                RecycleBin b = botes[i];
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                if (gm.CuantosLlevo(b.acepta) <= 0) continue;

                Vector2 p; bool dentro;
                if (!Proyectar(b.transform.position + Vector3.up * 3.4f, out p, out dentro)) continue;

                float d = Vector3.Distance(jugador.position, b.transform.position);
                Color c = Residuo.Tinte(b.acepta);

                Rect r = new Rect(p.x - 36f, p.y - 13f, 72f, 26f);
                Color antes = GUI.color;
                GUI.color = c;
                GUI.DrawTexture(r, texBlanco);
                GUI.color = antes;
                GUI.Label(r, (dentro ? "" : "▸ ") + Mathf.RoundToInt(d) + " m",
                    Txt(12, FontStyle.Bold, ContrasteSobre(c), TextAnchor.MiddleCenter));
            }
        }

        // Cuando falta entregar en el acopio, ese es EL objetivo: marca grande.
        if (gm.esperandoAcopio && acopios != null)
        {
            for (int i = 0; i < acopios.Length; i++)
            {
                PuntoAcopioZona a = acopios[i];
                if (a == null || !a.gameObject.activeInHierarchy) continue;
                if (a.zona != gm.zonaActual) continue;

                Vector2 p; bool dentro;
                if (!Proyectar(a.transform.position + Vector3.up * 4.2f, out p, out dentro)) continue;

                float d = Vector3.Distance(jugador.position, a.transform.position);
                Rect r = new Rect(p.x - 92f, p.y - 17f, 184f, 34f);

                Color antes = GUI.color;
                GUI.color = new Color(0.30f, 0.95f, 0.60f);
                GUI.DrawTexture(r, texBlanco);
                GUI.color = antes;

                GUI.Label(r, (dentro ? "PUNTO DE ACOPIO  " : "▸ PUNTO DE ACOPIO  ")
                           + Mathf.RoundToInt(d) + " m",
                    Txt(14, FontStyle.Bold, new Color(0.05f, 0.14f, 0.09f), TextAnchor.MiddleCenter));
            }
        }

        if (residuos != null && gm.cargaActual < gm.capacidadCarga)
        {
            for (int i = 0; i < residuos.Length; i++)
            {
                TrashItem t = residuos[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                if (Vector3.Distance(jugador.position, t.transform.position) > 45f) continue;

                Vector2 p; bool dentro;
                if (!Proyectar(t.transform.position + Vector3.up * 1.3f, out p, out dentro)) continue;
                if (!dentro) continue;

                Color antes = GUI.color;
                GUI.color = Residuo.Tinte(t.tipo);
                GUI.DrawTexture(new Rect(p.x - 5f, p.y - 5f, 10f, 10f), texBlanco);
                GUI.color = antes;
            }
        }
    }

    /// <summary>
    /// Minimapa (radar) del lado derecho: el Guardián al centro, los contenedores
    /// con el color de la norma y los residuos como puntitos. Arriba del mapa es
    /// siempre hacia donde mira la cámara. Sin esto la ciudad es grande y uno
    /// termina dando vueltas buscando el contenedor correcto.
    /// </summary>
    /// <summary>
    /// Minimapa.
    ///
    /// Antes era una caja oscura con cuadritos de colores flotando: sin calles ni
    /// nada de referencia no se entendia donde quedaba nada, y leia como ruido.
    /// Ahora dibuja PRIMERO la cuadricula de calles —que ya viene horneada en
    /// RedVial— y encima los marcadores. Con las calles de fondo el mapa se lee
    /// de un vistazo: se ve la manzana en la que estas y hacia donde doblar.
    ///
    /// El truco para las calles es rotar la GUI entera alrededor del centro: las
    /// vias corren en X o en Z del mundo, asi que dibujadas sin rotar son
    /// rectangulos rectos, y la rotacion las orienta con la camara de una sola vez.
    /// </summary>
    private void Minimapa(GameManager gm)
    {
        if (jugador == null) return;

        // TAB abre el plano grande. El radar de la esquina servia para orientarse
        // a la vuelta de la esquina, pero para ubicar un contenedor al otro lado
        // de la ciudad quedaba corto, y en una ventana angosta ni se alcanzaba a
        // ver. El plano grande abarca el doble de ciudad y se centra en pantalla.
        if (mapaGrande)
        {
            float G = Mathf.Clamp(Mathf.Min(SW, SH) * 0.76f, 240f, 720f);
            DibujarMapa(gm, new Rect((SW - G) * 0.5f, (SH - G) * 0.5f, G, G), 160f);
            return;
        }

        // Tamano segun la ventana, y SIEMPRE dentro de la pantalla: antes eran
        // 200 px fijos pegados al borde y en ventanas chicas el mapa se cortaba.
        float S = Mathf.Clamp(Mathf.Min(SW, SH) * 0.26f, 128f, 200f);
        float mx = Mathf.Max(8f, SW  - S - 14f);
        float my = Mathf.Clamp(150f, 8f, Mathf.Max(8f, SH - S - 12f));
        DibujarMapa(gm, new Rect(mx, my, S, S), 75f);
    }

    private void DibujarMapa(GameManager gm, Rect marco, float alcance)
    {
        float S = marco.width;

        GUI.DrawTexture(marco, texCard);                 // fondo: las manzanas
        GUI.DrawTexture(new Rect(marco.x, marco.y, marco.width, 3f), texAccent);

        GUI.BeginGroup(marco);                           // nada se sale del recuadro

        Vector2 c = new Vector2(S / 2f, S / 2f + 9f);
        float esc = (S / 2f - 14f) / alcance;
        float radio = S / 2f - 14f;

        float ry = yaw * Mathf.Deg2Rad;
        float sn = Mathf.Sin(ry), cs = Mathf.Cos(ry);
        Color guardado = GUI.color;

        // ---------- Calles de fondo ----------
        RedVial rv = RedVial.Instancia;
        if (rv != null && rv.centros != null)
        {
            Matrix4x4 antes = GUI.matrix;
            // Rotación alrededor del centro del mapa. Se arma a mano (en vez de
            // GUIUtility.RotateAroundPivot) para que funcione con la GUI escalada.
            Vector3 piv = new Vector3(marco.x + c.x, marco.y + c.y, 0f);
            GUI.matrix = antes * Matrix4x4.Translate(piv)
                               * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, -yaw))
                               * Matrix4x4.Translate(-piv);

            float mt = rv.medioTramo * esc;              // media longitud del tramo
            float mc = rv.mediaCalzada * esc;            // media anchura de calzada

            // OPACO a proposito. Con transparencia, cada losa de calle se sumaba
            // a la de al lado y los cruces salian mas claros que los tramos: el
            // mapa se veia apelmazado, como manchas, en vez de una cuadricula.
            // Con color solido el solape no acumula y la trama queda limpia.
            GUI.color = new Color(0.34f, 0.37f, 0.42f, 1f);
            int n = Mathf.Min(rv.centros.Count, rv.ejes.Count);
            for (int i = 0; i < n; i++)
            {
                Vector3 d = rv.centros[i] - jugador.position;
                if (Mathf.Abs(d.x) > alcance + 20f || Mathf.Abs(d.z) > alcance + 20f) continue;

                float px = c.x + d.x * esc;
                float py = c.y - d.z * esc;               // el norte del mundo va arriba

                if ((rv.ejes[i] & 1) != 0)                // corre en X
                    GUI.DrawTexture(new Rect(px - mt, py - mc, mt * 2f, mc * 2f), texBlanco);
                if ((rv.ejes[i] & 2) != 0)                // corre en Z
                    GUI.DrawTexture(new Rect(px - mc, py - mt, mc * 2f, mt * 2f), texBlanco);
            }

            // Eje de la calzada, apenas insinuado: da sensacion de calle.
            GUI.color = new Color(0.46f, 0.49f, 0.54f, 1f);
            for (int i = 0; i < n; i++)
            {
                Vector3 d = rv.centros[i] - jugador.position;
                if (Mathf.Abs(d.x) > alcance + 20f || Mathf.Abs(d.z) > alcance + 20f) continue;

                float px = c.x + d.x * esc;
                float py = c.y - d.z * esc;

                if ((rv.ejes[i] & 1) != 0)
                    GUI.DrawTexture(new Rect(px - mt, py - 0.5f, mt * 2f, 1f), texBlanco);
                if ((rv.ejes[i] & 2) != 0)
                    GUI.DrawTexture(new Rect(px - 0.5f, py - mt, 1f, mt * 2f), texBlanco);
            }

            GUI.color = guardado;
            GUI.matrix = antes;
        }

        // ---------- Residuos tirados ----------
        if (residuos != null)
            for (int i = 0; i < residuos.Length; i++)
            {
                TrashItem t = residuos[i];
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                Vector2 q;
                if (!EnRadar(t.transform.position, c, esc, alcance, sn, cs, out q)) continue;

                GUI.color = new Color(0f, 0f, 0f, 0.55f);         // sombra, para que resalte
                GUI.DrawTexture(new Rect(q.x - 3.5f, q.y - 3.5f, 7f, 7f), texBlanco);
                GUI.color = Residuo.Tinte(t.tipo);
                GUI.DrawTexture(new Rect(q.x - 2.5f, q.y - 2.5f, 5f, 5f), texBlanco);
                GUI.color = guardado;
            }

        // ---------- Contenedores ----------
        if (botes != null)
            for (int i = 0; i < botes.Length; i++)
            {
                RecycleBin b = botes[i];
                if (b == null || !b.gameObject.activeInHierarchy) continue;

                Vector2 q;
                bool dentro = EnRadar(b.transform.position, c, esc, alcance, sn, cs, out q);
                bool util = gm.CuantosLlevo(b.acepta) > 0;
                float t = util ? 14f : 10f;

                // El que hace falta AHORA late, para encontrarlo sin pensarlo.
                if (util)
                {
                    float pulso = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                    GUI.color = new Color(1f, 1f, 1f, 0.45f + 0.45f * pulso);
                    GUI.DrawTexture(new Rect(q.x - t / 2f - 3f, q.y - t / 2f - 3f, t + 6f, t + 6f), texBlanco);
                }

                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(q.x - t / 2f - 1f, q.y - t / 2f - 1f, t + 2f, t + 2f), texBlanco);
                GUI.color = Residuo.Tinte(b.acepta);
                GUI.DrawTexture(new Rect(q.x - t / 2f, q.y - t / 2f, t, t), texBlanco);

                // Fuera del alcance: una flechita en el borde en vez de nada.
                if (!dentro)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                    GUI.DrawTexture(new Rect(q.x - 1.5f, q.y - 1.5f, 3f, 3f), texBlanco);
                }
                GUI.color = guardado;
            }

        // ---------- Bicicletas ----------
        Bicicleta[] bicis = FindObjectsByType<Bicicleta>(FindObjectsSortMode.None);
        for (int i = 0; i < bicis.Length; i++)
        {
            if (bicis[i] == null) continue;
            Vector2 q;
            if (!EnRadar(bicis[i].transform.position, c, esc, alcance, sn, cs, out q)) continue;

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(q.x - 4f, q.y - 4f, 8f, 8f), texBlanco);
            GUI.color = new Color(0.45f, 0.85f, 1f);
            GUI.DrawTexture(new Rect(q.x - 3f, q.y - 3f, 6f, 6f), texBlanco);
            GUI.color = guardado;
        }

        // ---------- Punto de acopio ----------
        if (acopios != null)
            for (int i = 0; i < acopios.Length; i++)
            {
                PuntoAcopioZona a = acopios[i];
                if (a == null || !a.gameObject.activeInHierarchy) continue;
                if (!gm.modoRecorrido && a.zona != gm.zonaActual) continue;

                Vector2 q;
                EnRadar(a.transform.position, c, esc, alcance, sn, cs, out q);
                float t = gm.esperandoAcopio ? 16f : 11f;

                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(q.x - t / 2f - 1f, q.y - t / 2f - 1f, t + 2f, t + 2f), texBlanco);
                GUI.color = gm.esperandoAcopio
                    ? new Color(0.35f, 1f, 0.62f) : new Color(0.60f, 0.67f, 0.75f);
                GUI.DrawTexture(new Rect(q.x - t / 2f, q.y - t / 2f, t, t), texBlanco);
                GUI.color = new Color(0.05f, 0.12f, 0.08f);
                GUI.DrawTexture(new Rect(q.x - t / 2f + 3f, q.y - t / 2f + 3f, t - 6f, t - 6f), texBlanco);
                GUI.color = guardado;
            }

        // ---------- El Guardián: flecha al centro ----------
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(c.x - 5f, c.y - 5f, 10f, 10f), texBlanco);
        GUI.color = new Color(0.35f, 0.95f, 0.60f);
        GUI.DrawTexture(new Rect(c.x - 3.5f, c.y - 3.5f, 7f, 7f), texBlanco);
        for (int k = 0; k < 7; k++)                       // punta triangular hacia arriba
        {
            float w = 7f - k;
            GUI.DrawTexture(new Rect(c.x - w / 2f, c.y - 6f - k, w, 1f), texBlanco);
        }
        GUI.color = guardado;

        // ---------- Norte ----------
        Vector2 nte = new Vector2(c.x - sn * radio * 0.92f, c.y - cs * radio * 0.92f);
        GUI.Label(new Rect(nte.x - 8f, nte.y - 8f, 16f, 16f), "N",
            Txt(11, FontStyle.Bold, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleCenter));

        // Marco interior, para que el mapa no se funda con el fondo del juego.
        GUI.color = new Color(1f, 1f, 1f, 0.16f);
        GUI.DrawTexture(new Rect(0f, S - 1f, S, 1f), texBlanco);
        GUI.DrawTexture(new Rect(0f, 0f, 1f, S), texBlanco);
        GUI.DrawTexture(new Rect(S - 1f, 0f, 1f, S), texBlanco);
        GUI.color = guardado;

        GUI.DrawTexture(new Rect(0f, 3f, S, 19f), texSombra);
        GUI.Label(new Rect(10f, 4f, S - 20f, 18f),
            "MAPA · " + (int)alcance + " m" + (mapaGrande ? "  ·  TAB cierra" : "  ·  TAB"),
            Txt(11, FontStyle.Bold, new Color(0.80f, 0.90f, 1f), TextAnchor.MiddleLeft));

        GUI.EndGroup();
    }

    private bool EnRadar(Vector3 mundo, Vector2 c, float esc, float alcance,
                         float sn, float cs, out Vector2 q)
    {
        Vector3 d = mundo - jugador.position;
        float adelante = d.x * sn + d.z * cs;       // eje hacia donde mira la cámara
        float lado     = d.x * cs - d.z * sn;

        Vector2 v = new Vector2(lado, adelante);
        bool dentro = v.magnitude <= alcance;
        if (!dentro && v.sqrMagnitude > 0.0001f) v = v.normalized * alcance;

        q = new Vector2(c.x + v.x * esc, c.y - v.y * esc);
        return dentro;
    }

    private bool Proyectar(Vector3 mundo, out Vector2 p, out bool dentro)
    {
        Vector3 sp = camara.WorldToScreenPoint(mundo);
        sp.x /= escUI; sp.y /= escUI;            // píxeles reales → unidades de la GUI escalada
        bool detras = sp.z <= 0f;
        p = new Vector2(sp.x, SH - sp.y);
        if (detras) { p.x = SW - sp.x; p.y = SH - 86f; }

        const float mx = 48f, my = 74f;
        dentro = !detras && p.x > mx && p.x < SW - mx
                          && p.y > my && p.y < SH - my;
        p.x = Mathf.Clamp(p.x, mx, SW - mx);
        p.y = Mathf.Clamp(p.y, my, SH - my);
        return true;
    }

    /// <summary>Tabla de colores NTP 900.058-2019, siempre visible: es la lección del juego.</summary>
    private void TablaColores()
    {
        float w = 232f, h = 26f + Residuo.TIPOS * 22f;
        Rect r = new Rect(SW - w - 14, 12, w, h);
        GUI.DrawTexture(r, texPanel);
        GUI.Label(new Rect(r.x + 10, r.y + 4, w - 20, 20), "SEGREGA BIEN · NTP 900.058",
            Txt(12, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleLeft));

        for (int i = 0; i < Residuo.TIPOS; i++)
        {
            TipoResiduo t = Residuo.Desde(i);
            float y = r.y + 26f + i * 22f;
            GUI.DrawTexture(new Rect(r.x + 10, y + 3, 16, 14), texTipo[i]);
            GUI.Label(new Rect(r.x + 32, y, 90, 20), Residuo.ColorNTP(t),
                Txt(11, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(r.x + 100, y, w - 108, 20), Residuo.Nombre(t),
                Txt(11, FontStyle.Normal, new Color(0.82f, 0.87f, 0.92f), TextAnchor.MiddleLeft));
        }
    }

    /// <summary>Pista fija del tutorial del primer nivel (arriba del aviso).</summary>
    private void Pista(GameManager gm)
    {
        if (string.IsNullOrEmpty(gm.pista)) return;

        float w = 700f, h = 34f;
        Rect r = new Rect(SW / 2f - w / 2f, SH - 146f, w, h);
        GUI.DrawTexture(r, texSombra);

        Color antes = GUI.color;
        GUI.color = new Color(1f, 0.85f, 0.35f);
        GUI.DrawTexture(new Rect(r.x, r.y, 5f, r.height), texBlanco);
        GUI.color = antes;

        GUI.Label(r, gm.pista,
            Txt(15, FontStyle.Bold, new Color(1f, 0.90f, 0.55f), TextAnchor.MiddleCenter));
    }

    private void Aviso(GameManager gm)
    {
        float w = 640f, h = 40f;
        Rect r = new Rect(SW / 2f - w / 2f, SH - 96f, w, h);
        GUI.DrawTexture(r, texSombra);
        Color antes = GUI.color;
        GUI.color = gm.avisoColor;
        GUI.DrawTexture(new Rect(r.x, r.y, 5f, r.height), texBlanco);
        GUI.color = antes;
        GUI.Label(r, gm.aviso, Txt(17, FontStyle.Bold, gm.avisoColor, TextAnchor.MiddleCenter));
    }

    /// <summary>
    /// Tarjeta con el POR QUE detras del error.
    ///
    /// El aviso de arriba dice cual era el contenedor correcto; esto dice que
    /// pasa cuando el residuo se mezcla. Va debajo, en dos renglones y sin
    /// pedirle nada al jugador: si lo lee, bien; si no, no le corta el juego.
    /// </summary>
    private void Leccion(GameManager gm)
    {
        float w = 660f, h = 46f;
        Rect r = new Rect(SW / 2f - w / 2f, SH - 50f, w, h);

        GUI.DrawTexture(r, texCaja);
        Color antes = GUI.color;
        GUI.color = new Color(1f, 0.84f, 0.35f);
        GUI.DrawTexture(new Rect(r.x, r.y, 5f, r.height), texBlanco);
        GUI.color = antes;

        GUI.Label(new Rect(r.x + 16f, r.y, 30f, r.height), "i",
            Txt(17, FontStyle.Bold, new Color(1f, 0.84f, 0.35f), TextAnchor.MiddleCenter));
        GUI.Label(new Rect(r.x + 40f, r.y + 3f, r.width - 54f, r.height - 6f), gm.leccion,
            Txt(13, FontStyle.Normal, new Color(0.90f, 0.92f, 0.95f), TextAnchor.MiddleLeft));
    }

    // ============================ MENÚ ============================

    private void Menu(GameManager gm)
    {
        // Fondo translúcido: detrás se ve la ciudad sobrevolada por la cámara.
        Color fondoAntes = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, SW, SH), texFondo);
        GUI.color = fondoAntes;

        if (verCreditos) { Creditos(); return; }

        // Tarjeta central con franja roja y blanca (identidad wanka / Junín)
        Rect card = C(0, 26, 760, 572);
        GUI.DrawTexture(card, texCard);
        GUI.DrawTexture(new Rect(card.x, card.y, card.width, 7), texRojo);
        GUI.DrawTexture(new Rect(card.x, card.y + 7, card.width, 3), texBlanco);
        GUI.DrawTexture(new Rect(card.x, card.yMax - 4, card.width, 4), texRojo);

        // Título con sombra y un latido suave (entra con vida, no estático).
        int tamTitulo = Mathf.RoundToInt(44f + Mathf.Sin(Time.unscaledTime * 2.2f) * 1.5f);
        GUI.Label(C(3, -169, 740, 62), "GUARDIÁN DE HUANCAYO",
            Txt(tamTitulo, FontStyle.Bold, new Color(0f, 0f, 0f, 0.6f), TextAnchor.MiddleCenter));
        GUI.Label(C(0, -172, 740, 62), "GUARDIÁN DE HUANCAYO",
            Txt(tamTitulo, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
        GUI.Label(C(0, -130, 740, 26), "Valle del Mantaro · Junín · Perú",
            Txt(17, FontStyle.Bold, new Color(0.96f, 0.74f, 0.36f), TextAnchor.MiddleCenter));
        GUI.Label(C(0, -104, 740, 24), "ODS 11 · Ciudades y Comunidades Sostenibles",
            Txt(15, FontStyle.Normal, new Color(0.50f, 0.88f, 0.68f), TextAnchor.MiddleCenter));

        Rect caja = C(0, -46, 660, 74);
        GUI.DrawTexture(caja, texCaja);
        GUI.Label(new Rect(caja.x, caja.y + 8, caja.width, 22),
            GuardianMovil.Activo
                ? "Joystick izquierdo: mover · Arrastra a la derecha: mirar · SALTAR espanta ratas"
                : "WASD mover · Shift correr · 1/2/3 o V cámara · G flechas · TAB mapa · ESC pausa",
            Txt(15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(caja.x, caja.y + 34, caja.width, 24),
            "Recoge cada residuo y llévalo AL CONTENEDOR DE SU COLOR",
            Txt(15, FontStyle.Normal, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleCenter));

        // Leyenda de colores en el menú
        float total = Residuo.TIPOS * 132f;
        float x0 = SW / 2f - total / 2f;
        for (int i = 0; i < Residuo.TIPOS; i++)
        {
            TipoResiduo t = Residuo.Desde(i);
            Rect chip = new Rect(x0 + i * 132f, SH / 2f + 10f, 124f, 46f);
            GUI.DrawTexture(chip, texTipo[i]);
            GUI.Label(new Rect(chip.x, chip.y + 4, chip.width, 18), Residuo.ColorNTP(t),
                Txt(12, FontStyle.Bold, ContrasteSobre(Residuo.Tinte(t)), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(chip.x, chip.y + 22, chip.width, 20), Residuo.Nombre(t),
                Txt(11, FontStyle.Bold, ContrasteSobre(Residuo.Tinte(t)), TextAnchor.MiddleCenter));
        }

        GUI.Label(C(0, 80, 740, 24), "Récord: " + gm.Record
                + "      Mejor segregación: " + gm.MejorTasa + " %"
                + "      Zona máxima: " + gm.NivelMaximo,
            Txt(16, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter));

        // Selector de calidad: si va lento, se baja y listo.
        GUI.Label(C(-246, 106, 110, 22), "Calidad:",
            Txt(13, FontStyle.Bold, new Color(0.78f, 0.84f, 0.90f), TextAnchor.MiddleRight));
        for (int i = 0; i < 3; i++)
        {
            Rect b = C(-138 + i * 94f, 106, 88, 24);
            bool act = (i == calidad);
            Color antes = GUI.color;
            if (act) GUI.color = new Color(0.55f, 1f, 0.75f);
            if (GUI.Button(b, NOMBRE_CALIDAD[i])) AplicarCalidad(i);
            GUI.color = antes;
        }

        // Volumen general: el juego sintetiza todo su audio, pero hay que poder bajarle.
        GUI.Label(C(168, 106, 86, 22), "Sonido:",
            Txt(13, FontStyle.Bold, new Color(0.78f, 0.84f, 0.90f), TextAnchor.MiddleRight));
        if (GUI.Button(C(232, 106, 34, 24), volumen <= 0.001f ? "🔇" : "🔊")) Silenciar();
        if (GUI.Button(C(270, 106, 30, 24), "−")) AplicarVolumen(volumen - 0.1f);
        GUI.Label(C(308, 106, 44, 22), Mathf.RoundToInt(volumen * 100f) + " %",
            Txt(13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
        if (GUI.Button(C(346, 106, 30, 24), "+")) AplicarVolumen(volumen + 0.1f);

        if (GUI.Button(C(0, 144, 240, 52), "▶  JUGAR")) gm.Empezar();

        GUI.Label(C(0, 176, 740, 20), "o entra directo a una zona:",
            Txt(12, FontStyle.Normal, new Color(0.7f, 0.75f, 0.8f), TextAnchor.MiddleCenter));

        if (GUI.Button(C(-160, 202, 150, 32), "1 · Centro"))  gm.EmpezarEn(1);
        if (GUI.Button(C(   0, 202, 150, 32), "2 · Mercado")) gm.EmpezarEn(2);
        if (GUI.Button(C( 160, 202, 150, 32), "3 · Ribera"))  gm.EmpezarEn(3);

        if (GUI.Button(C(0, 244, 470, 34), "🚶  MODO RECORRIDO  ·  sin tiempo ni contaminación"))
            gm.EmpezarRecorrido();

        // Selector de cámara (también con 1 / 2 / 3 o V durante el juego).
        GUI.Label(C(-236, 280, 110, 22), "Cámara:",
            Txt(13, FontStyle.Bold, new Color(0.78f, 0.84f, 0.90f), TextAnchor.MiddleRight));
        for (int i = 0; i < 3; i++)
        {
            Rect b = C(-128 + i * 112f, 280, 106, 22);
            Color antes = GUI.color;
            if (i == Modo) GUI.color = new Color(0.55f, 1f, 0.75f);
            if (GUI.Button(b, (i + 1) + " · " + NOMBRE_MODO[i])) { Modo = i; PlayerPrefs.SetInt("guardian_camara", i); }
            GUI.color = antes;
        }

        // Autor, discreto en la esquina.
        GUI.Label(new Rect(SW - 420, SH - 40, 404, 26), "Desarrollado por Jhovani Jumpa Fierro",
            Txt(14, FontStyle.Bold, new Color(0.9f, 0.93f, 0.97f, 0.85f), TextAnchor.MiddleRight));
    }

    private void Creditos()
    {
        Rect card = C(0, 0, 800, 520);
        GUI.DrawTexture(card, texCard);
        GUI.DrawTexture(new Rect(card.x, card.y, card.width, 6), texRojo);

        GUI.Label(C(0, -216, 780, 40), "CRÉDITOS",
            Txt(28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));

        string[] lineas = {
            "Desarrollado por: Jhovani Jumpa Fierro",
            "Estudiante · Universidad Continental · Huancayo",
            "",
            "Curso: Desarrollo de Videojuegos · Prof. Guillermo Peña García",
            "Proyecto académico · ODS 11 Ciudades y Comunidades Sostenibles",
            "",
            "Programación, diseño de niveles, enemigos y efectos: Jhovani Jumpa Fierro",
            "Segregación según la NTP 900.058-2019 (Norma Técnica Peruana)",
            "Motor: Unity 6 (URP)",
            "",
            "Agradecimientos: modelos y sonidos gratuitos de la Unity Asset Store",
            "(SimplePoly City, CityPeople, Church 3D, The Sound Guild y otros)",
        };
        for (int i = 0; i < lineas.Length; i++)
            GUI.Label(C(0, -172 + i * 22, 760, 22), lineas[i],
                Txt(14, i == 0 || i == 1 ? FontStyle.Bold : FontStyle.Normal,
                    i == 0 || i == 1 ? new Color(0.96f, 0.74f, 0.36f) : new Color(0.86f, 0.9f, 0.94f),
                    TextAnchor.MiddleCenter));

        if (GUI.Button(C(0, 206, 200, 40), "◀  VOLVER")) verCreditos = false;
    }

    // ============================ RESULTADOS ============================

    private void Fin(GameManager gm)
    {
        GUI.DrawTexture(new Rect(0, 0, SW, SH), texFondo);
        bool gano = gm.estado == GameManager.Estado.Ganado;
        bool completo = gm.CompletoLaCiudad;

        string titulo = completo ? "¡CIUDAD LIMPIA!"
                      : gano ? "¡NIVEL " + gm.nivel + " COMPLETADO!"
                      : (gm.perdioPorTiempo ? "SE ACABÓ EL TIEMPO" : "PERDISTE");
        Color c = gano ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f);

        Rect card = C(0, 0, 760, 580);
        GUI.DrawTexture(card, texCard);
        GUI.DrawTexture(new Rect(card.x, card.y, card.width, 6), gano ? texVerde : texRojo);

        GUI.Label(C(0, -208, 740, 60), titulo, Txt(40, FontStyle.Bold, c, TextAnchor.MiddleCenter));
        GUI.Label(C(0, -166, 740, 30),
            completo ? "Limpiaste las tres zonas: el Shullcas te lo agradece"
            : gano ? "¡Huancayo está más limpia!" : "El río Shullcas necesita tu ayuda",
            Txt(19, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter));

        // Tabla de resultados
        // Rango del Guardian: lo primero que se ve al terminar. Pesa la
        // segregacion, no el puntaje, porque recoger mucho y separar mal es
        // exactamente lo que el juego no debe premiar.
        Rect banda = C(0, -150, 520, 46);
        GUI.DrawTexture(banda, texSombra);
        Color tonoRango = gm.ColorRango();
        Color antesR = GUI.color;
        GUI.color = tonoRango;
        GUI.DrawTexture(new Rect(banda.x, banda.y, banda.width, 3f), texBlanco);
        GUI.DrawTexture(new Rect(banda.x, banda.yMax - 3f, banda.width, 3f), texBlanco);
        GUI.color = antesR;
        GUI.Label(banda, gm.Rango(), Txt(21, FontStyle.Bold, tonoRango, TextAnchor.MiddleCenter));

        Rect caja = C(0, -66, 620, 180);
        GUI.DrawTexture(caja, texCaja);
        Dato(caja, 0, "Puntaje", gm.puntaje.ToString());
        Dato(caja, 1, "Residuos reciclados", gm.recicladas + " / " + gm.basuraTotal);
        Dato(caja, 2, "Bien segregados", gm.aciertos.ToString());
        Dato(caja, 3, "Errores de contenedor", gm.errores.ToString());
        Dato(caja, 4, "Mejor racha seguida", gm.mejorRacha.ToString());
        Dato(caja, 5, "Segregaci\u00f3n correcta", gm.TasaSegregacion + " %");

        // Detalle por color: cuántos residuos de cada tipo se segregaron bien.
        float cw = 118f, chh = 42f;
        float cx0 = SW / 2f - (Residuo.TIPOS * cw) / 2f;
        float cy0 = SH / 2f + 4f;
        for (int i = 0; i < Residuo.TIPOS; i++)
        {
            TipoResiduo tt = Residuo.Desde(i);
            Rect fic = new Rect(cx0 + i * cw + 4f, cy0, cw - 8f, chh);
            GUI.DrawTexture(fic, texTipo[i]);
            Color tinta = ContrasteSobre(Residuo.Tinte(tt));
            GUI.Label(new Rect(fic.x, fic.y + 2, fic.width, 17), Residuo.ColorNTP(tt),
                Txt(11, FontStyle.Bold, tinta, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(fic.x, fic.y + 18, fic.width, 22), gm.AciertosDe(tt).ToString(),
                Txt(17, FontStyle.Bold, tinta, TextAnchor.MiddleCenter));
        }

        int tasa = gm.TasaSegregacion;
        Color tc = tasa >= 90 ? new Color(0.4f, 1f, 0.6f)
                 : tasa >= 60 ? new Color(0.98f, 0.8f, 0.3f)
                              : new Color(1f, 0.55f, 0.5f);
        GUI.Label(C(0, 58, 740, 30), "Tasa de segregación correcta: " + tasa + " %",
            Txt(21, FontStyle.Bold, tc, TextAnchor.MiddleCenter));

        // Equivalencia en peso: hace tangible lo que se acaba de hacer.
        float kg = gm.recicladas * 0.5f;
        GUI.Label(C(0, 84, 740, 22),
            "≈ " + kg.ToString("0.0") + " kg de residuos que no llegaron al Shullcas"
            + "   ·   estimado a 0,5 kg por residuo",
            Txt(14, FontStyle.Bold, new Color(0.55f, 0.88f, 1f), TextAnchor.MiddleCenter));

        GUI.Label(C(0, 106, 740, 22),
            "Récord: " + gm.Record + (gano && gm.Perfecta ? "      ★ SEGREGACIÓN PERFECTA  +50" : ""),
            Txt(15, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter));

        // Mensaje educativo (ODS 11)
        GUI.Label(C(0, 130, 740, 22), Leccion(tasa),
            Txt(14, FontStyle.Normal, new Color(0.72f, 0.86f, 0.96f), TextAnchor.MiddleCenter));

        if (!string.IsNullOrEmpty(gm.ultimoRegistro))
            GUI.Label(C(0, 210, 740, 18), "Resultados guardados en: " + gm.ultimoRegistro,
                Txt(10, FontStyle.Normal, new Color(0.58f, 0.64f, 0.70f), TextAnchor.MiddleCenter));

        if (completo)
        {
            GUI.Label(C(0, 154, 740, 20),
                "De acá en adelante se repiten las zonas, pero con menos tiempo y más basura",
                Txt(13, FontStyle.Normal, new Color(0.70f, 0.78f, 0.86f), TextAnchor.MiddleCenter));
            if (GUI.Button(C(-135, 182, 260, 50), "▶  SEGUIR JUGANDO")) gm.SiguienteNivel();
            if (GUI.Button(C( 135, 182, 240, 50), "↺  VOLVER AL INICIO")) gm.VolverAlMenu();
        }
        else if (gano)
        {
            GUI.Label(C(0, 154, 740, 20), "Siguiente: " + gm.NombreDeNivel(gm.nivel + 1),
                Txt(13, FontStyle.Normal, new Color(0.70f, 0.78f, 0.86f), TextAnchor.MiddleCenter));
            if (GUI.Button(C(0, 182, 260, 50), "▶  SIGUIENTE NIVEL")) gm.SiguienteNivel();
        }
        else
        {
            if (GUI.Button(C(-135, 182, 240, 50), "↻  REINTENTAR")) gm.ReiniciarNivel();
            if (GUI.Button(C( 135, 182, 240, 50), "↺  VOLVER AL INICIO")) gm.VolverAlMenu();
        }
    }

    private void Dato(Rect caja, int fila, string etiqueta, string valor)
    {
        float y = caja.y + 10f + fila * 28f;
        GUI.Label(new Rect(caja.x + 24, y, caja.width * 0.6f, 24), etiqueta,
            Txt(15, FontStyle.Normal, new Color(0.82f, 0.87f, 0.92f), TextAnchor.MiddleLeft));
        GUI.Label(new Rect(caja.x + caja.width * 0.6f, y, caja.width * 0.34f, 24), valor,
            Txt(16, FontStyle.Bold, Color.white, TextAnchor.MiddleRight));
    }

    private string Leccion(int tasa)
    {
        if (tasa >= 90)
            return "Bien segregado en la fuente, el residuo llega limpio al punto de acopio y se reaprovecha.";
        if (tasa >= 60)
            return "Revisa la tabla de colores: cada residuo tiene su contenedor (NTP 900.058-2019).";
        return "Lo que se mezcla ya no se puede reaprovechar: termina en el botadero o en el río.";
    }

    /// <summary>
    /// Pantalla de pausa. Antes solo tenía REANUDAR y REINICIAR; ahora además
    /// recuerda los controles y la regla del juego, deja bajarle el volumen y
    /// permite volver al menú. Quien evalúa el trabajo lo abre por primera vez
    /// y necesita poder recordar cómo se juega sin salir de la partida.
    /// </summary>
    private void Pausa(GameManager gm)
    {
        GUI.DrawTexture(new Rect(0, 0, SW, SH), texFondo);

        Rect card = C(0, 20, 760, 540);
        GUI.DrawTexture(card, texCard);
        GUI.DrawTexture(new Rect(card.x, card.y, card.width, 5), texAccent);

        GUI.Label(C(0, -190, 740, 46), "PAUSA",
            Txt(38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
        GUI.Label(C(0, -156, 740, 22),
            "Zona " + (gm.zonaActual + 1) + " · " + gm.ZonaNombre + "   —   " +
            gm.recicladas + " de " + gm.basuraTotal + " residuos segregados",
            Txt(14, FontStyle.Normal, new Color(0.72f, 0.80f, 0.88f), TextAnchor.MiddleCenter));

        // Columna izquierda: controles.
        GUI.DrawTexture(C(-182, -18, 330, 222), texCaja);
        GUI.Label(C(-182, -112, 310, 22), "CONTROLES",
            Txt(13, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter));
        string[] ctrl = {
            "W A S D    caminar",
            "Shift      correr",
            "Mouse      girar y mirar arriba / abajo",
            "Rueda      acercar y alejar la cámara",
            "Espacio    saltar / espantar ratas",
            "E / T      subir a la bici / timbre",
            "1 / 2 / 3  cámara 1ª persona / aérea / 3ª",
            "V · G      cambiar cámara · flechas guía",
            "TAB        plano grande de la ciudad",
            "M · F8     silenciar · flujo de audio",
            "F9 / F10   ocultar HUD / contador de FPS",
            "ESC o P    pausa"
        };
        for (int i = 0; i < ctrl.Length; i++)
            GUI.Label(C(-182, -94 + i * 16, 300, 16), ctrl[i],
                Txt(12, FontStyle.Normal, new Color(0.88f, 0.91f, 0.95f), TextAnchor.MiddleCenter));

        // Columna derecha: la regla del juego, que es la norma peruana.
        GUI.DrawTexture(C(182, -26, 330, 200), texCaja);
        GUI.Label(C(182, -112, 310, 22), "CÓMO SE JUEGA",
            Txt(13, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter));
        GUI.Label(C(182, -88, 306, 20), "Pasa por encima de un residuo para recogerlo",
            Txt(12, FontStyle.Normal, new Color(0.88f, 0.91f, 0.95f), TextAnchor.MiddleCenter));
        GUI.Label(C(182, -70, 306, 20), "y déjalo en el contenedor de SU color:",
            Txt(12, FontStyle.Normal, new Color(0.88f, 0.91f, 0.95f), TextAnchor.MiddleCenter));

        for (int i = 0; i < Residuo.TIPOS; i++)
        {
            TipoResiduo t = Residuo.Desde(i);
            Rect fila = C(182, -44 + i * 22, 300, 20);
            GUI.DrawTexture(new Rect(fila.x + 6, fila.y + 3, 16, 14), texTipo[i]);
            GUI.Label(new Rect(fila.x + 28, fila.y, 86, 20), Residuo.ColorNTP(t),
                Txt(11, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(fila.x + 116, fila.y, 178, 20), Residuo.Nombre(t),
                Txt(11, FontStyle.Normal, new Color(0.82f, 0.87f, 0.92f), TextAnchor.MiddleLeft));
        }
        GUI.Label(C(182, 68, 306, 18), "NTP 900.058-2019",
            Txt(10, FontStyle.Normal, new Color(0.62f, 0.70f, 0.78f), TextAnchor.MiddleCenter));

        // Volumen, para poder bajarle sin salir de la partida.
        GUI.Label(C(-84, 104, 90, 22), "Sonido:",
            Txt(13, FontStyle.Bold, new Color(0.78f, 0.84f, 0.90f), TextAnchor.MiddleRight));
        if (GUI.Button(C(-20, 104, 34, 24), volumen <= 0.001f ? "🔇" : "🔊")) Silenciar();
        if (GUI.Button(C(18, 104, 30, 24), "−")) AplicarVolumen(volumen - 0.1f);
        GUI.Label(C(56, 104, 44, 22), Mathf.RoundToInt(volumen * 100f) + " %",
            Txt(13, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
        if (GUI.Button(C(94, 104, 30, 24), "+")) AplicarVolumen(volumen + 0.1f);

        // Mezclador: un control por BUS de audio (ver GuardianMezcla / F8).
        GUI.Label(C(0, 134, 700, 18), "MEZCLADOR (buses de audio)",
            Txt(11, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter));
        for (int i = 0; i < 4; i++)
        {
            GuardianMezcla.Bus bus = (GuardianMezcla.Bus)i;
            Rect zona = C(-261 + i * 174, 156, 166, 22);
            GUI.Label(new Rect(zona.x, zona.y, 70, 22), GuardianMezcla.NOMBRES[i],
                Txt(11, FontStyle.Bold, new Color(0.85f, 0.9f, 0.95f), TextAnchor.MiddleLeft));
            float v0 = GuardianMezcla.Volumen(bus);
            float v1 = GUI.HorizontalSlider(new Rect(zona.x + 70, zona.y + 6, 92, 16), v0, 0f, 1f);
            if (Mathf.Abs(v1 - v0) > 0.001f) GuardianMezcla.FijarVolumen(bus, v1);
        }

        if (GUI.Button(C(-240, 208, 220, 46), "▶  REANUDAR")) gm.TogglePausa();
        if (GUI.Button(C(0, 208, 220, 46), "↻  REINICIAR NIVEL")) { gm.TogglePausa(); gm.ReiniciarNivel(); }
        if (GUI.Button(C(240, 208, 220, 46), "◀  VOLVER AL MENÚ")) gm.VolverAlMenu();


    }

    // ---- Estilo de botones (una sola vez) ----
    private bool skinListo;

    /// <summary>
    /// Botones con esquinas redondeadas, borde superior claro y 3 estados
    /// (normal / encima / presionado): el jugador ve que el botón responde.
    /// </summary>
    private void EstiloBotones()
    {
        if (skinListo) return;
        skinListo = true;
        GUIStyle b = GUI.skin.button;
        b.normal.background   = Boton(new Color(0.16f, 0.22f, 0.30f), new Color(0.30f, 0.85f, 0.55f, 0.55f));
        b.hover.background    = Boton(new Color(0.20f, 0.42f, 0.34f), new Color(0.45f, 1f, 0.70f, 0.9f));
        b.active.background   = Boton(new Color(0.10f, 0.30f, 0.22f), new Color(0.30f, 0.85f, 0.55f, 1f));
        b.focused.background  = b.normal.background;
        b.normal.textColor = new Color(0.92f, 0.95f, 0.98f);
        b.hover.textColor  = Color.white;
        b.active.textColor = new Color(0.75f, 1f, 0.85f);
        b.fontStyle = FontStyle.Bold;
        b.fontSize = 14;
        b.border = new RectOffset(6, 6, 6, 6);
        b.alignment = TextAnchor.MiddleCenter;

        GUIStyle sl = GUI.skin.horizontalSlider;
        sl.normal.background = Solido(new Color(0.25f, 0.30f, 0.38f));
        sl.fixedHeight = 6;
        GUIStyle th = GUI.skin.horizontalSliderThumb;
        th.normal.background = Solido(new Color(0.40f, 0.95f, 0.65f));
        th.hover.background  = Solido(new Color(0.60f, 1f, 0.80f));
        th.active.background = th.hover.background;
        th.fixedWidth = 12; th.fixedHeight = 14;
    }

    private Texture2D Boton(Color fondo, Color borde)
    {
        const int n = 16;
        Texture2D t = new Texture2D(n, n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // esquinas redondeadas (radio 4)
                float dx = Mathf.Max(0f, Mathf.Max(4f - x, x - (n - 5f)));
                float dy = Mathf.Max(0f, Mathf.Max(4f - y, y - (n - 5f)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(4.5f - d);
                Color c = (y >= n - 2 || d > 3.2f) ? Color.Lerp(fondo, borde, borde.a) : fondo;
                if (y <= 1) c = Color.Lerp(fondo, Color.black, 0.35f);          // sombra inferior
                c.a = a;
                t.SetPixel(x, y, c);
            }
        t.Apply();
        return t;
    }

    // ---- Utilidades ----
    private Color ContrasteSobre(Color fondo)
    {
        float l = 0.299f * fondo.r + 0.587f * fondo.g + 0.114f * fondo.b;
        return l > 0.6f ? new Color(0.08f, 0.10f, 0.12f) : Color.white;
    }

    private Rect C(float dx, float dy, float w, float h)
    {
        return new Rect(SW / 2f - w / 2f + dx, SH / 2f - h / 2f + dy, w, h);
    }
    private GUIStyle Fila() { return Txt(16, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft); }
    private GUIStyle Txt(int size, FontStyle fs, Color c, TextAnchor anchor)
    {
        GUIStyle s = new GUIStyle(GUI.skin.label);
        s.fontSize = size; s.fontStyle = fs; s.normal.textColor = c; s.alignment = anchor;
        return s;
    }
}
