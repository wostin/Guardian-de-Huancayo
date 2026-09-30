using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Ambiente sonoro de la ciudad.
/// Viento del valle + rumor de tránsito, GENERADOS POR CÓDIGO (no usa archivos
/// de audio de terceros, así no hay problema de licencias en el trabajo).
/// El viento sube y baja en ráfagas para que no suene plano.
/// </summary>
public class AmbienteCiudad : MonoBehaviour
{
    [Range(0f, 0.4f)] public float volumen = 0.07f;

    private AudioSource src;

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.clip = Ambiente();
        src.loop = true;
        src.volume = volumen;
        src.spatialBlend = 0f;      // 2D: acompaña al jugador
        src.priority = 200;
        src.playOnAwake = false;
        src.Play();
    }

    void Update()
    {
        // Bus AMBIENTE del mezclador (volumen + ducking).
        if (src != null)
        {
            src.volume = volumen * GuardianMezcla.Ganancia(GuardianMezcla.Bus.Ambiente);
            GuardianMezcla.Marca(GuardianMezcla.Bus.Ambiente);
        }
    }

    private static AudioClip _clip;

    private static AudioClip Ambiente()
    {
        if (_clip != null) return _clip;

        int sr = 44100;
        int len = sr * 6;                 // 6 segundos en bucle
        float[] d = new float[len];

        float viento = 0f, calle = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / sr;
            float blanco = Random.value * 2f - 1f;

            viento = viento * 0.9860f + blanco * 0.0140f;   // ruido muy filtrado = viento
            calle  = calle  * 0.9200f + blanco * 0.0800f;   // rumor lejano de tránsito

            float rafaga = 0.45f + 0.55f * (Mathf.Sin(2f * Mathf.PI * 0.07f * t) * 0.5f + 0.5f);
            d[i] = viento * 3.4f * rafaga + calle * 0.22f;
        }

        // Fundido en los extremos para que el bucle no chasquee.
        int f = sr / 2;
        for (int i = 0; i < f; i++)
        {
            float k = (float)i / f;
            d[i] *= k;
            d[len - 1 - i] *= k;
        }

        _clip = AudioClip.Create("ambiente_huancayo", len, 1, sr, false);
        _clip.SetData(d, 0);
        return _clip;
    }
}
