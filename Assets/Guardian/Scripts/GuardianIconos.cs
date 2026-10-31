using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - ÍCONOS DIBUJADOS POR CÓDIGO.
///
/// El profesor pidió "menos texto y más imágenes". Este script es la fábrica de
/// imágenes del juego: medallas de las insignias, pictogramas de la NTP
/// 900.058-2019, contenedores de colores y los objetos del Reto NTP (botella,
/// lata, periódico, cáscara de plátano…). No se descarga ninguna imagen: todo
/// se pinta en un Texture2D con figuras de borde suave (funciones de distancia
/// con signo, SDF), así que se ven limpias a cualquier tamaño de pantalla.
///
/// Uso: GuardianIconos.Insignia(id, desbloqueada) · Objeto(i) · Contenedor(t)
///      · Pictograma(t). Todo queda en caché: se dibuja una sola vez.
/// </summary>
public static class GuardianIconos
{
    // ------------------------------------------------------------------ lienzo

    /// <summary>
    /// Lienzo cuadrado. Las coordenadas van de 0 a 1 con el origen ABAJO a la
    /// izquierda (igual que Texture2D). Con Marco() se dibuja una figura más
    /// chica dentro de otra (el pictograma dentro de la medalla, por ejemplo).
    /// </summary>
    public class Lienzo
    {
        private readonly int n;
        private readonly Color[] px;
        private float ox = 0.5f, oy = 0.5f, sc = 1f;
        private float borde;
        private Color colorBorde = new Color(0.10f, 0.11f, 0.14f);

        public Lienzo(int tam)
        {
            n = Mathf.Max(8, tam);
            px = new Color[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0f, 0f, 0f, 0f);
        }

        /// <summary>Lo que se dibuje después ocupa el cuadro centrado en (cx, cy) de lado 'lado'.</summary>
        public void Marco(float cx, float cy, float lado) { ox = cx; oy = cy; sc = lado; }
        public void SinMarco() { ox = 0.5f; oy = 0.5f; sc = 1f; }

        /// <summary>Contorno oscuro alrededor de cada figura (estilo caricatura).</summary>
        public void Borde(float grosor, Color c) { borde = grosor; colorBorde = c; }
        public void SinBorde() { borde = 0f; }

        private float X(float u) { return ox + (u - 0.5f) * sc; }
        private float Y(float v) { return oy + (v - 0.5f) * sc; }
        private float R(float r) { return r * sc; }

        private delegate float Distancia(float x, float y);

        // --- figuras (coordenadas locales 0..1) ---

        public void Circulo(float cx, float cy, float r, Color c)
        {
            float x = X(cx), y = Y(cy), rr = R(r);
            Figura(x - rr, y - rr, x + rr, y + rr, (a, b) => Largo(a - x, b - y) - rr, c);
        }

        public void Anillo(float cx, float cy, float r, float grosor, Color c)
        {
            float x = X(cx), y = Y(cy), rr = R(r), g = R(grosor) * 0.5f;
            Figura(x - rr - g, y - rr - g, x + rr + g, y + rr + g,
                   (a, b) => Mathf.Abs(Largo(a - x, b - y) - rr) - g, c);
        }

        /// <summary>Elipse girada 'ang' grados.</summary>
        public void Elipse(float cx, float cy, float rx, float ry, Color c, float ang = 0f)
        {
            float x = X(cx), y = Y(cy), ax = Mathf.Max(1e-4f, R(rx)), ay = Mathf.Max(1e-4f, R(ry));
            float cs = Mathf.Cos(-ang * Mathf.Deg2Rad), sn = Mathf.Sin(-ang * Mathf.Deg2Rad);
            float m = Mathf.Max(ax, ay);
            Figura(x - m, y - m, x + m, y + m, (a, b) =>
            {
                float qx = (a - x) * cs - (b - y) * sn;
                float qy = (a - x) * sn + (b - y) * cs;
                float k0 = Largo(qx / ax, qy / ay);
                float k1 = Largo(qx / (ax * ax), qy / (ay * ay));
                return k1 < 1e-6f ? -Mathf.Min(ax, ay) : k0 * (k0 - 1f) / k1;
            }, c);
        }

        /// <summary>Rectángulo de semiancho hw y semialto hh, esquinas de radio 'radio', girado 'ang' grados.</summary>
        public void Caja(float cx, float cy, float hw, float hh, float radio, Color c, float ang = 0f)
        {
            float x = X(cx), y = Y(cy), w = R(hw), h = R(hh), rad = Mathf.Min(R(radio), Mathf.Min(w, h));
            float cs = Mathf.Cos(-ang * Mathf.Deg2Rad), sn = Mathf.Sin(-ang * Mathf.Deg2Rad);
            float m = Largo(w, h);
            Figura(x - m, y - m, x + m, y + m, (a, b) =>
            {
                float qx = Mathf.Abs((a - x) * cs - (b - y) * sn) - (w - rad);
                float qy = Mathf.Abs((a - x) * sn + (b - y) * cs) - (h - rad);
                float fuera = Largo(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f));
                return fuera + Mathf.Min(Mathf.Max(qx, qy), 0f) - rad;
            }, c);
        }

        /// <summary>Trazo grueso con puntas redondas de A a B.</summary>
        public void Trazo(float ax, float ay, float bx, float by, float grosor, Color c)
        {
            float x0 = X(ax), y0 = Y(ay), x1 = X(bx), y1 = Y(by), r = R(grosor) * 0.5f;
            float dx = x1 - x0, dy = y1 - y0, dd = Mathf.Max(1e-8f, dx * dx + dy * dy);
            Figura(Mathf.Min(x0, x1) - r, Mathf.Min(y0, y1) - r, Mathf.Max(x0, x1) + r, Mathf.Max(y0, y1) + r, (a, b) =>
            {
                float pa = a - x0, pb = b - y0;
                float h = Mathf.Clamp01((pa * dx + pb * dy) / dd);
                return Largo(pa - dx * h, pb - dy * h) - r;
            }, c);
        }

