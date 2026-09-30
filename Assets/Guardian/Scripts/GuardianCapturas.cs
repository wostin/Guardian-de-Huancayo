using System.Collections;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Guardián de Huancayo - Capturas para el informe / GDD.
///
/// F12 = una captura de lo que se ve.
/// F7  = SESIÓN DE FOTOS automática: recorre las 3 cámaras, dispara cada VFX de
///       retroalimentación, abre la pausa (mezclador), el panel de audio (F8) y
///       el menú, y guarda una imagen de cada uno.
/// Las imágenes van a la carpeta "Capturas_Juego" del proyecto (fuera de Assets,
/// así Unity no las importa ni pesan en el .exe).
/// </summary>
public class GuardianCapturas : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (FindObjectOfType<GuardianCapturas>() != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianCapturas").AddComponent<GuardianCapturas>();
    }

    public static bool EnSesion { get; private set; }

    private static string Carpeta
    {
        get
        {
            string c = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Capturas_Juego"));
            if (!Directory.Exists(c)) Directory.CreateDirectory(c);
            return c;
        }
    }

    public static void Foto(string nombre)
    {
        int s = Screen.width < 1600 ? 2 : 1;
        string ruta = Path.Combine(Carpeta, nombre + ".png");
        ScreenCapture.CaptureScreenshot(ruta, s);
        Debug.Log("[Guardián] Captura guardada: " + ruta);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;
        if (k.f12Key.wasPressedThisFrame) Foto("captura_" + System.DateTime.Now.ToString("HHmmss"));
        if (k.f7Key.wasPressedThisFrame && !EnSesion) StartCoroutine(Sesion());
        if (k.f6Key.wasPressedThisFrame && !EnSesion) Fichas();
#else
        if (Input.GetKeyDown(KeyCode.F12)) Foto("captura_" + System.DateTime.Now.ToString("HHmmss"));
        if (Input.GetKeyDown(KeyCode.F7) && !EnSesion) StartCoroutine(Sesion());
        if (Input.GetKeyDown(KeyCode.F6) && !EnSesion) Fichas();
#endif
    }

    // ------------------------------------------------------------ F6: fichas de personajes

    /// <summary>
    /// Retratos de cada tipo de personaje / enemigo / objeto (bestiario del GDD).
    /// Cámara temporal en vista 3/4 que encuadra al objeto por sus bounds.
    /// Los enemigos de otras zonas (ocultos) se muestran solo para la foto.
    /// </summary>
    private void Fichas()
    {
        var hechos = new System.Collections.Generic.Dictionary<string, int>();
        int n = 0;
        GameObject pj = GameObject.FindGameObjectWithTag("Player");
        if (pj != null) { Retrato(pj, "ficha_guardian"); n++; }

        string[] tipos = { "Contaminante", "Ensuciador", "Aliado", "CamionRecolector", "Peaton", "Bicicleta", "EnemigoRata", "NubeToxica", "JefeBasuron" };
        int[] maximo   = { 6, 2, 1, 1, 2, 1, 1, 1, 1 };
        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string t = mb.GetType().Name;
            int i = System.Array.IndexOf(tipos, t);
            if (i < 0) continue;
            int ya; hechos.TryGetValue(t, out ya);
            if (ya >= maximo[i]) continue;
            // Contaminantes: uno por modelo distinto (auto, mototaxi, camión…).
            if (t == "Contaminante")
            {
                string modelo = mb.gameObject.name.Split(' ', '(', '_')[0];
                if (hechos.ContainsKey("m_" + modelo)) continue;
                hechos["m_" + modelo] = 1;
            }
            hechos[t] = ya + 1;
            Retrato(mb.gameObject, "ficha_" + t.ToLower() + "_" + (ya + 1) + "_" + Limpio(mb.gameObject.name));
            n++;
        }

        // Un residuo y un contenedor de cada color NTP.
        var tiposR = new System.Collections.Generic.HashSet<TipoResiduo>();
        foreach (TrashItem t in FindObjectsByType<TrashItem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (tiposR.Add(t.tipo)) { Retrato(t.gameObject, "ficha_residuo_" + Residuo.Nombre(t.tipo).ToLower(), 0.9f); n++; }
        var tiposB = new System.Collections.Generic.HashSet<TipoResiduo>();
        foreach (RecycleBin b in FindObjectsByType<RecycleBin>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (tiposB.Add(b.acepta)) { Retrato(b.gameObject, "ficha_contenedor_" + Residuo.ColorNTP(b.acepta).ToLower(), 1.1f); n++; }

        // Tomas amplias de los enemigos en su entorno (para el bestiario).
        GuardianEnemigos ge = GuardianEnemigos.Instancia;
        if (ge != null && ge.Jefe != null) { Retrato(ge.Jefe.gameObject, "escena_jefe_amplia", 2.6f); n++; }
        NubeToxica nube = FindObjectOfType<NubeToxica>();
        if (nube != null) { Retrato(nube.gameObject, "escena_humo_amplia", 2.2f); n++; }
        EnemigoRata rata = FindObjectOfType<EnemigoRata>();
        if (rata != null) { Retrato(rata.gameObject, "escena_rata_amplia", 3.5f); n++; }

        Debug.Log("[Guardián] " + n + " fichas guardadas en: " + Carpeta);
    }

    private static string Limpio(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in s) if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.Length > 24 ? sb.ToString(0, 24) : sb.ToString();
    }

    private static void Retrato(GameObject go, string nombre, float margen = 1.25f)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return;
        bool[] estaba = new bool[rs.Length];
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        bool hay = false;
        for (int i = 0; i < rs.Length; i++)
        {
            estaba[i] = rs[i].enabled;
            if (rs[i] is ParticleSystemRenderer) continue;
            rs[i].enabled = true;
            if (!hay) { b = rs[i].bounds; hay = true; } else b.Encapsulate(rs[i].bounds);
        }

        GameObject cgo = new GameObject("CamaraFicha");
        Camera cam = cgo.AddComponent<Camera>();
        cam.fieldOfView = 32f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 900f;
        float radio = Mathf.Max(0.4f, b.extents.magnitude) * margen;
        float dist = radio / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 dir = (go.transform.forward * 0.85f + go.transform.right * 0.6f + Vector3.up * 0.42f).normalized;
        cgo.transform.position = b.center + dir * dist;
        cgo.transform.LookAt(b.center);

        const int w = 1280, h = 960;
        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture antes = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = antes;
        cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(Carpeta, nombre + ".png"), tex.EncodeToPNG());
        Object.Destroy(tex); Object.Destroy(rt); Object.Destroy(cgo);

        for (int i = 0; i < rs.Length; i++) rs[i].enabled = estaba[i];
    }

    private IEnumerator Sesion()
    {
        GameManager gm = GameManager.Instance;
        GameObject pj = GameObject.FindGameObjectWithTag("Player");
        if (gm == null || pj == null) yield break;
        EnSesion = true;
        Transform j = pj.transform;

        if (gm.estado != GameManager.Estado.Jugando) { gm.Empezar(); yield return new WaitForSecondsRealtime(2.5f); }

        // --- Cámaras ---
        GuardianCameraHUD.Modo = 2; yield return new WaitForSecondsRealtime(2.2f);
        Foto("01_camara_tercera_persona"); yield return new WaitForSecondsRealtime(0.4f);
        GuardianCameraHUD.Modo = 1; yield return new WaitForSecondsRealtime(2.6f);
        Foto("02_camara_aerea_flechas"); yield return new WaitForSecondsRealtime(0.4f);
        GuardianCameraHUD.Modo = 0; yield return new WaitForSecondsRealtime(2.0f);
        Foto("03_camara_primera_persona"); yield return new WaitForSecondsRealtime(0.4f);
        GuardianCameraHUD.Modo = 2; yield return new WaitForSecondsRealtime(2.2f);

        // --- VFX de retroalimentación ---
        Vector3 frente = j.position + j.forward * 2.5f;
        GuardianEventos.AvisarRecogio(frente, TipoResiduo.Plastico);
        yield return new WaitForSecondsRealtime(0.22f);
        Foto("04_vfx_recoger_residuo"); yield return new WaitForSecondsRealtime(1.3f);

        Vector3 pb = frente + j.right * 1.5f;
        GuardianEventos.AvisarDeposito(pb, TipoResiduo.Vidrio, 2, 60);
        yield return new WaitForSecondsRealtime(0.35f);
        Foto("05_vfx_acierto_confeti"); yield return new WaitForSecondsRealtime(1.6f);

        GuardianEventos.AvisarError(pb, TipoResiduo.Organico);
        yield return new WaitForSecondsRealtime(0.3f);
        Foto("06_vfx_error_contenedor"); yield return new WaitForSecondsRealtime(1.6f);

        GuardianEventos.AvisarGolpe(j.position - j.forward * 2f);
        yield return new WaitForSecondsRealtime(0.18f);
        Foto("07_vfx_golpe_de_carro"); yield return new WaitForSecondsRealtime(2f);

        GuardianCameraHUD.Modo = 1; yield return new WaitForSecondsRealtime(2.2f);
        GuardianEventos.AvisarFin(true);
        yield return new WaitForSecondsRealtime(1.5f);
        Foto("08_vfx_victoria_fuegos"); yield return new WaitForSecondsRealtime(2.5f);
        GuardianCameraHUD.Modo = 2; yield return new WaitForSecondsRealtime(1.5f);

        // --- Audio: panel del flujo de señal ---
        GuardianMezcla.MostrarFlujo(true);
        yield return new WaitForSecondsRealtime(1.2f);
        Foto("09_flujo_de_audio_F8"); yield return new WaitForSecondsRealtime(0.4f);
        GuardianMezcla.MostrarFlujo(false);

        // --- Pausa con mezclador ---
        gm.TogglePausa();
        yield return new WaitForSecondsRealtime(0.8f);
        Foto("10_pausa_mezclador"); yield return new WaitForSecondsRealtime(0.4f);
        gm.TogglePausa();

        // --- Menú con vuelo de cámara ---
        gm.VolverAlMenu();
        yield return new WaitForSecondsRealtime(4f);
        Foto("11_menu_principal");
        yield return new WaitForSecondsRealtime(0.5f);

        Debug.Log("[Guardián] Sesión de fotos lista en: " + Carpeta);
        EnSesion = false;
    }
}
