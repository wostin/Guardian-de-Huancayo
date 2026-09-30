using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Agua del río Shullcas.
/// Desplaza la textura del material para que el agua se vea corriendo,
/// y mueve el plano muy suavemente para simular el oleaje.
/// </summary>
public class AguaRio : MonoBehaviour
{
    public Vector2 velocidad = new Vector2(0.02f, 0.12f);
    public float oleaje = 0.05f;

    [Header("El río refleja la contaminación de la ciudad (ODS 11)")]
    /// <summary>
    /// Azul verdoso de río de sierra, no celeste de piscina. El color anterior
    /// era (0.55, 0.80, 1.00) con brillo 0.92: tan claro y tan reflectante que
    /// con el cielo del valle y la neblina se lavaba a blanco, y desde arriba el
    /// Shullcas parecía una pista de concreto cruzando la ciudad. Un río se lee
    /// como río por ser MÁS OSCURO que lo que tiene alrededor, no más claro.
    /// </summary>
    public Color aguaLimpia = new Color(0.16f, 0.45f, 0.58f);
    public Color aguaSucia  = new Color(0.34f, 0.29f, 0.17f);

    private Material mat;
    private float yBase;

    void Start()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null) mat = r.material;   // instancia propia, no toca el asset
        yBase = transform.position.y;
    }

    void Update()
    {
        if (mat != null)
        {
            Vector2 off = new Vector2(Time.time * velocidad.x, Time.time * velocidad.y);
            if (mat.HasProperty("_BaseMap")) mat.SetTextureOffset("_BaseMap", off);
            if (mat.HasProperty("_MainTex")) mat.SetTextureOffset("_MainTex", off);

            // Mientras más basura quede tirada, más sucio baja el Shullcas.
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.maxContaminacion > 0f)
            {
                float f = Mathf.Clamp01(gm.contaminacion / gm.maxContaminacion);
                Color c = Color.Lerp(aguaLimpia, aguaSucia, f);
                mat.color = c;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", Mathf.Lerp(0.55f, 0.12f, f));
            }
        }

        if (oleaje > 0f)
        {
            Vector3 p = transform.position;
            p.y = yBase + Mathf.Sin(Time.time * 1.3f) * oleaje;
            transform.position = p;
        }
    }
}
