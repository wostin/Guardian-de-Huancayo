using UnityEngine;

/// <summary>
/// Guardián de Huancayo - FLECHAS GUÍA 3D.
///
/// 1. FLECHA BRÚJULA (alrededor del Guardián, a la altura de la cintura):
///    - Con la mochila vacía apunta al RESIDUO más cercano (color del residuo).
///    - Si llevas residuos apunta al CONTENEDOR del color que llevas.
///    - Si la zona ya está limpia apunta al PUNTO DE ACOPIO (verde).
///    - Si llevas algo y aún cabe más, aparece una SEGUNDA flecha más chica que
///      sigue señalando la basura: así se ve "dónde está la basura y dónde botarla".
/// 2. FLECHA BALIZA sobre el objetivo: flota, gira y rebota encima del
///    contenedor / residuo / acopio, con su color, visible de lejos.
/// Las flechas brillan (Bloom) y se achican al llegar, para no tapar la vista.
/// Tecla G: mostrar / ocultar las flechas.
/// </summary>
public class GuardianFlechas : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (FindObjectOfType<GuardianFlechas>() != null) return;
        if (GameObject.FindGameObjectWithTag("Player") == null) return;
        new GameObject("GuardianFlechas").AddComponent<GuardianFlechas>();
    }

    public static bool Visibles = true;

    private Transform jugador;
    private Transform brujula, brujula2, baliza, baliza2;
    private Material matB, matB2, matBal, matBal2;

    private RecycleBin[] botes;
    private TrashItem[] residuos;
    private PuntoAcopioZona[] acopios;
    private float refresco;

    private static Mesh mallaFlecha;
    private float escBrujula = 1.7f, escBrujula2 = 1.1f;

    void Start()
    {
        GameObject pj = GameObject.FindGameObjectWithTag("Player");
        if (pj != null) jugador = pj.transform;

        brujula  = Crear("Flecha_Guia",        out matB,   1.7f);
        brujula2 = Crear("Flecha_Guia_Basura", out matB2,  1.1f);
        baliza   = Crear("Flecha_Baliza",      out matBal, 2.4f);
        baliza2  = Crear("Flecha_Baliza_Basura", out matBal2, 1.6f);
        escBrujula = brujula.localScale.x; escBrujula2 = brujula2.localScale.x;
    }

    private Transform Crear(string nombre, out Material m, float escala)
    {
        GameObject go = new GameObject(nombre);
        go.AddComponent<MeshFilter>().sharedMesh = Malla();
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(sh);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);   // se ve por ambos lados
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        go.transform.localScale = Vector3.one * escala;

        // Borde oscuro detrás de la flecha: se lee sobre la vereda clara y sobre
        // la pista oscura por igual (contraste), igual que un ícono con contorno.
        GameObject borde = new GameObject("Borde");
        borde.transform.SetParent(go.transform, false);
        borde.transform.localScale = new Vector3(1.22f, 0.6f, 1.16f);
        borde.transform.localPosition = new Vector3(0f, -0.03f, -0.02f);
        borde.AddComponent<MeshFilter>().sharedMesh = Malla();
        MeshRenderer rb = borde.AddComponent<MeshRenderer>();
        Material mb = new Material(sh);
        if (mb.HasProperty("_Cull")) mb.SetFloat("_Cull", 0f);
        Color oscuro = new Color(0.04f, 0.05f, 0.07f, 1f);
        if (mb.HasProperty("_BaseColor")) mb.SetColor("_BaseColor", oscuro);
        mb.color = oscuro;
        rb.sharedMaterial = mb;
        rb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.SetActive(false);
        return go.transform;
    }

    private static void Pintar(Material m, Color c, float brillo)
    {
        Color hdr = new Color(c.r * brillo, c.g * brillo, c.b * brillo, 1f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
        m.color = hdr;
    }

    void Update()
    {
        if (TeclaG()) Visibles = !Visibles;

        GameManager gm = GameManager.Instance;
        bool jugando = gm != null && gm.estado == GameManager.Estado.Jugando && !gm.pausado;
        if (jugador == null || !jugando || !Visibles)
        {
            Apagar(); return;
        }

        if (Time.unscaledTime > refresco)
        {
            refresco = Time.unscaledTime + 0.75f;
            botes    = FindObjectsByType<RecycleBin>(FindObjectsSortMode.None);
            residuos = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
            acopios  = FindObjectsByType<PuntoAcopioZona>(FindObjectsSortMode.None);
        }

        Vector3 yo = jugador.position;

        // --- Objetivo principal ---
        Transform objetivo = null; Color col = Color.white; float altoBaliza = 3.6f;
        Transform basura = MasCercanoResiduo(yo, gm);

        if (gm.esperandoAcopio)
        {
            objetivo = Acopio(gm);
            col = new Color(0.30f, 1f, 0.6f); altoBaliza = 5f;
        }
        else if (gm.cargaActual > 0)
        {
            RecycleBin b = MasCercanoBote(yo, gm);
            if (b != null) { objetivo = b.transform; col = Residuo.Tinte(b.acepta); altoBaliza = 3.6f; }
        }
        else if (basura != null)
        {
            objetivo = basura;
            TrashItem ti = basura.GetComponent<TrashItem>();
            col = ti != null ? Residuo.Tinte(ti.tipo) : Color.white;
            altoBaliza = 2.2f;
        }

        Poner(brujula, baliza, matB, matBal, objetivo, col, altoBaliza, 1.7f, 1f);

        // --- Segunda flecha: sigue marcando basura mientras aún cabe en la mochila ---
        bool segunda = !gm.esperandoAcopio && gm.cargaActual > 0 && gm.cargaActual < gm.capacidadCarga
                       && basura != null && basura != objetivo;
        if (segunda)
        {
            TrashItem ti = basura.GetComponent<TrashItem>();
            Color c2 = ti != null ? Residuo.Tinte(ti.tipo) : Color.white;
            Poner(brujula2, baliza2, matB2, matBal2, basura, c2, 2.2f, 1.25f, 0.55f);
        }
        else { brujula2.gameObject.SetActive(false); baliza2.gameObject.SetActive(false); }
    }

    private void Poner(Transform flecha, Transform bal, Material mf, Material mb, Transform obj,
                       Color c, float alto, float radio, float alturaRel)
    {
        if (obj == null) { flecha.gameObject.SetActive(false); bal.gameObject.SetActive(false); return; }

        Vector3 yo = jugador.position;
        Vector3 d = obj.position - yo; d.y = 0f;
        float dist = d.magnitude;
        bool cerca = dist < 2.8f;

        // Brújula: gira alrededor del Guardián y apunta al objetivo.
        flecha.gameObject.SetActive(!cerca);
        if (!cerca)
        {
            Vector3 dir = d / Mathf.Max(0.001f, dist);
            bool fp = GuardianCameraHUD.PrimeraPersona;
            bool aerea = GuardianCameraHUD.Modo == 1;
            float escF = (flecha == brujula ? escBrujula : escBrujula2) * (aerea ? 1.7f : (fp ? 0.55f : 0.8f));
            flecha.localScale = Vector3.one * escF;
            float r = fp ? radio + 2.2f : radio * (aerea ? 1.6f : 1.05f);
            float h = fp ? 0.95f : 0.55f + alturaRel * 0.35f;
            Vector3 p = yo + dir * r + Vector3.up * (h + Mathf.Sin(Time.time * 4f) * 0.06f);
            flecha.position = Vector3.Lerp(flecha.position, p, flecha.gameObject.activeSelf ? 20f * Time.deltaTime : 1f);
            flecha.rotation = Quaternion.Slerp(flecha.rotation, Quaternion.LookRotation(dir, Vector3.up), 14f * Time.deltaTime);
            Pintar(mf, c, 1.6f + 0.5f * Mathf.Sin(Time.time * 6f));
        }

        // Baliza: flecha hacia abajo sobre el objetivo, rebotando y girando.
        bal.gameObject.SetActive(true);
        float rebote = Mathf.Abs(Mathf.Sin(Time.time * 3.2f)) * 0.6f;
        bal.position = obj.position + Vector3.up * (alto + rebote);
        bal.rotation = Quaternion.Euler(90f, Time.time * 120f, 0f);        // punta hacia abajo
        float esc = Mathf.Lerp(1.0f, 1.8f, Mathf.InverseLerp(10f, 60f, dist)); // de lejos más grande
        bal.localScale = Vector3.one * esc * 2.2f * (alturaRel < 1f ? 0.7f : 1f)
                         * (GuardianCameraHUD.Modo == 1 ? 1.5f : 1f);
        Pintar(mb, c, 2.2f);
    }

    private void Apagar()
    {
        if (brujula != null) brujula.gameObject.SetActive(false);
        if (brujula2 != null) brujula2.gameObject.SetActive(false);
        if (baliza != null) baliza.gameObject.SetActive(false);
        if (baliza2 != null) baliza2.gameObject.SetActive(false);
    }

    private Transform MasCercanoResiduo(Vector3 yo, GameManager gm)
    {
        if (residuos == null) return null;
        Transform mejor = null; float md = float.MaxValue;
        foreach (TrashItem t in residuos)
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            if (!gm.modoRecorrido && t.zona != gm.zonaActual) continue;
            Collider c = t.GetComponent<Collider>();
            if (c != null && !c.enabled) continue;
            float d = (t.transform.position - yo).sqrMagnitude;
            if (d < md) { md = d; mejor = t.transform; }
        }
        return mejor;
    }

    private RecycleBin MasCercanoBote(Vector3 yo, GameManager gm)
    {
        if (botes == null) return null;
        RecycleBin mejor = null; float md = float.MaxValue;
        foreach (RecycleBin b in botes)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            if (gm.CuantosLlevo(b.acepta) <= 0) continue;
            float d = (b.transform.position - yo).sqrMagnitude;
            if (d < md) { md = d; mejor = b; }
        }
        return mejor;
    }

    private Transform Acopio(GameManager gm)
    {
        if (acopios == null) return null;
        foreach (PuntoAcopioZona a in acopios)
            if (a != null && a.gameObject.activeInHierarchy && a.zona == gm.zonaActual) return a.transform;
        return null;
    }

    private bool TeclaG()
    {
#if ENABLE_INPUT_SYSTEM
        return UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.G);
