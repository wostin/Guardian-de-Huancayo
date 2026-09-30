using UnityEngine;

/// <summary>
/// Guardián de Huancayo - La ciudad se ve tan sucia como está.
/// Mientras la basura se acumula, la neblina del valle se vuelve parda y espesa,
/// el sol se apaga y el cielo se ensucia; conforme el Guardián segrega bien, todo
/// se vuelve a aclarar. Es el mensaje del ODS 11 hecho imagen: el aire de Huancayo
/// no es un decorado fijo, depende de lo que se hace con los residuos.
/// Todo se calcula en tiempo real, no usa imágenes ni efectos de terceros.
/// </summary>
public class ClimaContaminacion : MonoBehaviour
{
    [Header("Aire limpio")]
    public Color nieblaLimpia   = new Color(0.74f, 0.82f, 0.90f);
    public Color ambienteLimpio = new Color(0.58f, 0.62f, 0.66f);
    public Color solColorLimpio = new Color(1.00f, 0.97f, 0.90f);
    public float densidadLimpia = 0.0022f;
    public float solLimpio      = 1.25f;

    [Header("Aire contaminado")]
    public Color nieblaSucia   = new Color(0.56f, 0.49f, 0.37f);
    public Color ambienteSucio = new Color(0.46f, 0.42f, 0.35f);
    public Color solColorSucio = new Color(1.00f, 0.84f, 0.60f);
    public float densidadSucia = 0.0082f;
    public float solSucio      = 0.70f;

    [Header("Qué tan rápido cambia (0.35 = se nota pero no marea)")]
    public float velocidad = 0.35f;

    private Light sol;
    private Material cielo;          // COPIA del skybox: no toca el asset del proyecto
    private Material cieloOriginal;

    private float f;                 // 0 = limpio · 1 = contaminado (suavizado)
    private bool guardado;
    private Color fogIni, ambIni, solColIni;
    private float densIni, solIniInt;
    private bool fogIniOn;

    void Start()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sol = l; break; }

        // Guardar el estado original para devolverlo al salir de Play.
        fogIniOn = RenderSettings.fog;
        fogIni   = RenderSettings.fogColor;
        densIni  = RenderSettings.fogDensity;
        ambIni   = RenderSettings.ambientLight;
        if (sol != null) { solColIni = sol.color; solIniInt = sol.intensity; }
        guardado = true;

        cieloOriginal = RenderSettings.skybox;
        if (cieloOriginal != null)
        {
            cielo = new Material(cieloOriginal);
            RenderSettings.skybox = cielo;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        Aplicar(0f);
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;

        float objetivo = 0f;
        if (gm != null && gm.maxContaminacion > 0f && gm.estado == GameManager.Estado.Jugando)
            objetivo = Mathf.Clamp01(gm.contaminacion / gm.maxContaminacion);

        if (Mathf.Abs(f - objetivo) < 0.0005f) return;
        f = Mathf.MoveTowards(f, objetivo, velocidad * Time.deltaTime);
        Aplicar(f);
    }

    private void Aplicar(float k)
    {
        RenderSettings.fogColor   = Color.Lerp(nieblaLimpia, nieblaSucia, k);
        RenderSettings.fogDensity = Mathf.Lerp(densidadLimpia, densidadSucia, k);
        RenderSettings.ambientLight     = Color.Lerp(ambienteLimpio, ambienteSucio, k);
        RenderSettings.ambientIntensity = Mathf.Lerp(1.00f, 0.74f, k);

        if (sol != null)
        {
            sol.intensity = Mathf.Lerp(solLimpio, solSucio, k);
            sol.color     = Color.Lerp(solColorLimpio, solColorSucio, k);
        }

        if (cielo != null)
        {
            if (cielo.HasProperty("_SkyTint"))
                cielo.SetColor("_SkyTint",
                    Color.Lerp(new Color(0.55f, 0.70f, 0.92f), new Color(0.72f, 0.63f, 0.46f), k));
            if (cielo.HasProperty("_GroundColor"))
                cielo.SetColor("_GroundColor",
                    Color.Lerp(new Color(0.42f, 0.47f, 0.33f), new Color(0.38f, 0.34f, 0.26f), k));
            if (cielo.HasProperty("_AtmosphereThickness"))
                cielo.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.85f, 1.85f, k));
            if (cielo.HasProperty("_Exposure"))
                cielo.SetFloat("_Exposure", Mathf.Lerp(1.15f, 0.80f, k));
        }
    }

    void OnDestroy()
    {
        if (!guardado) return;
        RenderSettings.fog        = fogIniOn;
        RenderSettings.fogColor   = fogIni;
        RenderSettings.fogDensity = densIni;
        RenderSettings.ambientLight = ambIni;
        RenderSettings.ambientIntensity = 1f;
        if (sol != null) { sol.color = solColIni; sol.intensity = solIniInt; }
        if (cieloOriginal != null) RenderSettings.skybox = cieloOriginal;
    }
}
