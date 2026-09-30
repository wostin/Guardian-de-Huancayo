using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Campana de la Plaza Constitución.
/// Cada cierto rato suenan dos campanadas, generadas por código (varias
/// parciales inarmónicas con caída larga, que es como suena una campana real).
/// No usa ningún archivo de audio de terceros.
/// </summary>
public class CampanaIglesia : MonoBehaviour
{
    public float cadaMin = 45f;
    public float cadaMax = 80f;
    public float volumen = 0.45f;

    private AudioSource src;
    private float reloj;

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.clip = Campanada();
        src.loop = false;
        src.playOnAwake = false;
        src.volume = volumen;
        src.spatialBlend = 1f;          // 3D: se oye al acercarse a la plaza
        src.minDistance = 12f;
        src.maxDistance = 120f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.priority = 180;

        reloj = Random.Range(8f, 25f);
    }

    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.estado != GameManager.Estado.Jugando) return;

        reloj -= Time.deltaTime;
        if (reloj > 0f) return;
        reloj = Random.Range(cadaMin, cadaMax);

        if (src != null && !src.isPlaying) src.Play();
    }

    private static AudioClip _clip;

    private static AudioClip Campanada()
    {
        if (_clip != null) return _clip;

        int sr = 44100;
        int len = Mathf.RoundToInt(sr * 4.2f);        // dos campanadas con cola
        float[] d = new float[len];

        // Parciales típicas de una campana: no son múltiplos enteros.
        float f0 = 262f;
        float[] parcial = { 0.5f, 1.0f, 1.2f, 1.5f, 2.0f, 2.5f, 3.0f, 4.2f };
        float[] peso    = { 0.30f, 1.00f, 0.55f, 0.42f, 0.35f, 0.22f, 0.16f, 0.10f };
        float[] caida   = { 0.55f, 0.90f, 1.30f, 1.70f, 2.10f, 2.80f, 3.40f, 4.60f };

        for (int golpe = 0; golpe < 2; golpe++)
        {
            int ini = Mathf.RoundToInt(golpe * 1.55f * sr);
            for (int i = 0; ini + i < len; i++)
            {
                float t = (float)i / sr;
                float s = 0f;
                for (int k = 0; k < parcial.Length; k++)
                    s += Mathf.Sin(2f * Mathf.PI * f0 * parcial[k] * t)
                       * peso[k] * Mathf.Exp(-t * caida[k]);

                // Golpe del badajo al inicio.
                if (t < 0.02f) s += (Random.value * 2f - 1f) * (1f - t / 0.02f) * 0.5f;

                d[ini + i] += s * 0.16f;
            }
        }

        for (int i = 0; i < len; i++) d[i] = Mathf.Clamp(d[i], -0.95f, 0.95f);

        _clip = AudioClip.Create("campana_plaza", len, 1, sr, false);
        _clip.SetData(d, 0);
        return _clip;
    }
}