        /// <summary>Polígono cualquiera (pares x, y en coordenadas locales).</summary>
        public void Poligono(Color c, params float[] xy)
        {
            int k = xy.Length / 2;
            if (k < 3) return;
            float[] vx = new float[k], vy = new float[k];
            float x0 = 1e9f, y0 = 1e9f, x1 = -1e9f, y1 = -1e9f;
            for (int i = 0; i < k; i++)
            {
                vx[i] = X(xy[i * 2]); vy[i] = Y(xy[i * 2 + 1]);
                x0 = Mathf.Min(x0, vx[i]); y0 = Mathf.Min(y0, vy[i]);
                x1 = Mathf.Max(x1, vx[i]); y1 = Mathf.Max(y1, vy[i]);
            }
            Figura(x0, y0, x1, y1, (a, b) =>
            {
                float d = (a - vx[0]) * (a - vx[0]) + (b - vy[0]) * (b - vy[0]);
                float s = 1f;
                for (int i = 0, j = k - 1; i < k; j = i, i++)
                {
                    float ex = vx[j] - vx[i], ey = vy[j] - vy[i];
                    float wx = a - vx[i], wy = b - vy[i];
                    float t = Mathf.Clamp01((wx * ex + wy * ey) / Mathf.Max(1e-8f, ex * ex + ey * ey));
                    float bx = wx - ex * t, by = wy - ey * t;
                    d = Mathf.Min(d, bx * bx + by * by);
                    bool c1 = b >= vy[i], c2 = b < vy[j], c3 = ex * wy > ey * wx;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * Mathf.Sqrt(d);
            }, c);
        }

        /// <summary>Estrella de 'puntas' puntas.</summary>
        public void Estrella(float cx, float cy, float rExt, float rInt, int puntas, Color c, float giro = 90f)
        {
            float[] xy = new float[puntas * 4];
            for (int i = 0; i < puntas * 2; i++)
            {
                float a = (giro + i * 180f / puntas) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? rExt : rInt;
                xy[i * 2] = cx + Mathf.Cos(a) * r;
                xy[i * 2 + 1] = cy + Mathf.Sin(a) * r;
            }
            Poligono(c, xy);
        }

        // --- motor ---

        private static float Largo(float a, float b) { return Mathf.Sqrt(a * a + b * b); }

        private void Figura(float x0, float y0, float x1, float y1, Distancia f, Color c)
        {
            if (borde > 0f) Pintar(x0, y0, x1, y1, f, colorBorde, R(borde));
            Pintar(x0, y0, x1, y1, f, c, 0f);
        }

        private void Pintar(float x0, float y0, float x1, float y1, Distancia f, Color c, float crecer)
        {
            float margen = crecer + 2f / n;
            int ix0 = Mathf.Clamp(Mathf.FloorToInt((x0 - margen) * n), 0, n - 1);
            int iy0 = Mathf.Clamp(Mathf.FloorToInt((y0 - margen) * n), 0, n - 1);
            int ix1 = Mathf.Clamp(Mathf.CeilToInt((x1 + margen) * n), 0, n - 1);
            int iy1 = Mathf.Clamp(Mathf.CeilToInt((y1 + margen) * n), 0, n - 1);
            for (int y = iy0; y <= iy1; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = ix0; x <= ix1; x++)
                {
                    float u = (x + 0.5f) / n;
                    float d = (f(u, v) - crecer) * n;            // en píxeles
                    float cubre = Mathf.Clamp01(0.5f - d);
                    if (cubre <= 0f) continue;
                    Mezclar(y * n + x, c, cubre);
                }
            }
        }

        private void Mezclar(int i, Color c, float cubre)
        {
            float sa = c.a * cubre;
            Color d = px[i];
            float oa = sa + d.a * (1f - sa);
            if (oa < 1e-5f) { px[i] = new Color(0f, 0f, 0f, 0f); return; }
            float k = d.a * (1f - sa);
            px[i] = new Color((c.r * sa + d.r * k) / oa, (c.g * sa + d.g * k) / oa, (c.b * sa + d.b * k) / oa, oa);
        }