#endif
    }

    // ---------------------------------------------------------------- malla de flecha

    /// <summary>Flecha 3D extruida (cuerpo + punta), apunta a +Z, grosor 0.14.</summary>
    private static Mesh Malla()
    {
        if (mallaFlecha != null) return mallaFlecha;

        // Contorno en XZ (sentido horario visto desde arriba).
        Vector2[] c = {
            new Vector2(-0.16f, -0.55f), new Vector2(-0.16f, 0.05f), new Vector2(-0.42f, 0.05f),
            new Vector2( 0.00f,  0.60f), new Vector2( 0.42f, 0.05f), new Vector2( 0.16f, 0.05f),
            new Vector2( 0.16f, -0.55f)
        };
        float h = 0.07f;
        var v = new System.Collections.Generic.List<Vector3>();
        var tri = new System.Collections.Generic.List<int>();

        // Tapa superior e inferior: cuerpo (0,1,5,6) + punta (2,3,4)
        int[][] caras = { new[] { 0, 1, 5 }, new[] { 0, 5, 6 }, new[] { 2, 3, 4 } };
        foreach (float y in new[] { h, -h })
            foreach (int[] f in caras)
            {
                int b = v.Count;
                foreach (int i in f) v.Add(new Vector3(c[i].x, y, c[i].y));
                if (y > 0) { tri.Add(b); tri.Add(b + 1); tri.Add(b + 2); }
                else { tri.Add(b); tri.Add(b + 2); tri.Add(b + 1); }
            }

        // Lados
        for (int i = 0; i < c.Length; i++)
        {
            Vector2 a = c[i], bb = c[(i + 1) % c.Length];
            int b = v.Count;
            v.Add(new Vector3(a.x, h, a.y)); v.Add(new Vector3(bb.x, h, bb.y));
            v.Add(new Vector3(bb.x, -h, bb.y)); v.Add(new Vector3(a.x, -h, a.y));
            tri.Add(b); tri.Add(b + 2); tri.Add(b + 1);
            tri.Add(b); tri.Add(b + 3); tri.Add(b + 2);
        }

        Mesh m = new Mesh { name = "FlechaGuia" };
        m.SetVertices(v);
        m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        mallaFlecha = m;
        return m;
    }
}
