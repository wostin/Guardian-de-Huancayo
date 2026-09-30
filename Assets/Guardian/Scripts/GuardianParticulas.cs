using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Biblioteca de partículas generadas por código.
/// Texturas (círculo suave, anillo, estrella, cuadrito de confeti) y materiales
/// URP transparentes/aditivos se crean UNA vez y se reutilizan. Ningún asset
/// externo: todo sale de aquí.
/// </summary>
public static class GuardianParticulas
{
    public enum Tex { Suave, Anillo, Estrella, Cuadro }

    private static Texture2D[] texs = new Texture2D[4];
    private static Material[] matAlpha = new Material[4];
    private static Material[] matAditivo = new Material[4];

    // ------------------------------------------------------------------ texturas

    public static Texture2D Textura(Tex t)
    {
        int i = (int)t;
        if (texs[i] != null) return texs[i];

        int n = 64;
        Texture2D tx = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tx.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a;
                switch (t)
                {
                    case Tex.Anillo:
                        a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.78f) / 0.14f);
                        a *= a;
                        break;
                    case Tex.Estrella:
                        float cruz = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(u) * 9f) * Mathf.Clamp01(1f - Mathf.Abs(v)),
                                               Mathf.Clamp01(1f - Mathf.Abs(v) * 9f) * Mathf.Clamp01(1f - Mathf.Abs(u)));
                        a = Mathf.Clamp01(cruz + Mathf.Clamp01(1f - r * 2.4f));
                        break;
                    case Tex.Cuadro:
                        a = (Mathf.Abs(u) < 0.8f && Mathf.Abs(v) < 0.5f) ? 1f : 0f;
                        break;
                    default:
                        a = Mathf.Clamp01(1f - r);
                        a = a * a * (3f - 2f * a);
                        break;
                }
                tx.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tx.Apply();
        texs[i] = tx;
        return tx;
    }

    // ------------------------------------------------------------------ materiales

    public static Material Material(Tex t, bool aditivo)
    {
        Material[] cache = aditivo ? matAditivo : matAlpha;
        int i = (int)t;
        if (cache[i] != null) return cache[i];

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool urp = sh != null;
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material m = new Material(sh);
        m.name = "VFX_" + t + (aditivo ? "_Add" : "_Alpha");

        if (urp)
        {
            m.SetTexture("_BaseMap", Textura(t));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", aditivo ? 2f : 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", aditivo ? (int)UnityEngine.Rendering.BlendMode.One
                                          : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (aditivo) m.EnableKeyword("_BLENDMODE_ADD");
            m.renderQueue = 3000;
        }
        else m.mainTexture = Textura(t);

        cache[i] = m;
        return m;
    }

    // ------------------------------------------------------------------ ráfaga genérica

    /// <summary>
    /// Ráfaga única de partículas que se destruye sola.
    /// </summary>
    public static ParticleSystem Rafaga(Vector3 pos, Color c1, Color c2, int cantidad,
        float velocidad, float vida, float tamano, float gravedad,
        Tex tex = Tex.Suave, bool aditivo = true,
        ParticleSystemShapeType forma = ParticleSystemShapeType.Sphere, float radio = 0.2f,
        bool giran = false, float encoger = 1f)
    {
        GameObject go = new GameObject("VFX_Rafaga");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vida * 0.6f, vida);
        main.startSpeed = new ParticleSystem.MinMaxCurve(velocidad * 0.45f, velocidad);
        main.startSize = new ParticleSystem.MinMaxCurve(tamano * 0.6f, tamano);
        main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
        main.gravityModifier = gravedad;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(cantidad, 8);
        if (giran) main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });

        var sh = ps.shape;
        sh.shapeType = forma;
        sh.radius = radio;
        if (forma == ParticleSystemShapeType.Cone) { sh.angle = 30f; sh.rotation = new Vector3(-90f, 0f, 0f); }

        // Se desvanecen al final (alpha) y se achican: nunca "desaparecen de golpe".
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, Mathf.Max(0.001f, 1f - encoger))));

        if (giran)
        {
            var rol = ps.rotationOverLifetime;
            rol.enabled = true;
            rol.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        }

        // Frenado por aire: la explosión se abre rápido y luego flota.
        var lim = ps.limitVelocityOverLifetime;
        lim.enabled = true;
        lim.dampen = 0.12f;
        lim.limit = velocidad * 0.35f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Material(tex, aditivo);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        ps.Play();
        Object.Destroy(go, vida + 0.6f);
        return ps;
    }

    /// <summary>Onda expansiva plana sobre el suelo (anillo que crece y se apaga).</summary>
    public static void Anillo(Vector3 pos, Color c, float tamanoFinal = 4f, float vida = 0.55f)
    {
        GameObject go = new GameObject("VFX_Anillo");
        go.transform.position = pos + Vector3.up * 0.08f;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.1f; main.loop = false; main.playOnAwake = false;
        main.startLifetime = vida;
        main.startSpeed = 0f;
        main.startSize = tamanoFinal;
        main.startColor = c;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        var sh = ps.shape; sh.enabled = false;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.1f, 0f, 3f), new Keyframe(1f, 1f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        r.sharedMaterial = Material(Tex.Anillo, true);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        ps.Play();
        Object.Destroy(go, vida + 0.3f);
    }

    /// <summary>Confeti de colores que cae girando (acierto).</summary>
    public static void Confeti(Vector3 pos, Color principal, int cantidad)
    {
        Color claro = Color.Lerp(principal, Color.white, 0.55f);
        Rafaga(pos, principal, claro, cantidad, 7.5f, 1.6f, 0.22f, 1.1f,
               Tex.Cuadro, false, ParticleSystemShapeType.Cone, 0.3f, true, 0.3f);
        Rafaga(pos, new Color(1f, 0.92f, 0.45f), Color.white, cantidad / 3, 6f, 1.3f, 0.18f, 0.9f,
               Tex.Cuadro, false, ParticleSystemShapeType.Cone, 0.3f, true, 0.3f);
    }

    /// <summary>Nube de humo (error, golpe, perder).</summary>
    public static void Humo(Vector3 pos, Color c, int cantidad = 18, float tam = 1.2f)
    {
        Rafaga(pos, c, new Color(c.r * 0.7f, c.g * 0.7f, c.b * 0.7f, c.a), cantidad, 1.6f, 1.2f, tam, -0.08f,
               Tex.Suave, false, ParticleSystemShapeType.Sphere, 0.4f, false, -0.6f);
    }

    /// <summary>Fuego artificial: estallido esférico con chispas que caen.</summary>
    public static void FuegoArtificial(Vector3 pos, Color c)
    {
        Rafaga(pos, c, Color.white, 60, 9f, 1.4f, 0.35f, 0.45f, Tex.Estrella, true,
               ParticleSystemShapeType.Sphere, 0.1f, false, 0.9f);
        Rafaga(pos, Color.white, c, 1, 0f, 0.25f, 5f, 0f, Tex.Suave, true);   // destello central
    }
}
