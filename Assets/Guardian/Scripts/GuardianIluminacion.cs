using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Guardián de Huancayo - ILUMINACIÓN, SOMBRAS Y POST-PROCESO (URP).
///
/// ILUMINACIÓN (esquema de 3 luces adaptado a exteriores):
///  1. Luz clave  = el SOL (Directional). Cálida, de tarde, SOMBRAS SUAVES (Soft).
///  2. Luz de relleno = "cielo" (Directional sin sombras, azulada, 25%) desde el lado
///     opuesto: aclara las caras en sombra para que el personaje nunca quede negro.
///  3. Luz ambiente = la maneja ClimaContaminacion (cambia con la contaminación).
///
/// SOMBRAS: sombras suaves del sol, 4 cascadas, distancia según la calidad elegida
/// (Bajo 35 m sin cascadas · Medio 70 m · Alto 120 m), bias ajustado para que no
/// aparezca "acné" ni sombras despegadas (peter-panning). Todos los personajes,
/// carros, contenedores y residuos proyectan sombra y la reciben.
///
/// POST-PROCESO (Volume global): Tonemapping ACES, Bloom (brillo de las flechas y
/// las chispas), ajuste de color y viñeta. La saturación BAJA cuando sube la
/// contaminación: la ciudad se ve gris y apagada si no reciclas.
/// La viñeta se usa también como feedback: destello rojo al recibir daño o al
/// equivocarse, verde al acertar.
/// </summary>
public class GuardianIluminacion : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (FindObjectOfType<GuardianIluminacion>() != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianIluminacion").AddComponent<GuardianIluminacion>();
    }

    private static GuardianIluminacion inst;

    private Light sol, relleno;
    private Volume volumen;
    private VolumeProfile perfil;
    private ColorAdjustments color;
    private Vignette vineta;
    private Bloom bloom;

    private UniversalRenderPipelineAsset urp;
    private float urpSombraIni; private int urpCascadasIni; private bool urpGuardado;

    private Color destelloColor = Color.black;
    private float destello;          // 0..1, decae

    void Awake()
    {
        inst = this;
    }

    void Start()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.name != "Luz_Relleno_Cielo") { sol = l; break; }

        if (sol != null)
        {
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.82f;
            sol.shadowBias = 0.06f;
            sol.shadowNormalBias = 0.45f;
            RenderSettings.sun = sol;

            // Luz de relleno: opuesta al sol, sin sombras, color de cielo.
            GameObject go = new GameObject("Luz_Relleno_Cielo");
            relleno = go.AddComponent<Light>();
            relleno.type = LightType.Directional;
            relleno.shadows = LightShadows.None;
            relleno.color = new Color(0.62f, 0.74f, 1f);
            relleno.intensity = 0.28f;
            Vector3 e = sol.transform.eulerAngles;
            go.transform.rotation = Quaternion.Euler(35f, e.y + 180f, 0f);
        }

        // Que todo lo que se mueve proyecte y reciba sombra.
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r is ParticleSystemRenderer) continue;
            if (r.GetComponentInParent<TrashItem>() != null || r.GetComponentInParent<RecycleBin>() != null
                || r.GetComponentInParent<Contaminante>() != null || r.GetComponentInParent<PlayerController>() != null)
            {
                if (r.shadowCastingMode == ShadowCastingMode.Off) r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }

        urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urpSombraIni = urp.shadowDistance;
            urpCascadasIni = urp.shadowCascadeCount;
            urpGuardado = true;
        }
        AplicarCalidad(PlayerPrefs.GetInt("guardian_calidad", Application.isMobilePlatform ? 0 : 2));

        CrearPostProceso();
    }

    /// <summary>Calidad de sombras y post-proceso (lo llama el selector de Calidad del menú).</summary>
    public static void AplicarCalidad(int nivel)
    {
        if (inst == null) return;
        inst.Calidad(nivel);
    }

    private void Calidad(int nivel)
    {
        float dist = nivel == 0 ? 35f : (nivel == 1 ? 70f : 120f);
        int casc = nivel == 0 ? 1 : (nivel == 1 ? 2 : 4);
        if (urp != null)
        {
            urp.shadowDistance = dist;
            urp.shadowCascadeCount = casc;
        }
        if (sol != null) sol.shadows = nivel == 0 ? LightShadows.Hard : LightShadows.Soft;
        if (bloom != null) bloom.active = nivel > 0;

        Camera c = Camera.main;
        if (c != null)
        {
            var datos = c.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = true;
            datos.renderShadows = true;
            datos.antialiasing = nivel == 2 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing
                               : (nivel == 1 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.None);
        }
    }

    private void CrearPostProceso()
    {
        GameObject go = new GameObject("PostProceso_Global");
        volumen = go.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 10f;
        perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        volumen.profile = perfil;

        Tonemapping tm = perfil.Add<Tonemapping>(true);
        tm.mode.Override(TonemappingMode.ACES);

        bloom = perfil.Add<Bloom>(true);
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.55f);
        bloom.scatter.Override(0.6f);
        bloom.active = PlayerPrefs.GetInt("guardian_calidad", Application.isMobilePlatform ? 0 : 2) > 0;

        color = perfil.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.25f);
        color.contrast.Override(12f);
        color.saturation.Override(12f);

        vineta = perfil.Add<Vignette>(true);
        vineta.intensity.Override(0.22f);
        vineta.smoothness.Override(0.45f);
        vineta.color.Override(Color.black);

        Camera c = Camera.main;
        if (c != null) c.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    /// <summary>Destello de color en los bordes de la pantalla (feedback).</summary>
    public static void Destello(Color c, float fuerza)
    {
        if (inst == null) return;
        if (fuerza >= inst.destello) { inst.destelloColor = c; inst.destello = Mathf.Clamp01(fuerza); }
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;

        // Contaminación → la ciudad pierde color.
        float k = 0f;
        if (gm != null && gm.maxContaminacion > 0f && gm.estado == GameManager.Estado.Jugando)
            k = Mathf.Clamp01(gm.contaminacion / gm.maxContaminacion);
        if (color != null)
        {
            float objetivo = Mathf.Lerp(14f, -38f, k);
            color.saturation.value = Mathf.Lerp(color.saturation.value, objetivo, Time.deltaTime * 1.5f);
        }

        // Viñeta: normal negra; con destello se tiñe y se cierra un momento.
        destello = Mathf.MoveTowards(destello, 0f, Time.unscaledDeltaTime * 1.6f);
        if (vineta != null)
        {
            float peligro = gm != null && gm.estado == GameManager.Estado.Jugando && gm.vidas == 1 ? 0.12f + 0.06f * Mathf.Sin(Time.time * 4f) : 0f;
            vineta.intensity.value = 0.22f + destello * 0.33f + peligro;
            Color baseC = peligro > 0f ? new Color(0.35f, 0f, 0f) : Color.black;
            vineta.color.value = Color.Lerp(baseC, destelloColor, destello);
        }
    }

    void OnDestroy()
    {
        // El asset de URP es del proyecto: se devuelve como estaba al salir de Play.
        if (urpGuardado && urp != null)
        {
            urp.shadowDistance = urpSombraIni;
            urp.shadowCascadeCount = urpCascadasIni;
        }
        if (perfil != null) Destroy(perfil);
    }
}