        public Texture2D Textura(string nombre)
        {
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.name = nombre;
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            t.SetPixels(px);
            t.Apply(false);
            return t;
        }
    }

    // ------------------------------------------------------------------ colores

    private static readonly Color BLANCO = new Color(0.98f, 0.98f, 0.97f);
    private static readonly Color TINTA = new Color(0.10f, 0.11f, 0.14f);

    private static Color Oscuro(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, c.a); }
    private static Color Gris(Color c)
    {
        float g = c.r * 0.30f + c.g * 0.59f + c.b * 0.11f;
        return new Color(g, g, g, c.a);
    }

    // ------------------------------------------------------------------ cachés

    private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    private static Texture2D Desde(string clave, System.Func<Texture2D> crear)
    {
        Texture2D t;
        if (cache.TryGetValue(clave, out t) && t != null) return t;
        t = crear();
        cache[clave] = t;
        return t;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar() { cache.Clear(); }

    // ------------------------------------------------------------------ PICTOGRAMAS NTP

    /// <summary>Pictograma blanco del residuo (fondo transparente). 'detalle' pinta las líneas interiores.</summary>
    public static Texture2D Pictograma(TipoResiduo t)
    {
        return Desde("pic" + (int)t, () =>
        {
            Lienzo l = new Lienzo(96);
            l.Borde(0.035f, new Color(0f, 0f, 0f, 0.55f));
            DibujarPictograma(l, t, BLANCO, Oscuro(Residuo.Tinte(t), 0.75f));
            return l.Textura("Pictograma_" + t);
        });
    }

    /// <summary>Dibuja el pictograma del tipo dentro del marco actual del lienzo.</summary>
    public static void DibujarPictograma(Lienzo l, TipoResiduo t, Color c, Color detalle)
    {
        switch (t)
        {
            case TipoResiduo.Plastico:      // botella PET con estrías
                l.Caja(0.5f, 0.38f, 0.17f, 0.27f, 0.09f, c);
                l.Poligono(c, 0.36f, 0.60f, 0.64f, 0.60f, 0.57f, 0.72f, 0.43f, 0.72f);
                l.Caja(0.5f, 0.76f, 0.07f, 0.06f, 0.015f, c);
                l.Caja(0.5f, 0.86f, 0.09f, 0.045f, 0.02f, c);
                l.SinBorde();
                l.Trazo(0.38f, 0.30f, 0.62f, 0.30f, 0.035f, detalle);
                l.Trazo(0.38f, 0.42f, 0.62f, 0.42f, 0.035f, detalle);
                break;

            case TipoResiduo.Vidrio:        // botella de cuello largo con brillo
                l.Caja(0.5f, 0.32f, 0.16f, 0.22f, 0.05f, c);
                l.Elipse(0.5f, 0.52f, 0.16f, 0.09f, c);
                l.Caja(0.5f, 0.70f, 0.055f, 0.14f, 0.02f, c);
                l.Caja(0.5f, 0.86f, 0.072f, 0.03f, 0.015f, c);
                l.SinBorde();
                l.Trazo(0.42f, 0.18f, 0.42f, 0.44f, 0.04f, detalle);
                break;

            case TipoResiduo.Papel:         // hoja de periódico con esquina doblada
                l.Poligono(c, 0.24f, 0.14f, 0.76f, 0.14f, 0.76f, 0.70f, 0.62f, 0.86f, 0.24f, 0.86f);
                l.SinBorde();
                l.Poligono(detalle, 0.62f, 0.86f, 0.62f, 0.70f, 0.76f, 0.70f);
                l.Caja(0.40f, 0.70f, 0.10f, 0.05f, 0.01f, detalle);
                l.Trazo(0.32f, 0.54f, 0.68f, 0.54f, 0.035f, detalle);
                l.Trazo(0.32f, 0.43f, 0.68f, 0.43f, 0.035f, detalle);
                l.Trazo(0.32f, 0.32f, 0.68f, 0.32f, 0.035f, detalle);
                l.Trazo(0.32f, 0.21f, 0.55f, 0.21f, 0.035f, detalle);
                break;

            case TipoResiduo.Metal:         // lata con anilla
                l.Caja(0.5f, 0.48f, 0.19f, 0.30f, 0.03f, c);
                l.Elipse(0.5f, 0.78f, 0.19f, 0.055f, c);
                l.Elipse(0.5f, 0.18f, 0.19f, 0.055f, c);
                l.SinBorde();
                l.Elipse(0.5f, 0.78f, 0.15f, 0.035f, detalle);
                l.Anillo(0.54f, 0.79f, 0.035f, 0.018f, c);
                l.Trazo(0.33f, 0.62f, 0.67f, 0.62f, 0.03f, detalle);
                l.Trazo(0.33f, 0.34f, 0.67f, 0.34f, 0.03f, detalle);
                break;

            default:                        // manzana con hoja (orgánicos)
                l.Circulo(0.40f, 0.42f, 0.22f, c);
                l.Circulo(0.60f, 0.42f, 0.22f, c);
                l.Circulo(0.5f, 0.30f, 0.20f, c);
                l.Trazo(0.50f, 0.60f, 0.47f, 0.80f, 0.045f, c);
                l.Elipse(0.62f, 0.76f, 0.11f, 0.05f, c, 30f);
                l.SinBorde();
                l.Elipse(0.38f, 0.48f, 0.05f, 0.09f, detalle, -20f);
                break;
        }
    }

    // ------------------------------------------------------------------ CONTENEDORES

    /// <summary>Contenedor del color de la norma con su pictograma (para los botones del Reto NTP).</summary>
    public static Texture2D Contenedor(TipoResiduo t)
    {
        return Desde("bin" + (int)t, () =>
        {
            Color c = Residuo.Tinte(t);
            Color tapa = Oscuro(c, 0.78f);
            Lienzo l = new Lienzo(128);
            l.Borde(0.025f, TINTA);
            // ruedas
            l.Circulo(0.30f, 0.10f, 0.06f, new Color(0.18f, 0.19f, 0.22f));
            l.Circulo(0.70f, 0.10f, 0.06f, new Color(0.18f, 0.19f, 0.22f));
            // cuerpo (un poco más ancho arriba)
            l.Poligono(c, 0.20f, 0.12f, 0.80f, 0.12f, 0.86f, 0.72f, 0.14f, 0.72f);
            // tapa y agarradera
            l.Caja(0.5f, 0.77f, 0.40f, 0.06f, 0.03f, tapa);
            l.Caja(0.5f, 0.86f, 0.12f, 0.03f, 0.02f, tapa);
            // placa con el pictograma
            l.SinBorde();
            l.Caja(0.5f, 0.43f, 0.22f, 0.20f, 0.04f, new Color(1f, 1f, 1f, 0.92f));
            l.Marco(0.5f, 0.43f, 0.40f);
            l.Borde(0.03f, new Color(0f, 0f, 0f, 0.30f));
            DibujarPictograma(l, t, Oscuro(c, t == TipoResiduo.Plastico ? 0.55f : 0.85f), new Color(1f, 1f, 1f, 0.9f));
            l.SinMarco();
            l.SinBorde();
            l.Trazo(0.17f, 0.66f, 0.83f, 0.66f, 0.02f, Oscuro(c, 0.65f));
            return l.Textura("Contenedor_" + t);
        });
    }

    // ------------------------------------------------------------------ INSIGNIAS

    public const int INSIGNIAS = 16;

    /// <summary>Medalla de la insignia 'id'. Bloqueada = gris y apagada.</summary>
    public static Texture2D Insignia(int id, Color colorFondo, bool desbloqueada)
    {
        return Desde("ins" + id + (desbloqueada ? "_on" : "_off"), () =>
        {
            Color oro = desbloqueada ? new Color(1f, 0.80f, 0.30f) : new Color(0.46f, 0.48f, 0.52f);
            Color oroOsc = desbloqueada ? new Color(0.58f, 0.38f, 0.08f) : new Color(0.22f, 0.23f, 0.26f);
            Color disco = desbloqueada ? colorFondo : new Color(0.20f, 0.23f, 0.28f);
            Color figura = desbloqueada ? BLANCO : new Color(0.38f, 0.42f, 0.48f);
            Color detalle = desbloqueada ? Oscuro(colorFondo, 0.72f) : new Color(0.24f, 0.27f, 0.32f);

            Lienzo l = new Lienzo(128);
            // cintas
            if (desbloqueada)
            {
                l.Poligono(new Color(0.85f, 0.18f, 0.20f), 0.30f, 0.30f, 0.44f, 0.30f, 0.36f, 0.02f, 0.28f, 0.10f, 0.20f, 0.04f);
                l.Poligono(new Color(0.95f, 0.95f, 0.95f), 0.56f, 0.30f, 0.70f, 0.30f, 0.80f, 0.04f, 0.72f, 0.10f, 0.64f, 0.02f);
            }
            l.Circulo(0.5f, 0.54f, 0.44f, oroOsc);
            l.Circulo(0.5f, 0.54f, 0.41f, oro);
            l.Circulo(0.5f, 0.54f, 0.345f, disco);
            if (desbloqueada)
            {
                // brillo arriba a la izquierda
                l.Anillo(0.5f, 0.54f, 0.375f, 0.012f, new Color(1f, 0.95f, 0.75f, 0.8f));
            }
            l.Marco(0.5f, 0.54f, 0.56f);
            l.Borde(0.03f, new Color(0f, 0f, 0f, desbloqueada ? 0.35f : 0.15f));
            DibujarFiguraInsignia(l, id, figura, detalle, desbloqueada);
            return l.Textura("Insignia_" + id);
        });
    }

    private static void DibujarFiguraInsignia(Lienzo l, int id, Color c, Color det, bool on)
    {
        switch (id)
        {
            case 0:   // PRIMER RESIDUO: bolsa con nudo
                l.Caja(0.5f, 0.40f, 0.26f, 0.26f, 0.12f, c);
                l.Poligono(c, 0.40f, 0.62f, 0.60f, 0.62f, 0.54f, 0.72f, 0.46f, 0.72f);
                l.Elipse(0.40f, 0.80f, 0.08f, 0.05f, c, 30f);
                l.Elipse(0.60f, 0.80f, 0.08f, 0.05f, c, -30f);
                l.SinBorde();
                l.Trazo(0.36f, 0.30f, 0.46f, 0.48f, 0.035f, det);
                break;

            case 1:   // BUEN OJO: ojo
                l.Elipse(0.5f, 0.5f, 0.40f, 0.22f, c);
                l.SinBorde();
                l.Circulo(0.5f, 0.5f, 0.15f, det);
                l.Circulo(0.5f, 0.5f, 0.07f, TINTA);
                l.Circulo(0.55f, 0.55f, 0.035f, BLANCO);
                break;

            case 2:   // RACHA x5: llama
                l.Circulo(0.5f, 0.36f, 0.24f, c);
                l.Poligono(c, 0.27f, 0.42f, 0.73f, 0.42f, 0.56f, 0.92f, 0.50f, 0.70f, 0.40f, 0.80f);
                l.SinBorde();
                l.Circulo(0.5f, 0.32f, 0.12f, det);
                l.Poligono(det, 0.39f, 0.34f, 0.61f, 0.34f, 0.52f, 0.62f);
                break;

            case 3:   // IMPARABLE: rayo
                l.Poligono(c, 0.58f, 0.92f, 0.28f, 0.46f, 0.48f, 0.46f, 0.40f, 0.08f, 0.74f, 0.58f, 0.52f, 0.58f);
                break;

            case 4:   // SEGREGACIÓN PERFECTA: estrella
                l.Estrella(0.5f, 0.48f, 0.44f, 0.19f, 5, c);
                break;

            case 5:   // PLAZA LIMPIA: fachada de iglesia con dos torres
                l.Caja(0.5f, 0.32f, 0.24f, 0.20f, 0.01f, c);
                l.Poligono(c, 0.24f, 0.52f, 0.76f, 0.52f, 0.5f, 0.68f);
                l.Caja(0.20f, 0.42f, 0.09f, 0.30f, 0.01f, c);
                l.Caja(0.80f, 0.42f, 0.09f, 0.30f, 0.01f, c);
                l.Poligono(c, 0.10f, 0.72f, 0.30f, 0.72f, 0.20f, 0.86f);
                l.Poligono(c, 0.70f, 0.72f, 0.90f, 0.72f, 0.80f, 0.86f);
                l.Trazo(0.5f, 0.70f, 0.5f, 0.90f, 0.04f, c);
                l.Trazo(0.44f, 0.84f, 0.56f, 0.84f, 0.04f, c);
                l.SinBorde();
                l.Caja(0.5f, 0.20f, 0.07f, 0.09f, 0.06f, det);
                l.Circulo(0.5f, 0.45f, 0.05f, det);
                break;

            case 6:   // MERCADO LIMPIO: puesto con toldo a rayas
                l.Trazo(0.20f, 0.10f, 0.20f, 0.62f, 0.05f, c);
                l.Trazo(0.80f, 0.10f, 0.80f, 0.62f, 0.05f, c);
                l.Caja(0.5f, 0.30f, 0.34f, 0.08f, 0.02f, c);
                l.Poligono(c, 0.10f, 0.62f, 0.90f, 0.62f, 0.80f, 0.86f, 0.20f, 0.86f);
                l.SinBorde();
                l.Poligono(det, 0.25f, 0.62f, 0.38f, 0.62f, 0.36f, 0.86f, 0.28f, 0.86f);
                l.Poligono(det, 0.51f, 0.62f, 0.64f, 0.62f, 0.60f, 0.86f, 0.52f, 0.86f);
                l.Circulo(0.38f, 0.42f, 0.06f, det);
                l.Circulo(0.52f, 0.42f, 0.06f, det);
                l.Circulo(0.65f, 0.42f, 0.06f, det);
                break;

            case 7:   // SHULLCAS VIVO: pez sobre las olas
                l.Elipse(0.46f, 0.60f, 0.26f, 0.14f, c);
                l.Poligono(c, 0.68f, 0.60f, 0.88f, 0.76f, 0.88f, 0.44f);
                l.Trazo(0.10f, 0.26f, 0.30f, 0.20f, 0.06f, c);
                l.Trazo(0.30f, 0.20f, 0.50f, 0.26f, 0.06f, c);
                l.Trazo(0.50f, 0.26f, 0.70f, 0.20f, 0.06f, c);
                l.Trazo(0.70f, 0.20f, 0.90f, 0.26f, 0.06f, c);
                l.SinBorde();
                l.Circulo(0.32f, 0.63f, 0.035f, TINTA);
                l.Trazo(0.50f, 0.50f, 0.50f, 0.70f, 0.03f, det);
                break;

            case 8:   // LOS 5 COLORES: cinco círculos de la norma
                for (int i = 0; i < Residuo.TIPOS; i++)
                {
                    float a = (90f + i * 72f) * Mathf.Deg2Rad;
                    Color col = on ? Residuo.Tinte(Residuo.Desde(i)) : c;
                    l.Circulo(0.5f + Mathf.Cos(a) * 0.30f, 0.5f + Mathf.Sin(a) * 0.30f, 0.13f, col);
                }
                l.SinBorde();
                l.Trazo(0.42f, 0.50f, 0.48f, 0.43f, 0.06f, c);
                l.Trazo(0.48f, 0.43f, 0.60f, 0.58f, 0.06f, c);
                break;

            case 9:   // VECINO CONSCIENTE: dos personas y un corazón
                l.Circulo(0.32f, 0.62f, 0.11f, c);
                l.Caja(0.32f, 0.28f, 0.17f, 0.18f, 0.12f, c);
                l.Circulo(0.68f, 0.62f, 0.11f, c);
                l.Caja(0.68f, 0.28f, 0.17f, 0.18f, 0.12f, c);
                l.SinBorde();
                l.Circulo(0.46f, 0.86f, 0.06f, det);
                l.Circulo(0.56f, 0.86f, 0.06f, det);
                l.Poligono(det, 0.405f, 0.84f, 0.605f, 0.84f, 0.505f, 0.72f);
                break;

            case 10:  // BUENA CHARLA: globo de diálogo
                l.Caja(0.5f, 0.56f, 0.38f, 0.26f, 0.12f, c);
                l.Poligono(c, 0.24f, 0.36f, 0.42f, 0.32f, 0.20f, 0.14f);
                l.SinBorde();
                l.Circulo(0.34f, 0.56f, 0.055f, det);
                l.Circulo(0.50f, 0.56f, 0.055f, det);
                l.Circulo(0.66f, 0.56f, 0.055f, det);
                break;

            case 11:  // MOVILIDAD SOSTENIBLE: bicicleta
                l.Anillo(0.24f, 0.32f, 0.16f, 0.055f, c);
                l.Anillo(0.76f, 0.32f, 0.16f, 0.055f, c);
                l.Trazo(0.24f, 0.32f, 0.42f, 0.56f, 0.05f, c);
                l.Trazo(0.42f, 0.56f, 0.66f, 0.56f, 0.05f, c);
                l.Trazo(0.66f, 0.56f, 0.76f, 0.32f, 0.05f, c);
                l.Trazo(0.24f, 0.32f, 0.50f, 0.32f, 0.05f, c);
                l.Trazo(0.50f, 0.32f, 0.66f, 0.56f, 0.05f, c);
                l.Trazo(0.42f, 0.56f, 0.38f, 0.68f, 0.05f, c);
                l.Trazo(0.32f, 0.70f, 0.44f, 0.70f, 0.05f, c);
                l.Trazo(0.66f, 0.56f, 0.64f, 0.70f, 0.05f, c);
                l.Trazo(0.60f, 0.72f, 0.72f, 0.72f, 0.05f, c);
                break;

            case 12:  // PEATÓN SEGURO: semáforo
                l.Caja(0.5f, 0.52f, 0.17f, 0.38f, 0.07f, c);
                l.Trazo(0.5f, 0.02f, 0.5f, 0.16f, 0.06f, c);
                l.SinBorde();
                l.Circulo(0.5f, 0.76f, 0.09f, on ? new Color(0.90f, 0.20f, 0.18f) : det);
                l.Circulo(0.5f, 0.52f, 0.09f, on ? new Color(0.98f, 0.72f, 0.12f) : det);
                l.Circulo(0.5f, 0.28f, 0.09f, on ? new Color(0.20f, 0.80f, 0.35f) : det);
                break;

            case 13:  // REY DESTRONADO: corona
                l.Poligono(c, 0.14f, 0.30f, 0.86f, 0.30f, 0.90f, 0.70f, 0.70f, 0.50f, 0.50f, 0.76f, 0.30f, 0.50f, 0.10f, 0.70f);
                l.Caja(0.5f, 0.24f, 0.36f, 0.06f, 0.02f, c);
                l.Circulo(0.10f, 0.72f, 0.05f, c);
                l.Circulo(0.50f, 0.78f, 0.05f, c);
                l.Circulo(0.90f, 0.72f, 0.05f, c);
                l.SinBorde();
                l.Circulo(0.30f, 0.38f, 0.04f, det);
                l.Circulo(0.50f, 0.38f, 0.04f, det);
                l.Circulo(0.70f, 0.38f, 0.04f, det);
                break;

            case 14:  // QUIZ MAESTRO: foco encendido
                l.Circulo(0.5f, 0.60f, 0.27f, c);
                l.Poligono(c, 0.36f, 0.44f, 0.64f, 0.44f, 0.60f, 0.30f, 0.40f, 0.30f);
                l.Caja(0.5f, 0.24f, 0.11f, 0.05f, 0.02f, c);
                l.Caja(0.5f, 0.14f, 0.08f, 0.04f, 0.03f, c);
                l.SinBorde();
                l.Trazo(0.44f, 0.50f, 0.47f, 0.62f, 0.035f, det);
                l.Trazo(0.47f, 0.62f, 0.53f, 0.62f, 0.035f, det);
                l.Trazo(0.53f, 0.62f, 0.56f, 0.50f, 0.035f, det);
                break;

            default:  // 100 RESIDUOS: trofeo
                l.Anillo(0.24f, 0.66f, 0.10f, 0.05f, c);
                l.Anillo(0.76f, 0.66f, 0.10f, 0.05f, c);
                l.Poligono(c, 0.26f, 0.84f, 0.74f, 0.84f, 0.68f, 0.52f, 0.58f, 0.42f, 0.42f, 0.42f, 0.32f, 0.52f);
                l.Caja(0.5f, 0.32f, 0.05f, 0.10f, 0.01f, c);
                l.Caja(0.5f, 0.17f, 0.20f, 0.06f, 0.02f, c);
                l.SinBorde();
                l.Estrella(0.5f, 0.64f, 0.11f, 0.05f, 5, det);
                break;
        }
    }

    // ------------------------------------------------------------------ OBJETOS DEL RETO NTP

    /// <summary>Un residuo de la vida diaria, dibujado a color para el Reto NTP.</summary>
    public struct ObjetoReto
    {
        public string nombre;
        public TipoResiduo tipo;
        public ObjetoReto(string n, TipoResiduo t) { nombre = n; tipo = t; }
    }

    public static readonly ObjetoReto[] OBJETOS =
    {
        new ObjetoReto("Botella de plástico",   TipoResiduo.Plastico),
        new ObjetoReto("Bolsa plástica",        TipoResiduo.Plastico),
        new ObjetoReto("Envase de yogur",       TipoResiduo.Plastico),
        new ObjetoReto("Bidón de agua",         TipoResiduo.Plastico),
        new ObjetoReto("Botella de vidrio",     TipoResiduo.Vidrio),
        new ObjetoReto("Frasco de mermelada",   TipoResiduo.Vidrio),
        new ObjetoReto("Periódico",             TipoResiduo.Papel),
        new ObjetoReto("Caja de cartón",        TipoResiduo.Papel),
        new ObjetoReto("Cuaderno viejo",        TipoResiduo.Papel),
        new ObjetoReto("Lata de gaseosa",       TipoResiduo.Metal),
        new ObjetoReto("Lata de atún",          TipoResiduo.Metal),
        new ObjetoReto("Cáscara de plátano",    TipoResiduo.Organico),
        new ObjetoReto("Resto de manzana",      TipoResiduo.Organico),
        new ObjetoReto("Cáscaras de papa",      TipoResiduo.Organico),
        new ObjetoReto("Cáscaras de huevo",     TipoResiduo.Organico),
    };

    public static Texture2D Objeto(int i)
    {
        i = Mathf.Clamp(i, 0, OBJETOS.Length - 1);
        return Desde("obj" + i, () =>
        {
            Lienzo l = new Lienzo(160);
            l.Borde(0.022f, TINTA);
            DibujarObjeto(l, i);
            return l.Textura("Objeto_" + i);
        });
    }

    private static void DibujarObjeto(Lienzo l, int i)
    {
        Color sombra = new Color(0f, 0f, 0f, 0.22f);
        l.SinBorde();
        l.Elipse(0.5f, 0.08f, 0.32f, 0.045f, sombra);
        l.Borde(0.022f, TINTA);

        switch (i)
        {
            case 0:  // botella PET transparente celeste
            {
                Color cuerpo = new Color(0.62f, 0.86f, 0.98f);
                l.Caja(0.5f, 0.36f, 0.16f, 0.27f, 0.08f, cuerpo);
                l.Poligono(cuerpo, 0.35f, 0.58f, 0.65f, 0.58f, 0.57f, 0.74f, 0.43f, 0.74f);
                l.Caja(0.5f, 0.78f, 0.065f, 0.05f, 0.01f, cuerpo);
                l.Caja(0.5f, 0.87f, 0.085f, 0.045f, 0.02f, new Color(0.15f, 0.45f, 0.90f));
                l.Caja(0.5f, 0.40f, 0.165f, 0.07f, 0.01f, new Color(0.95f, 0.30f, 0.28f));
                l.SinBorde();
                l.Trazo(0.40f, 0.16f, 0.40f, 0.30f, 0.03f, new Color(1f, 1f, 1f, 0.75f));
                l.Trazo(0.37f, 0.40f, 0.63f, 0.40f, 0.02f, new Color(1f, 1f, 1f, 0.8f));
                break;
            }
            case 1:  // bolsa plástica blanca con asas
            {
                Color b = new Color(0.94f, 0.95f, 0.97f);
                l.Anillo(0.38f, 0.74f, 0.08f, 0.04f, b);
                l.Anillo(0.62f, 0.74f, 0.08f, 0.04f, b);
                l.Poligono(b, 0.20f, 0.12f, 0.80f, 0.12f, 0.74f, 0.68f, 0.26f, 0.68f);
                l.SinBorde();
                l.Trazo(0.38f, 0.20f, 0.42f, 0.56f, 0.02f, new Color(0.70f, 0.74f, 0.80f));
                l.Trazo(0.60f, 0.22f, 0.58f, 0.52f, 0.02f, new Color(0.70f, 0.74f, 0.80f));
                break;
            }
            case 2:  // envase de yogur
            {
                l.Poligono(new Color(0.98f, 0.97f, 0.94f), 0.30f, 0.66f, 0.70f, 0.66f, 0.63f, 0.14f, 0.37f, 0.14f);
                l.Caja(0.5f, 0.69f, 0.22f, 0.035f, 0.01f, new Color(0.85f, 0.25f, 0.55f));
                l.SinBorde();
                l.Poligono(new Color(0.95f, 0.45f, 0.65f), 0.33f, 0.50f, 0.67f, 0.50f, 0.645f, 0.32f, 0.355f, 0.32f);
                l.Circulo(0.5f, 0.41f, 0.05f, new Color(1f, 1f, 1f, 0.9f));
                break;
            }
            case 3:  // bidón de agua con asa
            {
                Color c = new Color(0.55f, 0.80f, 0.96f);
                l.Caja(0.47f, 0.40f, 0.24f, 0.30f, 0.07f, c);
                l.Anillo(0.70f, 0.56f, 0.10f, 0.05f, c);
                l.Caja(0.40f, 0.76f, 0.07f, 0.05f, 0.01f, c);
                l.Caja(0.40f, 0.84f, 0.09f, 0.04f, 0.02f, new Color(0.20f, 0.50f, 0.92f));
                l.SinBorde();
                l.Trazo(0.30f, 0.20f, 0.30f, 0.60f, 0.03f, new Color(1f, 1f, 1f, 0.7f));
                l.Trazo(0.30f, 0.30f, 0.64f, 0.30f, 0.02f, new Color(0.35f, 0.62f, 0.85f));
                break;
            }
            case 4:  // botella de vidrio verde
            {
                Color v = new Color(0.16f, 0.58f, 0.30f);
                l.Caja(0.5f, 0.30f, 0.15f, 0.22f, 0.04f, v);
                l.Elipse(0.5f, 0.50f, 0.15f, 0.08f, v);
                l.Caja(0.5f, 0.68f, 0.05f, 0.14f, 0.02f, v);
                l.Caja(0.5f, 0.85f, 0.065f, 0.03f, 0.01f, new Color(0.80f, 0.70f, 0.30f));
                l.Caja(0.5f, 0.30f, 0.15f, 0.08f, 0.01f, new Color(0.96f, 0.90f, 0.70f));
                l.SinBorde();
                l.Trazo(0.41f, 0.12f, 0.41f, 0.46f, 0.03f, new Color(1f, 1f, 1f, 0.45f));
                break;
            }
            case 5:  // frasco de mermelada
            {
                l.Caja(0.5f, 0.38f, 0.22f, 0.26f, 0.07f, new Color(0.80f, 0.92f, 0.96f, 0.85f));
                l.Caja(0.5f, 0.70f, 0.24f, 0.06f, 0.02f, new Color(0.88f, 0.70f, 0.20f));
                l.SinBorde();
                l.Caja(0.5f, 0.33f, 0.18f, 0.18f, 0.05f, new Color(0.80f, 0.12f, 0.22f));
                l.Caja(0.5f, 0.42f, 0.13f, 0.07f, 0.01f, new Color(0.98f, 0.95f, 0.85f));
                l.Trazo(0.34f, 0.20f, 0.34f, 0.56f, 0.03f, new Color(1f, 1f, 1f, 0.55f));
                break;
            }
            case 6:  // periódico doblado
            {
                l.Caja(0.5f, 0.45f, 0.32f, 0.28f, 0.02f, new Color(0.93f, 0.92f, 0.86f), -6f);
                l.SinBorde();
                l.Caja(0.40f, 0.60f, 0.14f, 0.05f, 0.01f, new Color(0.25f, 0.25f, 0.28f), -6f);
                for (int k = 0; k < 4; k++)
                    l.Trazo(0.25f, 0.46f - k * 0.08f, 0.70f, 0.41f - k * 0.08f, 0.025f, new Color(0.45f, 0.45f, 0.50f));
                l.Caja(0.68f, 0.58f, 0.08f, 0.06f, 0.01f, new Color(0.60f, 0.65f, 0.72f), -6f);
                break;
            }
            case 7:  // caja de cartón con cinta
            {
                Color c = new Color(0.78f, 0.58f, 0.36f);
                l.Poligono(Oscuro(c, 0.85f), 0.70f, 0.14f, 0.86f, 0.28f, 0.86f, 0.64f, 0.70f, 0.52f);
                l.Poligono(c, 0.16f, 0.14f, 0.70f, 0.14f, 0.70f, 0.52f, 0.16f, 0.52f);
                l.Poligono(Oscuro(c, 1.12f), 0.16f, 0.52f, 0.70f, 0.52f, 0.86f, 0.64f, 0.32f, 0.64f);
                l.SinBorde();
                l.Poligono(new Color(0.92f, 0.80f, 0.55f), 0.38f, 0.52f, 0.48f, 0.52f, 0.64f, 0.64f, 0.54f, 0.64f);
                l.Caja(0.43f, 0.34f, 0.05f, 0.18f, 0f, new Color(0.92f, 0.80f, 0.55f));
                break;
            }
            case 8:  // cuaderno con espiral
            {
                l.Caja(0.53f, 0.46f, 0.27f, 0.34f, 0.03f, new Color(0.20f, 0.45f, 0.80f));
                l.SinBorde();
                l.Caja(0.56f, 0.56f, 0.15f, 0.06f, 0.01f, new Color(0.96f, 0.96f, 0.96f));
                for (int k = 0; k < 6; k++)
                    l.Anillo(0.27f, 0.20f + k * 0.105f, 0.04f, 0.022f, new Color(0.75f, 0.77f, 0.80f));
                break;
            }
            case 9:  // lata de gaseosa roja
            {
                Color r = new Color(0.86f, 0.14f, 0.16f);
                l.Caja(0.5f, 0.44f, 0.17f, 0.30f, 0.03f, r);
                l.Elipse(0.5f, 0.74f, 0.17f, 0.05f, new Color(0.78f, 0.80f, 0.84f));
                l.Elipse(0.5f, 0.14f, 0.17f, 0.05f, new Color(0.70f, 0.72f, 0.76f));
                l.SinBorde();
                l.Anillo(0.54f, 0.75f, 0.03f, 0.016f, new Color(0.55f, 0.58f, 0.62f));
                l.Trazo(0.36f, 0.30f, 0.64f, 0.58f, 0.06f, new Color(1f, 1f, 1f, 0.9f));
                l.Trazo(0.39f, 0.20f, 0.39f, 0.66f, 0.025f, new Color(1f, 1f, 1f, 0.35f));
                break;
            }
            case 10: // lata de atún
            {
                Color g = new Color(0.72f, 0.75f, 0.80f);
                l.Caja(0.5f, 0.30f, 0.28f, 0.13f, 0.02f, new Color(0.18f, 0.42f, 0.80f));
                l.Elipse(0.5f, 0.43f, 0.28f, 0.08f, g);
                l.SinBorde();
                l.Elipse(0.5f, 0.43f, 0.22f, 0.055f, Oscuro(g, 0.85f));
                l.Anillo(0.70f, 0.45f, 0.04f, 0.02f, Oscuro(g, 0.65f));
                l.Elipse(0.40f, 0.29f, 0.10f, 0.05f, new Color(0.98f, 0.60f, 0.45f));
                break;
            }
            case 11: // cáscara de plátano
            {
                Color y = new Color(0.98f, 0.84f, 0.22f);
                l.Elipse(0.50f, 0.30f, 0.14f, 0.10f, y);
                l.Elipse(0.32f, 0.36f, 0.18f, 0.06f, y, 35f);
                l.Elipse(0.68f, 0.36f, 0.18f, 0.06f, y, -35f);
                l.Elipse(0.50f, 0.42f, 0.06f, 0.18f, y);
                l.Caja(0.5f, 0.68f, 0.035f, 0.10f, 0.02f, new Color(0.55f, 0.42f, 0.18f));
                l.SinBorde();
                l.Circulo(0.66f, 0.30f, 0.025f, new Color(0.45f, 0.33f, 0.12f));
                l.Circulo(0.36f, 0.40f, 0.02f, new Color(0.45f, 0.33f, 0.12f));
                break;
            }
            case 12: // resto de manzana
            {
                Color r = new Color(0.86f, 0.18f, 0.20f);
                Color pulpa = new Color(0.98f, 0.94f, 0.78f);
                l.Elipse(0.5f, 0.76f, 0.16f, 0.06f, r);
                l.Elipse(0.5f, 0.20f, 0.15f, 0.06f, r);
                l.Caja(0.5f, 0.48f, 0.09f, 0.24f, 0.05f, pulpa);
                l.Trazo(0.5f, 0.80f, 0.53f, 0.92f, 0.035f, new Color(0.40f, 0.26f, 0.12f));
                l.SinBorde();
                l.Elipse(0.47f, 0.50f, 0.02f, 0.035f, new Color(0.35f, 0.20f, 0.10f));
                l.Elipse(0.53f, 0.44f, 0.02f, 0.035f, new Color(0.35f, 0.20f, 0.10f));
                break;
            }
            case 13: // cáscaras de papa
            {
                Color p = new Color(0.72f, 0.52f, 0.30f);
                Color in2 = new Color(0.96f, 0.88f, 0.62f);
                l.Elipse(0.40f, 0.30f, 0.20f, 0.08f, p, 15f);
                l.Elipse(0.62f, 0.42f, 0.20f, 0.07f, p, -25f);
                l.Elipse(0.45f, 0.56f, 0.18f, 0.07f, p, 10f);
                l.SinBorde();
                l.Elipse(0.40f, 0.31f, 0.15f, 0.035f, in2, 15f);
                l.Elipse(0.62f, 0.43f, 0.15f, 0.03f, in2, -25f);
                l.Elipse(0.45f, 0.57f, 0.13f, 0.03f, in2, 10f);
                break;
            }
            default: // cáscaras de huevo
            {
                Color h = new Color(0.97f, 0.93f, 0.86f);
                l.Poligono(h, 0.18f, 0.40f, 0.48f, 0.40f, 0.45f, 0.30f, 0.38f, 0.22f, 0.26f, 0.20f, 0.19f, 0.28f);
                l.Poligono(h, 0.18f, 0.40f, 0.24f, 0.48f, 0.30f, 0.42f, 0.36f, 0.50f, 0.42f, 0.43f, 0.48f, 0.40f);
                l.Poligono(h, 0.52f, 0.36f, 0.84f, 0.36f, 0.80f, 0.24f, 0.70f, 0.18f, 0.60f, 0.19f, 0.54f, 0.26f);
                l.Poligono(h, 0.52f, 0.36f, 0.58f, 0.44f, 0.64f, 0.38f, 0.70f, 0.46f, 0.77f, 0.39f, 0.84f, 0.36f);
                l.SinBorde();
                l.Circulo(0.30f, 0.30f, 0.03f, new Color(0.90f, 0.82f, 0.70f));
                l.Circulo(0.70f, 0.28f, 0.03f, new Color(0.90f, 0.82f, 0.70f));
                break;
            }
        }
    }

    // ------------------------------------------------------------------ texturas de interfaz

    /// <summary>
    /// Rectángulo redondeado BLANCO (se tiñe con GUI.color al dibujarlo). Uno
    /// por radio: así los paneles que cambian de color cada cuadro —la barra de
    /// tiempo, por ejemplo— no crean una textura nueva en cada cuadro.
    /// </summary>
    public static Texture2D Panel(int radioPx = 10)
    {
        radioPx = Mathf.Clamp(radioPx, 2, 32);
        return Desde("pan" + radioPx, () =>
        {
            int n = radioPx * 2 + 4;
            Lienzo l = new Lienzo(n);
            float r = (float)radioPx / n;
            l.Caja(0.5f, 0.5f, 0.5f - 0.5f / n, 0.5f - 0.5f / n, r, Color.white);
            return l.Textura("Panel");
        });
    }

    /// <summary>Círculo lleno (para indicadores y viñetas).</summary>
    public static Texture2D Punto()
    {
        return Desde("punto", () =>
        {
            Lienzo l = new Lienzo(48);
            l.Circulo(0.5f, 0.5f, 0.46f, Color.white);
            return l.Textura("Punto");
        });
    }

    /// <summary>Visto bueno (✔) blanco.</summary>
    public static Texture2D Check()
    {
        return Desde("check", () =>
        {
            Lienzo l = new Lienzo(64);
            l.Borde(0.06f, new Color(0f, 0f, 0f, 0.5f));
            l.Trazo(0.20f, 0.50f, 0.42f, 0.28f, 0.14f, Color.white);
            l.Trazo(0.42f, 0.28f, 0.80f, 0.74f, 0.14f, Color.white);
            return l.Textura("Check");
        });
    }

    /// <summary>Equis (✖) blanca.</summary>
    public static Texture2D Equis()
    {
        return Desde("equis", () =>
        {
            Lienzo l = new Lienzo(64);
            l.Borde(0.06f, new Color(0f, 0f, 0f, 0.5f));
            l.Trazo(0.24f, 0.24f, 0.76f, 0.76f, 0.14f, Color.white);
            l.Trazo(0.24f, 0.76f, 0.76f, 0.24f, 0.14f, Color.white);
            return l.Textura("Equis");
        });
    }

    /// <summary>Engranaje (botón de Opciones).</summary>
    public static Texture2D Engranaje()
    {
        return Desde("engranaje", () =>
        {
            Lienzo l = new Lienzo(64);
            Color c = new Color(0.80f, 0.86f, 0.94f);
            l.Borde(0.045f, new Color(0f, 0f, 0f, 0.55f));
            for (int i = 0; i < 8; i++)
                l.Caja(0.5f, 0.5f, 0.07f, 0.44f, 0.02f, c, i * 22.5f);
            l.Circulo(0.5f, 0.5f, 0.33f, c);
            l.SinBorde();
            l.Circulo(0.5f, 0.5f, 0.14f, new Color(0.14f, 0.18f, 0.24f));
            return l.Textura("Engranaje");
        });
    }

    /// <summary>Candado (insignias bloqueadas).</summary>
    public static Texture2D Candado()
    {
        return Desde("candado", () =>
        {
            Lienzo l = new Lienzo(64);
            l.Borde(0.05f, new Color(0f, 0f, 0f, 0.6f));
            l.Anillo(0.5f, 0.60f, 0.16f, 0.08f, new Color(0.80f, 0.82f, 0.86f));
            l.Caja(0.5f, 0.38f, 0.26f, 0.20f, 0.05f, new Color(0.95f, 0.78f, 0.25f));
            l.SinBorde();
            l.Circulo(0.5f, 0.40f, 0.05f, new Color(0.35f, 0.25f, 0.08f));
            l.Trazo(0.5f, 0.40f, 0.5f, 0.28f, 0.05f, new Color(0.35f, 0.25f, 0.08f));
            return l.Textura("Candado");
        });
    }
}
