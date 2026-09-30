using System.Collections;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Animaciones procedurales de retroalimentación.
///
/// - El residuo VUELA en arco hasta la mochila del Guardián (en vez de desaparecer).
/// - El contenedor hace "squash & stretch" al recibir, y tiembla si te equivocas.
/// - El Guardián parpadea y retrocede al ser golpeado (invulnerabilidad visible).
/// - Los residuos cercanos "respiran" (pulso de escala) para verse interactivos.
/// Son curvas de easing (EaseOutBack, seno amortiguado) aplicadas por código:
/// no hacen falta clips extra ni tocar el Animator del pack.
/// </summary>
public class GuardianAnimaciones : MonoBehaviour
{
    private static GuardianAnimaciones inst;

    private static GuardianAnimaciones I
    {
        get
        {
            if (inst == null)
            {
                inst = FindObjectOfType<GuardianAnimaciones>();
                if (inst == null) inst = new GameObject("GuardianAnimaciones").AddComponent<GuardianAnimaciones>();
            }
            return inst;
        }
    }

    // ---------------------------------------------------------------- residuo → mochila

    /// <summary>Hace volar el residuo hasta el jugador y luego lo desactiva (sin destruirlo).</summary>
    public static void RecogerResiduo(GameObject residuo)
    {
        if (residuo == null) return;
        GameObject pj = GameObject.FindGameObjectWithTag("Player");
        if (pj == null || !Application.isPlaying) { residuo.SetActive(false); return; }
        I.StartCoroutine(I.Volar(residuo, pj.transform));
    }

    private IEnumerator Volar(GameObject go, Transform destino)
    {
        Transform t = go.transform;
        Vector3 p0 = t.position;
        Vector3 s0 = t.localScale;
        GuardianEscalaBase eb = go.GetComponent<GuardianEscalaBase>();
        if (eb != null) s0 = eb.escala;
        Quaternion r0 = t.rotation;

        Collider[] cols = go.GetComponentsInChildren<Collider>();
        foreach (Collider c in cols) if (c != null) c.enabled = false;

        const float dur = 0.38f;
        float k = 0f;
        while (k < 1f && go != null && go.activeInHierarchy)
        {
            k += Time.deltaTime / dur;
            float e = k * k;                                  // acelera hacia la mochila
            Vector3 fin = destino.position + Vector3.up * 1.25f - destino.forward * 0.25f;
            Vector3 p = Vector3.Lerp(p0, fin, e);
            p.y += Mathf.Sin(k * Mathf.PI) * 1.6f;           // arco
            t.position = p;
            t.localScale = s0 * Mathf.Lerp(1.15f, 0.15f, e);
            t.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }

        if (go != null)
        {
            t.position = p0; t.localScale = s0; t.rotation = r0;
            foreach (Collider c in cols) if (c != null) c.enabled = true;
            go.SetActive(false);
        }
    }

    // ---------------------------------------------------------------- golpe de escala

    /// <summary>Squash &amp; stretch: se aplasta y rebota a su tamaño (muelle amortiguado).</summary>
    public static void Rebote(Transform t, float fuerza = 0.25f, float dur = 0.5f)
    {
        if (t == null || !Application.isPlaying) return;
        I.StartCoroutine(I.CRebote(t, fuerza, dur));
    }

    private IEnumerator CRebote(Transform t, float fuerza, float dur)
    {
        Vector3 s0 = t.localScale;
        float k = 0f;
        while (k < 1f && t != null)
        {
            k += Time.unscaledDeltaTime / dur;
            float m = Mathf.Sin(k * Mathf.PI * 3f) * Mathf.Exp(-k * 4f) * fuerza;
            t.localScale = new Vector3(s0.x * (1f + m), s0.y * (1f - m), s0.z * (1f + m));
            yield return null;
        }
        if (t != null) t.localScale = s0;
    }

    /// <summary>Temblor horizontal ("no") para el contenedor equivocado.</summary>
    public static void Temblor(Transform t, float amplitud = 0.18f, float dur = 0.45f)
    {
        if (t == null || !Application.isPlaying) return;
        I.StartCoroutine(I.CTemblor(t, amplitud, dur));
    }

    private IEnumerator CTemblor(Transform t, float amp, float dur)
    {
        Vector3 p0 = t.position;
        float k = 0f;
        while (k < 1f && t != null)
        {
            k += Time.unscaledDeltaTime / dur;
            t.position = p0 + t.right * Mathf.Sin(k * Mathf.PI * 8f) * amp * (1f - k);
            yield return null;
        }
        if (t != null) t.position = p0;
    }

    // ---------------------------------------------------------------- parpadeo del jugador

    /// <summary>El personaje parpadea: marca los segundos en que es invulnerable.</summary>
    public static void Parpadeo(GameObject obj, float dur = 1.4f)
    {
        if (obj == null || !Application.isPlaying) return;
        I.StartCoroutine(I.CParpadeo(obj, dur));
    }

    private IEnumerator CParpadeo(GameObject obj, float dur)
    {
        Renderer[] rs = obj.GetComponentsInChildren<Renderer>();
        float fin = Time.time + dur;
        bool on = true;
        while (Time.time < fin)
        {
            on = !on;
            foreach (Renderer r in rs) if (r != null && !(r is ParticleSystemRenderer)) r.enabled = on;
            yield return new WaitForSeconds(0.08f);
        }
        foreach (Renderer r in rs) if (r != null) r.enabled = true;
    }

    // ---------------------------------------------------------------- pulso de residuos

    private TrashItem[] residuos;
    private Vector3[] escalas;
    private float refresco;
    private Transform jugador;

    void Update()
    {
        if (jugador == null)
        {
            GameObject pj = GameObject.FindGameObjectWithTag("Player");
            if (pj != null) jugador = pj.transform;
        }

        if (Time.unscaledTime > refresco)
        {
            refresco = Time.unscaledTime + 2f;
            residuos = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
            escalas = new Vector3[residuos.Length];
            for (int i = 0; i < residuos.Length; i++)
            {
                GuardianEscalaBase b = residuos[i].GetComponent<GuardianEscalaBase>();
                if (b == null) { b = residuos[i].gameObject.AddComponent<GuardianEscalaBase>(); b.escala = residuos[i].transform.localScale; }
                escalas[i] = b.escala;
            }
        }
        if (residuos == null || jugador == null) return;

        // Los que están a menos de 12 m "respiran": el ojo los encuentra solo.
        for (int i = 0; i < residuos.Length; i++)
        {
            TrashItem t = residuos[i];
            if (t == null || !t.gameObject.activeInHierarchy) continue;
            Collider c = t.GetComponent<Collider>();
            if (c != null && !c.enabled) continue;              // está volando
            float d = Vector3.Distance(jugador.position, t.transform.position);
            float pulso = d < 12f ? 1f + 0.10f * Mathf.Sin(Time.time * 5f + i) * Mathf.InverseLerp(12f, 4f, d) : 1f;
            t.transform.localScale = escalas[i] * pulso;
        }
    }
}

/// <summary>Guarda la escala original de un objeto que se anima (para no acumular errores).</summary>
public class GuardianEscalaBase : MonoBehaviour
{
    public Vector3 escala = Vector3.one;
}
