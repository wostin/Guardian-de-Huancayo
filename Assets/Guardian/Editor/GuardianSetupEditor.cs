#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Guardián de Huancayo - Herramienta de editor (SEGURA y REVERSIBLE).
/// Agrega un menú "Tools > Guardián de Huancayo" para colocar un personaje real
/// del pack CityPeople como jugador, ya conectado (CharacterController + PlayerController),
/// con GameManager y basura de prueba.
///
/// SEGURIDAD: solo AÑADE objetos a la escena y todo queda con Undo (Ctrl+Z).
/// No borra, no sobreescribe ni modifica tus assets ni tus prefabs originales.
///
/// USO: menú superior de Unity -> Tools -> Guardián de Huancayo -> (elige personaje).
/// Después pon ACTIVO = false en GuardianAutoSetup.cs para que no salga la cápsula.
/// </summary>
public static class GuardianSetupEditor
{
    private const string BASE = "Assets/DenysAlmaral/CityPeople/Prefabs/";
    private const string TBASE = "Assets/Hash Game studios/Trash Bag & Trash Box/Resources/Prefabs/";

    // Residuos reales agrupados por TIPO, según el código de colores de la
    // NTP 900.058-2019 (0 plástico, 1 vidrio, 2 papel/cartón, 3 metal, 4 orgánico).
    private static readonly string[][] PREFABS_POR_TIPO = new string[][]
    {
        // 0 · PLÁSTICO (contenedor BLANCO)
        new string[] {
            TBASE + "Garbage/Bottles/2/Bottle 2.prefab",
            TBASE + "Garbage/Bottles/2/Bottle 2 dirt.prefab",
            TBASE + "Garbage/Bottles/2/Bottle 2 crumple.prefab",
            TBASE + "Garbage/Gallon/Gallon 2 plastic.prefab",
            TBASE + "Garbage/Gallon/Gallon 20L.prefab",
        },
        // 1 · VIDRIO (contenedor VERDE)
        new string[] {
            TBASE + "Garbage/Bottles/1/Bottle 1 green glass.prefab",
            TBASE + "Garbage/Bottles/1/Bottle 1 red glass.prefab",
            TBASE + "Garbage/Bottles/1/Bottle 1 blue glass.prefab",
            TBASE + "Garbage/Bottles/1/Bottle 1 glass dirt.prefab",
        },
        // 2 · PAPEL Y CARTÓN (contenedor AZUL)
        new string[] {
            TBASE + "Garbage/Pizza box/Pizza box 1.prefab",
            TBASE + "Garbage/Pizza box/Pizza box 2.prefab",
            TBASE + "Garbage/Pizza box/Pizza box crumple 1.prefab",
            TBASE + "Recycle cardboard/Recycle cardboard folded 1.prefab",
            TBASE + "Recycle cardboard/Crumpled cardboard Closed Type 1.prefab",
        },
        // 3 · METALES (contenedor AMARILLO)
        new string[] {
            TBASE + "Garbage/Canned/Soda/Soda can 1 blue.prefab",
            TBASE + "Garbage/Canned/Soda/Soda can 1 red.prefab",
            TBASE + "Garbage/Canned/Soda/Soda can 1 orange dirt.prefab",
            TBASE + "Garbage/Canned/Food/Canned food 1.prefab",
            TBASE + "Garbage/Canned/Food/Canned food 2 crumple.prefab",
            TBASE + "Garbage/Gallon/Gallon 2 metal.prefab",
        },
        // 4 · ORGÁNICOS (contenedor MARRÓN) - restos del mercado
        new string[] {
            TBASE + "Garbage/Froots/Orange mold.prefab",
            TBASE + "Garbage/Froots/Orange.prefab",
            "Assets/FREE Food Pack/Prefabs/Banana.prefab",
            "Assets/FREE Food Pack/Prefabs/Salad.prefab",
            "Assets/FREE Food Pack/Prefabs/Onion.prefab",
        },
    };

    // Un contenedor por tipo. Se repintan con el color exacto de la norma.
    private static readonly string[] PREFABS_BOTE_TIPO = {
        TBASE + "Recycle bin/Recycle 1 bin green.prefab",   // plástico  (se pinta BLANCO)
        TBASE + "Recycle bin/Recycle 1 bin green.prefab",   // vidrio    (VERDE)
        TBASE + "Recycle bin/Recycle 1 bin blue.prefab",    // papel     (AZUL)
        TBASE + "Recycle bin/Recycle 1 bin yellow.prefab",  // metal     (AMARILLO)
        TBASE + "Recycle bin/Recycle 1 bin red.prefab",     // orgánico  (se pinta MARRÓN)
    };

    // Colores oficiales de la NTP 900.058-2019 (los mismos que usa el HUD).
    private static readonly Color[] COLOR_NTP = {
        new Color(0.94f, 0.94f, 0.94f),   // BLANCO   · plástico
        new Color(0.20f, 0.72f, 0.33f),   // VERDE    · vidrio
        new Color(0.18f, 0.48f, 0.85f),   // AZUL     · papel y cartón
        new Color(0.96f, 0.78f, 0.13f),   // AMARILLO · metales
        new Color(0.55f, 0.36f, 0.20f),   // MARRÓN   · orgánicos
    };

    private static readonly string[] NOMBRE_NTP = {
        "PLASTICO", "VIDRIO", "PAPEL Y CARTON", "METALES", "ORGANICOS"
    };

    // Carros del pack SimplePoly usados como enemigos contaminantes.
    private const string VBASE = "Assets/SimplePoly City - Low Poly Assets/Prefab/Vehicles/Vehicle with Static Wheels/";
    // Antes solo se usaban cuatro: el mismo auto rojo y el mismo SUV daban la
    // vuelta a la ciudad una y otra vez. El pack trae veintidos vehiculos y no
    // costaba nada aprovecharlos: con taxi, combi grande, camion y ambulancia el
    // trafico deja de verse copiado y pegado.
    private static readonly string[] PREFABS_CARROS = {
        VBASE + "Vehicle_Car_color01.prefab",
        VBASE + "Vehicle_Taxi.prefab",
        VBASE + "Vehicle_Car_color02.prefab",
        VBASE + "Vehicle_Pick up Truck_color01.prefab",
        VBASE + "Vehicle_SUV_color01.prefab",
        VBASE + "Vehicle_Car_color03.prefab",
        VBASE + "Vehicle_Truck_color01.prefab",
        VBASE + "Vehicle_Pick up Truck_color02.prefab",
        VBASE + "Vehicle_SUV_color02.prefab",
        VBASE + "Vehicle_Bus_color01.prefab",
        VBASE + "Vehicle_Truck_color02.prefab",
        VBASE + "Vehicle_SUV_color03.prefab",
        VBASE + "Vehicle_Pick up Truck_color03.prefab",
        VBASE + "Vehicle_Car_color01.prefab",
        VBASE + "Vehicle_Bus_color02.prefab",
        VBASE + "Vehicle_Taxi.prefab",
    };

    /// <summary>Camion de basura de la municipalidad, tomado del pack.</summary>
    private static readonly string[] PREFABS_CAMION = {
        VBASE + "Vehicle_Container_color01.prefab",
        VBASE + "Vehicle_Container_color02.prefab",
        VBASE + "Vehicle_Container_color03.prefab",
        VBASE + "Vehicle_Truck_color03.prefab",
    };

    // Peatones NPC del asset npc_casual_set_00.
    private const string NBASE = "Assets/npc_casual_set_00/Prefabs/";
    private static readonly string[] PREFABS_NPC = {
        NBASE + "npc_csl_00_character_01f_01.prefab",
        NBASE + "npc_csl_00_character_01m_01.prefab",
        NBASE + "npc_csl_00_character_02f_02.prefab",
        NBASE + "npc_csl_00_character_02m_02.prefab",
        NBASE + "npc_csl_00_character_01f_03.prefab",
        NBASE + "npc_csl_00_character_01m_03.prefab",
    };

    // Comida real del pack "FREE Food Pack" para los puestos del mercado.
    private const string FBASE = "Assets/FREE Food Pack/Prefabs/";
    private static readonly string[] PREFABS_COMIDA = {
        FBASE + "Tomato.prefab",  FBASE + "Onion.prefab",     FBASE + "Pepper.prefab",
        FBASE + "Chili.prefab",   FBASE + "SweetPepper.prefab", FBASE + "Avocado.prefab",
        FBASE + "Banana.prefab",  FBASE + "Pineapple.prefab", FBASE + "Watermelon.prefab",
        FBASE + "Bread.prefab",   FBASE + "Mushroom.prefab",  FBASE + "Salad.prefab",
        FBASE + "Egg.prefab",     FBASE + "Coconut.prefab",
    };

    // Vegetación del pack "Lowpoly Forest Pack" para la ribera del Shullcas.
    private const string VEGBASE = "Assets/Lowpoly Forest Pack/Prefabs/";
    private static readonly string[] PREFABS_ARBOLES = {
        VEGBASE + "Tree01.prefab", VEGBASE + "Tree02.prefab", VEGBASE + "Tree03.prefab",
        VEGBASE + "Tree04.prefab", VEGBASE + "Tree05.prefab", VEGBASE + "Tree06.prefab",
    };
    private static readonly string[] PREFABS_ARBUSTOS = {
        VEGBASE + "Bush01.prefab", VEGBASE + "Bush02.prefab",
        VEGBASE + "Bush03.prefab", VEGBASE + "Bush04.prefab",
    };
    private static readonly string[] PREFABS_ROCAS = {
        VEGBASE + "Rock01.prefab", VEGBASE + "Rock02.prefab", VEGBASE + "Rock03.prefab",
    };

    /// <summary>
    /// Convierte a URP los materiales de los packs antiguos. Sin esto, un shader
    /// hecho para Built-in (como el del pack de comida) sale magenta o negro.
    /// Conserva la textura y el color originales.
    /// </summary>
    private static void ArreglarMaterialesURP(GameObject go)
    {
        if (go == null) return;
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null) return;
        AsegurarCarpetas();

        bool algo = false;
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;
                if (m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline")) continue;

                Texture tex = null;
                if (m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");
                if (tex == null && m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");

                string limpio = m.name.Replace("/", "_").Replace(" ", "_").Replace(":", "_");
                string ruta = "Assets/Guardian/Materiales/URP_" + limpio + ".mat";

                Material nuevo = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (nuevo == null)
                {
                    nuevo = new Material(urp);
                    AssetDatabase.CreateAsset(nuevo, ruta);
                }

                Color c = Color.white;
                if (m.HasProperty("_Color")) c = m.GetColor("_Color");
                else if (m.HasProperty("_BaseColor")) c = m.GetColor("_BaseColor");
                if (c.a < 0.2f) c.a = 1f;

                nuevo.color = c;
                if (nuevo.HasProperty("_BaseColor")) nuevo.SetColor("_BaseColor", c);
                if (nuevo.HasProperty("_Smoothness")) nuevo.SetFloat("_Smoothness", 0.18f);
                if (tex != null)
                {
                    if (nuevo.HasProperty("_BaseMap")) nuevo.SetTexture("_BaseMap", tex);
                    if (nuevo.HasProperty("_MainTex")) nuevo.SetTexture("_MainTex", tex);
                }
                EditorUtility.SetDirty(nuevo);
                mats[i] = nuevo;
                algo = true;
            }
            r.sharedMaterials = mats;
        }
        if (algo) AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Instancia un prefab como hijo y lo escala a un tamaño objetivo, porque
    /// cada pack viene en su propia escala.
    /// </summary>
    private static GameObject PiezaPrefab(GameObject padre, string ruta, Vector3 pos,
                                          float tamObjetivo, Vector3 rot, string nombre)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) return null;

        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        g.name = nombre;
        g.transform.SetParent(padre != null ? padre.transform : null, false);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = rot;
        g.transform.localScale = Vector3.one;

        foreach (Collider c in g.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        ArreglarMaterialesURP(g);   // los packs viejos traen shaders que URP no entiende

        if (tamObjetivo > 0f)
        {
            Renderer[] rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (mayor > 0.0001f) g.transform.localScale = Vector3.one * (tamObjetivo / mayor);
            }
        }
        return g;
    }

    /// <summary>
    /// Apoya un objeto en un punto del mundo POR LA BASE REAL DE SU MODELO.
    /// No usa Renderer.bounds (en el editor puede estar sin refrescar): calcula la
    /// caja a partir de las mallas y de los transforms, que siempre están al día.
    /// </summary>
    /// <summary>Caja envolvente real de un objeto, en mundo (incluye a sus hijos).</summary>
    private static Bounds CajaDe(GameObject g)
    {
        Bounds b = new Bounds(g.transform.position, Vector3.zero);
        bool hay = false;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static void ApoyarEn(GameObject g, Vector3 punto, float hundir)
    {
        if (g == null) return;

        Bounds caja = new Bounds();
        bool hay = false;

        foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>())
        {
            if (mf == null || mf.sharedMesh == null) continue;
            Bounds mb = mf.sharedMesh.bounds;
            for (int e = 0; e < 8; e++)
            {
                Vector3 esq = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (e & 1) == 0 ? -1f : 1f,
                    (e & 2) == 0 ? -1f : 1f,
                    (e & 4) == 0 ? -1f : 1f));
                Vector3 pl = g.transform.InverseTransformPoint(mf.transform.TransformPoint(esq));
                if (!hay) { caja = new Bounds(pl, Vector3.zero); hay = true; }
                else caja.Encapsulate(pl);
            }
        }

        if (!hay) { g.transform.position = punto; return; }

        Vector3 pie = g.transform.TransformPoint(new Vector3(caja.center.x, caja.min.y, caja.center.z));
        Vector3 ajuste = g.transform.position - pie;          // del pie del modelo al pivote
        g.transform.position = punto + ajuste - Vector3.up * hundir;
    }

    // Recolecta las posiciones de los tramos de carretera de la ciudad.
    private static List<Vector3> RecolectarVias()
    {
        List<Vector3> vias = new List<Vector3>();
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            string n = t.name;
            if (n.StartsWith("Road Lane") || n.StartsWith("Road Intersection")
                || n.StartsWith("Road Corner") || n.StartsWith("Road T"))
                vias.Add(t.position);
        }
        return vias;
    }

    // ============================================================
    //  GEOMETRÍA REAL DE LA CIUDAD  (para no poner nada en la pista)
    //
    //  Medido sobre la propia escena del pack SimplePoly City:
    //   · la ciudad es una cuadrícula de 20 u y cada tramo mide 20 x 20;
    //   · sus 126 postes de luz, 25 hidrantes y 43 semáforos están TODOS
    //     a 7.5 u del eje de la calle  ->  el asfalto llega hasta ~6.5
    //     y la vereda va de 6.5 a 10.
    //  La vereda no es un objeto aparte: viene dentro del prefab de la calle,
    //  por eso hay que calcularla.
    // ============================================================

    private const float TRAMO         = 20f;   // lado de un tramo de vía
    private const float MEDIO_TRAMO   = 10f;
    private const float MEDIA_CALZADA = 7.0f;  // hasta aquí es pista (asfalto)
    private const float VEREDA        = 8.3f;  // centro de la vereda

    private struct Tramo
    {
        public Vector3 c;      // centro del tramo
        public bool ejeX;      // la calle corre en X
        public bool ejeZ;      // la calle corre en Z
    }

    /// <summary>Deduce la orientación de cada tramo mirando a sus vecinos a 20 u.</summary>
    private static List<Tramo> AnalizarVias(List<Vector3> vias)
    {
        List<Tramo> r = new List<Tramo>(vias.Count);
        for (int i = 0; i < vias.Count; i++)
        {
            Tramo t = new Tramo();
            t.c = vias[i]; t.ejeX = false; t.ejeZ = false;
            for (int j = 0; j < vias.Count; j++)
            {
                if (i == j) continue;
                float dx = vias[j].x - vias[i].x;
                float dz = vias[j].z - vias[i].z;
                if (Mathf.Abs(dz) < 4f && Mathf.Abs(Mathf.Abs(dx) - TRAMO) < 6f) t.ejeX = true;
                if (Mathf.Abs(dx) < 4f && Mathf.Abs(Mathf.Abs(dz) - TRAMO) < 6f) t.ejeZ = true;
            }
            if (!t.ejeX && !t.ejeZ) { t.ejeX = true; t.ejeZ = true; } // suelto: ser prudente
            r.Add(t);
        }
        return r;
    }

    /// <summary>¿Este punto cae sobre el asfalto? (entonces NO se pone nada ahí)</summary>
    private static bool SobreLaPista(Vector3 p, List<Tramo> tramos)
    {
        for (int i = 0; i < tramos.Count; i++)
        {
            float dx = Mathf.Abs(p.x - tramos[i].c.x);
            float dz = Mathf.Abs(p.z - tramos[i].c.z);
            if (dx > MEDIO_TRAMO || dz > MEDIO_TRAMO) continue;
            if (tramos[i].ejeX && dz < MEDIA_CALZADA) return true;
            if (tramos[i].ejeZ && dx < MEDIA_CALZADA) return true;
        }
        return false;
    }

    /// <summary>
    /// Puntos sobre la VEREDA alrededor de un centro, del más cercano al más lejano.
    /// En 'haciaCalle' devuelve, para cada punto, la dirección en la que está la
    /// pista, para que lo que se coloque ahí quede mirando a la calle.
    /// </summary>
    private static List<Vector3> PuntosDeVereda(List<Tramo> tramos, Vector3 centro,
                                                float radio, List<Vector3> haciaCalle)
    {
        List<Vector3> pts = new List<Vector3>();
        List<Vector3> dirs = new List<Vector3>();
        float r2 = radio * radio;

        for (int i = 0; i < tramos.Count; i++)
        {
            Vector3 c = tramos[i].c;
            Vector3 d = new Vector3(c.x - centro.x, 0f, c.z - centro.z);
            if (d.sqrMagnitude > r2) continue;

            bool soloX = tramos[i].ejeX && !tramos[i].ejeZ;
            bool soloZ = tramos[i].ejeZ && !tramos[i].ejeX;

            for (int s = -1; s <= 1; s += 2)
            {
                if (soloX)
                {
                    for (float a = -10f; a <= 10.01f; a += 5f)
                    { pts.Add(new Vector3(c.x + a, 0f, c.z + s * VEREDA)); dirs.Add(new Vector3(0f, 0f, -s)); }
                }
                else if (soloZ)
                {
                    for (float a = -10f; a <= 10.01f; a += 5f)
                    { pts.Add(new Vector3(c.x + s * VEREDA, 0f, c.z + a)); dirs.Add(new Vector3(-s, 0f, 0f)); }
                }
                else
                {
                    // cruce: solo las 4 esquinas son vereda
                    for (int s2 = -1; s2 <= 1; s2 += 2)
                    { pts.Add(new Vector3(c.x + s * VEREDA, 0f, c.z + s2 * VEREDA)); dirs.Add(new Vector3(-s, 0f, 0f)); }
                }
            }
        }

        // Quitar los que igual caigan en pista (tramos vecinos) y ordenar por cercanía.
        for (int i = pts.Count - 1; i >= 0; i--)
            if (SobreLaPista(pts[i], tramos)) { pts.RemoveAt(i); dirs.RemoveAt(i); }

        for (int i = 0; i < pts.Count; i++)
            for (int j = i + 1; j < pts.Count; j++)
                if ((pts[j] - centro).sqrMagnitude < (pts[i] - centro).sqrMagnitude)
                {
                    Vector3 tp = pts[i]; pts[i] = pts[j]; pts[j] = tp;
                    Vector3 td = dirs[i]; dirs[i] = dirs[j]; dirs[j] = td;
                }

        if (haciaCalle != null) { haciaCalle.Clear(); haciaCalle.AddRange(dirs); }
        return pts;
    }

    /// <summary>
    /// Sitios que ya ocupan cosas nuestras en esa zona (puestos, contenedores,
    /// letreros, iglesia), para no encimarlos. Se lee de la escena, así siempre
    /// está al día aunque se llame a una sola herramienta del menú.
    /// </summary>
    private static List<Vector3> OcupadosDeZona(int z)
    {
        List<Vector3> l = new List<Vector3>();
        string sufijo = "_z" + z + "_";
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            if (t == null) continue;
            string n = t.name;
            if ((n.StartsWith("PuestoMercado") || n.StartsWith("ContenedorReciclaje")
                 || n.StartsWith("VendedorPuesto") || n.StartsWith("MuralWanka")
                 || n.StartsWith("PanelODS") || n.StartsWith("TricicloReciclador")
                 || n.StartsWith("PuntoAcopio")
                 || n.StartsWith("CarretillaEmoliente")) && n.Contains(sufijo)) l.Add(t.position);
            else if (n.StartsWith("Letrero_") || n.StartsWith("Iglesia_")
                     || n.StartsWith("Inicio_z")) l.Add(t.position);
        }
        return l;
    }

    /// <summary>
    /// Igual que TomarVereda pero va aflojando la separación exigida hasta
    /// encontrar sitio. Para los objetos grandes de la plaza, que si no se
    /// quedaban sin colocar porque la vereda ya estaba ocupada.
    /// </summary>
    private static int TomarVeredaSuave(List<Vector3> pts, List<Vector3> dirs, List<Vector3> usados,
                                        Vector3 centro, float angulo, float distMin, float distMax,
                                        float separacion)
    {
        float[] sep = { separacion, separacion * 0.7f, separacion * 0.45f, 3f };
        for (int i = 0; i < sep.Length; i++)
        {
            int k = TomarVereda(pts, dirs, usados, centro, angulo, distMin, distMax, sep[i]);
            if (k >= 0) return k;
        }
        return TomarVereda(pts, dirs, usados, centro, -1f, 6f, distMax + 24f, 2.5f);
    }

    /// <summary>¿Está este punto lo bastante lejos de lo que ya colocamos?</summary>
    private static bool Separado(Vector3 p, List<Vector3> usados, float minDist)
    {
        float m2 = minDist * minDist;
        for (int i = 0; i < usados.Count; i++)
        {
            Vector3 d = usados[i] - p; d.y = 0f;
            if (d.sqrMagnitude < m2) return false;
        }
        return true;
    }

    /// <summary>
    /// Toma el primer punto de vereda libre (opcionalmente el más cercano a una
    /// dirección dada desde el centro) y lo marca como usado.
    /// </summary>
    private static int TomarVereda(List<Vector3> pts, List<Vector3> dirs, List<Vector3> usados,
                                   Vector3 centro, float anguloObjetivo, float distMin, float distMax,
                                   float separacion)
    {
        int mejor = -1;
        float mejorPuntaje = float.MaxValue;
        Vector3 objetivo = new Vector3(Mathf.Cos(anguloObjetivo), 0f, Mathf.Sin(anguloObjetivo));

        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 d = pts[i] - centro; d.y = 0f;
            float dist = d.magnitude;
            if (dist < distMin || dist > distMax) continue;
            if (!Separado(pts[i], usados, separacion)) continue;
            if (!LugarLibre(pts[i], 6f)) continue;

            float desvio = anguloObjetivo >= 0f
                ? Vector3.Angle(d.normalized, objetivo)
                : 0f;
            float puntaje = desvio * 0.6f + dist * 0.35f;
            if (puntaje < mejorPuntaje) { mejorPuntaje = puntaje; mejor = i; }
        }
        if (mejor >= 0) usados.Add(pts[mejor]);
        return mejor;
    }

    // Altura del suelo real en (x,z) usando raycast; devuelve fallbackY si no hay suelo.
    private static float SueloY(float x, float z, float fallbackY)
    {
        bool qht = Physics.queriesHitTriggers;
        Physics.queriesHitTriggers = false;
        float y = fallbackY;
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(x, 40f, z), Vector3.down, out hit, 80f) && hit.point.y < 4f)
            y = hit.point.y;
        Physics.queriesHitTriggers = qht;
        return y;
    }

    // 3 centros de zona bien separados, tomados de la red de vías (determinista).
    private static Vector3[] ElegirZonas(List<Vector3> vias, Vector3 centro)
    {
        Vector3[] z = new Vector3[3];
        if (vias.Count < 3)
        {
            z[0] = centro;
            z[1] = centro + new Vector3(35f, 0f, 0f);
            z[2] = centro + new Vector3(0f, 0f, 35f);
            return z;
        }
        // Antes se tomaban los tramos MÁS LEJANOS entre sí y las zonas caían en las
        // esquinas del mapa (los carros terminaban fuera del asfalto). Ahora se eligen
        // tres puntos repartidos DENTRO de la ciudad, encajados en la vía más cercana.
        Vector3 min = vias[0], max = vias[0];
        foreach (Vector3 v in vias) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }

        Vector2[] rel = {
            new Vector2(0.30f, 0.35f),   // Centro
            new Vector2(0.70f, 0.45f),   // Mercado
            // El rio corre pasado el borde de z MENOR (CrearRio lo pone en
            // min.z - 32), asi que la zona de la Ribera tiene que estar en ese
            // extremo. Estaba en 0.75, o sea en la punta OPUESTA de la ciudad:
            // el nivel se llamaba "Ribera del Shullcas" y el rio no se veia.
            new Vector2(0.45f, 0.08f),   // Ribera (pegada al Shullcas)
        };

        for (int i = 0; i < 3; i++)
        {
            Vector3 objetivo = new Vector3(
                Mathf.Lerp(min.x, max.x, rel[i].x), 0f,
                Mathf.Lerp(min.z, max.z, rel[i].y));

            Vector3 mejor = vias[0]; float d = float.MaxValue;
            foreach (Vector3 v in vias)
            {
                float dd = (new Vector3(v.x, 0f, v.z) - objetivo).sqrMagnitude;
                if (dd < d) { d = dd; mejor = v; }
            }
            z[i] = mejor;
        }
        return z;
    }

    [MenuItem("Tools/Guardián de Huancayo/★ ARMAR JUEGO COMPLETO", false, 0)]
    private static void ArmarJuegoCompleto()
    {
        Vector3 centro = Vector3.zero;
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null) centro = sv.pivot;
        centro.y = 0f;

        // 1) Personaje (si no hay uno)
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador == null)
            jugador = InstanciarJugador(BASE + "worker_Male_constructor_B.prefab", centro);
        if (jugador != null) centro = jugador.transform.position;

        // Asegurar animación de caminar también en un jugador ya existente.
        if (jugador != null)
        {
            RuntimeAnimatorController ctrl = CargarControlador(false);
            Animator an = jugador.GetComponentInChildren<Animator>();
            if (an != null && ctrl != null)
            {
                Undo.RecordObject(an, "Animator caminar");
                an.runtimeAnimatorController = ctrl;
            }
        }

        // 2) Piso, basura y botes
        CrearPiso(centro);
        SubirJugador(jugador);
        GenerarBasura(12, centro);
        GenerarBotes(centro);
        GenerarContaminantes(centro);
        GenerarNPCs(centro);
        GenerarPolicias(centro);
        CrearRio(centro);
        CrearCerros(centro);
        DecorarCerros();
        GenerarPuestosMercado(centro);
        CrearIglesia(centro);
        DetallesEducativos(centro);
        PuntosDeAcopio(centro);
        CrearLetreros(centro);
        DetallesDeBarrio(centro);
        CrearPuntosDeInicio(centro);
        CieloAndino();
        VidaEnLaCalle(centro);
        PonerColisionadores();
        ActivarSemaforos();

        // Vecinos que ensucian: la basura no se acaba sola.
        List<TrashItem> reserva = CrearReservaBasura(8);
        int ensucian = RepartirEnsuciadores(reserva, 3);

        // Bicicletas en la vereda: movilidad sostenible y menos caminata.
        int bicis = ColocarBicicletas(centro);

        // Musica y efectos del proyecto (Assets/Sounds).
        int clipsAudio = AudioDelProyecto();

        // Colisiones que falten y Static para que rinda mejor.
        int colNuevos, staticNuevos;
        ReforzarCiudad(out colNuevos, out staticNuevos);

        // Ultima pasada: bajar al piso lo que haya quedado en el aire. Va al
        // final a proposito, cuando ya estan puestos el rio, los cerros y todos
        // los adornos, porque el piso de un adorno puede ser otro adorno.
        int apoyados = ApoyarLoQueFlota();

        // Reajustar dificultad en el GameManager (aunque ya existiera de antes).
        GameManager gm = Object.FindObjectOfType<GameManager>();
        if (gm != null)
        {
            Undo.RecordObject(gm, "Configurar dificultad");
            gm.contaminacionInicial = 5f;
            gm.subeContaminacionBase = 0.30f;
            gm.capacidadCarga = 6;        // cabe un residuo de cada tipo + uno extra
            gm.tiempoLimiteBase = 210f;   // hay que caminar hasta el contenedor correcto
            gm.maxExtrasPorNivel = 4;     // tope de basura que botan los vecinos

            // Ambiente sonoro del valle (viento + rumor de calle).
            if (gm.GetComponent<AmbienteCiudad>() == null)
                Undo.AddComponent<AmbienteCiudad>(gm.gameObject);

            // La ciudad se ensucia a la vista cuando sube la contaminación.
            if (gm.GetComponent<ClimaContaminacion>() == null)
                Undo.AddComponent<ClimaContaminacion>(gm.gameObject);

            // Regla del cruce peatonal (necesita el mapa de la calzada).
            if (gm.GetComponent<SeguridadVial>() == null)
                Undo.AddComponent<SeguridadVial>(gm.gameObject);

            BakearRedVial(gm);
        }

        // Dejar la jerarquía limpia y agrupada.
        int movidos = OrdenarEscena();

        if (jugador != null)
        {
            Selection.activeGameObject = jugador;
            EditorGUIUtility.PingObject(jugador);
        }

        Debug.Log("[Guardian] ARMAR terminado · " + apoyados + " objetos bajados al piso · "
                + colNuevos + " colisiones nuevas · " + clipsAudio + " clips de audio");

        EditorUtility.DisplayDialog("Guardián de Huancayo",
            "¡Juego armado y ordenado!\n\n" +
            "• Guardián con control WASD\n" +
            "• 30 residuos de 5 tipos repartidos en 3 zonas\n" +
            "• SEGREGACIÓN NTP 900.058-2019: 5 contenedores por zona\n" +
            "  (blanco plástico · verde vidrio · azul papel · amarillo metal\n" +
            "   · marrón orgánico), cada uno con su cartel\n" +
            "• CONTENEDORES NUEVOS: cuerpo, tapa, ruedas, franja reflectiva,\n" +
            "  PICTOGRAMA del residuo y círculo pintado en la vereda; ya son\n" +
            "  sólidos (no se atraviesan)\n" +
            "• Panel del ODS 11 en la plaza con el código de colores\n" +
            "• Triciclo de reciclador RECORRIENDO la vereda, empujado por su dueño\n" +
            "• Regla de cruce peatonal: cruzar con el semáforo en verde para los\n" +
            "  autos avisa y cuesta puntos (F10 muestra los FPS)\n" +
            "• " + ensucian + " vecinos que botan basura mientras juegas\n" +
            "• PUNTO DE ACOPIO municipal por zona: al dejar la zona limpia hay\n" +
            "  que ir hasta ahí a cerrar la jornada (+50 puntos)\n" +
            "• La ciudad SE ENSUCIA A LA VISTA: neblina parda, sol apagado y\n" +
            "  río turbio cuando sube la contaminación; se aclara al reciclar\n" +
            "• Semáforos de 3 luces con ámbar; los carros frenan y tocan bocina\n" +
            "• Palomas en el atrio, campana de la plaza y un perro por zona\n" +
            "• BASURA FLOTANDO en el Shullcas: se ve más cuando la ciudad\n" +
            "  está sucia y menos cuando segregas bien\n" +
            "• MINIMAPA en el HUD para ubicar los contenedores\n" +
            "• " + bicis + " BICICLETAS en la vereda: pulsa E para subir (va mucho más\n" +
            "  rápido) y T para el timbre · movilidad sostenible del ODS 11\n" +
            "• " + colNuevos + " colisionadores nuevos y " + staticNuevos + " objetos marcados Static\n" +
            "• AUDIO del proyecto enganchado: " + clipsAudio + " clips (música por zona,\n" +
            "  pasos, salto y fanfarria), comprimidos para que no inflen el .exe\n" +
            "• CAMIÓN RECOLECTOR de la municipalidad por zona: pasa junto a los\n" +
            "  contenedores y se lleva lo segregado al punto de acopio\n" +
            "• MODO RECORRIDO en el menú: toda la ciudad sin tiempo, y la tecla C\n" +
            "  ensucia/limpia el valle para mostrar el efecto en vivo\n" +
            "• Zona 0 peatonal · Zonas 1 y 2 con tráfico\n" +
            "• Tránsito peruano: mototaxis, combis y carros SÓLIDOS\n" +
            "  (ya no se pueden atravesar)\n" +
            "• Todo sobre la VEREDA, nada en medio de la pista\n" +
            "• 8 puestos con toldo, banderines, precios y vendedor\n" +
            "• Mural wanka y carretilla de emoliente en la vereda\n" +
            "• Peatones y policías que caminan POR LA VEREDA\n" +
            "• Árboles, postes y bancas ya son sólidos\n" +
            "• Cerros con la cumbre nevada y neblina del valle\n" +
            "• Iglesia en la Plaza Constitución\n" +
            "• 12 peatones que caminan de verdad (con animación)\n" +
            "• Río Shullcas fuera de la pista, con ribera arbolada\n" +
            "• Cerros del valle con rocas y pinos en la ladera\n" +
            "• Jerarquía agrupada (" + movidos + " objetos)\n\n" +
            "Presiona PLAY: recoge basura y llévala a un contenedor.\n" +
            "Todo es reversible con Ctrl+Z.", "¡A jugar!");
    }

    /// <summary>
    /// Guarda en la escena el mapa de la calzada (centro y orientación de cada
    /// tramo de vía) para que en tiempo de juego se pueda saber si el Guardián
    /// está parado sobre el asfalto. Es el mismo cálculo que usa la herramienta
    /// para no poner nada sobre la pista, pero disponible en Play.
    /// </summary>
    private static void BakearRedVial(GameManager gm)
    {
        if (gm == null) return;

        RedVial rv = gm.GetComponent<RedVial>();
        if (rv == null) rv = Undo.AddComponent<RedVial>(gm.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);

        Undo.RecordObject(rv, "Guardar red vial");
        rv.centros = new List<Vector3>(tramos.Count);
        rv.ejes = new List<int>(tramos.Count);
        for (int i = 0; i < tramos.Count; i++)
        {
            rv.centros.Add(tramos[i].c);
            rv.ejes.Add((tramos[i].ejeX ? 1 : 0) | (tramos[i].ejeZ ? 2 : 0));
        }
        rv.medioTramo = MEDIO_TRAMO;
        rv.mediaCalzada = MEDIA_CALZADA;
        EditorUtility.SetDirty(rv);
    }

    [MenuItem("Tools/Guardián de Huancayo/Crear Guardián - Obrero (recomendado)")]
    private static void CrearObrero() => CrearJugador(BASE + "worker_Male_constructor_B.prefab");

    [MenuItem("Tools/Guardián de Huancayo/Crear Guardián - Casual Hombre")]
    private static void CrearCasualM() => CrearJugador(BASE + "city/casual_Male_G.prefab");

    [MenuItem("Tools/Guardián de Huancayo/Crear Guardián - Casual Mujer")]
    private static void CrearCasualF() => CrearJugador(BASE + "city/casual_Female_G.prefab");

    [MenuItem("Tools/Guardián de Huancayo/Crear Guardián - Policía")]
    private static void CrearPolicia() => CrearJugador(BASE + "professions/police_Female_A.prefab");

    [MenuItem("Tools/Guardián de Huancayo/Paisaje: río Shullcas + cerros del valle", false, 3)]
    private static void PaisajeMenu()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        Vector3 c = (p != null) ? p.transform.position : Vector3.zero;
        CrearRio(c);
        CrearCerros(c);
        int adornos = DecorarCerros();
        CrearLetreros(c);
        CieloAndino();
        OrdenarEscena();
        EditorUtility.DisplayDialog("Guardián de Huancayo",
            "Paisaje de Huancayo actualizado:\n\n" +
            "• Río Shullcas FUERA de la ciudad, con ribera y agua que corre.\n" +
            "• 20 cerros del valle del Mantaro, con pasto abajo, roca arriba\n" +
            "  y cumbre nevada.  Adornos colocados en las laderas: " + adornos + "\n" +
            "• Cielo de altura (aire delgado, azul profundo) y sol de tarde.\n" +
            "• Letreros: Plaza Constitución, Mercado Mayorista, Ribera del\n" +
            "  Shullcas y tiendas del barrio.\n\n" +
            "Todo es reversible con Ctrl+Z.", "Listo");
    }

    // ============================================================
    //  COMPILAR EL JUEGO (.exe)
    //  Se hace por código para no depender de los diálogos de Unity.
    // ============================================================

    [MenuItem("Tools/Guardián de Huancayo/🏗 Compilar el juego (.exe)", false, 6)]
    private static void CompilarJuego()
    {
        UnityEngine.SceneManagement.Scene esc = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(esc.path))
        {
            EditorUtility.DisplayDialog("Guardián de Huancayo",
                "Primero guarda la escena (Ctrl+S) y vuelve a intentar.", "Ok");
            return;
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(esc);

        PlayerSettings.productName = "Guardian de Huancayo";
        PlayerSettings.companyName = "Universidad Continental";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        // La escena del juego tiene que estar en la lista de compilación.
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] {
            new EditorBuildSettingsScene(esc.path, true)
        };

        string raiz = System.IO.Directory.GetCurrentDirectory();
        string carpeta = System.IO.Path.Combine(raiz, "Build_GuardianHuancayo");
        System.IO.Directory.CreateDirectory(carpeta);

        BuildPlayerOptions op = new BuildPlayerOptions();
        op.scenes = new string[] { esc.path };
        op.locationPathName = System.IO.Path.Combine(carpeta, "GuardianDeHuancayo.exe");
        op.target = BuildTarget.StandaloneWindows64;
        op.targetGroup = BuildTargetGroup.Standalone;
        op.options = BuildOptions.None;

        UnityEditor.Build.Reporting.BuildReport rep = BuildPipeline.BuildPlayer(op);
        UnityEditor.Build.Reporting.BuildSummary sum = rep.summary;

        if (sum.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            int borradas = LimpiarCarpetasDeDepuracion(carpeta);
            EscribirLeeme(carpeta);
            long bytes = TamanoCarpeta(carpeta);

            EditorUtility.DisplayDialog("Guardián de Huancayo",
                "¡Juego compilado!\n\n" +
                "Carpeta: Build_GuardianHuancayo\n" +
                "Ejecutable: GuardianDeHuancayo.exe\n\n" +
                "Peso de la entrega: " + (bytes / (1024 * 1024)) + " MB\n" +
                "Tiempo: " + sum.totalTime.ToString(@"mm\:ss") + "\n" +
                (borradas > 0 ? "Carpetas de depuración borradas: " + borradas + "\n" : "") +
                "Se agregó LEEME.txt con las instrucciones.\n\n" +
                "Para entregarlo, comprime TODA la carpeta Build_GuardianHuancayo\n" +
                "(el .exe solo no funciona sin la carpeta _Data).", "Listo");
        }
        else
        {
            EditorUtility.DisplayDialog("Guardián de Huancayo",
                "La compilación falló (" + sum.result + ").\n" +
                "Errores: " + sum.totalErrors + "\n\n" +
                "Revisa la consola de Unity para ver el detalle.", "Ok");
        }
    }

    /// <summary>Peso real de una carpeta en disco (lo que va a pesar el ZIP de la entrega).</summary>
    private static long TamanoCarpeta(string ruta)
    {
        long total = 0;
        try
        {
            foreach (string f in System.IO.Directory.GetFiles(ruta, "*", System.IO.SearchOption.AllDirectories))
                total += new System.IO.FileInfo(f).Length;
        }
        catch { }
        return total;
    }

    /// <summary>
    /// Unity deja una carpeta "..._BurstDebugInformation_DoNotShip" al compilar.
    /// No sirve para jugar y pesa: se borra antes de entregar.
    /// </summary>
    private static int LimpiarCarpetasDeDepuracion(string carpeta)
    {
        int n = 0;
        try
        {
            foreach (string d in System.IO.Directory.GetDirectories(carpeta))
                if (d.EndsWith("_DoNotShip") || d.EndsWith("_BackUpThisFolder_ButDontShipItWithYourGame"))
                {
                    System.IO.Directory.Delete(d, true);
                    n++;
                }
        }
        catch { }
        return n;
    }

    /// <summary>Instrucciones para quien reciba la carpeta compilada.</summary>
    private static void EscribirLeeme(string carpeta)
    {
        string txt =
            "GUARDIAN DE HUANCAYO\r\n" +
            "Videojuego educativo sobre el ODS 11 (Ciudades y Comunidades Sostenibles)\r\n" +
            "Residuos solidos y contaminacion del rio Shullcas - Huancayo, Peru\r\n" +
            "Universidad Continental - Desarrollo de Videojuegos\r\n" +
            "\r\n" +
            "COMO JUGARLO\r\n" +
            "  1. Descomprime TODA la carpeta (el .exe solo no arranca).\r\n" +
            "  2. Abre GuardianDeHuancayo.exe\r\n" +
            "  3. Si Windows muestra el aviso de SmartScreen: Mas informacion > Ejecutar de todas formas.\r\n" +
            "\r\n" +
            "CONTROLES\r\n" +
            "  WASD / flechas ... caminar\r\n" +
            "  Mouse ........... girar la camara\r\n" +
            "  Shift ........... correr\r\n" +
            "  Espacio ......... saltar\r\n" +
            "  P / Esc ......... pausa (muestra controles, tabla de colores y volumen)\r\n" +
            "  M ............... silenciar / volver a activar el audio\r\n" +
            "  F9 .............. ocultar el HUD (para tomar capturas limpias)\r\n" +
            "  F10 ............. contador de FPS\r\n" +
            "\r\n" +
            "SI VA LENTO\r\n" +
            "  En el menu de inicio, pon la calidad en Bajo o Medio: baja sombras,\r\n" +
            "  antialiasing y distancia de dibujado sin cambiar nada del juego.\r\n" +
            "  Al lado esta el control de volumen, y se guarda para la proxima vez.\r\n" +
            "\r\n" +
            "SI SOLO QUIERES VER LA CIUDAD\r\n" +
            "  Boton MODO RECORRIDO del menu: se pasea sin tiempo, sin perder vidas\r\n" +
            "  y sin que suba la contaminacion. Ahi la tecla C ensucia o limpia la\r\n" +
            "  ciudad de golpe, para ver como cambian la neblina, el sol y el rio.\r\n" +
            "\r\n" +
            "OBJETIVO\r\n" +
            "  Recoge los residuos de la calle y llevalos AL CONTENEDOR DE SU COLOR,\r\n" +
            "  segun la Norma Tecnica Peruana NTP 900.058-2019:\r\n" +
            "    BLANCO   - plastico (botellas, envases, bolsas)\r\n" +
            "    VERDE    - vidrio (botellas, frascos)\r\n" +
            "    AZUL     - papel y carton\r\n" +
            "    AMARILLO - metales (latas, chatarra)\r\n" +
            "    MARRON   - organicos (restos de comida, cascaras)\r\n" +
            "  Si te equivocas de contenedor no acepta el residuo y te dice cual es el correcto.\r\n" +
            "  Ojo: algunos vecinos siguen botando basura a la vereda mientras juegas.\r\n" +
            "\r\n" +
            "TODO EL CONTENIDO GENERADO PARA ESTE TRABAJO (texturas, sonidos, mallas)\r\n" +
            "ES PROPIO O DE PAQUETES CON LICENCIA DE USO LIBRE.\r\n";
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(carpeta, "LEEME.txt"), txt,
                                        System.Text.Encoding.UTF8);
        }
        catch { }
    }

    /// <summary>
    /// Deja la entrega lista en un solo archivo .zip (es lo que se sube al aula
    /// virtual: el .exe suelto no sirve sin la carpeta _Data).
    /// </summary>
    [MenuItem("Tools/Guardián de Huancayo/🗜 Comprimir la entrega (.zip)", false, 9)]
    private static void ComprimirEntrega()
    {
        string raiz = System.IO.Directory.GetCurrentDirectory();
        string carpeta = System.IO.Path.Combine(raiz, "Build_GuardianHuancayo");

        if (!System.IO.Directory.Exists(carpeta))
        {
            EditorUtility.DisplayDialog("Guardián de Huancayo",
                "Todavía no hay nada compilado.\n\n" +
                "Usa primero “🏗 Compilar el juego (.exe)”.", "Ok");
            return;
        }

        string zip = System.IO.Path.Combine(raiz, "GuardianDeHuancayo_Entrega.zip");
        try
        {
            EditorUtility.DisplayProgressBar("Guardián de Huancayo",
                "Comprimiendo la entrega... puede tardar un par de minutos.", 0.4f);
            if (System.IO.File.Exists(zip)) System.IO.File.Delete(zip);
            System.IO.Compression.ZipFile.CreateFromDirectory(
                carpeta, zip, System.IO.Compression.CompressionLevel.Optimal, true);
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Guardián de Huancayo",
                "No se pudo comprimir:\n" + e.Message, "Ok");
            return;
        }
        EditorUtility.ClearProgressBar();

        long b = new System.IO.FileInfo(zip).Length;
        EditorUtility.DisplayDialog("Guardián de Huancayo",
            "¡Entrega lista!\n\n" +
            "Archivo: GuardianDeHuancayo_Entrega.zip\n" +
            "Peso: " + (b / (1024 * 1024)) + " MB\n\n" +
            "Está en la carpeta del proyecto, al lado de Assets.\n" +
            "Ese es el archivo que subes al aula virtual.", "Listo");
    }

    /// <summary>Resumen del proyecto para el informe del curso.</summary>
    [MenuItem("Tools/Guardián de Huancayo/ℹ Datos del proyecto (para el informe)", false, 8)]
    private static void DatosDelProyecto()
    {
        int basura = Object.FindObjectsOfType<TrashItem>().Length;
        int botes = Object.FindObjectsOfType<RecycleBin>(true).Length;
        int peatones = Object.FindObjectsOfType<Peaton>().Length;
        int policias = Object.FindObjectsOfType<Aliado>().Length;
        int carros = Object.FindObjectsOfType<Contaminante>().Length;
        int ensucian = Object.FindObjectsOfType<Ensuciador>().Length;

        int objetos = 0;
        foreach (Transform t in Object.FindObjectsOfType<Transform>()) { if (t != null) objetos++; }

        int scripts = 0;
        foreach (string g in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Guardian" })) { scripts++; }

        string raiz = System.IO.Directory.GetCurrentDirectory();
        string carpeta = System.IO.Path.Combine(raiz, "Build_GuardianHuancayo");
        string peso = System.IO.Directory.Exists(carpeta)
            ? (TamanoCarpeta(carpeta) / (1024 * 1024)) + " MB"
            : "todavía no compilado";

        string texto =
            "GUARDIÁN DE HUANCAYO · datos para el informe\n\n" +
            "Motor: Unity " + Application.unityVersion + " (URP)\n" +
            "Escena: " + SceneManager.GetActiveScene().name + "\n" +
            "Objetos en la escena: " + objetos + "\n" +
            "Scripts propios: " + scripts + "\n\n" +
            "Residuos recolectables: " + basura + "\n" +
            "Contenedores NTP 900.058-2019: " + botes + "\n" +
            "Peatones: " + peatones + "   ·   Policías aliados: " + policias + "\n" +
            "Vehículos contaminantes: " + carros + "\n" +
            "Vecinos que ensucian: " + ensucian + "\n\n" +
            "Peso de la carpeta compilada: " + peso;

        Debug.Log(texto);
        EditorUtility.DisplayDialog("Guardián de Huancayo", texto, "Copiar al Log y cerrar");
    }

    [MenuItem("Tools/Guardián de Huancayo/📦 Aligerar texturas (achica el .exe)", false, 7)]
    private static void AligerarTexturas()
    {
        const int MAX = 1024;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D");
        int cambiadas = 0, revisadas = 0;

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(ruta)) continue;
                if (ruta.StartsWith("Assets/Guardian")) continue;      // las nuestras son chicas
                if (!ruta.StartsWith("Assets/")) continue;             // no tocar paquetes

                TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
                if (ti == null) continue;
                revisadas++;

                bool toca = false;
                if (ti.maxTextureSize > MAX) { ti.maxTextureSize = MAX; toca = true; }
                if (ti.textureCompression != TextureImporterCompression.Compressed)
                { ti.textureCompression = TextureImporterCompression.Compressed; toca = true; }

                if (!toca) continue;
                EditorUtility.SetDirty(ti);
                ti.SaveAndReimport();
                cambiadas++;

                if (i % 25 == 0)
                    EditorUtility.DisplayProgressBar("Aligerando texturas",
                        cambiadas + " ajustadas...", (float)i / Mathf.Max(1, guids.Length));
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Guardián de Huancayo",
            "Texturas revisadas: " + revisadas + "\n" +
            "Ajustadas a " + MAX + " px y comprimidas: " + cambiadas + "\n\n" +
            "Vuelve a compilar el .exe: debería pesar bastante menos.\n" +
            "Es reversible desde el Inspector de cada textura.", "Listo");
    }

    // ============================================================
    //  FOTOS DEL ESCENARIO
    //  Renderiza el escenario a PNG sin tocar la cámara del juego.
    //  Sirve para el informe del curso (y para revisar cómo quedó).
    // ============================================================

    [MenuItem("Tools/Guardián de Huancayo/📷 Fotos del escenario (informe)", false, 5)]
    private static void FotosDelEscenario()
    {
        AsegurarCarpetas();
        if (!AssetDatabase.IsValidFolder("Assets/Guardian/Imagenes/Capturas"))
            AssetDatabase.CreateFolder("Assets/Guardian/Imagenes", "Capturas");

        // Borrar las capturas anteriores (y los duplicados "-1" que deja Windows)
        // para que la carpeta del informe quede con una sola versión de cada una.
        foreach (string g in AssetDatabase.FindAssets("t:Texture2D",
                 new string[] { "Assets/Guardian/Imagenes/Capturas" }))
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(g));

        int W = 1280, H = 720;
        int n = 0;

        // En Play solo circulan los vehículos de la zona activa; los demás quedan
        // con los renderers apagados. Para las fotos se encienden todos y al
        // terminar se deja cada uno como estaba.
        List<Renderer> apagados = new List<Renderer>();
        foreach (Contaminante veh in Object.FindObjectsOfType<Contaminante>())
        {
            if (veh == null) continue;
            foreach (Renderer rr in veh.GetComponentsInChildren<Renderer>(true))
                if (rr != null && !rr.enabled) { rr.enabled = true; apagados.Add(rr); }
        }

        Transform ig = BuscarPorNombre("Iglesia_PlazaConstitucion");
        if (ig != null) { FotoFrente("01_iglesia", ig, 40f, 14f, 8f, W, H); n++; }

        Transform pu = BuscarPorNombre("PuestoMercado_z1_0");
        if (pu != null) { FotoFrente("02_mercado", pu, 14f, 6f, 2f, W, H); n++; }

        Transform bo = BuscarPorNombre("ContenedorReciclaje_z0_VIDRIO");
        if (bo == null) bo = BuscarPorNombre("ContenedorReciclaje_z1_VIDRIO");
        if (bo != null) { FotoFrente("03_contenedor", bo, 6.5f, 2.9f, 1.3f, W, H); n++; }

        Transform rio = BuscarPorNombre("Rio_Shullcas");
        if (rio != null)
        {
            // Desde la orilla y a lo largo del cauce: así se ve el agua y lo que baja.
            Vector3 cr = rio.position;
            Capturar("04_rio_shullcas", cr + new Vector3(-24f, 4.6f, -14f),
                                        cr + new Vector3( 12f, 0.5f,   1f), W, H);
            n++;
        }

        Vector3 c, tam;
        if (LimitesCiudad(out c, out tam))
        {
            float d = Mathf.Max(tam.x, tam.z);
            FotoAngulo("05_valle", c, d * 1.05f, d * 0.45f, 215f, W, H); n++;
        }

        Transform pan = BuscarPorNombre("PanelODS_z0_0");
        if (pan != null) { FotoOblicua("06_panel_ODS11", pan, 10f, 4.4f, 3.1f, 0.38f, W, H); n++; }

        Transform tri = BuscarPorNombre("TricicloReciclador_z0_0");
        if (tri == null) tri = BuscarPorNombre("TricicloReciclador_z1_0");
        if (tri != null) { FotoTresCuartos("07_triciclo", tri, 7.5f, 3.0f, 1.2f, W, H); n++; }

        Transform ac = BuscarPorNombre("PuntoAcopio_z0_0");
        if (ac == null) ac = BuscarPorNombre("PuntoAcopio_z1_0");
        if (ac == null) ac = BuscarPorNombre("PuntoAcopio_z2_0");
        if (ac != null) { FotoOblicua("08_acopio", ac, 11f, 4.0f, 1.9f, 0.34f, W, H); n++; }

        Transform pal = BuscarPorNombre("Paloma_0");
        if (pal != null) { FotoAngulo("09_plaza", pal.position, 13f, 5.5f, 40f, W, H); n++; }

        Transform cam = BuscarPorNombre("CamionRecolector_z1_0");
        if (cam == null) cam = BuscarPorNombre("CamionRecolector_z2_0");
        if (cam != null) { FotoTresCuartos("10_camion", cam, 15f, 4.8f, 1.8f, W, H); n++; }

        for (int i = 0; i < apagados.Count; i++)
            if (apagados[i] != null) apagados[i].enabled = false;

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Guardián de Huancayo",
            n + " fotos guardadas en:\nAssets/Guardian/Imagenes/Capturas\n\n" +
            "Se pueden usar tal cual en el informe del curso.\n" +
            "Truco: entra a PLAY y recién ahí toma las fotos, así los personajes\n" +
            "salen animados y no en pose T.", "Listo");
    }

    private static Transform BuscarPorNombre(string nombre)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name == nombre) return t;
        return null;
    }

    /// <summary>Foto de frente al objeto (por donde mira su cara principal).</summary>
    private static void FotoFrente(string nombre, Transform obj, float dist, float alto,
                                   float mirarAlto, int w, int h)
    {
        Vector3 f = obj.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        f.Normalize();
        Capturar(nombre, obj.position + f * dist + Vector3.up * alto,
                 obj.position + Vector3.up * mirarAlto, w, h);
    }

    /// <summary>Foto de frente pero corrida hacia un lado (esquiva postes y árboles).</summary>
    private static void FotoOblicua(string nombre, Transform obj, float dist, float alto,
                                    float mirarAlto, float corrimiento, int w, int h)
    {
        Vector3 f = obj.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.01f) f = Vector3.forward;
        f.Normalize();
        Vector3 lado = Vector3.Cross(Vector3.up, f);
        Vector3 d = (f + lado * corrimiento).normalized;
        Capturar(nombre, obj.position + d * dist + Vector3.up * alto,
                 obj.position + Vector3.up * mirarAlto, w, h);
    }

    /// <summary>Foto en tres cuartos desde ATRÁS del objeto (se le ve el volumen).</summary>
    private static void FotoTresCuartos(string nombre, Transform obj, float dist, float alto,
                                        float mirarAlto, int w, int h)
    {
        Vector3 d = -obj.forward * 1.0f + obj.right * 0.85f;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
        d.Normalize();
        Capturar(nombre, obj.position + d * dist + Vector3.up * alto,
                 obj.position + Vector3.up * mirarAlto, w, h);
    }

    /// <summary>Foto desde un ángulo dado alrededor de un punto.</summary>
    private static void FotoAngulo(string nombre, Vector3 objetivo, float dist, float alto,
                                   float grados, int w, int h)
    {
        Vector3 d = Quaternion.Euler(0f, grados, 0f) * Vector3.forward;
        Capturar(nombre, objetivo + d * dist + Vector3.up * alto,
                 objetivo + Vector3.up * 3f, w, h);
    }

    private static void Capturar(string nombre, Vector3 desde, Vector3 hacia, int w, int h)
    {
        GameObject go = new GameObject("__camFoto");
        go.transform.position = desde;
        go.transform.LookAt(hacia);

        Camera cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 55f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 3000f;
        cam.cullingMask = ~0;

        // URP necesita su componente de datos en la cámara.
        System.Type tipo = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (tipo != null && go.GetComponent(tipo) == null) go.AddComponent(tipo);

        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture antes = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0f, 0f, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = antes;
        cam.targetTexture = null;

        string ruta = "Assets/Guardian/Imagenes/Capturas/" + nombre + ".png";
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ruta),
            tex.EncodeToPNG());

        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
        rt.Release();
        Object.DestroyImmediate(rt);
        AssetDatabase.ImportAsset(ruta);
    }

    [MenuItem("Tools/Guardián de Huancayo/Regar 12 basuras")]
    private static void SoloBasura() => GenerarBasura(12, Vector3.zero);

    [MenuItem("Tools/Guardián de Huancayo/Crear contenedores de reciclaje")]
    private static void CrearBotesMenu()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        Vector3 c = (p != null) ? p.transform.position : Vector3.zero;
        GenerarBotes(c);
    }

    [MenuItem("Tools/Guardián de Huancayo/Crear piso de seguridad (invisible)")]
    private static void CrearPisoSeguridad()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        Vector3 c = (p != null) ? p.transform.position : Vector3.zero;
        CrearPiso(c);
        SubirJugador(p);
    }

    private static void CrearPiso(Vector3 centro)
    {
        BorrarPorNombre("Piso_Seguridad");
        GameObject viejo = GameObject.Find("Piso_Seguridad");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);

        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Plane);
        piso.name = "Piso_Seguridad";
        piso.transform.position = new Vector3(centro.x, 0f, centro.z);
        piso.transform.localScale = new Vector3(80f, 1f, 80f); // 800 x 800 (para no caerse)
        MeshRenderer mr = piso.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false; // invisible: solo colisionador
        Undo.RegisterCreatedObjectUndo(piso, "Crear piso de seguridad");
    }

    private static void SubirJugador(GameObject p)
    {
        if (p == null) return;
        Undo.RecordObject(p.transform, "Reubicar Guardián");
        Vector3 pos = p.transform.position;
        pos.y = 0.6f;
        p.transform.position = pos;
    }

    // ------------------------------------------------------------

    private static void CrearJugador(string ruta)
    {
        Vector3 pos = Vector3.zero;
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null) pos = sv.pivot;
        pos.y = 0.1f;

        GameObject jugador = InstanciarJugador(ruta, pos);
        if (jugador == null) return;

        AsegurarGameManager();
        Selection.activeGameObject = jugador;
        EditorGUIUtility.PingObject(jugador);

        if (Object.FindObjectOfType<TrashItem>() == null)
            GenerarBasura(12, jugador.transform.position);

        EditorUtility.DisplayDialog("Guardián de Huancayo",
            "¡Listo! Se colocó '" + jugador.name + "' como jugador.\n\n" +
            "Tip: usa '★ ARMAR JUEGO COMPLETO' para añadir piso, basura\n" +
            "y contenedores de una sola vez.\n\n" +
            "Todo es reversible con Ctrl+Z.", "Entendido");
    }

    private static GameObject InstanciarJugador(string ruta, Vector3 pos)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Guardián de Huancayo",
                "No se encontró el prefab:\n" + ruta, "OK");
            return null;
        }

        GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        jugador.name = "Guardian";
        jugador.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(jugador, "Crear Guardián");
        jugador.tag = "Player";

        // Desactivar IA de CityPeople para que no se mueva solo.
        foreach (MonoBehaviour mb in jugador.GetComponentsInChildren<MonoBehaviour>())
            if (mb != null && mb.GetType().Name == "CityPeople")
                mb.enabled = false;

        CharacterController cc = jugador.GetComponent<CharacterController>();
        if (cc == null) cc = jugador.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.height = 1.8f;
        cc.radius = 0.3f;

        if (jugador.GetComponent<PlayerController>() == null)
            jugador.AddComponent<PlayerController>();

        // Asignar un Animator con clips de caminar/idle (City M o City F) para que
        // el personaje camine en vez de deslizarse.
        RuntimeAnimatorController ctrl = CargarControlador(ruta.ToLower().Contains("female"));
        Animator an = jugador.GetComponentInChildren<Animator>();
        if (an != null && ctrl != null) an.runtimeAnimatorController = ctrl;

        return jugador;
    }

    private static RuntimeAnimatorController CargarControlador(bool female)
    {
        string nombre = female ? "City F Animator" : "City M Animator";
        string[] guids = AssetDatabase.FindAssets(nombre + " t:AnimatorController");
        foreach (string g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.Contains(nombre))
                return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(p);
        }
        string ruta = "Assets/DenysAlmaral/CityPeople/Animations/" + nombre + ".controller";
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ruta);
    }

    // ============================================================
    //  AUDIO DEL PROYECTO
    //  Los archivos de Assets/Sounds se enganchan solos: la musica por zona,
    //  los pasos y el salto del Guardian, y la fanfarria de victoria. Si algun
    //  archivo no esta, esa parte sigue usando el sonido generado por codigo,
    //  asi el juego nunca se queda mudo por un asset que falte.
    // ============================================================

    /// <summary>Busca un AudioClip en Assets/Sounds por nombre (sin extension).</summary>
    private static AudioClip BuscarSonido(string nombre)
    {
        return BuscarSonido(nombre, "Assets/Sounds");
    }

    /// <summary>Busca un AudioClip por nombre dentro de una carpeta del proyecto.</summary>
    private static AudioClip BuscarSonido(string nombre, string carpeta)
    {
        if (!AssetDatabase.IsValidFolder(carpeta)) carpeta = "Assets";
        string[] guids = AssetDatabase.FindAssets(nombre + " t:AudioClip", new[] { carpeta });
        for (int i = 0; i < guids.Length; i++)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (System.IO.Path.GetFileNameWithoutExtension(ruta).Equals(nombre,
                    System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }
        // Si no hay coincidencia exacta, vale la primera parecida.
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        return null;
    }

    /// <summary>
    /// Deja la musica en Vorbis y en streaming, y los efectos cortos listos para
    /// dispararse. Sin esto los WAV entran crudos a la compilacion: los temas del
    /// proyecto suman mas de 200 MB en WAV y comprimidos bajan a una fraccion.
    /// Solo reimporta si los ajustes cambian, porque reimportar es lento.
    /// </summary>
    private static bool AjustarImportacion(AudioClip clip, bool esMusica)
    {
        if (clip == null) return false;
        string ruta = AssetDatabase.GetAssetPath(clip);
        AudioImporter ai = AssetImporter.GetAtPath(ruta) as AudioImporter;
        if (ai == null) return false;

        AudioImporterSampleSettings st = ai.defaultSampleSettings;
        AudioClipLoadType carga = esMusica ? AudioClipLoadType.Streaming
                                           : AudioClipLoadType.DecompressOnLoad;
        AudioCompressionFormat formato = AudioCompressionFormat.Vorbis;
        float calidad = esMusica ? 0.45f : 0.70f;

        bool cambia = st.loadType != carga
                   || st.compressionFormat != formato
                   || Mathf.Abs(st.quality - calidad) > 0.01f;
        if (!cambia) return false;

        st.loadType = carga;
        st.compressionFormat = formato;
        st.quality = calidad;
        ai.defaultSampleSettings = st;
        ai.loadInBackground = esMusica;

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        return true;
    }

    /// <summary>Engancha musica y efectos, y devuelve cuantos clips encontro.</summary>
    /// <summary>
    /// Familia de sonidos: todos los clips cuyo nombre empieza con el prefijo.
    /// Sirve para los packs que vienen numerados (Footstep_Grass_01, _02, ...),
    /// donde lo que se quiere no es UN clip sino el grupo entero para ir
    /// alternando y que dos pasos seguidos no suenen calcados.
    /// </summary>
    private static AudioClip[] FamiliaDeSonidos(string prefijo, int tope)
    {
        return FamiliaDeSonidos(prefijo, tope, false);
    }

    private static AudioClip[] FamiliaDeSonidos(string prefijo, int tope, bool esMusica)
    {
        List<AudioClip> lista = new List<AudioClip>();
        string[] guids = AssetDatabase.FindAssets(prefijo + " t:AudioClip", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
            string nom = System.IO.Path.GetFileNameWithoutExtension(ruta);
            if (!nom.StartsWith(prefijo, System.StringComparison.OrdinalIgnoreCase)) continue;
            AudioClip c = AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            if (c != null) lista.Add(c);
            if (lista.Count >= tope) break;
        }
        lista.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
        // Una sola pasada de importacion y con el ajuste correcto: antes esto
        // marcaba todo como efecto corto y despues la musica se reimportaba otra
        // vez como musica, con lo que cada tema del pack de ambiente pasaba dos
        // veces por el importador. Se notaba: ARMAR se quedaba minutos importando.
        for (int i = 0; i < lista.Count; i++) AjustarImportacion(lista[i], esMusica);
        return lista.ToArray();
    }

    private static int AudioDelProyecto()
    {
        // ---------------- Musica ----------------
        // El proyecto trae mas temas de los que se estaban usando: quedaban
        // sueltos Boss_Battle, Underwater-World, Time_Cave, Overworld_Theme,
        // Cool-Adventure-Intro, Victory_Fanfare_Loop y todo el pack de ambiente.
        // Cada uno tiene ahora un momento propio del juego.
        AudioClip menu       = BuscarSonido("Title_Screen");
        AudioClip menu2      = BuscarSonido("Stratosphere_Looping");
        AudioClip zona0      = BuscarSonido("Town_Theme");                      // Centro
        AudioClip zona1      = BuscarSonido("Shuffliin-Through-Central-Park");  // Mercado
        AudioClip zona2      = BuscarSonido("Deep_Forest");                     // Ribera
        AudioClip rio        = BuscarSonido("Underwater-World");                // cerca del Shullcas
        AudioClip victoria   = BuscarSonido("Victory_Fanfare_Intro");
        AudioClip victoriaB  = BuscarSonido("Victory_Fanfare_Loop", "Assets");
        AudioClip derrota    = BuscarSonido("Time_Cave");
        AudioClip alertaIn   = BuscarSonido("Boss_Battle_Intro");               // ciudad al limite
        AudioClip alerta     = BuscarSonido("Boss_Battle_Loop");
        AudioClip apuro      = BuscarSonido("Escape");                          // ultimos segundos
        AudioClip stinger    = BuscarSonido("Cool-Adventure-Intro");            // zona nueva

        if (zona1 == null) zona1 = BuscarSonido("Overworld_Theme");
        if (zona2 == null) zona2 = BuscarSonido("Overworld_Theme");
        if (alerta == null) alerta = apuro;

        // Colchon de ambiente: cuerdas, maderas, piano y arpa del pack libre.
        List<AudioClip> amb = new List<AudioClip>();
        string[] pref = { "Strings", "Woodwinds", "Piano", "Harp" };
        for (int i = 0; i < pref.Length; i++)
        {
            AudioClip[] f = FamiliaDeSonidos(pref[i], 3, true);
            for (int k = 0; k < f.Length; k++) if (f[k] != null) amb.Add(f[k]);
        }

        int reimportados = 0;
        AudioClip[] musica = { menu, menu2, zona0, zona1, zona2, rio, victoria, victoriaB,
                               derrota, alertaIn, alerta, apuro, stinger };
        for (int i = 0; i < musica.Length; i++)
            if (AjustarImportacion(musica[i], true)) reimportados++;

        // ---------------- Pisadas por superficie ----------------
        // Pack The Sound Guild. Antes sonaban dos pasos iguales en todas partes.
        AudioClip[] pasVereda = FamiliaDeSonidos("Footstep_Shoe_On_Street", 9);
        if (pasVereda.Length == 0) pasVereda = FamiliaDeSonidos("Footstep_Shoe", 6);
        AudioClip[] pasSneak  = FamiliaDeSonidos("Footstep_Sneakers", 8);
        AudioClip[] pasPasto  = FamiliaDeSonidos("Foostep_Grass", 7);           // el pack trae este typo
        AudioClip[] pasPastoB = FamiliaDeSonidos("Footstep_Grass_Alternative", 7);
        AudioClip[] pasArena  = FamiliaDeSonidos("Footstep_Sand", 8);
        AudioClip[] pasMadera = FamiliaDeSonidos("Footstep_Wood", 5);
        AudioClip[] pasMetal  = FamiliaDeSonidos("Footstep_Metal", 7);
        AudioClip[] pasCaida  = FamiliaDeSonidos("Footstep_Deep", 7);

        // La vereda mezcla zapato de calle con zapatilla: mas variedad, menos
        // sensacion de bucle cuando el recorrido es largo.
        List<AudioClip> vereda = new List<AudioClip>(pasVereda);
        vereda.AddRange(pasSneak);
        List<AudioClip> pasto = new List<AudioClip>(pasPasto);
        pasto.AddRange(pasPastoB);

        AudioClip paso1 = BuscarSonido("paso1");
        AudioClip paso2 = BuscarSonido("paso2");
        AudioClip salto = BuscarSonido("salto");
        AudioClip[] efectos = { paso1, paso2, salto };
        for (int i = 0; i < efectos.Length; i++)
            if (AjustarImportacion(efectos[i], false)) reimportados++;

        // ---------------- Objeto de musica EN LA ESCENA ----------------
        GuardianMusic gmus = Object.FindObjectOfType<GuardianMusic>();
        if (gmus == null)
        {
            GameObject g = new GameObject("GuardianMusic");
            gmus = g.AddComponent<GuardianMusic>();
            Undo.RegisterCreatedObjectUndo(g, "Crear musica");
        }
        Undo.RecordObject(gmus, "Enganchar musica");
        gmus.temaMenu       = menu;
        gmus.temaMenu2      = menu2;
        gmus.temaZona0      = zona0;
        gmus.temaZona1      = zona1;
        gmus.temaZona2      = zona2;
        gmus.temaRio        = rio;
        gmus.fanfarria      = victoria;
        gmus.fanfarriaBucle = victoriaB;
        gmus.temaDerrota    = derrota;
        gmus.introAlerta    = alertaIn;
        gmus.temaAlerta     = alerta;
        gmus.temaApuro      = apuro;
        gmus.stingerNivel   = stinger;
        gmus.ambientes      = amb.ToArray();
        EditorUtility.SetDirty(gmus);

        // ---------------- Pasos y salto del jugador ----------------
        PlayerController pc = Object.FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            Undo.RecordObject(pc, "Enganchar pasos");
            List<AudioClip> ps = new List<AudioClip>();
            if (paso1 != null) ps.Add(paso1);
            if (paso2 != null) ps.Add(paso2);
            pc.clipsPaso    = ps.ToArray();
            pc.clipSalto    = salto;
            pc.pasosVereda  = vereda.ToArray();
            pc.pasosPasto   = pasto.ToArray();
            pc.pasosArena   = pasArena;
            pc.pasosMadera  = pasMadera;
            pc.pasosMetal   = pasMetal;
            pc.clipsCaida   = pasCaida;
            EditorUtility.SetDirty(pc);
        }

        // ---------------- Motor de los carros ----------------
        // El pack trae el ralenti y las subidas de vueltas por separado. Con el
        // ralenti solo, un carro a fondo seguia sonando a carro parado; ahora se
        // cruzan dos capas y ademas hay arranque.
        AudioClip motor    = BuscarSonido("idle", "Assets");
        if (motor == null) motor = BuscarSonido("low_on", "Assets");
        AudioClip acelera  = BuscarSonido("med_on", "Assets");
        if (acelera == null) acelera = BuscarSonido("high_on", "Assets");
        AudioClip arranque = BuscarSonido("startup", "Assets");
        AjustarImportacion(motor, false);
        AjustarImportacion(acelera, false);
        AjustarImportacion(arranque, false);

        int carros = 0;
        foreach (Contaminante co in Object.FindObjectsOfType<Contaminante>())
        {
            if (co == null) continue;
            Undo.RecordObject(co, "Enganchar motor");
            co.clipMotor    = motor;
            co.clipAcelera  = acelera;
            co.clipArranque = arranque;
            EditorUtility.SetDirty(co);
            carros++;
        }

        int hallados = 0;
        AudioClip[] todos = { menu, menu2, zona0, zona1, zona2, rio, victoria, victoriaB,
                              derrota, alertaIn, alerta, apuro, stinger,
                              paso1, paso2, salto, motor, acelera, arranque };
        for (int i = 0; i < todos.Length; i++) if (todos[i] != null) hallados++;
        hallados += amb.Count + vereda.Count + pasto.Count
                  + pasArena.Length + pasMadera.Length + pasMetal.Length + pasCaida.Length;

        Debug.Log("[Guardian] Audio enganchado \u00b7 " + hallados + " clips \u00b7 "
                + vereda.Count + " pisadas de vereda, " + pasto.Count + " de pasto, "
                + pasArena.Length + " de arena \u00b7 ambiente: " + amb.Count
                + " \u00b7 carros con motor: " + carros);
        return hallados;
    }

    private static void AsegurarGameManager()
    {
        if (Object.FindObjectOfType<GameManager>() == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
            Undo.RegisterCreatedObjectUndo(gm, "Crear GameManager");
        }
    }

    private static void GenerarBasura(int cantidad, Vector3 centro)
    {
        AsegurarGameManager();

        // Quitar basura anterior (cubos u otra) para no acumular.
        foreach (TrashItem viejo in Object.FindObjectsOfType<TrashItem>())
            Undo.DestroyObjectImmediate(viejo.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);
        int porZona = 10;                     // 2 de cada uno de los 5 tipos

        bool qht = Physics.queriesHitTriggers;
        Physics.queriesHitTriggers = false; // no chocar raycast contra otras basuras

        int idx = 0;
        for (int z = 0; z < zonas.Length; z++)
        {
            // La basura se acumula en la VEREDA, la cuneta y las esquinas, que es
            // donde se junta de verdad; no tirada en medio del carril.
            List<Vector3> dirsB = new List<Vector3>();
            List<Vector3> veredaB = PuntosDeVereda(tramos, zonas[z], 34f, dirsB);

            for (int k = 0; k < porZona; k++)
            {
                // Reparto parejo de los 5 tipos para que siempre haya de todo.
                int tipo = k % PREFABS_POR_TIPO.Length;
                GameObject prefab = PrefabDeTipo(tipo);

                GameObject b;
                if (prefab != null)
                    b = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                else
                {
                    b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    b.transform.localScale = Vector3.one * 0.6f;
                    Renderer rr = b.GetComponent<Renderer>();
                    if (rr != null) rr.sharedMaterial = MatColor("Residuo_" + tipo, COLOR_NTP[tipo], 0.1f);
                }
                b.name = "Basura_z" + z + "_" + idx;

                // Repartir por la vereda de la zona y apoyar sobre el suelo real.
                Vector3 pos;

                // ZONA 2 = LIMPIAR EL RIO. La mitad de sus residuos no van en la
                // vereda sino en la ORILLA del Shullcas, entre la ciudad y el
                // agua, que es donde termina de verdad lo que la ciudad no
                // recoge. Asi el tercer nivel es literalmente ir a limpiar el
                // rio, y no otra cuadra mas con otro nombre.
                bool enLaOrilla = (z == 2) && (k % 2 == 0);
                if (enLaOrilla)
                {
                    float zRio = EjeDelRio(26f);   // el mismo eje que usa CrearRio

                    float xMin = zonas[z].x - 55f;
                    float xMax = zonas[z].x + 55f;
                    pos = new Vector3(
                        Random.Range(xMin, xMax), 0f,
                        zRio + (Random.value < 0.5f ? Random.Range(14f, 26f)     // orilla ciudad
                                                    : Random.Range(-24f, -14f)));// orilla campo
                }
                else if (veredaB.Count > 0)
                {
                    int iv = (k * 7 + z * 3) % veredaB.Count;
                    Vector3 baseP = veredaB[iv];
                    Vector3 hacia = dirsB[iv];
                    Vector3 lado = Vector3.Cross(Vector3.up, hacia);
                    pos = baseP + lado * Random.Range(-2.2f, 2.2f)
                                + hacia * Random.Range(-0.7f, 1.1f);
                }
                else
                {
                    Vector2 r = Random.insideUnitCircle * 11f;
                    pos = new Vector3(zonas[z].x + r.x, 0f, zonas[z].z + r.y);
                }
                pos.y = 0.2f;

                RaycastHit hit;
                if (Physics.Raycast(new Vector3(pos.x, 40f, pos.z), Vector3.down, out hit, 80f)
                    && hit.point.y < 3f && hit.normal.y > 0.5f)
                    pos.y = hit.point.y + 0.15f;
                b.transform.position = pos;
                b.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                foreach (Collider viejoCol in b.GetComponentsInChildren<Collider>())
                    viejoCol.enabled = false;

                SphereCollider trig = b.AddComponent<SphereCollider>();
                trig.isTrigger = true;
                trig.radius = 0.9f;
                Rigidbody rb = b.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                ArreglarMaterialesURP(b);   // algunos packs traen shaders Built-in

                TrashItem ti = b.AddComponent<TrashItem>();
                ti.valor = 10;
                ti.zona = z;
                ti.tipo = (TipoResiduo)tipo;

                Undo.RegisterCreatedObjectUndo(b, "Crear basura");
                idx++;
            }
        }

        Physics.queriesHitTriggers = qht;
    }

    /// <summary>Primer prefab que exista de ese tipo de residuo (los packs varían).</summary>
    private static GameObject PrefabDeTipo(int tipo)
    {
        string[] lista = PREFABS_POR_TIPO[Mathf.Clamp(tipo, 0, PREFABS_POR_TIPO.Length - 1)];
        int inicio = Random.Range(0, lista.Length);
        for (int i = 0; i < lista.Length; i++)
        {
            GameObject g = AssetDatabase.LoadAssetAtPath<GameObject>(lista[(inicio + i) % lista.Length]);
            if (g != null) return g;
        }
        return null;
    }

    // Ordena los tramos de vía cercanos al centro de una zona en un CIRCUITO
    // continuo (cadena por vecino más cercano) para que el carro tenga una ruta
    // fija y predecible por las calles de esa zona.
    private static List<Vector3> RutaDeZona(List<Vector3> vias, Vector3 centro, float radio)
    {
        List<Vector3> cerca = new List<Vector3>();
        foreach (Vector3 v in vias)
            if ((v - centro).sqrMagnitude <= radio * radio) cerca.Add(v);

        if (cerca.Count < 4)
        {
            // Manzana de respaldo alrededor del centro de la zona.
            float r = 14f;
            return new List<Vector3> {
                centro + new Vector3( r, 0f,  r),
                centro + new Vector3(-r, 0f,  r),
                centro + new Vector3(-r, 0f, -r),
                centro + new Vector3( r, 0f, -r),
            };
        }

        // Recorrido que PREFIERE SEGUIR DERECHO y solo dobla en las esquinas,
        // como un carro real circulando por la avenida (antes zigzagueaba).
        List<Vector3> ruta = new List<Vector3>();
        bool[] usado = new bool[cerca.Count];

        int actual = 0; float dmin = float.MaxValue;
        for (int i = 0; i < cerca.Count; i++)
        {
            float d = (cerca[i] - centro).sqrMagnitude;
            if (d < dmin) { dmin = d; actual = i; }
        }
        usado[actual] = true; ruta.Add(cerca[actual]);
        Vector3 dir = Vector3.forward;

        for (int paso = 1; paso < cerca.Count; paso++)
        {
            int mejor = -1; float mejorPuntaje = -9999f;
            for (int j = 0; j < cerca.Count; j++)
            {
                if (usado[j]) continue;
                Vector3 d = cerca[j] - cerca[actual]; d.y = 0f;
                float dist = d.magnitude;
                if (dist < 0.5f || dist > 26f) continue;       // la cuadrícula real es de 20 u:
                                                               // con más salto cortaban por dentro
                                                               // de la manzana (atravesaban casas)
                float recto = Vector3.Dot(d / dist, dir);      // 1 = sigue derecho
                float puntaje = recto * 2f - dist * 0.05f;     // premia la recta
                if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = j; }
            }
            if (mejor < 0) break;

            Vector3 nd = cerca[mejor] - cerca[actual]; nd.y = 0f;
            if (nd.sqrMagnitude > 0.01f) dir = nd.normalized;
            usado[mejor] = true; actual = mejor; ruta.Add(cerca[mejor]);
        }

        if (ruta.Count < 3) return ruta;

        // Circular por el CARRIL DERECHO (en Perú se maneja por la derecha),
        // así no van por el medio de la pista ni se cruzan entre ellos.
        List<Vector3> carril = new List<Vector3>();
        for (int i = 0; i < ruta.Count; i++)
        {
            Vector3 a = ruta[i];
            Vector3 b = ruta[(i + 1) % ruta.Count];
            Vector3 d = b - a; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) { carril.Add(a); continue; }
            Vector3 der = Vector3.Cross(Vector3.up, d.normalized);
            carril.Add(a + der * 2.6f);
        }
        return carril;
    }

    private static void GenerarContaminantes(Vector3 centro)
    {
        foreach (Contaminante viejo in Object.FindObjectsOfType<Contaminante>())
            Undo.DestroyObjectImmediate(viejo.gameObject);

        List<Vector3> vias = RecolectarVias();
        Vector3[] zonas = ElegirZonas(vias, centro);

        // La zona 0 es PEATONAL (sin carros). Las zonas 1..n tienen tráfico:
        // 2 carros por zona que recorren una ruta fija de esa zona.
        int idx = 0;
        for (int z = 1; z < zonas.Length; z++)
        {
            List<Vector3> ruta = RutaDeZona(vias, zonas[z], 65f);

            for (int k = 0; k < 2; k++)
            {
                // Tránsito peruano: mototaxi, combi y carro del pack, alternados.
                int tipo = idx % 3;
                bool esMoto  = (tipo == 0);
                bool esCombi = (tipo == 1);
                Color[] coloresMoto = {
                    new Color(0.95f, 0.75f, 0.10f), new Color(0.15f, 0.45f, 0.80f),
                    new Color(0.85f, 0.25f, 0.15f), new Color(0.20f, 0.60f, 0.35f),
                };
                Color[] coloresCombi = {
                    new Color(0.85f, 0.18f, 0.18f), new Color(0.15f, 0.40f, 0.75f),
                    new Color(0.20f, 0.60f, 0.35f), new Color(0.95f, 0.65f, 0.10f),
                };

                string nombreV = "CarroContaminante_z" + z + "_" + idx;
                GameObject e;
                if (esMoto)
                {
                    e = CrearMototaxi(coloresMoto[idx % coloresMoto.Length], nombreV);
                }
                else if (esCombi)
                {
                    e = CrearCombi(coloresCombi[idx % coloresCombi.Length], nombreV);
                }
                else
                {
                    // El paso 7 es primo con 16: recorre la lista entera sin
                    // repetir y sin que dos carros vecinos salgan iguales.
                    string rp = PREFABS_CARROS[(idx * 7 + z * 3) % PREFABS_CARROS.Length];
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rp);
                    if (prefab != null)
                        e = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    else
                    {
                        e = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        e.transform.localScale = new Vector3(2f, 1f, 4f);
                    }
                    e.name = nombreV;
                }

                // Repartir los 2 carros en puntos opuestos de la ruta y, muy importante,
                // que arranquen MIRANDO hacia donde van (antes salían en ángulos al azar).
                int ini = (k * ruta.Count / 2) % ruta.Count;
                Vector3 p = ruta[ini];
                Vector3 sig = ruta[(ini + 1) % ruta.Count];
                Vector3 mira = sig - p; mira.y = 0f;

                float suelo = SueloY(p.x, p.z, p.y);
                e.transform.position = new Vector3(p.x, suelo + 0.05f, p.z);
                e.transform.rotation = (mira.sqrMagnitude > 0.01f)
                    ? Quaternion.LookRotation(mira) : Quaternion.identity;

                // Desactivar colisionadores propios (que no bloqueen) y usar un trigger de golpe.
                foreach (Collider viejoCol in e.GetComponentsInChildren<Collider>())
                    viejoCol.enabled = false;
                BoxCollider golpe = e.AddComponent<BoxCollider>();
                golpe.isTrigger = true;
                golpe.center = new Vector3(0f, 0.7f, 0f);
                golpe.size = esMoto  ? new Vector3(1.4f, 1.6f, 2.8f)
                           : esCombi ? new Vector3(2.1f, 2.0f, 5.2f)
                                     : new Vector3(2.2f, 1.6f, 4.5f);

                // Cuerpo SÓLIDO (hijo "Cuerpo"): el Guardián ya no atraviesa los carros.
                // Va aparte del trigger de golpe para no pelearse con él.
                GameObject cuerpo = new GameObject("Cuerpo");
                cuerpo.transform.SetParent(e.transform, false);
                cuerpo.transform.localPosition = Vector3.zero;
                BoxCollider solido = cuerpo.AddComponent<BoxCollider>();
                solido.isTrigger = false;
                solido.center = new Vector3(0f, 0.7f, 0f);
                solido.size = golpe.size * 0.80f;

                Rigidbody rb = e.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                Contaminante comp = e.AddComponent<Contaminante>();
                comp.velocidad = 3.5f + z * 0.6f;   // zonas más avanzadas => carros más rápidos
                comp.radio = 16f;
                comp.zona = z;
                comp.ruta = new List<Vector3>(ruta);

                // Humo negro: por algo se llaman contaminantes (el mototaxi de 2 tiempos, peor).
                HumoContaminante(e, esMoto ? -1.30f : esCombi ? -2.60f : -2.10f);

                Undo.RegisterCreatedObjectUndo(e, "Crear carro contaminante");
                idx++;
            }

            // --- Camión recolector de la municipalidad (uno por zona con tráfico) ---
            GameObject cam = CrearCamionRecolector("CamionRecolector_z" + z + "_0");

            int iniC = (ruta.Count / 3) % ruta.Count;
            Vector3 pc = ruta[iniC];
            Vector3 sigC = ruta[(iniC + 1) % ruta.Count];
            Vector3 miraC = sigC - pc; miraC.y = 0f;

            cam.transform.position = new Vector3(pc.x, SueloY(pc.x, pc.z, pc.y) + 0.05f, pc.z);
            cam.transform.rotation = (miraC.sqrMagnitude > 0.01f)
                ? Quaternion.LookRotation(miraC) : Quaternion.identity;

            BoxCollider golpeC = cam.AddComponent<BoxCollider>();
            golpeC.isTrigger = true;
            golpeC.center = new Vector3(0f, 1.25f, -0.2f);
            golpeC.size = new Vector3(2.4f, 2.7f, 7.2f);

            GameObject cuerpoC = new GameObject("Cuerpo");
            cuerpoC.transform.SetParent(cam.transform, false);
            cuerpoC.transform.localPosition = Vector3.zero;
            BoxCollider solidoC = cuerpoC.AddComponent<BoxCollider>();
            solidoC.isTrigger = false;
            solidoC.center = golpeC.center;
            solidoC.size = golpeC.size * 0.82f;

            Rigidbody rbC = cam.AddComponent<Rigidbody>();
            rbC.isKinematic = true;
            rbC.useGravity = false;

            Contaminante compC = cam.AddComponent<Contaminante>();
            compC.velocidad = 3.0f + z * 0.4f;     // va despacio, como todo camión de basura
            compC.radio = 16f;
            compC.zona = z;
            compC.ruta = new List<Vector3>(ruta);

            cam.AddComponent<CamionRecolector>();
            HumoContaminante(cam, -3.7f);

            Undo.RegisterCreatedObjectUndo(cam, "Crear camión recolector");
        }
    }

    private static void GenerarNPCs(Vector3 centro)
    {
        foreach (Peaton viejo in Object.FindObjectsOfType<Peaton>())
            Undo.DestroyObjectImmediate(viejo.gameObject);

        List<Vector3> vias = RecolectarVias();

        Vector3[] zonas = ElegirZonas(vias, centro);

        // Personajes de CityPeople: estos SÍ tienen clips de caminar.
        // (Los del pack npc_casual no traen Animator y se quedaban como estatuas.)
        List<GameObject> modelos = new List<GameObject>();
        foreach (string g in AssetDatabase.FindAssets("casual t:Prefab",
                 new string[] { "Assets/DenysAlmaral/CityPeople/Prefabs" }))
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
            if (pf != null) modelos.Add(pf);
        }

        RuntimeAnimatorController ctrlM = CargarControlador(false);
        RuntimeAnimatorController ctrlF = CargarControlador(true);

        // Los peatones caminan por la VEREDA, no por el medio de la pista.
        List<Tramo> tramos = AnalizarVias(vias);
        List<Vector3>[] veredas = new List<Vector3>[zonas.Length];
        for (int z = 0; z < zonas.Length; z++)
            veredas[z] = PuntosDeVereda(tramos, zonas[z], 30f, null);

        for (int i = 0; i < 12; i++)
        {
            GameObject npc;
            if (modelos.Count > 0)
            {
                npc = (GameObject)PrefabUtility.InstantiatePrefab(modelos[i % modelos.Count]);
            }
            else
            {
                string ruta = PREFABS_NPC[i % PREFABS_NPC.Length];
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null) continue;
                npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }

            bool mujer = npc.name.ToLower().Contains("female");
            npc.name = "Peaton_" + i;

            // Repartidos por las tres zonas, sobre la vereda.
            int zz = i % zonas.Length;
            Vector3 pz = zonas[zz];
            Vector3 baseP;
            if (veredas[zz] != null && veredas[zz].Count > 0)
                baseP = veredas[zz][(i * 5 + zz * 2) % veredas[zz].Count];
            else
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(10f, 16f);
                baseP = pz + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
            }

            float suelo = SueloY(baseP.x, baseP.z, 0f);
            npc.transform.position = new Vector3(baseP.x, suelo, baseP.z);
            npc.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // Apagar la IA propia del pack para que mande nuestro script.
            foreach (MonoBehaviour mb in npc.GetComponentsInChildren<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == "CityPeople") mb.enabled = false;

            Animator an = npc.GetComponentInChildren<Animator>();
            RuntimeAnimatorController ctrl = mujer ? ctrlF : ctrlM;
            if (an != null && ctrl != null) an.runtimeAnimatorController = ctrl;

            // Camina por la VEREDA siguiendo un recorrido, no en círculo al azar:
            // así ya no atraviesa casas ni se mete a la pista.
            Peaton pe = npc.AddComponent<Peaton>();
            if (veredas[zz] != null && veredas[zz].Count > 0)
                pe.ruta = RutaDeVereda(veredas[zz], npc.transform.position, 7);
            CuerpoDePersona(npc);

            Undo.RegisterCreatedObjectUndo(npc, "Crear peatón");
        }
    }

    private static void GenerarPolicias(Vector3 centro)
    {
        foreach (Aliado viejo in Object.FindObjectsOfType<Aliado>())
            Undo.DestroyObjectImmediate(viejo.gameObject);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE + "professions/police_Female_A.prefab");
        RuntimeAnimatorController ctrl = CargarControlador(true); // City F (con clips de caminar)
        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramosPol = AnalizarVias(vias);
        Vector3[] zonasPol = ElegirZonas(vias, centro);
        List<Vector3>[] veredasPol = new List<Vector3>[zonasPol.Length];
        for (int z = 0; z < zonasPol.Length; z++)
            veredasPol[z] = PuntosDeVereda(tramosPol, zonasPol[z], 26f, null);

        for (int i = 0; i < 3; i++)
        {
            GameObject pol;
            if (prefab != null) pol = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            else pol = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pol.name = "PoliciaAliado_" + i;

            // Los policías patrullan por la vereda de cada zona.
            Vector3 baseP;
            if (veredasPol[i % veredasPol.Length] != null && veredasPol[i % veredasPol.Length].Count > 0)
            {
                List<Vector3> vv = veredasPol[i % veredasPol.Length];
                baseP = vv[(i * 11 + 3) % vv.Count];
            }
            else if (vias.Count > 0)
            {
                Vector3 v = vias[Random.Range(0, vias.Count)];
                baseP = v + new Vector3(Random.Range(-9f, 9f), 0f, Random.Range(-9f, 9f));
            }
            else { Vector2 r = Random.insideUnitCircle * 15f; baseP = new Vector3(centro.x + r.x, 0f, centro.z + r.y); }

            float suelo = SueloY(baseP.x, baseP.z, 0f);
            pol.transform.position = new Vector3(baseP.x, suelo, baseP.z);

            foreach (MonoBehaviour mb in pol.GetComponentsInChildren<MonoBehaviour>())
                if (mb != null && mb.GetType().Name == "CityPeople") mb.enabled = false;

            Animator an = pol.GetComponentInChildren<Animator>();
            if (an != null && ctrl != null) an.runtimeAnimatorController = ctrl;

            Aliado al = pol.AddComponent<Aliado>();
            List<Vector3> vpol = veredasPol[i % veredasPol.Length];
            if (vpol != null && vpol.Count > 0)
                al.ruta = RutaDeVereda(vpol, pol.transform.position, 8);
            CuerpoDePersona(pol);

            Undo.RegisterCreatedObjectUndo(pol, "Crear policía aliado");
        }
    }

    // Límites de la ciudad según la red de calles (para no poner nada encima de la pista).
    private static bool LimitesCiudad(out Vector3 centro, out Vector3 tam)
    {
        List<Vector3> vias = RecolectarVias();
        centro = Vector3.zero; tam = Vector3.zero;
        if (vias.Count == 0) return false;

        Vector3 min = vias[0], max = vias[0];
        foreach (Vector3 v in vias)
        {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }
        centro = (min + max) * 0.5f; centro.y = 0f;
        tam = max - min;
        return true;
    }

    private static Material MaterialSimple(string ruta, Color color, float smooth, Texture2D mapa, Vector2 tiling)
    {
        AsegurarCarpetas();
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (mapa != null)
        {
            if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", mapa); m.SetTextureScale("_BaseMap", tiling); }
            if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", mapa); m.SetTextureScale("_MainTex", tiling); }
        }
        else
        {
            // LIMPIAR la textura, no dejar la anterior. Esto costo caro: las
            // texturas que genera esta herramienta son Texture2D de memoria, y
            // al recargar el dominio quedan destruidas. El material seguia
            // apuntando a esa textura muerta y la pintaba de gris, asi que el
            // rio Shullcas se veia como una pista de concreto por mas que se le
            // cambiara el color. Sin este else, pasar null no limpiaba nada.
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
        }
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    // ---- Texturas reales del pack "Free Stylized Textures" (Game Buffs) ----
    // 25 superficies con albedo, normal y mask. Hasta ahora todo el terreno del
    // juego era color plano o textura generada por codigo, y eso es justo lo que
    // hacia que el suelo se viera pobre al lado de los edificios del pack de
    // ciudad. Estas son fotograficas y estilizadas a la vez: pegan con SimplePoly.
    private const string TXBASE = "Assets/Game Buffs/Free Stylized Textures/Textures/";

    /// <summary>Mapa de un material del pack: "Albedo", "Normal" o "Mask".</summary>
    private static Texture2D MapaDelPack(string carpeta, string mapa)
    {
        string ruta = TXBASE + carpeta + "/" + carpeta + "_" + mapa + ".png";
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    /// <summary>
    /// Material URP con las texturas del pack. Se guarda como asset propio para
    /// no tocar los .mat del pack —que son suyos y conviene dejarlos intactos— y
    /// para poder darle a cada superficie su propio mosaico.
    /// </summary>
    private static Material MaterialDelPack(string nombreMat, string carpeta,
                                            Vector2 tiling, float smooth, Color tinte)
    {
        AsegurarCarpetas();
        Texture2D alb = MapaDelPack(carpeta, "Albedo");
        if (alb == null) return null;                  // el pack no esta: que el llamador decida

        string ruta = "Assets/Guardian/Materiales/" + nombreMat + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");

        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); }
        if (sh != null && m.shader != sh) m.shader = sh;

        if (m.HasProperty("_BaseMap"))   m.SetTexture("_BaseMap", alb);
        if (m.HasProperty("_MainTex"))   m.SetTexture("_MainTex", alb);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tinte);
        m.color = tinte;

        Texture2D nrm = MapaDelPack(carpeta, "Normal");
        if (nrm != null && m.HasProperty("_BumpMap"))
        {
            // El relieve es la mitad del efecto: sin el, la textura se ve pegada.
            TextureImporter tin = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(nrm)) as TextureImporter;
            if (tin != null && tin.textureType != TextureImporterType.NormalMap)
            {
                tin.textureType = TextureImporterType.NormalMap;
                tin.SaveAndReimport();
                nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(nrm));
            }
            m.SetTexture("_BumpMap", nrm);
            m.EnableKeyword("_NORMALMAP");
            if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", 1f);
        }

        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic", 0f);

        if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling);
        if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling);
        if (m.HasProperty("_BumpMap")) m.SetTextureScale("_BumpMap", tiling);

        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    /// <summary>Deja una textura legible para poder muestrearla con GetPixel.</summary>
    private static Texture2D Legible(Texture2D t)
    {
        if (t == null) return null;
        string ruta = AssetDatabase.GetAssetPath(t);
        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null && !ti.isReadable)
        {
            ti.isReadable = true;
            ti.SaveAndReimport();
            t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }
        return t;
    }

    /// <summary>
    /// Modelos de montana que haya en el proyecto, ordenados por preferencia.
    ///
    /// No se nombra ningun pack en concreto a proposito: se BUSCA por nombre. Asi
    /// basta con soltar un pack nuevo de montanas en Assets —autumn, alpine, lo
    /// que sea— y la proxima vez que se arme el juego los cerros salen con ese
    /// modelo, sin tocar una linea. Antes los cerros eran mallas generadas por
    /// codigo: cumplian, pero un cono con ruido nunca va a tener la silueta de
    /// una montana modelada a mano, y el valle del Mantaro se merece mas.
    /// </summary>
    private static List<string> PrefabsDeCerro()
    {
        // Cuanto mas arriba en la lista, mas prioridad. El otono va primero
        // porque es lo ultimo que se agrego al proyecto.
        string[] gusta = { "autumn", "mountain", "montana", "hill", "peak", "cliff", "alpine" };
        string[] veta   = { "rock_set", "rock0", "tree", "bush", "grass", "stone_small", "pebble" };

        List<string> rutas = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        for (int prio = 0; prio < gusta.Length; prio++)
            for (int i = 0; i < guids.Length; i++)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
                string baja = ruta.ToLowerInvariant();
                if (baja.IndexOf(gusta[prio], System.StringComparison.Ordinal) < 0) continue;

                bool malo = false;
                string archivo = System.IO.Path.GetFileNameWithoutExtension(baja);
                for (int v = 0; v < veta.Length; v++)
                    if (archivo.IndexOf(veta[v], System.StringComparison.Ordinal) >= 0) { malo = true; break; }
                if (malo) continue;

                // Tiene que tener malla de verdad, no ser un prefab de logica.
                GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (pf == null || pf.GetComponentInChildren<MeshFilter>() == null) continue;

                if (!rutas.Contains(ruta)) rutas.Add(ruta);
            }
        return rutas;
    }

    /// <summary>
    /// Ruido de Perlin que CIERRA en los bordes. Perlin no es periodico, asi que
    /// una textura hecha con el directo muestra una costura cada vez que se
    /// repite, y sobre un prado de 2200 x 2200 esa costura sale cientos de
    /// veces. La mezcla de las cuatro esquinas la borra.
    /// </summary>
    private static float RuidoQueRepite(float u, float v, float f)
    {
        float a = Mathf.PerlinNoise(u * f, v * f);
        float b = Mathf.PerlinNoise((u - 1f) * f, v * f);
        float c = Mathf.PerlinNoise(u * f, (v - 1f) * f);
        float d = Mathf.PerlinNoise((u - 1f) * f, (v - 1f) * f);
        return a * (1f - u) * (1f - v) + b * u * (1f - v)
             + c * (1f - u) * v        + d * u * v;
    }

    /// <summary>
    /// Pasto del valle a partir de la textura del pack, pero SUAVIZADA.
    ///
    /// Grass_37 tal cual tiene matas muy marcadas y mucho contraste: sobre un
    /// prado de dos kilometros se lee como un campo de brocolis, que es
    /// exactamente lo que no parece el pasto visto desde la altura de una
    /// persona. Aca se muestrea a dos escalas distintas —para romper la
    /// repeticion— y se acerca el resultado a su propio tono medio, que baja el
    /// contraste sin lavar el color. Las matas siguen ahi, pero como grano, no
    /// como bultos.
    /// </summary>
    private static Texture2D TexturaPastoSuave()
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Imagenes/pasto_suave.png";
        Texture2D ya = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (ya != null) return ya;

        Texture2D src = Legible(MapaDelPack("Grass_37", "Albedo"));
        if (src == null) return TexturaPasto();

        // Tono medio de la textura, muestreado en rejilla.
        Color media = Color.black;
        int M = 48;
        for (int y = 0; y < M; y++)
            for (int x = 0; x < M; x++)
                media += src.GetPixelBilinear((float)x / M, (float)y / M);
        media /= (M * M);

        const int N = 512;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);

        // Un punto de pasto seco del valle en seca, muy leve.
        Color seco = new Color(media.r * 1.22f, media.g * 1.10f, media.b * 0.70f);

        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (float)x / N, v = (float)y / N;

                // Dos frecuencias enteras: las dos cierran en el borde, asi que
                // el mosaico sigue sin costura.
                Color a = src.GetPixelBilinear(u * 3f, v * 3f);
                Color b = src.GetPixelBilinear(u + 0.37f, v + 0.11f);
                Color c = Color.Lerp(a, b, 0.45f);

                // Menos contraste: 55 % de la textura, 45 % de su tono medio.
                c = Color.Lerp(media, c, 0.55f);

                // Manchones amplios de verde mas claro y mas oscuro.
                float n = RuidoQueRepite(u, v, 2.5f);
                c *= Mathf.Lerp(0.90f, 1.12f, n);

                // Alguna zona seca, suelta.
                float am = Mathf.Clamp01((RuidoQueRepite(u, v, 4f) - 0.66f) * 3f);
                c = Color.Lerp(c, seco, am * 0.35f);

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();

        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ruta),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null)
        {
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 8;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    /// <summary>
    /// Textura del pasto del valle. El suelo era un verde plano y a la vista se
    /// notaba de inmediato: una alfombra de billar pegada a una vereda llena de
    /// detalle. Con manchas grandes de tono, briznas finas y algo de pasto seco
    /// —que es como se ve de verdad el valle del Mantaro en seca— la superficie
    /// deja de leerse como un plano pintado.
    /// </summary>
    private static Texture2D TexturaPasto()
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Imagenes/pasto_valle.png";
        Texture2D ya = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (ya != null) return ya;

        const int N = 256;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);

        Color verdeHondo = new Color(0.22f, 0.36f, 0.16f);
        Color verdeClaro = new Color(0.44f, 0.58f, 0.27f);
        Color seco       = new Color(0.56f, 0.55f, 0.31f);

        Random.State st = Random.state;
        Random.InitState(90211);

        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (float)x / N, v = (float)y / N;

                // Manchas grandes: el prado no es de un solo verde.
                float grande = RuidoQueRepite(u, v, 3f);
                // Mata sobre mata.
                float medio  = RuidoQueRepite(u, v, 11f);
                Color c = Color.Lerp(verdeHondo, verdeClaro, Mathf.Clamp01(grande * 0.75f + medio * 0.45f));

                // Zonas secas, sueltas y suaves.
                float amarillo = Mathf.Clamp01((RuidoQueRepite(u, v, 5f) - 0.62f) * 3.2f);
                c = Color.Lerp(c, seco, amarillo * 0.55f);

                // Briznas: grano fino, no ruido blanco, si no parece arena.
                float brizna = (Random.value - 0.5f) * 0.10f
                             + (RuidoQueRepite(u, v, 42f) - 0.5f) * 0.14f;
                c.r = Mathf.Clamp01(c.r + brizna * 0.8f);
                c.g = Mathf.Clamp01(c.g + brizna);
                c.b = Mathf.Clamp01(c.b + brizna * 0.6f);

                tex.SetPixel(x, y, c);
            }
        Random.state = st;
        tex.Apply();

        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ruta),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null)
        {
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 8;                  // el prado se ve casi de canto: hace falta
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    /// <summary>Genera (una sola vez) la textura del agua en Assets/Guardian/Imagenes.</summary>
    private static Texture2D TexturaAgua()
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Imagenes/agua_shullcas.png";
        Texture2D ya = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (ya != null) return ya;

        int N = 128;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (float)x / N, v = (float)y / N;
                float n = Mathf.PerlinNoise(u * 6f, v * 6f) * 0.6f
                        + Mathf.PerlinNoise(u * 14f, v * 14f) * 0.4f;
                tex.SetPixel(x, y, Color.Lerp(new Color(0.10f, 0.30f, 0.60f),
                                              new Color(0.38f, 0.70f, 0.90f), n));
            }
        tex.Apply();

        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ruta),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null) { ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    /// <summary>Textura suave y redonda para el humo (se genera una sola vez).</summary>
    private static Texture2D TexturaHumo()
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Imagenes/humo.png";
        Texture2D ya = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (ya != null) return ya;

        int N = 64;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N - 0.5f;
                float dy = (y + 0.5f) / N - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;   // 0 centro, 1 borde
                float a = Mathf.Clamp01(1f - d); a *= a;        // desvanecido suave
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();

        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ruta),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null)
        {
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    private static Material MaterialHumo()
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Materiales/Humo.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        Texture2D t = TexturaHumo();
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", t);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    /// <summary>Humo negro saliendo del tubo de escape del carro contaminante.</summary>
    private static void HumoContaminante(GameObject carro, float zOffset)
    {
        Transform viejo = carro.transform.Find("Humo");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        GameObject h = new GameObject("Humo");
        h.transform.SetParent(carro.transform, false);
        h.transform.localPosition = new Vector3(0f, 0.55f, zOffset);

        ParticleSystem ps = h.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 1.7f;
        main.startSpeed = 0.5f;
        main.startSize = 0.45f;
        main.startColor = new Color(0.22f, 0.21f, 0.20f, 0.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 12f;

        ParticleSystem.ShapeModule shp = ps.shape;
        shp.shapeType = ParticleSystemShapeType.Sphere;
        shp.radius = 0.12f;

        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(0.9f);

        ParticleSystem.SizeOverLifetimeModule siz = ps.sizeOverLifetime;
        siz.enabled = true;
        siz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.8f));

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.20f, 0.19f, 0.18f), 0f),
                new GradientColorKey(new Color(0.55f, 0.55f, 0.56f), 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.60f, 0f),
                new GradientAlphaKey(0.00f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        ParticleSystemRenderer r = h.GetComponent<ParticleSystemRenderer>();
        if (r != null) { r.sharedMaterial = MaterialHumo(); r.sortingFudge = 5f; }
    }

    /// <summary>
    /// Río Shullcas FUERA de la ciudad (nunca encima de la pista): se coloca más allá
    /// del borde de la red de calles, con su ribera verde.
    /// </summary>
    /// <summary>
    /// Rio Shullcas al borde de la ciudad, con su ribera arbolada.
    ///
    /// Se intento meterlo POR DENTRO de la ciudad, que es lo fiel al Shullcas de
    /// verdad —separa Huancayo de El Tambo y por eso le cae la basura urbana—,
    /// ocupando una calle entera con puentes en cada cruce. Funcionaba en el
    /// papel y el despeje era correcto, pero el agua nunca llego a verse: entre
    /// el suelo del pack, el grosor del asfalto y una textura generada en
    /// memoria que moria en cada recarga, el cauce quedaba siempre tapado o
    /// gris. Antes que entregar una franja de concreto cruzando Huancayo, vale
    /// mas el rio de las afueras, que se ve bien y se entiende. Lo aprendido en
    /// el intento queda igual: la altura del suelo se MIDE, no se supone, y los
    /// materiales limpian su textura vieja.
    /// </summary>
    /// <summary>
    /// Donde va el eje del cauce del Shullcas.
    ///
    /// Antes se ponia a 32 unidades de la ultima VIA, y por eso las casas
    /// quedaban con los pies dentro del agua: los edificios de la ultima manzana
    /// sobresalen bastante mas alla del asfalto, asi que medir contra la red de
    /// calles no alcanzaba. Ahora se mide contra lo CONSTRUIDO y se deja una
    /// franja de ribera entre la ultima casa y la orilla. El tope de 45 u evita
    /// que el cauce se escape: sin el, la vegetacion que el propio rio planta
    /// contaba como ciudad en el calculo siguiente y el rio se iba alejando un
    /// tramo mas cada vez que se armaba el juego.
    /// </summary>
    private static float EjeDelRio(float ancho)
    {
        Vector3 cC, tm;
        if (!LimitesCiudad(out cC, out tm)) return -60f;

        float largo = Mathf.Max(140f, tm.x + 80f);
        float xMin = cC.x - largo * 0.5f;
        float xMax = cC.x + largo * 0.5f;

        float bordeVias  = cC.z - tm.z * 0.5f;
        float bordeCasas = BordeConstruido(xMin, xMax, bordeVias);

        float borde = Mathf.Max(bordeCasas, bordeVias - 45f);
        borde = Mathf.Min(borde, bordeVias);

        return borde - 16f - ancho * 0.5f;   // 16 u de ribera entre la casa y el agua
    }

    private static void CrearRio(Vector3 centro)
    {
        // La arena se creaba con nombre numerado y no entraba en esta limpieza:
        // cada vez que se recalculaba el rio quedaba otro par de playas encima
        // de las anteriores, y la jerarquia se iba llenando de copias.
        BorrarPorNombre("Arena_Shullcas", "Rio_Shullcas", "Ribera_Shullcas", "Puentes_Shullcas");

        foreach (string n in new[] { "Rio_Shullcas", "Ribera_Shullcas", "Puentes_Shullcas" })
        {
            GameObject viejo = GameObject.Find(n);
            if (viejo != null) Undo.DestroyObjectImmediate(viejo);
        }

        int devueltas = 0;

        Vector3 cCiudad, tam;
        if (!LimitesCiudad(out cCiudad, out tam)) { cCiudad = centro; tam = new Vector3(120f, 0f, 120f); }

        float largo = Mathf.Max(140f, tam.x + 80f);     // corre a lo ancho del valle
        float ancho = 26f;
        float zRio = EjeDelRio(ancho);
        Vector3 pos = new Vector3(cCiudad.x, 0f, zRio);

        // La ribera de pasto se estira hacia el valle, pero por el lado de la
        // ciudad se corta ANTES de la primera casa. Si se dejara simetrica, el
        // pasto entraria por debajo de la manzana igual que entraba el agua.
        float bordeCiudad = zRio + ancho * 0.5f + 16f;
        float riberaSur   = zRio - (ancho * 0.5f + 28f);
        float riberaNorte = Mathf.Min(zRio + ancho * 0.5f + 28f, bordeCiudad - 1f);
        float anchoRibera = Mathf.Max(ancho + 12f, riberaNorte - riberaSur);
        float zRibera     = (riberaNorte + riberaSur) * 0.5f;

        GameObject ribera = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ribera.name = "Ribera_Shullcas";
        ribera.transform.position = new Vector3(pos.x, 0.02f, zRibera);
        ribera.transform.localScale = new Vector3((largo + 60f) / 10f, 1f, anchoRibera / 10f);
        // Misma textura que el prado del valle, con el mosaico proporcional al
        // tamano del plano para que la brizna mida lo mismo en los dos y no se
        // note donde termina uno y empieza el otro.
        float tileX = (largo + 60f) / 7.3f;
        float tileZ = anchoRibera / 7.3f;
        Material matRibera = MaterialSimple("Assets/Guardian/Materiales/Ribera_pasto.mat",
            Color.white, 0.04f, TexturaPastoSuave(), new Vector2(tileX, tileZ));
        ribera.GetComponent<Renderer>().sharedMaterial = matRibera;

        // Playa de arena entre el pasto y el agua: sin ella el pasto cortaba a
        // pico contra el rio, que es exactamente lo que no pasa en un rio real.
        for (int lado = 0; lado < 2; lado++)
        {
            GameObject playa = GameObject.CreatePrimitive(PrimitiveType.Plane);
            playa.name = "Arena_Shullcas_" + lado;
            float z0 = pos.z + (lado == 0 ? 1f : -1f) * (ancho * 0.5f + 3.2f);
            playa.transform.position = new Vector3(pos.x, 0.04f, z0);
            playa.transform.localScale = new Vector3(largo / 10f, 1f, 7.4f / 10f);
            Material matArena = MaterialDelPack("Arena_rio", "Muddy_Cracked_Sand_6",
                                                new Vector2(largo / 5f, 1.6f), 0.06f, Color.white);
            if (matArena == null)
                matArena = MaterialSimple("Assets/Guardian/Materiales/Arena_rio.mat",
                                          new Color(0.62f, 0.56f, 0.42f), 0.05f, null, Vector2.one);
            playa.GetComponent<Renderer>().sharedMaterial = matArena;
            Collider pc2 = playa.GetComponent<Collider>();
            if (pc2 != null) pc2.enabled = false;
            Undo.RegisterCreatedObjectUndo(playa, "Crear playa del rio");
        }
        Collider rc = ribera.GetComponent<Collider>();
        if (rc != null) rc.enabled = false;
        Undo.RegisterCreatedObjectUndo(ribera, "Crear ribera");

        // Agua. SIN textura: la que se generaba por codigo vivia solo en memoria
        // y al recargar el dominio dejaba al material apuntando a una textura
        // muerta, que se pintaba gris. Un azul plano no se rompe nunca.
        GameObject rio = GameObject.CreatePrimitive(PrimitiveType.Plane);
        rio.name = "Rio_Shullcas";
        rio.transform.position = new Vector3(pos.x, 0.06f, pos.z);
        rio.transform.localScale = new Vector3(largo / 10f, 1f, ancho / 10f);
        // El agua era un azul plano: como AguaRio desplaza la textura del material,
        // sin textura el rio se veia quieto aunque el script corriera. Con la del
        // pack (Water_6) el desplazamiento por fin se nota como corriente.
        Material matAgua = MaterialDelPack("Agua_Shullcas_pack", "Water_6",
                                           new Vector2(largo / 12f, ancho / 12f), 0.85f,
                                           new Color(0.62f, 0.86f, 1f));
        if (matAgua == null)
            matAgua = MaterialSimple("Assets/Guardian/Materiales/Agua_Shullcas.mat",
                           new Color(0.30f, 0.58f, 0.76f), 0.70f, null, Vector2.one);
        rio.GetComponent<Renderer>().sharedMaterial = matAgua;
        Collider col = rio.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        AguaRio ar = rio.AddComponent<AguaRio>();
        ar.aguaLimpia = new Color(0.30f, 0.58f, 0.76f);
        ar.aguaSucia  = new Color(0.36f, 0.31f, 0.18f);
        ar.oleaje = 0.05f;
        Undo.RegisterCreatedObjectUndo(rio, "Crear rio Shullcas");

        VegetacionRibera(pos, largo, ancho);
        BasuraFlotante(pos, largo, ancho);

        Debug.Log("[Guardian] Shullcas en la ribera (z=" + pos.z.ToString("0") + ")"
                  + " · " + devueltas + " construcciones devueltas a la ciudad");
    }

    /// <summary>
    /// A que altura esta la superficie por la que se camina en el cauce.
    ///
    /// Se mide de dos formas y se toma la mas alta, porque ninguna sola es
    /// confiable: los rayos hacia abajo solo ven lo que tenga collider (y el
    /// suelo base del pack podria no tenerlo), y las bounds solo ven lo que
    /// tenga malla. Dar por hecho que el suelo esta en Y=0 fue justamente el
    /// error que dejo el rio enterrado dos veces seguidas.
    /// </summary>
    private static float AlturaDelSuelo(float xMin, float xMax, float zCauce)
    {
        float y = -999f;

        // 1) Rayos hacia abajo a lo largo del cauce.
        for (int m = 0; m <= 6; m++)
        {
            float xm = Mathf.Lerp(xMin + 10f, xMax - 10f, m / 6f);
            float ym = SueloY(xm, zCauce, -999f);
            if (ym > y) y = ym;
        }

        // 2) Techo de lo que realmente se pisa en el cauce.
        //    Van dos familias:
        //      · las mallas GRANDES (el suelo continuo de la ciudad), y
        //      · las LOSAS DE CALLE, que miden 20x20 y por eso se colaban por
        //        el filtro de tamano. Ese fue el error: el asfalto del pack
        //        tiene su propio grosor y su cara queda mas alta que el suelo
        //        base, asi que el agua puesta "por encima del suelo" seguia
        //        quedando por debajo de la pista y no se veia ni una gota.
        foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (mr == null || !mr.enabled || !mr.gameObject.activeInHierarchy) continue;

            string n = mr.name;
            if (n.StartsWith("Rio_") || n.StartsWith("Ribera_")
                || n.StartsWith("Malecon") || n.StartsWith("Puente")) continue;

            Bounds b = mr.bounds;

            // SIN filtro por nombre. Filtrar por "Road..." no servia: en este pack
            // el GameObject se llama "Road Lane_x" pero el MeshRenderer cuelga de
            // un hijo con otro nombre, asi que el asfalto se colaba igual y el
            // agua terminaba por debajo de la pista. Como el cauce ya quedo
            // despejado de construcciones, lo unico que sigue ahi es suelo y
            // pista: basta con quedarse con lo mas alto que sea bajo.
            if (b.max.x < xMin || b.min.x > xMax) continue;
            if (b.max.z < zCauce || b.min.z > zCauce) continue;
            if (b.max.y > 3f) continue;                          // postes y arboles, no
            if (b.size.y > 3f) continue;                         // paredes, no

            if (b.max.y > y) y = b.max.y;
        }

        if (y < -50f) y = 0f;
        return y;
    }

    /// <summary>
    /// Elige POR CUAL CALLE este-oeste va a correr el rio. Toma una fila de vias
    /// que corran en X, lejos del centro de la ciudad (para no partir la plaza en
    /// dos) pero todavia dentro de la trama urbana.
    /// </summary>
    private static float ElegirCalleDelRio(List<Tramo> tramos, Vector3 cCiudad)
    {
        // Agrupa las vias este-oeste por su z, redondeado a la cuadricula.
        Dictionary<int, int> filas = new Dictionary<int, int>();
        for (int i = 0; i < tramos.Count; i++)
        {
            if (!tramos[i].ejeX) continue;
            int clave = Mathf.RoundToInt(tramos[i].c.z / TRAMO);
            if (filas.ContainsKey(clave)) filas[clave]++;
            else filas[clave] = 1;
        }
        if (filas.Count == 0) return cCiudad.z - 40f;

        // La mejor fila: larga (muchas vias) y a una distancia media del centro.
        float mejorZ = cCiudad.z - 40f;
        float mejorNota = float.MinValue;

        foreach (KeyValuePair<int, int> f in filas)
        {
            float z = f.Key * TRAMO;
            float d = Mathf.Abs(z - cCiudad.z);
            if (f.Value < 3) continue;                 // fila muy corta: no cruza la ciudad
            if (d < TRAMO * 1.5f) continue;            // demasiado al centro: partiria la plaza

            float nota = f.Value * 2f - Mathf.Abs(d - TRAMO * 2.5f) * 0.5f;
            if (nota > mejorNota) { mejorNota = nota; mejorZ = z; }
        }
        return mejorZ;
    }

    /// <summary>
    /// Un puente por cada calle norte-sur que cruzaba el cauce. Deck, dos
    /// barandas y pilares: sin ellos el rio cortaria la ciudad en dos mitades
    /// incomunicadas y el nivel seria injugable.
    /// </summary>
    /// <summary>X de cada calle norte-sur que cruza el cauce, ordenadas.</summary>
    private static List<float> ColumnasDePuente(List<Tramo> tramos, float xMin, float xMax)
    {
        List<float> columnas = new List<float>();
        for (int i = 0; i < tramos.Count; i++)
        {
            if (!tramos[i].ejeZ) continue;
            float x = Mathf.Round(tramos[i].c.x / TRAMO) * TRAMO;
            if (x < xMin || x > xMax) continue;

            bool repetida = false;
            for (int k = 0; k < columnas.Count && !repetida; k++)
                if (Mathf.Abs(columnas[k] - x) < 1f) repetida = true;
            if (!repetida) columnas.Add(x);
        }
        columnas.Sort();
        return columnas;
    }

    private static int PuentesSobreElRio(List<float> columnas, float zCauce, float ancho, float ySuelo)
    {
        GameObject raiz = new GameObject("Puentes_Shullcas");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear puentes");

        Material calzada = MatColor("Puente_calzada", new Color(0.30f, 0.30f, 0.32f), 0.15f);
        Material baranda = MatColor("Puente_baranda", new Color(0.78f, 0.76f, 0.72f), 0.25f);

        float largoPuente = ancho + 9f;                // apoya en los dos malecones

        for (int i = 0; i < columnas.Count; i++)
        {
            float x = columnas[i];

            GameObject p = new GameObject("Puente_" + Mathf.RoundToInt(x));
            p.transform.SetParent(raiz.transform, false);
            p.transform.position = new Vector3(x, ySuelo, zCauce);

            // Tablero: apenas por encima del agua (0.10) y con escalon bajo
            // desde la calle, para que el CharacterController pueda subirlo.
            Pieza(p, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f),
                  new Vector3(MEDIA_CALZADA * 2f, 0.34f, largoPuente), Vector3.zero,
                  calzada, "Tablero");

            // Barandas a los dos lados.
            for (int lado = -1; lado <= 1; lado += 2)
            {
                Pieza(p, PrimitiveType.Cube,
                      new Vector3(lado * (MEDIA_CALZADA - 0.25f), 0.62f, 0f),
                      new Vector3(0.28f, 0.80f, largoPuente * 0.92f), Vector3.zero,
                      baranda, "Baranda");
            }

            // Pilares dentro del agua: dan idea de que el puente se sostiene.
            for (int k = -1; k <= 1; k += 2)
            {
                Pieza(p, PrimitiveType.Cube,
                      new Vector3(0f, -0.10f, k * ancho * 0.30f),
                      new Vector3(MEDIA_CALZADA * 1.6f, 0.55f, 1.2f), Vector3.zero,
                      calzada, "Pilar");
            }
        }
        return columnas.Count;
    }

    /// <summary>
    /// Borde sur de lo CONSTRUIDO: recorre los objetos de la ciudad (casas, muros,
    /// props) dentro del ancho del cauce y devuelve el z más bajo que alcanzan.
    /// Deja fuera el terreno, los cerros y la vegetación, que son paisaje y sí
    /// pueden quedar por debajo del río.
    /// </summary>
    private static float BordeConstruido(float xMin, float xMax, float porDefecto)
    {
        float borde = porDefecto;

        foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (mr == null || !EsDeLaCiudad(mr.transform)) continue;

            Bounds b = mr.bounds;
            if (b.max.x < xMin || b.min.x > xMax) continue;     // fuera del ancho del río
            if (b.size.x > 60f || b.size.z > 60f) continue;     // suelo/terreno: no es una casa

            if (b.min.z < borde) borde = b.min.z;
        }
        return borde;
    }

    /// <summary>Lado mayor (en planta) de todo lo que cuelga de un objeto.</summary>
    private static float TamanoPlano(Transform t)
    {
        Bounds b = new Bounds(t.position, Vector3.zero);
        bool hay = false;
        foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
        {
            if (r == null) continue;
            if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
        }
        return hay ? Mathf.Max(b.size.x, b.size.z) : 0f;
    }

    /// <summary>¿Este objeto es parte de la ciudad construida?</summary>
    /// <summary>
    /// Borra de la escena todo lo que se llame asi, incluido lo que este
    /// DESACTIVADO.
    ///
    /// Es la diferencia que importa: Object.FindObjectsOfType no devuelve objetos
    /// apagados, y esta herramienta apaga cosas a proposito (lo que estorba al
    /// templo o al cauce). Un objeto apagado que no se puede encontrar tampoco se
    /// puede borrar, asi que la pasada siguiente creaba uno nuevo encima y la
    /// escena se iba llenando de copias invisibles: dos rios, dos riberas, dos
    /// pisos de seguridad.
    /// </summary>
    private static int BorrarPorNombre(params string[] nombres)
    {
        int n = 0;
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Sin filtrar por raiz: despues de ORDENAR ESCENA estos objetos
            // cuelgan del grupo "Escenario", asi que pedir que no tengan padre
            // era justamente dejar fuera a los que hay que borrar.
            if (t == null) continue;
            for (int i = 0; i < nombres.Length; i++)
            {
                if (!t.name.StartsWith(nombres[i], System.StringComparison.Ordinal)) continue;
                Undo.DestroyObjectImmediate(t.gameObject);
                n++;
                break;
            }
        }
        return n;
    }

    private static bool EsDeLaCiudad(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            string n = p.name;
            if (n.StartsWith("=== GUARDIÁN")) return false;      // lo que arma esta herramienta
            if (n.StartsWith("Rio_") || n.StartsWith("Ribera_")) return false;
            if (n.StartsWith("Cerro") || n.StartsWith("Terrain")
                || n.Contains("Mountain") || n.Contains("Forest")
                || n.StartsWith("Nature") || n.StartsWith("Tree")) return false;
            if (n.StartsWith("Ground") || n.StartsWith("Suelo")
                || n.StartsWith("Grass") || n.StartsWith("Base")) return false;
            if (n.StartsWith("Vegetacion") || n.StartsWith("BasuraRio")
                || n.StartsWith("Piso_") || n.StartsWith("Letrero_")) return false;
            // El atrio, el cerco y las jardineras los pone esta misma herramienta
            // DESPUES de despejar el lote. Si se cuelan aqui, la pasada siguiente
            // los desactiva creyendo que son casas en medio del templo, y como
            // FindObjectsOfType no ve objetos apagados, tampoco se los puede
            // borrar despues: se acumulan invisibles en la escena.
            if (n.StartsWith("Iglesia_") || n.StartsWith("Plaza_")) return false;
        }
        return true;
    }

    /// <summary>
    /// Aparta de la escena lo que haya quedado dentro del cauce. No lo destruye:
    /// lo desactiva, así se puede volver atrás si hiciera falta.
    /// </summary>
    private static int DespejarCauce(float xMin, float xMax, float zMin, float zMax)
    {
        int apartados = 0;
        List<GameObject> fuera = new List<GameObject>();

        foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (mr == null || !mr.gameObject.activeInHierarchy) continue;
            if (!EsDeLaCiudad(mr.transform)) continue;

            Bounds b = mr.bounds;
            if (b.size.x > 60f || b.size.z > 60f) continue;     // el suelo base no se toca
            if (b.max.x < xMin || b.min.x > xMax) continue;
            if (b.max.z < zMin || b.min.z > zMax) continue;

            // Se aparta la pieza entera (la casa), no una pared suelta. Sube como
            // mucho dos niveles Y solo mientras el padre siga siendo una pieza
            // chica: sin ese freno se llegaría al contenedor de toda la ciudad y
            // se apagaría Huancayo completa de un solo clic.
            Transform raiz = mr.transform;
            for (int k = 0; k < 2 && raiz.parent != null; k++)
            {
                Transform pa = raiz.parent;
                if (pa.name.StartsWith("===")) break;
                if (TamanoPlano(pa) > 45f) break;
                raiz = pa;
            }
            if (!fuera.Contains(raiz.gameObject)) fuera.Add(raiz.gameObject);
        }

        foreach (GameObject g in fuera)
        {
            Undo.RecordObject(g, "Despejar cauce del Shullcas");
            if (g.GetComponent<DespejadoPorElRio>() == null)
                Undo.AddComponent<DespejadoPorElRio>(g);
            g.SetActive(false);
            apartados++;
        }
        return apartados;
    }

    /// <summary>
    /// Devuelve a la escena todo lo que un calculo anterior del cauce hubiera
    /// apartado. Hay que usar FindObjectsOfTypeAll porque los objetos estan
    /// desactivados y la busqueda normal no los ve.
    /// </summary>
    private static int RestaurarDespejado()
    {
        int devueltos = 0;
        foreach (DespejadoPorElRio m in Resources.FindObjectsOfTypeAll<DespejadoPorElRio>())
        {
            if (m == null) continue;
            GameObject g = m.gameObject;
            if (g.scene.IsValid() == false) continue;      // prefabs del proyecto, no la escena

            Undo.RecordObject(g, "Devolver al sitio");
            g.SetActive(true);
            Undo.DestroyObjectImmediate(m);
            devueltos++;
        }
        return devueltos;
    }

    /// <summary>
    /// Botellas, bolsas, latas y tecnopor bajando por el Shullcas. Cuántas se ven
    /// depende de la contaminación de la ciudad: es el resultado visible de no
    /// segregar. Todo modelado con primitivas, nada descargado.
    /// </summary>
    /// <summary>
    /// Basura que baja por el Shullcas — y que AHORA SE PUEDE SACAR DEL AGUA.
    ///
    /// Hasta esta version era puro decorado: se veia mas o menos basura segun la
    /// contaminacion de la ciudad, pero el jugador no podia tocarla. El juego se
    /// llama Guardian de Huancayo y el Shullcas es el problema del que habla el
    /// ODS 11 aca, asi que mirarlo pasar sin poder hacer nada era justamente la
    /// leccion equivocada. Cada pieza es ahora un residuo de verdad, del tipo que
    /// le corresponde, que cuenta para el nivel de la Ribera.
    ///
    /// Se puede vadear: el agua no tiene colisionador y el piso de seguridad esta
    /// justo debajo, asi que el Guardian entra al cauce con el agua por el tobillo
    /// y saca lo que baja, que es exactamente como se limpia un rio de verdad.
    /// </summary>
    private static void BasuraFlotante(Vector3 centroRio, float largo, float ancho)
    {
        BorrarPorNombre("BasuraRio_Shullcas");

        GameObject raiz = new GameObject("BasuraRio_Shullcas");
        raiz.transform.position = new Vector3(centroRio.x, 0.22f, centroRio.z);
        raiz.transform.rotation = Quaternion.identity;

        float tramo = Mathf.Min(largo, 110f);

        Material plastico = MatColor("Rio_botella",  new Color(0.55f, 0.74f, 0.82f), 0.70f);
        Material bolsa    = MatColor("Rio_bolsa",    new Color(0.13f, 0.13f, 0.15f), 0.30f);
        Material lata     = MatColor("Rio_lata",     new Color(0.72f, 0.73f, 0.76f), 0.65f);
        Material carton   = MatColor("Rio_carton",   new Color(0.62f, 0.48f, 0.32f), 0.12f);
        Material vidrio   = MatColor("Rio_vidrio",   new Color(0.35f, 0.62f, 0.40f), 0.85f);
        Material organico = MatColor("Rio_organico", new Color(0.45f, 0.38f, 0.20f), 0.20f);

        List<Transform> lista = new List<Transform>();
        const int N = 15;
        for (int i = 0; i < N; i++)
        {
            GameObject g = new GameObject("Flota_" + i);
            g.transform.SetParent(raiz.transform, false);

            // Repartidos a lo ancho pero sin pegarse a la orilla: hay que meterse
            // al agua para sacarlos, que es la gracia del nivel.
            g.transform.localPosition = new Vector3(
                (i / (float)N - 0.5f) * tramo, Random.Range(-0.02f, 0.04f),
                Random.Range(-ancho * 0.32f, ancho * 0.32f));

            // Los cinco tipos de la NTP, rotando: el nivel del rio repasa la tabla
            // entera en vez de ser todo plastico.
            TipoResiduo tipo;
            switch (i % 5)
            {
                case 0:
                    Pieza(g, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.30f, 0.48f, 0.30f),
                          new Vector3(90f, 0f, 12f), plastico, "Botella");
                    tipo = TipoResiduo.Plastico;
                    break;
                case 1:
                    Pieza(g, PrimitiveType.Cube, Vector3.zero, new Vector3(0.98f, 0.30f, 0.72f),
                          new Vector3(0f, 22f, 0f), bolsa, "Bolsa");
                    tipo = TipoResiduo.Plastico;
                    break;
                case 2:
                    Pieza(g, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.22f, 0.20f, 0.22f),
                          new Vector3(90f, 0f, 40f), lata, "Lata");
                    tipo = TipoResiduo.Metal;
                    break;
                case 3:
                    Pieza(g, PrimitiveType.Cube, Vector3.zero, new Vector3(0.92f, 0.26f, 0.66f),
                          new Vector3(0f, -14f, 0f), carton, "CajaMojada");
                    tipo = TipoResiduo.Papel;
                    break;
                default:
                    if (i % 10 == 4)
                    {
                        Pieza(g, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.26f, 0.34f, 0.26f),
                              new Vector3(90f, 0f, 28f), vidrio, "FrascoVidrio");
                        tipo = TipoResiduo.Vidrio;
                    }
                    else
                    {
                        Pieza(g, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.44f, 0.30f, 0.52f),
                              Vector3.zero, organico, "RestoOrganico");
                        tipo = TipoResiduo.Organico;
                    }
                    break;
            }

            // Recogible: trigger ancho, porque atrapar algo que baja con la
            // corriente ya es bastante dificil sin pedir punteria de cirujano.
            SphereCollider sc = g.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.45f;

            TrashItem ti = g.AddComponent<TrashItem>();
            ti.tipo = tipo;
            ti.zona = 2;                 // nivel de la Ribera del Shullcas
            ti.valor = 18;               // vale mas: hay que meterse al agua
            ti.deReserva = false;

            lista.Add(g.transform);
        }

        BasuraEnElRio comp = raiz.AddComponent<BasuraEnElRio>();
        comp.flotantes = lista;
        comp.largo = tramo;
        comp.velocidad = 1.5f;      // mas lento: si no, no hay quien los alcance
        comp.recogibles = true;

        Undo.RegisterCreatedObjectUndo(raiz, "Crear basura flotando en el río");
    }

    /// <summary>Árboles, arbustos y rocas a lo largo de las dos orillas del río.</summary>
    private static void VegetacionRibera(Vector3 centroRio, float largo, float ancho)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("VegetacionRio_"))
                Undo.DestroyObjectImmediate(t.gameObject);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_ARBOLES[0]) == null) return;

        Random.State st = Random.state;
        Random.InitState(4455);

        int n = 26;
        for (int i = 0; i < n; i++)
        {
            // Repartidos por el largo del río, a un lado o al otro de la orilla.
            float t01 = (i + Random.Range(0.1f, 0.9f)) / n;
            float x = centroRio.x - largo * 0.5f + largo * t01;
            float lado = (i % 2 == 0) ? 1f : -1f;

            float z = centroRio.z + lado * (ancho * 0.5f + Random.Range(3.5f, 15f));

            string ruta;
            float tam;
            float r = Random.value;
            if (r < 0.55f)      { ruta = PREFABS_ARBOLES[Random.Range(0, PREFABS_ARBOLES.Length)];  tam = Random.Range(6.5f, 10.5f); }
            else if (r < 0.85f) { ruta = PREFABS_ARBUSTOS[Random.Range(0, PREFABS_ARBUSTOS.Length)]; tam = Random.Range(1.4f, 2.6f); }
            else                { ruta = PREFABS_ROCAS[Random.Range(0, PREFABS_ROCAS.Length)];       tam = Random.Range(1.2f, 2.8f); }

            GameObject v = PiezaPrefab(null, ruta,
                Vector3.zero, tam, new Vector3(0f, Random.Range(0f, 360f), 0f),
                "VegetacionRio_" + i);
            if (v == null) continue;

            ApoyarEn(v, new Vector3(x, SueloY(x, z, 0f), z), tam * 0.05f);
            Undo.RegisterCreatedObjectUndo(v, "Crear vegetación de la ribera");
        }

        Random.state = st;
    }

    // ---- Cerros del valle del Mantaro (para que no parezca ciudad de EE.UU.) ----

    private static Mesh MallaCerro(int lados, float radio, float altura, int semilla)
    {
        Random.State st = Random.state;
        Random.InitState(semilla);

        Vector3[] aro = new Vector3[lados];
        for (int i = 0; i < lados; i++)
        {
            float a = (float)i / lados * Mathf.PI * 2f;
            float r = radio * Random.Range(0.72f, 1.28f);
            aro[i] = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
        }
        Vector3 cima = new Vector3(Random.Range(-radio * 0.18f, radio * 0.18f), altura,
                                   Random.Range(-radio * 0.18f, radio * 0.18f));

        List<Vector3> v = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> tri = new List<int>();
        for (int i = 0; i < lados; i++)
        {
            int idx = v.Count;
            v.Add(aro[i]); v.Add(aro[(i + 1) % lados]); v.Add(cima);
            // V va de 0 (falda) a 1 (cima): la textura pinta pasto abajo y roca arriba.
            uv.Add(new Vector2(i, 0f));
            uv.Add(new Vector2(i + 1f, 0f));
            uv.Add(new Vector2(i + 0.5f, 1f));
            tri.Add(idx); tri.Add(idx + 2); tri.Add(idx + 1);   // normales hacia afuera
        }

        Mesh m = new Mesh();
        m.SetVertices(v);
        m.SetUVs(0, uv);
        m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        Random.state = st;
        return m;
    }

    /// <summary>Textura del cerro: pasto seco abajo, tierra al medio y roca arriba.</summary>
    private static Texture2D TexturaCerro()
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Imagenes/cerro_valle_v2.png";
        Texture2D ya = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (ya != null) return ya;

        // Las cuatro franjas del cerro, sacadas de texturas de verdad en vez de
        // pintadas con un color plano cada una. El cerro es lo que mas sale en
        // pantalla despues del suelo, y con bandas lisas se veia de carton.
        Texture2D fPasto  = Legible(MapaDelPack("Grass_37", "Albedo"));
        Texture2D fTierra = Legible(MapaDelPack("Rocky_Dirt_2", "Albedo"));
        Texture2D fRoca   = Legible(MapaDelPack("Cliff_Rock_Surface_21", "Albedo"));
        Texture2D fNieve  = Legible(MapaDelPack("Dirty_Snow_2", "Albedo"));

        int W = 256, H = 512;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);

        // Respaldo por si el pack no estuviera: los colores planos de siempre.
        Color pasto = new Color(0.40f, 0.48f, 0.24f);
        Color seco  = new Color(0.55f, 0.51f, 0.28f);
        Color tierra= new Color(0.42f, 0.35f, 0.25f);
        Color roca  = new Color(0.52f, 0.50f, 0.48f);
        Color nieve = new Color(0.96f, 0.97f, 0.99f);

        for (int y = 0; y < H; y++)
        {
            float v = (float)y / (H - 1);              // 0 falda, 1 cima
            for (int x = 0; x < W; x++)
            {
                float u = (float)x / W;

                // Las franjas no son rectas: el ruido las deshilacha, que es como
                // se ve una ladera de verdad desde lejos.
                float n = Mathf.PerlinNoise(u * 9f, v * 9f) * 0.6f
                        + Mathf.PerlinNoise(u * 22f, v * 22f) * 0.4f;
                float h = Mathf.Clamp01(v + (n - 0.5f) * 0.30f);

                // Muestreo de la textura real, repetida varias veces a lo alto
                // para que la piedra tenga tamano de piedra y no de continente.
                float su = u * 4f, sv = v * 8f;
                Color cPasto  = (fPasto  != null) ? fPasto.GetPixelBilinear(su, sv)  : pasto;
                Color cTierra = (fTierra != null) ? fTierra.GetPixelBilinear(su, sv) : tierra;
                Color cRoca   = (fRoca   != null) ? fRoca.GetPixelBilinear(su, sv)   : roca;
                Color cNieve  = (fNieve  != null) ? fNieve.GetPixelBilinear(su, sv)  : nieve;
                Color cSeco   = Color.Lerp(cPasto, cTierra, 0.45f);
                if (fPasto == null) cSeco = seco;

                Color c;
                if (h < 0.34f)      c = Color.Lerp(cPasto, cSeco, h / 0.34f);
                else if (h < 0.66f) c = Color.Lerp(cSeco, cTierra, (h - 0.34f) / 0.32f);
                else if (h < 0.85f) c = Color.Lerp(cTierra, cRoca, (h - 0.66f) / 0.19f);
                else                c = Color.Lerp(cRoca, cNieve, Mathf.Clamp01((h - 0.85f) / 0.08f));

                // Sombreado suave por altura: el pie del cerro mas oscuro.
                c *= 0.86f + v * 0.20f;
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ruta),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null)
        {
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 6;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    /// <summary>Cielo de altura (3 250 m): aire delgado, azul profundo y sol de tarde.</summary>
    private static void CieloAndino()
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Materiales/Cielo_Huancayo.mat";
        Material cielo = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (cielo == null)
        {
            Shader sh = Shader.Find("Skybox/Procedural");
            if (sh == null) return;
            cielo = new Material(sh);
            AssetDatabase.CreateAsset(cielo, ruta);
        }

        if (cielo.HasProperty("_SunDisk"))             cielo.SetFloat("_SunDisk", 2f);
        if (cielo.HasProperty("_SunSize"))             cielo.SetFloat("_SunSize", 0.035f);
        if (cielo.HasProperty("_SunSizeConvergence"))  cielo.SetFloat("_SunSizeConvergence", 6f);
        if (cielo.HasProperty("_AtmosphereThickness")) cielo.SetFloat("_AtmosphereThickness", 0.62f);
        if (cielo.HasProperty("_SkyTint"))             cielo.SetColor("_SkyTint", new Color(0.30f, 0.50f, 0.82f));
        if (cielo.HasProperty("_GroundColor"))         cielo.SetColor("_GroundColor", new Color(0.42f, 0.47f, 0.33f));
        if (cielo.HasProperty("_Exposure"))            cielo.SetFloat("_Exposure", 1.30f);
        EditorUtility.SetDirty(cielo);
        AssetDatabase.SaveAssets();

        RenderSettings.skybox = cielo;

        // Neblina del valle: da profundidad y suaviza el borde del mapa.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.74f, 0.82f, 0.90f);
        RenderSettings.fogDensity = 0.0022f;

        Light sol = null;
        foreach (Light l in Object.FindObjectsOfType<Light>())
            if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null)
        {
            Undo.RecordObject(sol, "Sol andino");
            Undo.RecordObject(sol.transform, "Sol andino");
            sol.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            sol.color = new Color(1f, 0.96f, 0.88f);
            sol.intensity = 1.25f;
            RenderSettings.sun = sol;
        }

        DynamicGI.UpdateEnvironment();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    // ============================================================
    //  MOTOTAXI Y PUESTOS DE MERCADO (armados con primitivas, low poly)
    // ============================================================

    private static Material MatColor(string nombre, Color c, float smooth)
    {
        return MaterialSimple("Assets/Guardian/Materiales/" + nombre + ".mat", c, smooth, null, Vector2.one);
    }

    private static void Pieza(GameObject padre, PrimitiveType tipo, Vector3 pos, Vector3 escala,
                              Vector3 rot, Material mat, string nombre)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre.transform, false);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = rot;
        g.transform.localScale = escala;

        Collider c = g.GetComponent<Collider>();
        if (c != null) Object.DestroyImmediate(c);
        Renderer r = g.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    /// <summary>Mototaxi peruano: lo más reconocible de una ciudad como Huancayo.</summary>
    private static GameObject CrearMototaxi(Color color, string nombre)
    {
        GameObject m = new GameObject(nombre);

        Material cuerpo   = MatColor("Moto_" + ColorUtility.ToHtmlStringRGB(color), color, 0.35f);
        Material toldo    = MatColor("Moto_toldo", new Color(0.80f, 0.14f, 0.14f), 0.18f);
        Material negro    = MatColor("Moto_negro", new Color(0.10f, 0.10f, 0.11f), 0.25f);
        Material vidrio   = MatColor("Moto_vidrio", new Color(0.58f, 0.74f, 0.82f), 0.85f);

        Pieza(m, PrimitiveType.Cube,     new Vector3(0f, 0.48f, -0.15f), new Vector3(1.05f, 0.35f, 2.00f), Vector3.zero,               cuerpo, "Chasis");
        Pieza(m, PrimitiveType.Cube,     new Vector3(0f, 0.98f, -0.55f), new Vector3(1.15f, 0.75f, 1.15f), Vector3.zero,               cuerpo, "Cabina");
        Pieza(m, PrimitiveType.Cube,     new Vector3(0f, 1.45f, -0.45f), new Vector3(1.32f, 0.08f, 1.55f), new Vector3(-6f, 0f, 0f),   toldo,  "Toldo");
        Pieza(m, PrimitiveType.Cube,     new Vector3(0f, 0.78f,  0.75f), new Vector3(0.50f, 0.55f, 0.60f), Vector3.zero,               cuerpo, "Frente");
        Pieza(m, PrimitiveType.Cube,     new Vector3(0f, 1.14f,  0.72f), new Vector3(0.60f, 0.45f, 0.06f), new Vector3(-14f, 0f, 0f),  vidrio, "Parabrisas");
        Pieza(m, PrimitiveType.Cylinder, new Vector3(0f, 1.20f,  0.55f), new Vector3(0.05f, 0.35f, 0.05f), new Vector3(0f, 0f, 90f),   negro,  "Manubrio");

        // Tres ruedas: una adelante, dos atrás.
        Pieza(m, PrimitiveType.Cylinder, new Vector3( 0.00f, 0.30f,  1.05f), new Vector3(0.60f, 0.06f, 0.60f), new Vector3(0f, 0f, 90f), negro, "RuedaDelantera");
        Pieza(m, PrimitiveType.Cylinder, new Vector3(-0.52f, 0.30f, -0.75f), new Vector3(0.60f, 0.06f, 0.60f), new Vector3(0f, 0f, 90f), negro, "RuedaTrasIzq");
        Pieza(m, PrimitiveType.Cylinder, new Vector3( 0.52f, 0.30f, -0.75f), new Vector3(0.60f, 0.06f, 0.60f), new Vector3(0f, 0f, 90f), negro, "RuedaTrasDer");

        return m;
    }

    /// <summary>Combi de transporte público: blanca con franja de color.</summary>
    private static GameObject CrearCombi(Color franja, string nombre)
    {
        GameObject c = new GameObject(nombre);

        Material blanco = MatColor("Combi_blanco", new Color(0.92f, 0.92f, 0.90f), 0.35f);
        Material color  = MatColor("Combi_" + ColorUtility.ToHtmlStringRGB(franja), franja, 0.35f);
        Material vidrio = MatColor("Combi_vidrio", new Color(0.32f, 0.42f, 0.50f), 0.85f);
        Material negro  = MatColor("Moto_negro", new Color(0.10f, 0.10f, 0.11f), 0.25f);

        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 1.25f, -0.30f), new Vector3(2.00f, 1.70f, 4.40f), Vector3.zero,              blanco, "Carroceria");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 0.95f,  2.35f), new Vector3(1.95f, 1.10f, 1.10f), Vector3.zero,              blanco, "Trompa");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 0.95f, -0.30f), new Vector3(2.04f, 0.38f, 4.42f), Vector3.zero,              color,  "Franja");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 1.72f, -0.30f), new Vector3(2.06f, 0.70f, 3.90f), Vector3.zero,              vidrio, "Ventanas");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 1.42f,  2.30f), new Vector3(1.85f, 0.75f, 0.12f), new Vector3(-16f, 0f, 0f), vidrio, "Parabrisas");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 2.15f,  1.95f), new Vector3(1.40f, 0.30f, 0.08f), Vector3.zero,              color,  "LetreroRuta");

        Pieza(c, PrimitiveType.Cylinder, new Vector3(-1.00f, 0.42f,  1.55f), new Vector3(0.84f, 0.10f, 0.84f), new Vector3(0f, 0f, 90f), negro, "RuedaDelIzq");
        Pieza(c, PrimitiveType.Cylinder, new Vector3( 1.00f, 0.42f,  1.55f), new Vector3(0.84f, 0.10f, 0.84f), new Vector3(0f, 0f, 90f), negro, "RuedaDelDer");
        Pieza(c, PrimitiveType.Cylinder, new Vector3(-1.00f, 0.42f, -1.75f), new Vector3(0.84f, 0.10f, 0.84f), new Vector3(0f, 0f, 90f), negro, "RuedaTrasIzq");
        Pieza(c, PrimitiveType.Cylinder, new Vector3( 1.00f, 0.42f, -1.75f), new Vector3(0.84f, 0.10f, 0.84f), new Vector3(0f, 0f, 90f), negro, "RuedaTrasDer");

        return c;
    }

    /// <summary>¿Hay sitio libre de edificios alrededor de este punto?</summary>
    private static bool LugarLibre(Vector3 p, float radio)
    {
        float r2 = radio * radio;
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            if (t == null || !t.name.StartsWith("Building")) continue;
            Vector3 d = new Vector3(t.position.x - p.x, 0f, t.position.z - p.z);
            if (d.sqrMagnitude < r2) return false;
        }
        return true;
    }

    /// <summary>Iglesia de la Plaza Constitución: nave, fachada, dos torres y cruces.</summary>
    private static void CrearIglesia(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t != null && t.name.StartsWith("Iglesia_Plaza"))
                Undo.DestroyObjectImmediate(t.gameObject);

        // PRIMERO se devuelven las casas que se apartaron la vez anterior.
        // Si se hiciera despues de elegir el sitio, la busqueda estaria mirando
        // una ciudad con los huecos de la pasada pasada: creeria que hay espacio
        // libre donde en realidad hay casas, y al devolverlas tendria que apartar
        // cada vez mas (en dos pasadas ya iba de 11 a 26).
        RestaurarDespejado();

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);
        Vector3 pz = zonas[0];

        // El templo va EN UN LOTE DE LA MANZANA, detrás de la vereda y MIRANDO A
        // LA CALLE, alineado a la cuadrícula (antes caía en diagonal y en
        // cualquier hueco, por eso se veía fuera de lugar).
        List<Vector3> dirsIg = new List<Vector3>();
        List<Vector3> vereda = PuntosDeVereda(tramos, pz, 70f, dirsIg);

        // El templo se arma PRIMERO para poder medirlo. El prefab de iglesia del
        // proyecto es mucho mas grande que el templo de primitivas, y como antes
        // se reservaba un radio fijo de 19 u y se metia solo 16 u en la manzana,
        // la iglesia terminaba montada sobre la vereda y parte de la calzada.
        // Ahora el hueco que pide y lo que se mete en el lote salen de su tamano.
        GameObject ig = ArmarTemplo("Iglesia_PlazaConstitucion");

        Bounds caja = CajaDe(ig);
        float medioFondo  = Mathf.Max(caja.extents.z, caja.extents.x, 6f);
        float radioLibre  = medioFondo + 5f;
        float dentroDelLote = medioFondo + 7f;      // la fachada queda tras la vereda

        Vector3 pos = pz + new Vector3(30f, 0f, 0f);
        Vector3 haciaCalle = Vector3.forward;
        bool hallado = false;

        // Medio ancho real del templo, para comprobar TODA su huella.
        float medioAncho = Mathf.Max(caja.extents.x, caja.extents.z, 5f);

        for (int intento = 0; intento < 3 && !hallado; intento++)
        {
            // Si con el hueco ideal no entra en ningun lado se afloja un poco,
            // pero la comprobacion contra la calzada NO se afloja nunca.
            float pide = radioLibre * (1f - intento * 0.18f);

            for (int i = 0; i < vereda.Count && !hallado; i++)
            {
                Vector3 hc = dirsIg[i]; hc.y = 0f;
                if (hc.sqrMagnitude < 0.01f) continue;
                hc.Normalize();

                Vector3 q = vereda[i] - hc * dentroDelLote;    // dentro de la manzana
                Vector3 d = new Vector3(q.x - pz.x, 0f, q.z - pz.z);
                float dist = d.magnitude;
                if (dist < 16f || dist > 62f) continue;

                // ANTES solo se miraba el CENTRO de la iglesia. Por eso seguia
                // saliendo a la pista: el centro caia en la manzana pero el
                // templo es grande y sus esquinas se comian media calzada. Ahora
                // se comprueban las cuatro esquinas y los cuatro lados.
                if (HuellaSobreLaPista(q, hc, medioAncho, medioFondo, tramos)) continue;
                if (!LugarLibre(q, pide)) continue;

                pos = q; haciaCalle = hc; hallado = true;
            }
        }

        // Girarla ANTES de apoyarla: al rotar cambia su huella y el pie del modelo.
        ig.transform.rotation = Quaternion.LookRotation(haciaCalle);
        ApoyarEn(ig, new Vector3(pos.x, SueloY(pos.x, pos.z, 0f), pos.z), 0f);

        // Y ahora se le hace sitio: se apartan las casas que le quedan encima.
        // Es lo minimo necesario —solo las que se solapan de verdad con el
        // templo y su atrio— y es reversible, porque cada una queda marcada.
        Bounds huella = CajaDe(ig);
        huella.Expand(new Vector3(5f, 0f, 5f));
        huella.Encapsulate(ig.transform.position + haciaCalle * (medioFondo + 11f));  // el atrio
        int apartadas = DespejarCaja(huella, ig);

        // Campana de la plaza: suena cada tanto, generada por código.
        if (ig.GetComponent<CampanaIglesia>() == null) ig.AddComponent<CampanaIglesia>();

        // Atrio empedrado delante, entre el templo y la vereda. Se le pasa la
        // huella para que el empedrado cubra TODO el lote despejado: al apartar
        // las casas queda a la vista la losa desnuda de la manzana, y eso era lo
        // que se veia como un piso de tablas alrededor del templo.
        // La iglesia esta en la PLAZA CONSTITUCION: una plaza de ciudad es
        // empedrada de vereda a vereda, con jardineras, no un pastizal con un
        // templo en el medio. Si se puede medir la manzana, el atrio la toma
        // entera; si no, se queda con la huella del templo como antes.
        Bounds manzana;
        Bounds plaza = LimitesDeLaManzana(ig.transform.position, out manzana) ? manzana : huella;

        AtrioDeIglesia(pos, haciaCalle, plaza);

        // El cerco va solo alrededor del TEMPLO, no de la plaza entera: los
        // modulos del kit traen colisionador, y cercar la manzana completa
        // dejaria al jugador dando la vuelta buscando la unica entrada.
        CercoDeIglesia(huella, haciaCalle);
        JardinerasDeLaPlaza(plaza, ig);

        Undo.RegisterCreatedObjectUndo(ig, "Crear iglesia");

        Debug.Log("[Guardian] Iglesia " + (hallado ? "en su lote" : "SIN lote libre")
                  + " · " + apartadas + " construcciones apartadas para el atrio");
    }

    /// <summary>
    /// ¿La huella del templo pisa la calzada? Comprueba las cuatro esquinas y
    /// los puntos medios de los lados, no solo el centro.
    /// </summary>
    private static bool HuellaSobreLaPista(Vector3 centro, Vector3 hacia,
                                           float medioAncho, float medioFondo,
                                           List<Tramo> tramos)
    {
        Vector3 f = hacia.normalized;
        Vector3 l = Vector3.Cross(Vector3.up, f);

        for (int a = -1; a <= 1; a++)
            for (int b = -1; b <= 1; b++)
            {
                Vector3 p = centro + l * (a * medioAncho) + f * (b * medioFondo);
                if (SobreLaPista(p, tramos)) return true;
            }
        return false;
    }

    /// <summary>
    /// Aparta las construcciones que quedan dentro de una caja, salvo la propia
    /// pieza que se esta colocando. No las destruye: las desactiva y las marca,
    /// asi se pueden devolver despues.
    /// </summary>
    private static int DespejarCaja(Bounds caja, GameObject excepto)
    {
        List<GameObject> fuera = new List<GameObject>();

        foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (mr == null || !mr.gameObject.activeInHierarchy) continue;
            if (excepto != null && mr.transform.IsChildOf(excepto.transform)) continue;
            if (!EsDeLaCiudad(mr.transform)) continue;

            Bounds b = mr.bounds;
            if (b.size.x > 60f || b.size.z > 60f) continue;      // el suelo base no se toca

            // Solo en planta: la altura no importa para saber si estorba.
            if (b.max.x < caja.min.x || b.min.x > caja.max.x) continue;
            if (b.max.z < caja.min.z || b.min.z > caja.max.z) continue;

            Transform raiz = mr.transform;
            for (int k = 0; k < 2 && raiz.parent != null; k++)
            {
                Transform pa = raiz.parent;
                if (pa.name.StartsWith("===")) break;
                if (TamanoPlano(pa) > 45f) break;
                raiz = pa;
            }
            if (raiz.gameObject == excepto) continue;
            if (!fuera.Contains(raiz.gameObject)) fuera.Add(raiz.gameObject);
        }

        foreach (GameObject g in fuera)
        {
            Undo.RecordObject(g, "Apartar para la iglesia");
            if (g.GetComponent<DespejadoPorElRio>() == null)
                Undo.AddComponent<DespejadoPorElRio>(g);
            g.SetActive(false);
        }
        return fuera.Count;
    }

    /// <summary>
    /// Devuelve el templo: usa un prefab de iglesia del proyecto si existe
    /// (busca por nombre: church / iglesia / catedral / capilla), y si no,
    /// lo arma con primitivas al estilo de las iglesias del valle.
    /// </summary>
    private static GameObject ArmarTemplo(string nombre)
    {
        GameObject pf = BuscarPrefabIglesia();
        if (pf != null)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            g.name = nombre;
            g.transform.localScale = Vector3.one;
            ArreglarMaterialesURP(g);

            // Escalar a una fachada de ~18 m, que es lo que pide la manzana.
            Renderer[] rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                float ancho = Mathf.Max(b.size.x, b.size.z);
                if (ancho > 0.001f) g.transform.localScale = Vector3.one * (18f / ancho);
            }

            if (g.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider bc = g.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, 5f, -3f);
                bc.size = new Vector3(14f, 10f, 16f);
            }
            return g;
        }

        GameObject ig = new GameObject(nombre);
        TemploDePrimitivas(ig);
        return ig;
    }

    /// <summary>Busca en el proyecto un prefab que sea una iglesia.</summary>
    private static GameObject BuscarPrefabIglesia()
    {
        string[] claves = { "church", "iglesia", "cathedral", "catedral", "chapel", "capilla" };
        foreach (string k in claves)
        {
            foreach (string guid in AssetDatabase.FindAssets(k + " t:Prefab"))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(ruta)) continue;
                if (ruta.StartsWith("Assets/Guardian")) continue;
                GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (pf == null) continue;
                if (pf.GetComponentInChildren<Renderer>() == null) continue;
                return pf;
            }
        }
        return null;
    }

    /// <summary>
    /// Cerco perimetral del atrio con el Modular Fence Kit.
    ///
    /// Las iglesias del valle tienen su atrio cercado: no es decoracion, es como
    /// se ven de verdad. El kit es modular (Wall, Pillar, Corner), asi que en vez
    /// de dar un tamano a mano se MIDE un modulo y se calcula cuantos entran por
    /// lado; asi funciona igual si el kit cambia de escala o si el lote sale de
    /// otro tamano. Se deja un vano en el lado que da a la calle: un cerco sin
    /// entrada seria absurdo, y ademas el jugador tiene que poder pasar.
    /// </summary>
    private static void CercoDeIglesia(Bounds lote, Vector3 haciaCalle)
    {
        foreach (Transform vt in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (vt != null && vt.name.StartsWith("Iglesia_Cerco"))
                Undo.DestroyObjectImmediate(vt.gameObject);

        const string FBASE2 = "Assets/Modular Fence Kit/Prefabs/";
        GameObject pfMuro  = AssetDatabase.LoadAssetAtPath<GameObject>(FBASE2 + "Wall.prefab");
        GameObject pfPilar = AssetDatabase.LoadAssetAtPath<GameObject>(FBASE2 + "Pillar.prefab");
        if (pfPilar == null) pfPilar = AssetDatabase.LoadAssetAtPath<GameObject>(FBASE2 + "Corner.prefab");
        if (pfMuro == null) return;

        // Medir el modulo real del kit.
        GameObject tmp = (GameObject)PrefabUtility.InstantiatePrefab(pfMuro);
        Bounds bm = CajaDe(tmp);
        Object.DestroyImmediate(tmp);
        float largoBruto = Mathf.Max(bm.size.x, bm.size.z);
        float altoBruto  = Mathf.Max(bm.size.y, 0.01f);
        bool moduloEnX   = bm.size.x >= bm.size.z;
        if (largoBruto < 0.05f) return;

        // Cerco de 1.55 m: alto para que sea un cerco, bajo para que no tape la
        // fachada del templo, que es lo que el jugador tiene que ver.
        float esc = Mathf.Clamp(1.55f / altoBruto, 0.05f, 12f);
        float modulo = largoBruto * esc;

        GameObject raiz = new GameObject("Iglesia_Cerco");
        raiz.transform.position = new Vector3(lote.center.x,
            SueloY(lote.center.x, lote.center.z, 0f) + 0.12f, lote.center.z);
        Undo.RegisterCreatedObjectUndo(raiz, "Cerco de la iglesia");

        // El cerco va por dentro de la vereda perimetral.
        float mx = lote.size.x * 0.5f - 2.9f;
        float mz = lote.size.z * 0.5f - 2.9f;
        if (mx < 3f || mz < 3f) return;

        // Que lado mira a la calle: ahi va el vano de entrada.
        int ladoCalle = (Mathf.Abs(haciaCalle.x) > Mathf.Abs(haciaCalle.z))
            ? (haciaCalle.x > 0f ? 3 : 2)
            : (haciaCalle.z > 0f ? 1 : 0);

        int puestos = 0;
        for (int lado = 0; lado < 4; lado++)
        {
            bool enX = (lado < 2);                       // 0 = -Z, 1 = +Z, 2 = -X, 3 = +X
            float largoLado = enX ? mx * 2f : mz * 2f;
            int n = Mathf.FloorToInt(largoLado / modulo);
            if (n < 2) continue;

            // Vano de entrada centrado, de dos modulos (o uno si el lado es corto).
            int hueco = (lado == ladoCalle) ? Mathf.Max(1, n / 5) : 0;
            int desde = (n - hueco) / 2;

            float giro = enX ? (moduloEnX ? 0f : 90f) : (moduloEnX ? 90f : 0f);

            for (int i = 0; i < n; i++)
            {
                if (hueco > 0 && i >= desde && i < desde + hueco) continue;

                float t = -largoLado * 0.5f + modulo * (i + 0.5f);
                Vector3 loc = enX
                    ? new Vector3(t, 0f, (lado == 0 ? -mz : mz))
                    : new Vector3((lado == 2 ? -mx : mx), 0f, t);

                GameObject m = (GameObject)PrefabUtility.InstantiatePrefab(pfMuro);
                m.name = "Iglesia_CercoMuro_" + lado + "_" + i;
                m.transform.SetParent(raiz.transform, false);
                m.transform.localPosition = loc;
                m.transform.localEulerAngles = new Vector3(0f, giro, 0f);
                m.transform.localScale = Vector3.one * esc;
                ArreglarMaterialesURP(m);
                puestos++;
            }
        }

        // Pilares en las cuatro esquinas y a los lados del vano.
        if (pfPilar != null)
        {
            Vector3[] esquinas = {
                new Vector3(-mx, 0f, -mz), new Vector3( mx, 0f, -mz),
                new Vector3(-mx, 0f,  mz), new Vector3( mx, 0f,  mz),
            };
            for (int i = 0; i < esquinas.Length; i++)
            {
                GameObject pi = (GameObject)PrefabUtility.InstantiatePrefab(pfPilar);
                pi.name = "Iglesia_CercoPilar_" + i;
                pi.transform.SetParent(raiz.transform, false);
                pi.transform.localPosition = esquinas[i];
                pi.transform.localScale = Vector3.one * esc * 1.06f;
                ArreglarMaterialesURP(pi);
            }
        }

        Debug.Log("[Guardian] Cerco del atrio \u00b7 " + puestos + " m\u00f3dulos de "
                + modulo.ToString("0.0") + " m \u00b7 vano de entrada hacia la calle");
    }

    /// <summary>
    /// Jardineras de la plaza.
    ///
    /// Empedrar la manzana entera y dejarla pelada la volveria un estacionamiento.
    /// Las plazas del valle tienen canteros con pasto y arbolitos repartidos por
    /// la explanada; ademas devuelven algo de verde sin que el verde sea el piso.
    /// Se ponen en las cuatro esquinas de la manzana, que es donde no estorban al
    /// atrio ni al paso hacia la puerta.
    /// </summary>
    private static void JardinerasDeLaPlaza(Bounds plaza, GameObject templo)
    {
        foreach (Transform vt in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (vt != null && vt.name.StartsWith("Plaza_Jardinera"))
                Undo.DestroyObjectImmediate(vt.gameObject);

        Bounds caja = CajaDe(templo);
        caja.Expand(new Vector3(2.5f, 0f, 2.5f));

        Material pasto = MaterialSimple("Assets/Guardian/Materiales/Plaza_jardin.mat",
            Color.white, 0.03f, TexturaPastoSuave(), new Vector2(3f, 3f));
        Material borde = MaterialDelPack("Plaza_sardinel", "Cracked_Concrete_26",
                                         new Vector2(3f, 1f), 0.05f, new Color(0.86f, 0.85f, 0.82f));
        if (borde == null) borde = MatColor("Plaza_sardinel_plano", new Color(0.78f, 0.77f, 0.74f), 0.05f);

        float y = SueloY(plaza.center.x, plaza.center.z, 0f) + 0.12f;
        float mx = plaza.size.x * 0.5f - 4.6f;
        float mz = plaza.size.z * 0.5f - 4.6f;
        if (mx < 5f || mz < 5f) return;

        int puestas = 0;
        for (int i = 0; i < 4; i++)
        {
            float sx = (i % 2 == 0) ? -1f : 1f;
            float sz = (i < 2) ? -1f : 1f;

            float ancho = Mathf.Clamp(plaza.size.x * 0.22f, 4f, 9f);
            float fondo = Mathf.Clamp(plaza.size.z * 0.22f, 4f, 9f);

            Vector3 c = new Vector3(plaza.center.x + sx * (mx - ancho * 0.5f + 1.2f), y,
                                    plaza.center.z + sz * (mz - fondo * 0.5f + 1.2f));

            // Que no pise el templo ni su atrio.
            if (Mathf.Abs(c.x - caja.center.x) < (caja.size.x + ancho) * 0.5f &&
                Mathf.Abs(c.z - caja.center.z) < (caja.size.z + fondo) * 0.5f) continue;

            GameObject j = new GameObject("Plaza_Jardinera_" + i);
            j.transform.position = c;
            Undo.RegisterCreatedObjectUndo(j, "Jardinera de la plaza");

            // Sardinel y tierra de pasto, un palmo por encima del empedrado.
            Pieza(j, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f),
                  new Vector3(ancho, 0.18f, fondo), Vector3.zero, borde, "Sardinel");
            Pieza(j, PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f),
                  new Vector3(ancho - 0.55f, 0.16f, fondo - 0.55f), Vector3.zero, pasto, "Pasto");

            // Un arbolito y un par de arbustos por jardinera.
            string arbol = null;
            for (int k = 0; k < PREFABS_ARBOLES.Length && arbol == null; k++)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_ARBOLES[k]) != null)
                    arbol = PREFABS_ARBOLES[k];

            if (arbol != null)
            {
                GameObject ab = PiezaPrefab(j, arbol, Vector3.zero, Random.Range(5.5f, 7.5f),
                                            new Vector3(0f, Random.Range(0f, 360f), 0f), "Arbol");
                if (ab != null) ab.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            }
            for (int k = 0; k < 2; k++)
            {
                string ar = PREFABS_ARBUSTOS[Random.Range(0, PREFABS_ARBUSTOS.Length)];
                GameObject bu = PiezaPrefab(j, ar, Vector3.zero, Random.Range(1.1f, 1.9f),
                                            new Vector3(0f, Random.Range(0f, 360f), 0f), "Arbusto_" + k);
                if (bu != null)
                    bu.transform.localPosition = new Vector3(
                        Random.Range(-ancho * 0.28f, ancho * 0.28f), 0.22f,
                        Random.Range(-fondo * 0.28f, fondo * 0.28f));
            }
            puestas++;
        }

        Debug.Log("[Guardian] Plaza Constituci\u00f3n \u00b7 manzana de "
                + plaza.size.x.ToString("0") + " x " + plaza.size.z.ToString("0")
                + " m empedrada \u00b7 " + puestas + " jardineras");
    }

    /// <summary>
    /// Limites de la MANZANA que contiene un punto.
    ///
    /// No hace falta conocer el trazado: se camina desde el centro hacia los
    /// cuatro rumbos hasta pisar calzada, y ahi esta el borde. Sirve igual si la
    /// manzana es cuadrada o alargada, y no depende de que el grid de SimplePoly
    /// siga siendo de 20 unidades.
    /// </summary>
    private static bool LimitesDeLaManzana(Vector3 centro, out Bounds manzana)
    {
        manzana = new Bounds(centro, Vector3.zero);

        List<Tramo> tramos = AnalizarVias(RecolectarVias());
        if (tramos.Count == 0) return false;
        if (SobreLaPista(centro, tramos)) return false;   // el centro esta en la pista

        Vector3[] rumbos = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        float[] hasta = new float[4];

        for (int r = 0; r < 4; r++)
        {
            float d = 0f;
            bool hallada = false;
            while (d < 70f)
            {
                d += 1f;
                if (SobreLaPista(centro + rumbos[r] * d, tramos)) { hallada = true; break; }
            }
            // Si por ese rumbo no aparece calzada —pasa en la manzana del borde,
            // donde la ciudad se acaba antes que la calle— se toma media manzana
            // tipica en vez de descartar la medicion entera, que era lo que hacia
            // que la plaza se quedara del tamano de la huella del templo.
            hasta[r] = hallada ? Mathf.Max(2f, d - 1f) : 24f;
        }

        float xMin = centro.x - hasta[1], xMax = centro.x + hasta[0];
        float zMin = centro.z - hasta[3], zMax = centro.z + hasta[2];

        manzana = new Bounds(
            new Vector3((xMin + xMax) * 0.5f, centro.y, (zMin + zMax) * 0.5f),
            new Vector3(xMax - xMin, 0.1f, zMax - zMin));

        // Una manzana de menos de 14 u no es una manzana: es que el rayo se topo
        // con algo raro. Mejor no tocar nada que empedrar media calle.
        bool vale = manzana.size.x > 14f && manzana.size.z > 14f
                 && manzana.size.x < 160f && manzana.size.z < 160f;
        Debug.Log("[Guardian] Manzana de la iglesia: " + manzana.size.x.ToString("0")
                + " x " + manzana.size.z.ToString("0") + (vale ? " (se usa)" : " (descartada)"));
        return vale;
    }

    /// <summary>Atrio de piedra con gradas y cruz, delante del templo.</summary>
    private static void AtrioDeIglesia(Vector3 posIglesia, Vector3 haciaCalle, Bounds lote)
    {
        Material piedra = MatColor("Iglesia_atrio", new Color(0.74f, 0.72f, 0.68f), 0.05f);
        Material oro    = MatColor("Iglesia_oro",   new Color(0.85f, 0.72f, 0.30f), 0.75f);

        // ---- Empedrado de TODO el lote ----
        // Un atrio no es pasto ni tablas: es piedra. En las iglesias del valle
        // del Mantaro el atrio es adoquinado y llega hasta la vereda, porque es
        // donde se para la gente a la salida de misa. Antes solo habia una losa
        // de 18 x 12 delante de la puerta y alrededor quedaba al aire el piso
        // desnudo de la manzana de SimplePoly, que se leia como un entablado.
        foreach (Transform vt in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (vt != null && vt.name == "Iglesia_PlazaEmpedrado")
                Undo.DestroyObjectImmediate(vt.gameObject);

        float anchoLote = Mathf.Clamp(lote.size.x + 2f, 18f, 48f);
        float fondoLote = Mathf.Clamp(lote.size.z + 2f, 18f, 48f);

        Material empedrado = MaterialDelPack("Iglesia_empedrado", "Ancient_Cobblestone_5",
                                             new Vector2(anchoLote / 3.2f, fondoLote / 3.2f),
                                             0.06f, new Color(0.92f, 0.90f, 0.86f));
        if (empedrado == null)
            empedrado = MatColor("Iglesia_atrio", new Color(0.74f, 0.72f, 0.68f), 0.05f);

        GameObject losa = GameObject.CreatePrimitive(PrimitiveType.Cube);
        losa.name = "Iglesia_PlazaEmpedrado";
        losa.transform.position = new Vector3(lote.center.x,
            SueloY(lote.center.x, lote.center.z, 0f) + 0.05f, lote.center.z);
        losa.transform.localScale = new Vector3(anchoLote, 0.10f, fondoLote);
        losa.GetComponent<Renderer>().sharedMaterial = empedrado;
        Collider colLosa = losa.GetComponent<Collider>();
        if (colLosa != null) colLosa.enabled = false;   // se camina sobre la vereda, no sobre la losa
        Undo.RegisterCreatedObjectUndo(losa, "Empedrado del atrio");

        // ---- Vereda perimetral ----
        // Sin esto el empedrado moria de golpe contra la pista y los pasos
        // peatonales del cruce desembocaban en el pasto, que es lo que se veia
        // raro: un paso de cebra que no lleva a ninguna vereda. La franja de
        // concreto rodea el lote y engancha con la vereda de la manzana.
        Material vereda = MaterialDelPack("Iglesia_vereda", "Pavement_7",
                                          new Vector2(6f, 1.4f), 0.05f, new Color(0.90f, 0.89f, 0.87f));
        if (vereda == null) vereda = piedra;

        float anchoV = 2.7f;
        float yLosa  = SueloY(lote.center.x, lote.center.z, 0f) + 0.11f;
        for (int lado = 0; lado < 4; lado++)
        {
            bool enX = (lado < 2);
            float largoF = enX ? anchoLote : fondoLote - anchoV * 2f;
            if (largoF < 1f) continue;

            float dx = enX ? 0f : (lado == 2 ? -1f : 1f) * (anchoLote * 0.5f - anchoV * 0.5f);
            float dz = enX ? (lado == 0 ? -1f : 1f) * (fondoLote * 0.5f - anchoV * 0.5f) : 0f;

            GameObject v = GameObject.CreatePrimitive(PrimitiveType.Cube);
            v.name = "Iglesia_PlazaVereda_" + lado;
            v.transform.position = new Vector3(lote.center.x + dx, yLosa, lote.center.z + dz);
            v.transform.localScale = enX ? new Vector3(largoF, 0.06f, anchoV)
                                         : new Vector3(anchoV, 0.06f, largoF);
            v.GetComponent<Renderer>().sharedMaterial = vereda;
            Collider cv = v.GetComponent<Collider>();
            if (cv != null) cv.enabled = false;
            Undo.RegisterCreatedObjectUndo(v, "Vereda del atrio");
        }

        GameObject a = new GameObject("Iglesia_PlazaAtrio");
        Vector3 c = posIglesia + haciaCalle * 9f;
        a.transform.position = new Vector3(c.x, SueloY(c.x, c.z, 0f) + 0.02f, c.z);
        a.transform.rotation = Quaternion.LookRotation(haciaCalle);

        // Losa clara delante de la puerta, un escalon por encima del empedrado:
        // marca el atrio propiamente dicho dentro de la explanada.
        Material laja = MaterialDelPack("Iglesia_laja", "Cracked_Concrete_26",
                                        new Vector2(5f, 3.4f), 0.05f, new Color(0.88f, 0.86f, 0.82f));
        if (laja == null) laja = piedra;
        Pieza(a, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(18f, 0.16f, 12f), Vector3.zero, laja, "Piso");

        // Gradas hacia la puerta del templo.
        for (int s = 0; s < 3; s++)
            Pieza(a, PrimitiveType.Cube, new Vector3(0f, 0.16f + s * 0.16f, -5.2f + s * 0.55f),
                  new Vector3(8.0f, 0.16f, 1.1f), Vector3.zero, piedra, "Grada");

        // Cruz de piedra del atrio (las plazas del valle siempre tienen una).
        Pieza(a, PrimitiveType.Cube,     new Vector3(0f, 0.35f, 4.4f), new Vector3(2.0f, 0.55f, 2.0f), Vector3.zero, piedra, "BasaCruz");
        Pieza(a, PrimitiveType.Cylinder, new Vector3(0f, 1.90f, 4.4f), new Vector3(0.22f, 1.30f, 0.22f), Vector3.zero, piedra, "FusteCruz");
        Pieza(a, PrimitiveType.Cube,     new Vector3(0f, 3.55f, 4.4f), new Vector3(0.22f, 1.30f, 0.22f), Vector3.zero, oro,    "CruzV");
        Pieza(a, PrimitiveType.Cube,     new Vector3(0f, 3.85f, 4.4f), new Vector3(1.10f, 0.22f, 0.22f), Vector3.zero, oro,    "CruzH");

        // Bancas a los costados del atrio.
        Material madera = MatColor("Atrio_banca", new Color(0.50f, 0.36f, 0.24f), 0.10f);
        for (int k = 0; k < 4; k++)
        {
            float sx = (k % 2 == 0) ? -6.6f : 6.6f;
            float sz = (k < 2) ? 1.2f : -1.8f;
            Pieza(a, PrimitiveType.Cube, new Vector3(sx, 0.55f, sz), new Vector3(0.6f, 0.14f, 2.2f), Vector3.zero, madera, "Banca");
            Pieza(a, PrimitiveType.Cube, new Vector3(sx, 0.28f, sz), new Vector3(0.2f, 0.42f, 1.8f), Vector3.zero, piedra, "PataBanca");
        }

        // Banderas rojo y blanco (colores wanka) a los costados del atrio.
        Material rojo   = MatColor("Bandera_roja",   new Color(0.80f, 0.12f, 0.12f), 0.05f);
        Material blanco = MatColor("Bandera_blanca", new Color(0.95f, 0.95f, 0.93f), 0.05f);
        for (int k = 0; k < 4; k++)
        {
            float sx = (k < 2) ? -7.9f : 7.9f;
            float sz = (k % 2 == 0) ? 3.6f : -2.4f;
            Pieza(a, PrimitiveType.Cylinder, new Vector3(sx, 2.2f, sz), new Vector3(0.09f, 2.2f, 0.09f), Vector3.zero, piedra, "MastilBandera");
            Pieza(a, PrimitiveType.Cube, new Vector3(sx + 0.78f, 3.85f, sz), new Vector3(1.50f, 0.42f, 0.04f), Vector3.zero, (k % 2 == 0) ? rojo : blanco, "BanderaA");
            Pieza(a, PrimitiveType.Cube, new Vector3(sx + 0.78f, 3.43f, sz), new Vector3(1.50f, 0.42f, 0.04f), Vector3.zero, (k % 2 == 0) ? blanco : rojo, "BanderaB");
        }

        // Luz cálida sobre la fachada: la iglesia es el hito de la plaza.
        GameObject luz = new GameObject("LuzFachada");
        luz.transform.SetParent(a.transform, false);
        luz.transform.localPosition = new Vector3(0f, 4.2f, -3.5f);
        Light lf = luz.AddComponent<Light>();
        lf.type = LightType.Point;
        lf.color = new Color(1f, 0.88f, 0.66f);
        lf.intensity = 2.4f;
        lf.range = 26f;
        lf.shadows = LightShadows.None;

        // Arbolitos en las esquinas del atrio.
        string arbolito = null;
        foreach (string ar in PREFABS_ARBOLES)
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ar) != null) { arbolito = ar; break; }
        if (arbolito != null)
        {
            for (int k = 0; k < 4; k++)
            {
                float sx = (k % 2 == 0) ? -7.2f : 7.2f;
                float sz = (k < 2) ? 5.0f : -4.2f;
                Vector3 mundoA = a.transform.TransformPoint(new Vector3(sx, 0f, sz));
                GameObject ab = PiezaPrefab(null, arbolito, Vector3.zero, Random.Range(4.5f, 6.5f),
                                            new Vector3(0f, Random.Range(0f, 360f), 0f),
                                            "Iglesia_PlazaArbol_" + k);
                if (ab == null) continue;
                ApoyarEn(ab, new Vector3(mundoA.x, SueloY(mundoA.x, mundoA.z, 0f), mundoA.z), 0.1f);
                Undo.RegisterCreatedObjectUndo(ab, "Arbol del atrio");
            }
        }

        // Los carros decorativos del pack que quedaron encima del atrio se apagan:
        // no tiene sentido un taxi estacionado en la puerta de la iglesia.
        LimpiarVehiculos(a.transform.position, 12f);

        Undo.RegisterCreatedObjectUndo(a, "Crear atrio de la iglesia");
    }

    /// <summary>Apaga los vehículos decorativos del pack que estorban en un punto.</summary>
    private static void LimpiarVehiculos(Vector3 centro, float radio)
    {
        float r2 = radio * radio;
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            if (t == null || !t.name.StartsWith("Vehicle")) continue;
            if (!t.gameObject.activeSelf) continue;
            Vector3 d = new Vector3(t.position.x - centro.x, 0f, t.position.z - centro.z);
            if (d.sqrMagnitude > r2) continue;
            Undo.RegisterCompleteObjectUndo(t.gameObject, "Quitar vehículo estorbo");
            t.gameObject.SetActive(false);
        }
    }

    private static void TemploDePrimitivas(GameObject ig)
    {

        Material muro   = MatColor("Iglesia_muro",   new Color(0.90f, 0.86f, 0.78f), 0.10f);
        Material techo  = MatColor("Iglesia_techo",  new Color(0.62f, 0.28f, 0.20f), 0.10f);
        Material puerta = MatColor("Iglesia_puerta", new Color(0.35f, 0.22f, 0.12f), 0.15f);
        Material oro    = MatColor("Iglesia_oro",    new Color(0.85f, 0.72f, 0.30f), 0.75f);

        Pieza(ig, PrimitiveType.Cube, new Vector3( 0f, 4.0f, -5.0f), new Vector3(9.0f,  8.0f, 15.0f), Vector3.zero,               muro,   "Nave");
        Pieza(ig, PrimitiveType.Cube, new Vector3(-2.3f, 8.6f, -5.0f), new Vector3(5.4f, 0.4f, 15.2f), new Vector3(0f, 0f,  28f), techo,  "TechoA");
        Pieza(ig, PrimitiveType.Cube, new Vector3( 2.3f, 8.6f, -5.0f), new Vector3(5.4f, 0.4f, 15.2f), new Vector3(0f, 0f, -28f), techo,  "TechoB");
        Pieza(ig, PrimitiveType.Cube, new Vector3( 0f, 5.0f,  2.6f), new Vector3(10.0f, 10.0f, 1.2f), Vector3.zero,               muro,   "Fachada");
        Pieza(ig, PrimitiveType.Cube, new Vector3( 0f, 1.9f,  3.3f), new Vector3(2.4f,  3.8f,  0.4f), Vector3.zero,               puerta, "Puerta");

        for (int k = 0; k < 2; k++)
        {
            float sx = (k == 0) ? -4.3f : 4.3f;
            Pieza(ig, PrimitiveType.Cube,     new Vector3(sx,  7.0f, 2.6f), new Vector3(3.00f, 14.0f, 3.00f), Vector3.zero, muro,  "Torre");
            Pieza(ig, PrimitiveType.Sphere,   new Vector3(sx, 14.6f, 2.6f), new Vector3(3.00f,  2.4f, 3.00f), Vector3.zero, techo, "Cupula");
            Pieza(ig, PrimitiveType.Cylinder, new Vector3(sx, 16.1f, 2.6f), new Vector3(0.12f,  0.7f, 0.12f), Vector3.zero, oro,   "MastilCruz");
            Pieza(ig, PrimitiveType.Cube,     new Vector3(sx, 16.5f, 2.6f), new Vector3(0.70f, 0.12f, 0.12f), Vector3.zero, oro,   "BrazoCruz");
        }

        Pieza(ig, PrimitiveType.Cube, new Vector3(0f, 11.2f, 2.6f), new Vector3(0.18f, 1.60f, 0.18f), Vector3.zero, oro, "CruzVertical");
        Pieza(ig, PrimitiveType.Cube, new Vector3(0f, 11.4f, 2.6f), new Vector3(0.90f, 0.18f, 0.18f), Vector3.zero, oro, "CruzHorizontal");

        // Que el templo sea sólido.
        BoxCollider cuerpo = ig.AddComponent<BoxCollider>();
        cuerpo.center = new Vector3(0f, 5f, -3.5f);
        cuerpo.size = new Vector3(11f, 10f, 17f);
    }

    /// <summary>Puesto de mercado con toldo de colores, mesa y productos.</summary>
    private static GameObject CrearPuestoMercado(int i, string nombre)
    {
        GameObject p = new GameObject(nombre);

        Color[] colores = {
            new Color(0.85f, 0.20f, 0.20f), new Color(0.20f, 0.45f, 0.80f),
            new Color(0.95f, 0.70f, 0.15f), new Color(0.20f, 0.62f, 0.35f),
        };
        Material mToldo = MatColor("Puesto_toldo_" + (i % colores.Length), colores[i % colores.Length], 0.15f);
        Material mMesa  = MatColor("Puesto_mesa",  new Color(0.52f, 0.38f, 0.24f), 0.15f);
        Material mPoste = MatColor("Puesto_poste", new Color(0.35f, 0.35f, 0.37f), 0.25f);

        Pieza(p, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f), new Vector3(2.4f, 0.12f, 1.2f), Vector3.zero, mMesa, "Mesa");

        for (int k = 0; k < 4; k++)
        {
            float sx = (k % 2 == 0) ? -1.05f : 1.05f;
            float sz = (k < 2) ? -0.45f : 0.45f;
            Pieza(p, PrimitiveType.Cube, new Vector3(sx, 0.39f, sz), new Vector3(0.1f, 0.78f, 0.1f), Vector3.zero, mMesa, "Pata");
            Pieza(p, PrimitiveType.Cylinder, new Vector3(sx * 1.1f, 1.05f, sz * 1.3f), new Vector3(0.06f, 1.05f, 0.06f), Vector3.zero, mPoste, "Poste");
        }

        // Toldo a dos aguas
        Pieza(p, PrimitiveType.Cube, new Vector3(0f, 2.22f, -0.42f), new Vector3(2.7f, 0.07f, 1.0f), new Vector3( 22f, 0f, 0f), mToldo, "ToldoA");
        Pieza(p, PrimitiveType.Cube, new Vector3(0f, 2.22f,  0.42f), new Vector3(2.7f, 0.07f, 1.0f), new Vector3(-22f, 0f, 0f), mToldo, "ToldoB");

        // Cajas y sacos al costado, como en cualquier puesto del mercado.
        Material mCaja = MatColor("Puesto_caja", new Color(0.66f, 0.47f, 0.28f), 0.08f);
        Material mSaco = MatColor("Puesto_saco", new Color(0.82f, 0.76f, 0.58f), 0.06f);
        Pieza(p, PrimitiveType.Cube, new Vector3(-1.55f, 0.22f, -0.35f), new Vector3(0.52f, 0.44f, 0.52f), new Vector3(0f, 12f, 0f), mCaja, "Caja");
        Pieza(p, PrimitiveType.Cube, new Vector3(-1.52f, 0.66f, -0.30f), new Vector3(0.50f, 0.44f, 0.50f), new Vector3(0f, -8f, 0f), mCaja, "Caja");
        Pieza(p, PrimitiveType.Cube, new Vector3( 1.58f, 0.22f, -0.32f), new Vector3(0.52f, 0.44f, 0.52f), new Vector3(0f, -15f, 0f), mCaja, "Caja");
        Pieza(p, PrimitiveType.Sphere, new Vector3(1.55f, 0.62f, -0.30f), new Vector3(0.60f, 0.46f, 0.55f), Vector3.zero, mSaco, "Saco");

        // Pizarra de precios con productos del valle del Mantaro.
        string[] oferta = {
            "PAPA HUAYRO\nS/ 2.50 kilo",  "OCA Y OLLUCO\nS/ 3.00 kilo",
            "CHOCLO\nS/ 2.00 unidad",     "QUESO FRESCO\nS/ 12.00 kilo",
            "TRUCHA\nS/ 18.00 kilo",      "HABAS VERDES\nS/ 4.00 kilo",
            "MAIZ CANCHA\nS/ 5.00 kilo",  "FRUTAS\nS/ 3.00 kilo",
        };
        PizarraPrecios(p, oferta[i % oferta.Length]);
        Banderines(p);

        // Productos reales del pack de comida sobre la mesa.
        bool hayComida = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_COMIDA[0]) != null;
        if (hayComida)
        {
            for (int k = 0; k < 8; k++)
            {
                string ruta = PREFABS_COMIDA[(i * 3 + k) % PREFABS_COMIDA.Length];
                PiezaPrefab(p, ruta,
                    new Vector3(-0.95f + k * 0.27f, 0.86f, Random.Range(-0.28f, 0.28f)),
                    Random.Range(0.26f, 0.36f),
                    new Vector3(0f, Random.Range(0f, 360f), 0f), "Producto");
            }
        }
        else
        {
            // Respaldo si el pack no está importado.
            Material[] prod = {
                MatColor("Prod_papa",    new Color(0.72f, 0.60f, 0.40f), 0.08f),
                MatColor("Prod_verde",   new Color(0.35f, 0.65f, 0.25f), 0.08f),
                MatColor("Prod_naranja", new Color(0.95f, 0.55f, 0.15f), 0.08f),
                MatColor("Prod_rojo",    new Color(0.80f, 0.20f, 0.20f), 0.08f),
            };
            for (int k = 0; k < 6; k++)
                Pieza(p, PrimitiveType.Sphere,
                      new Vector3(-0.9f + k * 0.36f, 0.95f, Random.Range(-0.25f, 0.25f)),
                      Vector3.one * Random.Range(0.18f, 0.28f), Vector3.zero,
                      prod[k % prod.Length], "Producto");
        }

        // Que el puesto sea SÓLIDO: el Guardián no puede atravesarlo.
        BoxCollider cuerpo = p.AddComponent<BoxCollider>();
        cuerpo.center = new Vector3(0f, 0.55f, 0f);
        cuerpo.size = new Vector3(2.5f, 1.1f, 1.3f);

        return p;
    }

    // ---- Detalles de barrio: mural wanka y carretilla de emoliente ----

    /// <summary>Patrón geométrico andino generado por código (grecas y rombos escalonados).</summary>
    private static Texture2D TexturaMural()
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Imagenes/mural_wanka.png";
        Texture2D ya = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (ya != null) return ya;

        int W = 512, H = 256;
        Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);

        Color fondo = new Color(0.76f, 0.33f, 0.20f);   // terracota
        Color crema = new Color(0.95f, 0.89f, 0.75f);
        Color ocre  = new Color(0.90f, 0.68f, 0.16f);
        Color negro = new Color(0.15f, 0.13f, 0.12f);
        Color verde = new Color(0.14f, 0.45f, 0.31f);

        for (int y = 0; y < H; y++)
        {
            float v = (float)y / (H - 1);
            for (int x = 0; x < W; x++)
            {
                float u = ((float)x / W) * 4f;      // 4 repeticiones del motivo
                float fu = u - Mathf.Floor(u);

                Color c;
                if (v < 0.07f || v > 0.93f)      c = negro;
                else if (v < 0.14f || v > 0.86f) c = ocre;
                else
                {
                    float dx = Mathf.Abs(fu - 0.5f);
                    float dy = Mathf.Abs(v - 0.5f) * 0.95f;
                    float d = dx + dy;
                    float q = Mathf.Floor(d * 14f) / 14f;   // escalonado, no curvo

                    if (q < 0.10f)      c = crema;
                    else if (q < 0.17f) c = negro;
                    else if (q < 0.24f) c = ocre;
                    else if (q < 0.31f) c = verde;
                    else                c = fondo;
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ruta),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null) { ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    /// <summary>Carretilla de emoliente: el desayuno de la esquina en cualquier ciudad del Perú.</summary>
    private static GameObject CrearCarretillaEmoliente(string nombre)
    {
        GameObject c = new GameObject(nombre);

        Material cuerpo = MatColor("Carretilla_cuerpo", new Color(0.16f, 0.42f, 0.72f), 0.20f);
        Material madera = MatColor("Carretilla_madera", new Color(0.55f, 0.40f, 0.26f), 0.10f);
        Material metal  = MatColor("Carretilla_metal",  new Color(0.74f, 0.74f, 0.77f), 0.65f);
        Material toldo  = MatColor("Carretilla_toldo",  new Color(0.92f, 0.82f, 0.18f), 0.10f);
        Material llanta = MatColor("Carretilla_llanta", new Color(0.12f, 0.12f, 0.13f), 0.20f);

        Pieza(c, PrimitiveType.Cube,     new Vector3(0f, 0.75f, 0f),      new Vector3(1.50f, 0.70f, 0.85f), Vector3.zero,            cuerpo, "Cajon");
        Pieza(c, PrimitiveType.Cube,     new Vector3(0f, 1.13f, 0f),      new Vector3(1.62f, 0.06f, 0.95f), Vector3.zero,            madera, "Tabla");
        Pieza(c, PrimitiveType.Cylinder, new Vector3(-0.42f, 1.33f, 0f),  new Vector3(0.36f, 0.20f, 0.36f), Vector3.zero,            metal,  "Olla");
        Pieza(c, PrimitiveType.Cylinder, new Vector3( 0.16f, 1.29f, 0f),  new Vector3(0.26f, 0.16f, 0.26f), Vector3.zero,            metal,  "OllaChica");
        for (int k = 0; k < 3; k++)
            Pieza(c, PrimitiveType.Cylinder, new Vector3(0.58f + k * 0.16f, 1.21f, 0.22f),
                  new Vector3(0.09f, 0.08f, 0.09f), Vector3.zero, metal, "Vaso");

        Pieza(c, PrimitiveType.Cylinder, new Vector3(-0.52f, 0.32f,  0.48f), new Vector3(0.62f, 0.07f, 0.62f), new Vector3(0f, 0f, 90f), llanta, "Rueda");
        Pieza(c, PrimitiveType.Cylinder, new Vector3(-0.52f, 0.32f, -0.48f), new Vector3(0.62f, 0.07f, 0.62f), new Vector3(0f, 0f, 90f), llanta, "Rueda");

        Pieza(c, PrimitiveType.Cylinder, new Vector3(0.95f, 0.88f,  0.34f), new Vector3(0.05f, 0.42f, 0.05f), new Vector3(0f, 0f, 68f), madera, "Manubrio");
        Pieza(c, PrimitiveType.Cylinder, new Vector3(0.95f, 0.88f, -0.34f), new Vector3(0.05f, 0.42f, 0.05f), new Vector3(0f, 0f, 68f), madera, "Manubrio");

        Pieza(c, PrimitiveType.Cylinder, new Vector3(0f, 1.78f, -0.28f), new Vector3(0.05f, 0.62f, 0.05f), Vector3.zero,           metal, "Palo");
        Pieza(c, PrimitiveType.Cube,     new Vector3(0f, 2.36f, -0.08f), new Vector3(1.90f, 0.06f, 1.40f), new Vector3(10f, 0f, 0f), toldo, "Sombrilla");

        BoxCollider bc = c.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.78f, 0f);
        bc.size = new Vector3(1.7f, 1.6f, 1.1f);
        return c;
    }

    /// <summary>Mural de la municipalidad y carretilla de emoliente en cada zona.</summary>
    private static void DetallesDeBarrio(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && (t.name.StartsWith("MuralWanka") || t.name.StartsWith("CarretillaEmoliente")))
                Undo.DestroyObjectImmediate(t.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);

        for (int z = 0; z < zonas.Length; z++)
        {
            List<Vector3> dirs = new List<Vector3>();
            List<Vector3> vereda = PuntosDeVereda(tramos, zonas[z], 40f, dirs);
            List<Vector3> usados = OcupadosDeZona(z);

            int ic = TomarVereda(vereda, dirs, usados, zonas[z], -1f, 8f, 30f, 6f);
            if (ic >= 0)
            {
                GameObject car = CrearCarretillaEmoliente("CarretillaEmoliente_z" + z + "_0");
                Vector3 p = vereda[ic];
                car.transform.position = new Vector3(p.x, SueloY(p.x, p.z, 0f), p.z);
                car.transform.rotation = Quaternion.LookRotation(dirs[ic]);
                Undo.RegisterCreatedObjectUndo(car, "Crear carretilla de emoliente");
            }

            if (z != 0) continue;   // el mural va en la plaza

            int im = TomarVereda(vereda, dirs, usados, zonas[z], -1f, 12f, 34f, 11f);
            if (im < 0) continue;

            Vector3 q = vereda[im];
            Vector3 haciaCalle = dirs[im];
            // Pegado al fondo de la vereda (contra los edificios), no al borde.
            q -= haciaCalle * 1.4f;

            GameObject mural = new GameObject("MuralWanka_z0_0");
            mural.transform.position = new Vector3(q.x, SueloY(q.x, q.z, 0f), q.z);
            mural.transform.rotation = Quaternion.LookRotation(haciaCalle);

            Material mm = MaterialSimple("Assets/Guardian/Materiales/Mural_wanka.mat",
                                         Color.white, 0.05f, TexturaMural(), Vector2.one);
            Material borde = MatColor("Mural_borde", new Color(0.30f, 0.28f, 0.26f), 0.10f);
            Pieza(mural, PrimitiveType.Cube, new Vector3(0f, 1.60f, 0f), new Vector3(7.0f, 3.20f, 0.30f), Vector3.zero, mm,    "Muro");
            Pieza(mural, PrimitiveType.Cube, new Vector3(0f, 3.28f, 0f), new Vector3(7.3f, 0.18f, 0.46f), Vector3.zero, borde, "Remate");

            BoxCollider bc = mural.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 1.6f, 0f);
            bc.size = new Vector3(7.0f, 3.2f, 0.4f);

            Undo.RegisterCreatedObjectUndo(mural, "Crear mural wanka");
        }
    }

    /// <summary>Banderines de fiesta colgados del toldo: puro mercado peruano.</summary>
    private static void Banderines(GameObject puesto)
    {
        Color[] cols = {
            new Color(0.85f, 0.15f, 0.15f), new Color(0.97f, 0.97f, 0.95f),
            new Color(0.96f, 0.76f, 0.12f), new Color(0.15f, 0.55f, 0.32f),
            new Color(0.20f, 0.42f, 0.78f),
        };

        Material cuerda = MatColor("Banderin_cuerda", new Color(0.30f, 0.28f, 0.24f), 0.05f);
        Pieza(puesto, PrimitiveType.Cube, new Vector3(0f, 2.52f, 0.58f),
              new Vector3(2.80f, 0.025f, 0.025f), Vector3.zero, cuerda, "Cuerda");

        for (int k = 0; k < 9; k++)
        {
            float x = -1.32f + k * 0.33f;
            Material m = MatColor("Banderin_" + (k % cols.Length), cols[k % cols.Length], 0.05f);
            Pieza(puesto, PrimitiveType.Cube, new Vector3(x, 2.40f, 0.58f),
                  new Vector3(0.21f, 0.21f, 0.015f), new Vector3(0f, 0f, 45f), m, "Banderin");
        }
    }

    /// <summary>Pizarra de precios colgada del toldo del puesto.</summary>
    private static void PizarraPrecios(GameObject puesto, string texto)
    {
        Material mPiz = MatColor("Puesto_pizarra", new Color(0.13f, 0.16f, 0.14f), 0.05f);
        Pieza(puesto, PrimitiveType.Cube, new Vector3(0f, 1.62f, 0.52f),
              new Vector3(1.5f, 0.62f, 0.05f), Vector3.zero, mPiz, "Pizarra");

        Font fuente = FuenteLetrero();
        if (fuente == null) return;

        GameObject t = new GameObject("PrecioTexto");
        t.transform.SetParent(puesto.transform, false);
        t.transform.localPosition = new Vector3(0f, 1.62f, 0.56f);
        t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        TextMesh tm = t.AddComponent<TextMesh>();
        tm.text = texto;
        tm.font = fuente;
        tm.fontSize = 90;
        tm.characterSize = 0.030f;
        tm.lineSpacing = 0.95f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(0.95f, 0.95f, 0.88f);

        MeshRenderer mr = t.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = fuente.material;
    }

    private static void GenerarPuestosMercado(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && (t.name.StartsWith("PuestoMercado_") || t.name.StartsWith("VendedorPuesto_")))
                Undo.DestroyObjectImmediate(t.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);
        int zi = Mathf.Min(1, zonas.Length - 1);          // zona del Mercado
        Vector3 pz = zonas[zi];

        // Los puestos van EN FILA SOBRE LA VEREDA, mirando a la calle, como un
        // mercado de verdad. Nunca en medio de la pista.
        List<Vector3> dirs = new List<Vector3>();
        List<Vector3> vereda = PuntosDeVereda(tramos, pz, 40f, dirs);
        List<Vector3> usados = OcupadosDeZona(zi);

        List<GameObject> vendedores = ModelosCiudadanos();
        RuntimeAnimatorController ctrlM = CargarControlador(false);
        RuntimeAnimatorController ctrlF = CargarControlador(true);

        Random.State st = Random.state;
        Random.InitState(7788);

        for (int i = 0; i < 8; i++)
        {
            int iv = TomarVereda(vereda, dirs, usados, pz, -1f, 6f, 30f, 5.2f);
            if (iv < 0) break;

            Vector3 q = vereda[iv];
            Vector3 haciaCalle = dirs[iv]; haciaCalle.y = 0f;
            if (haciaCalle.sqrMagnitude < 0.01f) haciaCalle = Vector3.forward;
            haciaCalle.Normalize();

            GameObject puesto = CrearPuestoMercado(i, "PuestoMercado_z" + zi + "_" + i);
            puesto.transform.position = new Vector3(q.x, SueloY(q.x, q.z, 0f), q.z);
            puesto.transform.rotation = Quaternion.LookRotation(haciaCalle);
            LimpiarVehiculos(puesto.transform.position, 4.5f);
            Undo.RegisterCreatedObjectUndo(puesto, "Crear puesto de mercado");

            // Vendedor detrás del puesto, mirando a la calle.
            if (vendedores.Count > 0)
            {
                GameObject v = (GameObject)PrefabUtility.InstantiatePrefab(
                    vendedores[(i * 3 + zi) % vendedores.Count]);
                bool mujer = v.name.ToLower().Contains("female");
                v.name = "VendedorPuesto_z" + zi + "_" + i;

                Vector3 atras = q - haciaCalle * 1.35f;
                v.transform.position = new Vector3(atras.x, SueloY(atras.x, atras.z, 0f), atras.z);
                v.transform.rotation = Quaternion.LookRotation(haciaCalle);

                foreach (MonoBehaviour mb in v.GetComponentsInChildren<MonoBehaviour>())
                    if (mb != null && mb.GetType().Name == "CityPeople") mb.enabled = false;

                Animator an = v.GetComponentInChildren<Animator>();
                RuntimeAnimatorController ctrl = mujer ? ctrlF : ctrlM;
                if (an != null && ctrl != null) an.runtimeAnimatorController = ctrl;

                Undo.RegisterCreatedObjectUndo(v, "Crear vendedor");
            }
        }
        Random.state = st;
    }

    /// <summary>
    /// Punto de aparición del Guardián en cada zona, SOBRE LA VEREDA. Antes salía
    /// en el centro de la zona, que es un tramo de vía: aparecía en plena pista.
    /// </summary>
    private static void CrearPuntosDeInicio(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("Inicio_z"))
                Undo.DestroyObjectImmediate(t.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);

        for (int z = 0; z < zonas.Length; z++)
        {
            List<Vector3> dirs = new List<Vector3>();
            List<Vector3> vereda = PuntosDeVereda(tramos, zonas[z], 30f, dirs);
            List<Vector3> usados = OcupadosDeZona(z);

            int iv = TomarVereda(vereda, dirs, usados, zonas[z], -1f, 4f, 22f, 4f);
            Vector3 p = (iv >= 0) ? vereda[iv] : zonas[z];
            Vector3 mira = (iv >= 0) ? dirs[iv] : Vector3.forward;

            GameObject g = new GameObject("Inicio_z" + z);
            g.transform.position = new Vector3(p.x, SueloY(p.x, p.z, 0f) + 1f, p.z);
            if (mira.sqrMagnitude > 0.01f) g.transform.rotation = Quaternion.LookRotation(mira);
            Undo.RegisterCreatedObjectUndo(g, "Crear punto de inicio");
        }
    }

    /// <summary>Prefabs de ciudadanos del pack CityPeople (los que sí caminan).</summary>
    private static List<GameObject> ModelosCiudadanos()
    {
        List<GameObject> modelos = new List<GameObject>();
        foreach (string g in AssetDatabase.FindAssets("casual t:Prefab",
                 new string[] { "Assets/DenysAlmaral/CityPeople/Prefabs" }))
        {
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
            if (pf != null) modelos.Add(pf);
        }
        return modelos;
    }

    // ---- Letreros con nombres huancaínos ----

    private static Font FuenteLetrero()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }

    private const int FUENTE_PX = 90;

    /// <summary>
    /// Cuántas unidades de mundo ocupa un texto dentro de un TextMesh.
    /// Un TextMesh arma la malla con las métricas en píxeles de la fuente y luego la
    /// escala por characterSize/10, así que el ancho real es la suma de los avances
    /// de cada carácter por ese factor. Sin medirlo no hay forma de saber si el texto
    /// entra en el tablero: antes el tablero tenía un ancho fijo y la letra salía solo
    /// de la altura, y por eso "HUANCAYO - CIUDAD INCONTRASTABLE" se salía por los dos
    /// lados del cartel.
    /// </summary>
    private static float AnchoDeTexto(Font fuente, string texto, float tamLetra)
    {
        if (fuente == null || string.IsNullOrEmpty(texto)) return 0f;

        fuente.RequestCharactersInTexture(texto, FUENTE_PX, FontStyle.Normal);

        float px = 0f;
        for (int i = 0; i < texto.Length; i++)
        {
            CharacterInfo ci;
            if (fuente.GetCharacterInfo(texto[i], out ci, FUENTE_PX, FontStyle.Normal))
                px += ci.advance;
            else
                px += FUENTE_PX * 0.62f;   // mayúsculas de Arial, por si la fuente no responde
        }
        return px * tamLetra * 0.1f * 1.04f;   // 4 % de holgura
    }

    private static void Letrero(string texto, Vector3 pos, Vector3 miraHacia, Color fondo,
                                float ancho, float alto, float altura)
    {
        GameObject raiz = new GameObject("Letrero_" + texto);
        raiz.transform.position = pos;

        Vector3 dir = miraHacia - pos; dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) raiz.transform.rotation = Quaternion.LookRotation(dir);

        // --- El tablero se ajusta al texto, no al revés ---
        Font fuente = FuenteLetrero();
        float margen  = alto * 0.35f;
        float tamLetra = alto * 0.060f;                 // ~54 % de la altura del tablero
        float anchoTexto = AnchoDeTexto(fuente, texto, tamLetra);

        float anchoFinal = ancho;
        float necesita = anchoTexto + margen * 2f;
        if (necesita > anchoFinal)                      // 1) ensancha el tablero, con tope
            anchoFinal = Mathf.Min(necesita, ancho * 1.6f);

        float util = anchoFinal - margen * 2f;
        if (anchoTexto > util && anchoTexto > 0.001f)   // 2) si aun así no entra, achica la letra
        {
            tamLetra *= util / anchoTexto;
            anchoTexto = util;
        }

        // Un cartel ancho sobre un solo poste flota; a partir de cierto ancho van dos.
        float[] postes = (anchoFinal > 7f)
            ? new float[] { -(anchoFinal * 0.5f - 0.6f), anchoFinal * 0.5f - 0.6f }
            : new float[] { 0f };

        Material matPoste = MaterialSimple(
            "Assets/Guardian/Materiales/Poste.mat", new Color(0.26f, 0.26f, 0.28f), 0.2f, null, Vector2.one);

        for (int i = 0; i < postes.Length; i++)
        {
            GameObject poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poste.name = "Poste";
            poste.transform.SetParent(raiz.transform, false);
            poste.transform.localPosition = new Vector3(postes[i], altura * 0.5f, 0f);
            poste.transform.localScale = new Vector3(0.12f, altura * 0.5f, 0.12f);
            Object.DestroyImmediate(poste.GetComponent<Collider>());
            poste.GetComponent<Renderer>().sharedMaterial = matPoste;
        }

        GameObject tablero = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tablero.name = "Tablero";
        tablero.transform.SetParent(raiz.transform, false);
        tablero.transform.localPosition = new Vector3(0f, altura, 0f);
        tablero.transform.localScale = new Vector3(anchoFinal, alto, 0.12f);
        Object.DestroyImmediate(tablero.GetComponent<Collider>());
        tablero.GetComponent<Renderer>().sharedMaterial = MaterialSimple(
            "Assets/Guardian/Materiales/Letrero_" + ColorUtility.ToHtmlStringRGB(fondo) + ".mat",
            fondo, 0.15f, null, Vector2.one);

        if (fuente != null)
        {
            // LAS DOS CARAS. El cartel se ve desde la vereda de enfrente tanto
            // como desde la de aca, y con texto en una sola cara el reverso se
            // leia en espejo. Con el material recortado por alfa, el tablero
            // opaco del medio tapa la cara de atras y cada lado se lee bien.
            Material matTexto = MaterialTexto(fuente);
            bool dosCaras = (matTexto != fuente.material);

            for (int cara = 0; cara < (dosCaras ? 2 : 1); cara++)
            {
                float z = (cara == 0) ? 0.085f : -0.085f;
                float giro = (cara == 0) ? 180f : 0f;

                GameObject t = new GameObject(cara == 0 ? "Texto" : "TextoReverso");
                t.transform.SetParent(raiz.transform, false);
                t.transform.localPosition = new Vector3(0f, altura, z);
                t.transform.localRotation = Quaternion.Euler(0f, giro, 0f);

                TextMesh tm = t.AddComponent<TextMesh>();
                tm.text = texto;
                tm.font = fuente;
                tm.fontSize = FUENTE_PX;
                tm.characterSize = tamLetra;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;

                MeshRenderer mr = t.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = matTexto;
            }
        }

        Undo.RegisterCreatedObjectUndo(raiz, "Crear letrero");
    }

    private static void CrearLetreros(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("Letrero_"))
                Undo.DestroyObjectImmediate(t.gameObject);

        List<Vector3> vias = RecolectarVias();
        Vector3[] zonas = ElegirZonas(vias, centro);

        string[] nombreZona = {
            "PLAZA CONSTITUCION",
            "MERCADO MAYORISTA - EL TAMBO",
            "RIBERA DEL RIO SHULLCAS",
        };
        Color[] colorZona = {
            new Color(0.72f, 0.16f, 0.16f),
            new Color(0.90f, 0.55f, 0.10f),
            new Color(0.12f, 0.42f, 0.62f),
        };
        string[][] tiendas = {
            new string[] { "Botica Shullcas", "Cabinas de Internet", "Panaderia La Merced" },
            new string[] { "Papa Huayro - Verduras", "Chicharroneria El Wanka", "Jugos y Extractos" },
            new string[] { "Polleria Huancaina", "Bodega Don Aurelio", "Truchas del Shullcas" },
        };

        List<Tramo> tramos = AnalizarVias(vias);

        // Cartel de bienvenida en la entrada de la ciudad.
        {
            Vector3 pz0 = zonas[0];
            List<Vector3> dBien = new List<Vector3>();
            List<Vector3> vBien = PuntosDeVereda(tramos, pz0, 120f, dBien);
            int mejor = -1; float lejos = -1f;
            for (int i = 0; i < vBien.Count; i++)
            {
                float d = (vBien[i] - pz0).magnitude;
                if (d > 130f) continue;
                if (d > lejos && LugarLibre(vBien[i], 6f)) { lejos = d; mejor = i; }
            }
            if (mejor >= 0)
            {
                Vector3 q0 = vBien[mejor];
                Letrero("HUANCAYO - CIUDAD INCONTRASTABLE",
                        new Vector3(q0.x, SueloY(q0.x, q0.z, 0f), q0.z),
                        q0 + dBien[mejor] * 6f,
                        new Color(0.72f, 0.16f, 0.16f), 15f, 2.3f, 6.8f);
            }
        }

        for (int z = 0; z < zonas.Length && z < nombreZona.Length; z++)
        {
            Vector3 pz = zonas[z];

            // Los letreros van al borde de la VEREDA, mirando a la calle.
            List<Vector3> dirs = new List<Vector3>();
            List<Vector3> vereda = PuntosDeVereda(tramos, pz, 44f, dirs);
            List<Vector3> usados = OcupadosDeZona(z);

            int iz = TomarVereda(vereda, dirs, usados, pz, -1f, 8f, 26f, 9f);
            Vector3 p = (iz >= 0) ? vereda[iz] : pz + new Vector3(9f, 0f, 9f);
            Vector3 dz = (iz >= 0) ? dirs[iz] : (pz - p).normalized;
            Letrero(nombreZona[z], new Vector3(p.x, SueloY(p.x, p.z, 0f), p.z), p + dz * 6f,
                    colorZona[z], 10f, 1.7f, 5.4f);

            for (int k = 0; k < tiendas[z].Length; k++)
            {
                float ang = (k / (float)tiendas[z].Length) * Mathf.PI * 2f + 0.8f;
                int iq = TomarVereda(vereda, dirs, usados, pz, ang, 14f, 40f, 9f);
                Vector3 q = (iq >= 0) ? vereda[iq]
                          : pz + new Vector3(Mathf.Cos(ang) * 19f, 0f, Mathf.Sin(ang) * 19f);
                Vector3 dq = (iq >= 0) ? dirs[iq] : (pz - q).normalized;
                Letrero(tiendas[z][k], new Vector3(q.x, SueloY(q.x, q.z, 0f), q.z), q + dq * 6f,
                        new Color(0.14f, 0.15f, 0.18f), 6f, 1.15f, 3.5f);
            }
        }
    }

    private static void CrearCerros(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("Cerro_"))
                Undo.DestroyObjectImmediate(t.gameObject);

        AsegurarCarpetas();

        // 3 mallas distintas guardadas como assets y reutilizadas
        Mesh[] mallas = new Mesh[3];
        for (int k = 0; k < mallas.Length; k++)
        {
            string ruta = "Assets/Guardian/Mallas/Cerro_uv_" + k + ".asset";
            mallas[k] = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (mallas[k] == null)
            {
                mallas[k] = MallaCerro(7 + k, 60f, 42f + k * 10f, 1234 + k * 77);
                AssetDatabase.CreateAsset(mallas[k], ruta);
            }
        }
        AssetDatabase.SaveAssets();

        Material matCerro = MaterialSimple("Assets/Guardian/Materiales/Cerro.mat",
                                           new Color(0.95f, 0.95f, 0.95f), 0.03f,
                                           TexturaCerro(), Vector2.one);

        Vector3 cCiudad, tam;
        if (!LimitesCiudad(out cCiudad, out tam)) { cCiudad = centro; tam = new Vector3(150f, 0f, 150f); }

        // La ciudad no es un círculo: se usa una ELIPSE con los límites reales.
        // Y a cada cerro se le suma SU PROPIO radio de falda, para que la ladera
        // nunca invada los edificios.
        float semiX = tam.x * 0.5f;
        float semiZ = tam.z * 0.5f;
        const float MARGEN = 75f;    // despejado entre la ciudad y el pie del cerro
        const float RADIO_MALLA = 78f; // radio máximo de la falda en la malla base

        // Suelo del valle: sin esto, más allá de la ciudad se ve el color de tierra
        // del cielo y parece que el mapa se termina de golpe.
        // TODOS, no solo el primero: GameObject.Find devuelve uno y si por lo que
        // fuera habia dos, el segundo se quedaba para siempre haciendo z-fighting
        // con el nuevo.
        foreach (Transform vt in Object.FindObjectsOfType<Transform>())
            if (vt != null && vt.name == "SueloValle")
                Undo.DestroyObjectImmediate(vt.gameObject);

        // ANTES: dos verdes distintos y a distinta altura. El prado del valle era
        // un plano liso (0.44, 0.52, 0.30) a y = -0.35 y la ribera del Shullcas
        // otro verde (0.33, 0.55, 0.24) a y = 0.02. Donde se cruzaban salia esa
        // cuna oscura de bordes rectos que no correspondia a nada del terreno: el
        // "pasto ilogico". Ahora los dos usan LA MISMA textura de pasto y quedan
        // casi al ras, asi que el verde se lee como un solo prado.
        GameObject sueloValle = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sueloValle.name = "SueloValle";
        sueloValle.transform.position = new Vector3(cCiudad.x, -0.05f, cCiudad.z);
        sueloValle.transform.localScale = new Vector3(220f, 1f, 220f);   // 2200 x 2200
        // Pasto del pack si esta; si no, el generado por codigo. Mosaico de ~15 u:
        // mas grande y se ve la repeticion, mas chico y titila a la distancia.
        // Mosaico de ~4.5 u. Con 15 u cada mata de pasto media dos metros y el
        // prado parecia de brocoli; a esta escala la mata mide un palmo, que es
        // lo que mide una mata.
        Material matPasto = MaterialSimple("Assets/Guardian/Materiales/Suelo_valle.mat",
            Color.white, 0.02f, TexturaPastoSuave(), new Vector2(300f, 300f));
        sueloValle.GetComponent<Renderer>().sharedMaterial = matPasto;
        Collider colSuelo = sueloValle.GetComponent<Collider>();
        if (colSuelo != null) colSuelo.enabled = false;
        Undo.RegisterCreatedObjectUndo(sueloValle, "Crear suelo del valle");

        // Modelos de montana del proyecto. Si hay, mandan ellos; si no, se cae a
        // las mallas generadas por codigo y el valle igual queda cerrado.
        List<string> modelos = PrefabsDeCerro();

        Random.State st = Random.state;
        Random.InitState(20260914);
        int n = 20;
        int conModelo = 0;
        for (int i = 0; i < n; i++)
        {
            float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.05f, 0.05f);

            float s  = Random.Range(0.80f, 1.50f);
            float sx = s * Random.Range(0.9f, 1.25f);
            float sz = s * Random.Range(0.9f, 1.25f);
            float radioFalda = RADIO_MALLA * Mathf.Max(sx, sz);

            float dx = (semiX + MARGEN + radioFalda) * Mathf.Cos(ang);
            float dz = (semiZ + MARGEN + radioFalda) * Mathf.Sin(ang);
            Vector3 p = cCiudad + new Vector3(dx, -4f, dz);

            GameObject cerro = null;

            if (modelos.Count > 0)
            {
                // Se alternan los modelos disponibles y cada uno sale con su
                // tamano y su giro: veinte copias identicas en circulo se notan
                // de inmediato, y es justo lo que hace que un paisaje parezca de
                // relleno en vez de un valle.
                string ruta = modelos[(i * 3 + 1) % modelos.Count];
                float anchoCerro = Random.Range(150f, 260f) * Mathf.Max(sx, sz) * 0.8f;

                cerro = PiezaPrefab(null, ruta, Vector3.zero, anchoCerro,
                                    new Vector3(0f, Random.Range(0f, 360f), 0f), "Cerro_" + i);

                if (cerro != null)
                {
                    conModelo++;

                    // Achatar o estirar un poco cada uno, sin tocar la planta:
                    // mas variedad de siluetas con los mismos dos modelos.
                    Vector3 esc = cerro.transform.localScale;
                    cerro.transform.localScale = new Vector3(esc.x, esc.y * Random.Range(0.72f, 1.22f), esc.z);

                    // Apoyado y un poco hundido: el pie de la montana tiene que
                    // morir en el pasto, no quedar cortado a pico sobre el.
                    ApoyarEn(cerro, new Vector3(p.x, 0f, p.z), anchoCerro * 0.035f);

                    foreach (MeshFilter mf in cerro.GetComponentsInChildren<MeshFilter>())
                    {
                        if (mf == null || mf.sharedMesh == null) continue;
                        if (mf.GetComponent<Collider>() != null) continue;
                        MeshCollider mcp = mf.gameObject.AddComponent<MeshCollider>();
                        mcp.sharedMesh = mf.sharedMesh;
                    }
                }
            }

            if (cerro == null)
            {
                cerro = new GameObject("Cerro_" + i);
                cerro.transform.position = p;
                cerro.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                cerro.transform.localScale = new Vector3(sx, s, sz);

                cerro.AddComponent<MeshFilter>().sharedMesh = mallas[i % mallas.Length];
                cerro.AddComponent<MeshRenderer>().sharedMaterial = matCerro;

                MeshCollider mc = cerro.AddComponent<MeshCollider>();
                mc.sharedMesh = mallas[i % mallas.Length];
            }

            Undo.RegisterCreatedObjectUndo(cerro, "Crear cerro");
        }
        Random.state = st;

        Debug.Log("[Guardian] Cerros del valle · " + conModelo + " de " + n
                + " con modelo del proyecto (" + modelos.Count + " modelos disponibles)"
                + (modelos.Count > 0 ? ": " + System.IO.Path.GetFileNameWithoutExtension(modelos[0]) : ""));
    }

    /// <summary>
    /// Adorna las laderas con las rocas y los pinos del pack "Hill Rock Mountain
    /// Terrain": los cerros dejan de ser conos pelados. Se apoyan con raycast
    /// sobre la malla real del cerro.
    /// </summary>
    private static int DecorarCerros()
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("AdornoCerro_"))
                Undo.DestroyObjectImmediate(t.gameObject);

        const string RB = "Assets/Hill Rock Mountain Terrain/Prefab/";
        List<string> lRocas = new List<string>();
        foreach (string r in new[] { "rock_set_01", "rock_set_02", "rock_set_03", "rock_set_04" })
            if (AssetDatabase.LoadAssetAtPath<GameObject>(RB + r + ".prefab") != null)
                lRocas.Add(RB + r + ".prefab");

        string pino = RB + "tree_02.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(pino) == null) pino = null;

        // Respaldo: las rocas y los árboles del Lowpoly Forest Pack.
        if (lRocas.Count == 0)
            foreach (string r in PREFABS_ROCAS)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(r) != null) lRocas.Add(r);
        if (pino == null)
            foreach (string a in PREFABS_ARBOLES)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(a) != null) { pino = a; break; }

        if (lRocas.Count == 0 && pino == null) return 0;
        string[] rocas = lRocas.ToArray();

        List<GameObject> cerros = new List<GameObject>();
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("Cerro_")) cerros.Add(t.gameObject);

        Random.State st = Random.state;
        Random.InitState(555777);

        int idx = 0;
        for (int c = 0; c < cerros.Count; c++)
        {
            GameObject cerro = cerros[c];
            MeshFilter mf = cerro.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            // Se colocan SOBRE LOS VÉRTICES de la malla del cerro: así quedan
            // pegados a la ladera sin depender de raycasts.
            Mesh malla = mf.sharedMesh;
            Vector3[] vs = malla.vertices;
            if (vs.Length == 0) continue;

            float minY = malla.bounds.min.y;
            float altoY = Mathf.Max(0.001f, malla.bounds.size.y);

            // Vértices de la FALDA del cono (y ~ 0 en local).
            List<Vector3> falda = new List<Vector3>();
            for (int i = 0; i < vs.Length; i++)
                if (vs[i].y - minY < altoY * 0.02f) falda.Add(vs[i]);
            if (falda.Count == 0) continue;

            List<Vector3> puestos = new List<Vector3>();
            int intentos = 0;

            while (puestos.Count < 9 && intentos < 400)
            {
                intentos++;

                // AL PIE del cerro, sobre el suelo del valle: es donde de verdad se
                // juntan las rocas y los pinos, y así nunca quedan flotando.
                Vector3 baseV = falda[Random.Range(0, falda.Count)];
                if (new Vector3(baseV.x, 0f, baseV.z).sqrMagnitude < 1f) continue;

                bool esArbol = (puestos.Count % 3 == 0) && pino != null;
                float f = esArbol ? Random.Range(1.02f, 1.34f) : Random.Range(0.88f, 1.22f);
                Vector3 v = new Vector3(baseV.x * f, 0f, baseV.z * f);

                Vector3 mundo = cerro.transform.TransformPoint(v);
                mundo.y = -0.35f;                               // suelo del valle

                bool cerca = false;
                for (int q = 0; q < puestos.Count && !cerca; q++)
                    if ((puestos[q] - mundo).sqrMagnitude < 16f * 16f) cerca = true;
                if (cerca) continue;
                if (!esArbol && rocas.Length == 0) continue;
                string ruta = esArbol ? pino : rocas[Random.Range(0, rocas.Length)];
                float tam = esArbol ? Random.Range(10f, 18f) : Random.Range(4f, 12f);

                GameObject g = PiezaPrefab(null, ruta, Vector3.zero, tam,
                                           new Vector3(0f, Random.Range(0f, 360f), 0f),
                                           "AdornoCerro_" + idx);
                if (g == null) continue;

                // El pivote de estos prefabs NO coincide con el modelo: si se deja
                // así, las rocas y los pinos quedan flotando en el aire.
                ApoyarEn(g, mundo, tam * (esArbol ? 0.06f : 0.22f));

                Undo.RegisterCreatedObjectUndo(g, "Decorar cerro");
                puestos.Add(mundo);
                idx++;
            }
        }

        Random.state = st;
        return idx;
    }

    // Agrega colisionadores a los edificios para que no se atraviesen las paredes.
    /// <summary>
    /// Vuelve SÓLIDO el escenario: sin esto el Guardián atravesaba árboles, postes,
    /// bancas y paraderos como si fueran humo.
    /// </summary>
    // ============================================================
    //  BICICLETAS  (movilidad sostenible - meta 11.2 del ODS 11)
    // ============================================================

    /// <summary>
    /// Deja bicicletas estacionadas en la vereda de cada zona. Usa el prefab del
    /// pack Sir_bike que ya esta en el proyecto; si no lo encuentra, arma una
    /// bicicleta sencilla por codigo para que la mecanica funcione igual.
    /// </summary>
    private static int ColocarBicicletas(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("Bicicleta_"))
                Undo.DestroyObjectImmediate(t.gameObject);

        GameObject prefab = null;
        string[] guids = AssetDatabase.FindAssets("Bicycle t:Prefab");
        for (int i = 0; i < guids.Length && prefab == null; i++)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (ruta.Contains("Sir_bike")) prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        }
        if (prefab == null && guids.Length > 0)
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);

        int puestas = 0;
        for (int z = 0; z < zonas.Length; z++)
        {
            List<Vector3> dirs = new List<Vector3>();
            List<Vector3> vereda = PuntosDeVereda(tramos, zonas[z], 40f, dirs);
            List<Vector3> usados = OcupadosDeZona(z);

            // Dos por zona: una cerca del centro y otra mas lejos, que es donde
            // mas se agradece no tener que volver caminando.
            float[] distancias = { 10f, 26f };
            for (int k = 0; k < distancias.Length; k++)
            {
                int iv = TomarVeredaSuave(vereda, dirs, usados, zonas[z],
                                          1.1f + k * 2.3f, distancias[k], distancias[k] + 16f, 6f);
                if (iv < 0) continue;

                Vector3 pv = vereda[iv];
                GameObject bici = (prefab != null)
                    ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                    : BicicletaSimple();

                bici.name = "Bicicleta_z" + z + "_" + k;

                // Estacionada en paralelo al borde, como se deja una bici de verdad.
                Vector3 lado = Vector3.Cross(Vector3.up, dirs[iv]);
                bici.transform.rotation = Quaternion.LookRotation(lado);
                ApoyarEn(bici, new Vector3(pv.x, SueloY(pv.x, pv.z, 0f), pv.z), 0.02f);

                // El prefab puede traer colliders solidos. Si se quedan, cuando la
                // bici se cuelga del Guardian quedan encimados con su
                // CharacterController y lo hacen vibrar. Se apagan: la bici no
                // tiene que frenar a nadie, solo poder usarse.
                foreach (Collider viejo in bici.GetComponentsInChildren<Collider>(true))
                    if (viejo != null) viejo.enabled = false;

                // Zona de uso: un trigger para poder acercarse y subir.
                SphereCollider sc = bici.GetComponent<SphereCollider>();
                if (sc == null) sc = bici.AddComponent<SphereCollider>();
                sc.enabled = true;
                sc.isTrigger = true;
                sc.radius = 2.2f;
                sc.center = new Vector3(0f, 0.7f, 0f);

                Bicicleta b = bici.GetComponent<Bicicleta>();
                if (b == null) b = bici.AddComponent<Bicicleta>();
                b.radioUso = 2.6f;

                Undo.RegisterCreatedObjectUndo(bici, "Colocar bicicleta");
                puestas++;
            }
        }
        return puestas;
    }

    /// <summary>Bicicleta de respaldo, por si el pack no estuviera en el proyecto.</summary>
    private static GameObject BicicletaSimple()
    {
        GameObject raiz = new GameObject("Bicicleta");
        Material metal = MatColor("Bici_cuadro", new Color(0.15f, 0.45f, 0.35f), 0.55f);
        Material goma  = MatColor("Bici_llanta", new Color(0.10f, 0.10f, 0.12f), 0.20f);

        Pieza(raiz, PrimitiveType.Cylinder, new Vector3(0f, 0.36f,  0.52f),
              new Vector3(0.36f, 0.05f, 0.36f), new Vector3(0f, 0f, 90f), goma,  "RuedaDelantera");
        Pieza(raiz, PrimitiveType.Cylinder, new Vector3(0f, 0.36f, -0.52f),
              new Vector3(0.36f, 0.05f, 0.36f), new Vector3(0f, 0f, 90f), goma,  "RuedaTrasera");
        Pieza(raiz, PrimitiveType.Cube,     new Vector3(0f, 0.55f,  0f),
              new Vector3(0.06f, 0.06f, 1.00f), Vector3.zero,             metal, "Cuadro");
        Pieza(raiz, PrimitiveType.Cube,     new Vector3(0f, 0.78f,  0.40f),
              new Vector3(0.50f, 0.05f, 0.05f), Vector3.zero,             metal, "Manubrio");
        Pieza(raiz, PrimitiveType.Cube,     new Vector3(0f, 0.80f, -0.32f),
              new Vector3(0.18f, 0.06f, 0.26f), Vector3.zero,             goma,  "Asiento");
        return raiz;
    }

    // ============================================================
    //  COLISIONES Y STATIC DE TODA LA CIUDAD
    // ============================================================

    /// <summary>
    /// Recorre la ciudad entera y deja dos cosas listas:
    ///   1. COLISIONES: todo lo que tenga malla y sea construccion recibe collider
    ///      si le falta, asi no se atraviesa ninguna pared.
    ///   2. STATIC: lo que no se mueve se marca como estatico, que es lo que deja
    ///      a Unity juntar mallas (batching) y hornear luz. Es la optimizacion
    ///      mas barata que hay en una escena de este tamano.
    /// Lo que se mueve —carros, peatones, el Guardian, la bici, el triciclo— se
    /// deja fuera a proposito: marcar static algo que se mueve lo rompe.
    /// </summary>
    /// <summary>
    /// Baja al piso todo lo que quedo flotando.
    ///
    /// Los adornos se colocan midiendo el suelo con un rayo, pero varios prefabs
    /// no tienen el pivote en la base —lo tienen al centro, o el modelo viene
    /// desplazado dentro del prefab—, asi que apoyar el PIVOTE no es apoyar el
    /// OBJETO y quedan uno o dos palmos en el aire. Esta pasada mide la caja
    /// real de cada uno, busca el piso justo debajo y lo baja esa diferencia.
    ///
    /// Solo toca lo que debe estar apoyado. Las palomas vuelan, la basura del rio
    /// flota y el humo sube: esos se saltan a proposito.
    /// </summary>
    private static int ApoyarLoQueFlota()
    {
        string[] familias = {
            "Letrero_", "Vegetacion", "VegetacionRio_", "AdornoCerro_", "Bici",
            "Residuo", "Basura_", "Bote_", "Contenedor", "Acopio", "Banca",
            "Poste", "Puesto", "Arbol", "Iglesia_", "Adorno", "Semaforo",
            "Peaton", "Vecino", "Aliado", "Perro", "Triciclo", "Carro", "Camion",
            "Kiosko", "Paradero", "Tacho", "Mural", "Panel", "Reja", "Muro",
        };
        string[] nunca = {
            "Paloma", "BasuraRio", "Humo", "Nube", "Cielo", "Rio_", "Ribera_",
            "SueloValle", "Piso_", "Agua", "Luz", "Sol", "Camara", "Canvas",
            // El atrio se arma por capas (empedrado, vereda, losa, gradas) y
            // cada capa se apoya en la anterior, no en el piso de la manzana:
            // bajarlas al piso las hundiria unas dentro de otras.
            "PlazaEmpedrado", "PlazaVereda", "PlazaAtrio", "Cerco", "Arena_", "Jardinera",
        };

        int bajados = 0;
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            if (t == null || t.parent != null) continue;          // solo raices
            GameObject g = t.gameObject;
            if (!g.activeInHierarchy) continue;
            if (g.GetComponentInChildren<Renderer>() == null) continue;

            string n = g.name;
            bool salta = false;
            for (int i = 0; i < nunca.Length; i++)
                if (n.IndexOf(nunca[i], System.StringComparison.OrdinalIgnoreCase) >= 0) { salta = true; break; }
            if (salta) continue;

            bool tocaba = false;
            for (int i = 0; i < familias.Length; i++)
                if (n.IndexOf(familias[i], System.StringComparison.OrdinalIgnoreCase) >= 0) { tocaba = true; break; }
            if (!tocaba) continue;

            Bounds caja = CajaDe(g);
            if (caja.size == Vector3.zero) continue;

            // El rayo sale DE ARRIBA del objeto y se lanza hacia abajo, ignorando
            // al propio objeto: si saliera del centro podria quedarse dentro de su
            // propia malla y devolver una altura sin sentido.
            Collider[] propios = g.GetComponentsInChildren<Collider>();
            bool[] estaban = new bool[propios.Length];
            for (int i = 0; i < propios.Length; i++)
            { estaban[i] = propios[i].enabled; propios[i].enabled = false; }

            RaycastHit h;
            bool hay = Physics.Raycast(new Vector3(caja.center.x, caja.max.y + 6f, caja.center.z),
                                       Vector3.down, out h, caja.size.y + 60f,
                                       ~0, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < propios.Length; i++) propios[i].enabled = estaban[i];
            if (!hay) continue;

            float hueco = caja.min.y - h.point.y;
            // Margen generoso hacia abajo, cortito hacia arriba: hundir un adorno
            // medio centimetro no se ve, dejarlo flotando si.
            if (hueco > 0.06f && hueco < 6f)
            {
                Undo.RecordObject(t, "Apoyar en el piso");
                t.position += Vector3.down * (hueco - 0.02f);
                EditorUtility.SetDirty(g);
                bajados++;
            }
        }
        return bajados;
    }

    [MenuItem("Tools/Guardián de Huancayo/🪶 Apoyar lo que flota", false, 11)]
    private static void ApoyarLoQueFlotaMenu()
    {
        int n = ApoyarLoQueFlota();
        EditorUtility.DisplayDialog("Guardián de Huancayo",
            n + " objetos que estaban en el aire quedaron apoyados en el piso.", "Listo");
    }

    [MenuItem("Tools/Guardián de Huancayo/🧱 Colisiones y Static de la ciudad", false, 10)]
    private static void ReforzarCiudad()
    {
        int colisiones = 0, estaticos = 0;
        ReforzarCiudad(out colisiones, out estaticos);

        EditorUtility.DisplayDialog("Guardián de Huancayo",
            "Ciudad reforzada.\n\n" +
            "• Colisionadores nuevos: " + colisiones + "\n" +
            "• Objetos marcados como Static: " + estaticos + "\n\n" +
            "Static deja que Unity junte mallas y hornee la luz: sube los FPS sin\n" +
            "cambiar nada de lo que se ve. Lo que se mueve queda fuera.", "Listo");
    }

    private static void ReforzarCiudad(out int colisiones, out int estaticos)
    {
        colisiones = 0; estaticos = 0;

        foreach (MeshRenderer mr in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (mr == null) continue;
            GameObject g = mr.gameObject;
            if (SeMueve(g.transform)) continue;

            MeshFilter mf = g.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            // 1) Collider donde falte
            if (g.GetComponent<Collider>() == null && EsSolido(g.transform))
            {
                MeshCollider mc = Undo.AddComponent<MeshCollider>(g);
                if (mc != null) colisiones++;
            }

            // 2) Static
            if (!GameObjectUtility.AreStaticEditorFlagsSet(g, StaticEditorFlags.BatchingStatic))
            {
                Undo.RecordObject(g, "Marcar static");
                GameObjectUtility.SetStaticEditorFlags(g,
                    StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic
                    | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ContributeGI
                    | StaticEditorFlags.ReflectionProbeStatic);
                estaticos++;
            }
        }
    }

    /// <summary>¿Este objeto se mueve durante la partida? No debe ser static.</summary>
    private static bool SeMueve(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            if (p.GetComponent<Contaminante>() != null) return true;
            if (p.GetComponent<Peaton>() != null) return true;
            if (p.GetComponent<Aliado>() != null) return true;
            if (p.GetComponent<Bicicleta>() != null) return true;
            if (p.GetComponent<Paloma>() != null) return true;
            if (p.GetComponent<TrashItem>() != null) return true;
            if (p.GetComponent<CamionRecolector>() != null) return true;
            if (p.GetComponent<AguaRio>() != null) return true;
            if (p.GetComponent<Semaforo>() != null) return true;
            if (p.GetComponent<PlayerController>() != null) return true;
            if (p.GetComponent<Rigidbody>() != null) return true;
            if (p.GetComponent<Animator>() != null) return true;

            string n = p.name;
            if (n.StartsWith("BasuraRio_")) return true;
            if (n.StartsWith("Bicicleta_")) return true;
            if (n.StartsWith("Triciclo")) return true;
        }
        return false;
    }

    /// <summary>¿Debe frenar al jugador? Las hojas y los carteles no.</summary>
    private static bool EsSolido(Transform t)
    {
        string n = t.name;
        if (n.StartsWith("Letrero_") || n == "Tablero" || n == "Texto") return false;
        if (n.StartsWith("Rio_") || n.StartsWith("Ribera_")) return false;
        if (n.StartsWith("Piso_") || n.StartsWith("Marca")) return false;
        if (n.Contains("Leaf") || n.Contains("Hoja")) return false;
        return true;
    }

    private static void PonerColisionadores()
    {
        string[] solidos = {
            "Building",
            "Natures_Fir Tree", "Natures_Big Tree", "Natures_Cube Tree", "Natures_Rock",
            "Props_Street Light", "Props_Bench", "Props_Bus Stop",
            "Props_Hydrant", "Props_Traffic Signal", "Props_BillBoard",
        };

        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            if (t == null) continue;
            if (t.GetComponent<Collider>() != null) continue;
            if (t.GetComponent<MeshFilter>() == null) continue;

            bool esSolido = false;
            for (int i = 0; i < solidos.Length && !esSolido; i++)
                if (t.name.StartsWith(solidos[i])) esSolido = true;
            if (!esSolido) continue;

            Undo.AddComponent<MeshCollider>(t.gameObject);
        }
    }

    /// <summary>Colisionador de persona: el jugador ya no atraviesa a los NPC.</summary>
    private static void CuerpoDePersona(GameObject g)
    {
        if (g == null) return;
        if (g.GetComponent<Collider>() == null)
        {
            CapsuleCollider cc = g.AddComponent<CapsuleCollider>();
            cc.radius = 0.26f;
            cc.height = 1.75f;
            cc.center = new Vector3(0f, 0.88f, 0f);
        }
        if (g.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = g.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    /// <summary>
    /// Cadena de puntos de vereda (ida y vuelta) para que un peatón camine por la
    /// acera de su cuadra y no cruce por dentro de los edificios ni por la pista.
    /// El salto máximo de 12 u impide que se pase a la vereda de enfrente.
    /// </summary>
    private static List<Vector3> RutaDeVereda(List<Vector3> vereda, Vector3 desde, int cuantos)
    {
        List<Vector3> cadena = new List<Vector3>();
        List<Vector3> libres = new List<Vector3>(vereda);

        for (int k = 0; k < cuantos && libres.Count > 0; k++)
        {
            Vector3 refp = (cadena.Count == 0) ? desde : cadena[cadena.Count - 1];
            int mejor = -1; float d = float.MaxValue;
            for (int i = 0; i < libres.Count; i++)
            {
                float dd = (libres[i] - refp).sqrMagnitude;
                if (dd < d) { d = dd; mejor = i; }
            }
            if (mejor < 0) break;
            if (cadena.Count > 0 && d > 12f * 12f) break;   // no cruzar la pista
            cadena.Add(libres[mejor]);
            libres.RemoveAt(mejor);
        }
        return cadena;
    }

    // Activa los semáforos existentes del pack SimplePoly.
    private static void ActivarSemaforos()
    {
        int n = 0;
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
        {
            if (t.name.StartsWith("Props_Traffic_Signal") && t.GetComponent<Semaforo>() == null)
            {
                Undo.AddComponent<Semaforo>(t.gameObject);
                n++;
                if (n >= 12) break;
            }
        }
    }

    // ============================================================
    //  CONTENEDOR NTP 900.058-2019 CONSTRUIDO PIEZA POR PIEZA
    //  Antes era un prefab del pack pintado de un solo color plano: se leía
    //  como un bloque de color y no se entendía qué residuo iba adentro.
    //  Ahora tiene la forma del contenedor con ruedas que se usa en la vía
    //  pública: cuerpo del color de la norma, tapa más oscura, franja
    //  reflectiva, placa con el PICTOGRAMA del residuo y marca pintada en
    //  el piso (para reconocer el color desde lejos).
    // ============================================================

    private static GameObject ContenedorNTP(int tipo, string nombre)
    {
        Color c = COLOR_NTP[tipo];
        string slug = NOMBRE_NTP[tipo].Replace(" ", "_");

        Material mCuerpo = MatColor("Cont_" + slug, c, 0.30f);
        Material mTapa   = MatColor("ContTapa_" + slug,
                                    new Color(c.r * 0.60f, c.g * 0.60f, c.b * 0.60f), 0.36f);
        Material mOscuro = MatColor("Cont_oscuro", new Color(0.13f, 0.14f, 0.15f), 0.30f);
        Material mMetal  = MatColor("Cont_metal",  new Color(0.62f, 0.63f, 0.66f), 0.70f);
        Material mRefl   = MatColor("Cont_reflectivo", new Color(0.93f, 0.94f, 0.92f), 0.78f);
        Material mPlaca  = MaterialSimple("Assets/Guardian/Materiales/ContPlaca_" + slug + ".mat",
                                          Color.white, 0.12f, TexturaIconoNTP(tipo), Vector2.one);

        GameObject b = new GameObject(nombre);

        // Cuerpo, zócalo, borde y tapa.
        Pieza(b, PrimitiveType.Cube, new Vector3(0f, 0.76f,  0f),     new Vector3(1.10f, 1.14f, 0.88f), Vector3.zero,             mCuerpo, "Cuerpo");
        Pieza(b, PrimitiveType.Cube, new Vector3(0f, 0.24f,  0f),     new Vector3(0.86f, 0.30f, 0.70f), Vector3.zero,             mOscuro, "Zocalo");
        Pieza(b, PrimitiveType.Cube, new Vector3(0f, 1.30f,  0f),     new Vector3(1.15f, 0.07f, 0.93f), Vector3.zero,             mOscuro, "Borde");
        Pieza(b, PrimitiveType.Cube, new Vector3(0f, 1.38f, -0.03f),  new Vector3(1.19f, 0.12f, 0.97f), new Vector3(-7f, 0f, 0f), mTapa,   "Tapa");
        Pieza(b, PrimitiveType.Cube, new Vector3(0f, 1.48f,  0.40f),  new Vector3(0.62f, 0.05f, 0.09f), new Vector3(-7f, 0f, 0f), mOscuro, "Agarradera");

        // Franja reflectiva y placa del pictograma: lo que se ve desde lejos.
        Pieza(b, PrimitiveType.Cube, new Vector3(0f, 0.33f, 0f),      new Vector3(1.12f, 0.08f, 0.90f), Vector3.zero, mRefl,  "FranjaReflectiva");
        Pieza(b, PrimitiveType.Cube, new Vector3(0f, 0.88f, 0.452f),  new Vector3(0.54f, 0.54f, 0.04f), Vector3.zero, mPlaca, "PlacaPictograma");

        // Ruedas, pedal y barras: detalles que lo hacen leer como contenedor.
        Pieza(b, PrimitiveType.Cylinder, new Vector3(-0.50f, 0.15f, -0.26f), new Vector3(0.30f, 0.05f, 0.30f), new Vector3(0f, 0f, 90f), mOscuro, "RuedaIzq");
        Pieza(b, PrimitiveType.Cylinder, new Vector3( 0.50f, 0.15f, -0.26f), new Vector3(0.30f, 0.05f, 0.30f), new Vector3(0f, 0f, 90f), mOscuro, "RuedaDer");
        Pieza(b, PrimitiveType.Cube,     new Vector3(0f,     0.09f,  0.44f), new Vector3(0.66f, 0.05f, 0.14f), Vector3.zero,             mMetal,  "Pedal");
        Pieza(b, PrimitiveType.Cylinder, new Vector3(-0.52f, 0.58f,  0.42f), new Vector3(0.04f, 0.48f, 0.04f), Vector3.zero,             mMetal,  "BarraIzq");
        Pieza(b, PrimitiveType.Cylinder, new Vector3( 0.52f, 0.58f,  0.42f), new Vector3(0.04f, 0.48f, 0.04f), Vector3.zero,             mMetal,  "BarraDer");

        return b;
    }

    // --- Dibujo de los pictogramas (todo generado por código, sin imágenes de terceros) ---

    private static void IcoRect(Color32[] px, int n, int x0, int y0, int x1, int y1, Color32 c)
    {
        x0 = Mathf.Clamp(x0, 0, n - 1); x1 = Mathf.Clamp(x1, 0, n - 1);
        y0 = Mathf.Clamp(y0, 0, n - 1); y1 = Mathf.Clamp(y1, 0, n - 1);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++) px[y * n + x] = c;
    }

    private static void IcoElipse(Color32[] px, int n, float cx, float cy, float rx, float ry, Color32 c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), x1 = Mathf.Min(n - 1, Mathf.CeilToInt(cx + rx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry)), y1 = Mathf.Min(n - 1, Mathf.CeilToInt(cy + ry));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) / Mathf.Max(0.01f, rx), dy = (y - cy) / Mathf.Max(0.01f, ry);
                if (dx * dx + dy * dy <= 1f) px[y * n + x] = c;
            }
    }

    private static void IcoTrazo(Color32[] px, int n, float x0, float y0, float x1, float y1,
                                 float grosor, Color32 c)
    {
        int ax = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - grosor));
        int bx = Mathf.Min(n - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + grosor));
        int ay = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - grosor));
        int by = Mathf.Min(n - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + grosor));
        float vx = x1 - x0, vy = y1 - y0;
        float ll = vx * vx + vy * vy; if (ll < 0.0001f) ll = 0.0001f;
        for (int y = ay; y <= by; y++)
            for (int x = ax; x <= bx; x++)
            {
                float t = Mathf.Clamp01(((x - x0) * vx + (y - y0) * vy) / ll);
                float dx = x - (x0 + vx * t), dy = y - (y0 + vy * t);
                if (dx * dx + dy * dy <= grosor * grosor) px[y * n + x] = c;
            }
    }

    /// <summary>
    /// Pictograma del residuo: botella (plástico), copa (vidrio), hoja (papel),
    /// lata (metal) y manzana (orgánicos), sobre fondo claro y con marco del
    /// color que manda la NTP 900.058-2019.
    /// </summary>
    private static Texture2D TexturaIconoNTP(int tipo)
    {
        AsegurarCarpetas();
        string ruta = "Assets/Guardian/Imagenes/icono_ntp_" + tipo + ".png";
        Texture2D ya = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (ya != null) return ya;

        const int N = 256;
        Color32[] px = new Color32[N * N];

        Color32 fondo = new Color32(247, 247, 244, 255);
        Color32 tinta = new Color32(36, 38, 42, 255);
        Color c = COLOR_NTP[tipo];
        Color32 marco = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), 255);
        // El blanco de la norma no se vería como marco: se le pone un gris de contorno.
        if (tipo == 0) marco = new Color32(120, 124, 130, 255);

        for (int i = 0; i < px.Length; i++) px[i] = fondo;

        int m = 16;
        IcoRect(px, N, 0, 0, N - 1, m, marco);
        IcoRect(px, N, 0, N - 1 - m, N - 1, N - 1, marco);
        IcoRect(px, N, 0, 0, m, N - 1, marco);
        IcoRect(px, N, N - 1 - m, 0, N - 1, N - 1, marco);

        switch (tipo)
        {
            case 0:   // PLÁSTICO · botella PET
                IcoRect(px, N, 110, 212, 146, 234, tinta);            // tapa
                IcoRect(px, N, 116, 184, 140, 214, tinta);            // cuello
                IcoTrazo(px, N, 120, 188, 100, 154, 10f, tinta);      // hombro izq
                IcoTrazo(px, N, 136, 188, 156, 154, 10f, tinta);      // hombro der
                IcoRect(px, N, 94, 46, 162, 158, tinta);              // cuerpo
                IcoElipse(px, N, 106, 52, 13, 13, tinta);
                IcoElipse(px, N, 150, 52, 13, 13, tinta);
                IcoRect(px, N, 94, 96, 162, 103, fondo);              // estrías
                IcoRect(px, N, 94, 114, 162, 121, fondo);
                break;

            case 1:   // VIDRIO · copa
                IcoRect(px, N, 92, 36, 164, 50, tinta);               // base
                IcoRect(px, N, 121, 50, 135, 124, tinta);             // tallo
                IcoElipse(px, N, 128, 150, 44, 46, tinta);            // cáliz
                IcoRect(px, N, 80, 150, 176, 200, fondo);             // se corta arriba
                IcoRect(px, N, 84, 146, 172, 157, tinta);             // borde de la copa
                IcoTrazo(px, N, 108, 120, 104, 138, 5f, fondo);       // brillo
                break;

            case 2:   // PAPEL Y CARTÓN · hoja con esquina doblada
                IcoRect(px, N, 76, 40, 178, 216, tinta);
                for (int y = 172; y <= 216; y++)                       // esquina doblada
                    for (int x = 178 - (y - 172); x <= 178; x++)
                        if (x >= 0 && x < N && y >= 0 && y < N) px[y * N + x] = fondo;
                IcoTrazo(px, N, 134, 216, 178, 172, 4f, tinta);
                for (int k = 0; k < 4; k++)
                    IcoRect(px, N, 92, 68 + k * 26, 150, 75 + k * 26, fondo);
                break;

            case 3:   // METALES · lata
                IcoRect(px, N, 96, 54, 160, 198, tinta);
                IcoElipse(px, N, 128, 198, 32, 14, tinta);            // tapa
                IcoElipse(px, N, 128, 54, 32, 14, tinta);             // base
                IcoElipse(px, N, 128, 200, 10, 5, fondo);             // anilla
                IcoRect(px, N, 96, 106, 160, 130, fondo);             // franja
                IcoTrazo(px, N, 104, 70, 104, 186, 4f, fondo);        // brillo
                break;

            default:  // ORGÁNICOS · manzana con hoja
                IcoElipse(px, N, 112, 104, 46, 52, tinta);
                IcoElipse(px, N, 146, 104, 46, 52, tinta);
                IcoElipse(px, N, 129, 156, 24, 15, fondo);            // muesca de arriba
                IcoTrazo(px, N, 129, 150, 135, 190, 6f, tinta);       // tallo
                IcoElipse(px, N, 160, 178, 27, 14, tinta);            // hoja
                IcoElipse(px, N, 154, 170, 25, 13, fondo);
                break;
        }

        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();

        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ruta),
            tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti != null) { ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    // ============================================================
    //  PANEL EDUCATIVO ODS 11 + TRICICLO DEL RECICLADOR
    // ============================================================

    /// <summary>Texto en 3D con la fuente del proyecto (devuelve null si no hay fuente).</summary>
    /// <summary>
    /// Material para el texto de los carteles.
    ///
    /// El material que trae la fuente usa el shader "GUI/Text Shader", que dibuja
    /// con ZTest Always: la letra se pinta SIEMPRE encima, atraviese lo que
    /// atraviese. Por eso el codigo anterior ponia el texto en una sola cara —si
    /// ponia otro atras, se veia por delante—, y el precio era que desde el otro
    /// lado el cartel se leia al reves, como espejo. Varios carteles de la ciudad
    /// se veian asi.
    ///
    /// Con un Unlit de URP recortado por alfa, la letra escribe y consulta
    /// profundidad como cualquier objeto solido: el tablero tapa el texto de la
    /// cara de atras y ya se puede poner texto en ambas caras.
    /// </summary>
    private static Material MaterialTexto(Font fuente)
    {
        if (fuente == null) return null;
        AsegurarCarpetas();

        string ruta = "Assets/Guardian/Materiales/Texto_letrero.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);

        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) return fuente.material;      // sin URP: como antes, una cara

        if (m == null)
        {
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, ruta);
        }
        if (m.shader != sh) m.shader = sh;

        Texture atlas = fuente.material != null ? fuente.material.mainTexture : null;
        if (m.HasProperty("_BaseMap"))   m.SetTexture("_BaseMap", atlas);
        if (m.HasProperty("_MainTex"))   m.SetTexture("_MainTex", atlas);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);

        // Recorte por alfa: asi la letra es solida y el fondo del glifo no existe.
        if (m.HasProperty("_Surface"))   m.SetFloat("_Surface", 0f);      // opaco
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
        if (m.HasProperty("_Cutoff"))    m.SetFloat("_Cutoff", 0.38f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.renderQueue = 2450;

        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    private static void TextoPanel(GameObject padre, string texto, Vector3 pos, float tam,
                                   Color color, TextAnchor anclaje, string nombre)
    {
        TextoPanel(padre, texto, pos, tam, color, anclaje, nombre, 180f);
    }

    private static void TextoPanel(GameObject padre, string texto, Vector3 pos, float tam,
                                   Color color, TextAnchor anclaje, string nombre, float giroY)
    {
        Font f = FuenteLetrero();
        if (f == null) return;

        GameObject t = new GameObject(nombre);
        t.transform.SetParent(padre.transform, false);
        t.transform.localPosition = pos;
        t.transform.localRotation = Quaternion.Euler(0f, giroY, 0f);

        TextMesh tm = t.AddComponent<TextMesh>();
        tm.text = texto;
        tm.font = f;
        tm.fontSize = 110;
        tm.characterSize = tam;
        tm.lineSpacing = 1.05f;
        tm.anchor = anclaje;
        tm.alignment = anclaje == TextAnchor.MiddleCenter ? TextAlignment.Center : TextAlignment.Left;
        tm.color = color;

        MeshRenderer mr = t.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = f.material;
    }

    /// <summary>
    /// Panel de la municipalidad con el ODS 11 y el código de colores de la
    /// NTP 900.058-2019. Sirve dentro del juego (el jugador aprende antes de
    /// empezar) y también como evidencia para el informe del curso.
    /// </summary>
    private static GameObject PanelODS(string nombre)
    {
        GameObject p = new GameObject(nombre);

        Color naranjaODS = new Color(0.976f, 0.616f, 0.149f);   // color oficial del ODS 11
        Material mFondo  = MatColor("Panel_fondo",  new Color(0.15f, 0.17f, 0.20f), 0.10f);
        Material mCabeza = MatColor("Panel_cabeza", naranjaODS, 0.12f);
        Material mMarco  = MatColor("Panel_marco",  new Color(0.22f, 0.24f, 0.27f), 0.25f);
        Material mPoste  = MatColor("Panel_poste",  new Color(0.35f, 0.36f, 0.38f), 0.45f);

        Pieza(p, PrimitiveType.Cylinder, new Vector3(-2.15f, 1.05f, 0f), new Vector3(0.13f, 1.05f, 0.13f), Vector3.zero, mPoste, "PosteIzq");
        Pieza(p, PrimitiveType.Cylinder, new Vector3( 2.15f, 1.05f, 0f), new Vector3(0.13f, 1.05f, 0.13f), Vector3.zero, mPoste, "PosteDer");

        Pieza(p, PrimitiveType.Cube, new Vector3(0f, 3.10f, 0f),   new Vector3(5.30f, 2.70f, 0.20f), Vector3.zero, mMarco,  "Marco");
        Pieza(p, PrimitiveType.Cube, new Vector3(0f, 3.10f, 0.11f), new Vector3(5.10f, 2.52f, 0.05f), Vector3.zero, mFondo,  "Tablero");
        Pieza(p, PrimitiveType.Cube, new Vector3(0f, 4.02f, 0.14f), new Vector3(5.10f, 0.68f, 0.04f), Vector3.zero, mCabeza, "Cabecera");

        TextoPanel(p, "ODS 11 · CIUDADES Y COMUNIDADES SOSTENIBLES",
                   new Vector3(0f, 4.02f, 0.17f), 0.0200f, new Color(1f, 1f, 1f),
                   TextAnchor.MiddleCenter, "Titulo");

        TextoPanel(p, "Huancayo separa sus residuos · NTP 900.058-2019",
                   new Vector3(0f, 3.48f, 0.17f), 0.0160f, new Color(0.98f, 0.86f, 0.42f),
                   TextAnchor.MiddleCenter, "Subtitulo");

        // Fila de colores de la norma, cada uno con su rótulo debajo.
        string[] etiqueta = { "PLÁSTICO", "VIDRIO", "PAPEL", "METAL", "ORGÁNICO" };
        for (int i = 0; i < COLOR_NTP.Length; i++)
        {
            float x = 1.92f - i * 0.96f;   // +X queda a la izquierda visto desde la calle
            Material mc = MatColor("PanelChip_" + i, COLOR_NTP[i], 0.20f);
            Pieza(p, PrimitiveType.Cube, new Vector3(x, 3.00f, 0.15f),
                  new Vector3(0.80f, 0.80f, 0.05f), Vector3.zero, mc, "Color_" + i);
            Pieza(p, PrimitiveType.Cube, new Vector3(x, 3.00f, 0.14f),
                  new Vector3(0.88f, 0.88f, 0.04f), Vector3.zero, mMarco, "ColorMarco_" + i);
            TextoPanel(p, etiqueta[i], new Vector3(x, 2.44f, 0.18f), 0.0135f,
                       new Color(0.97f, 0.97f, 0.95f), TextAnchor.MiddleCenter, "Rotulo_" + i);
        }

        TextoPanel(p,
            "El río Shullcas no es un botadero.\n" +
            "Segregar en la fuente reduce la basura que llega al río\n" +
            "y le da valor al trabajo de los recicladores de la ciudad.",
            new Vector3(0f, 2.10f, 0.18f), 0.0130f, new Color(0.90f, 0.92f, 0.94f),
            TextAnchor.MiddleCenter, "Mensaje");

        GameObject luz = new GameObject("LuzPanel");
        luz.transform.SetParent(p.transform, false);
        luz.transform.localPosition = new Vector3(0f, 3.30f, 2.20f);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.97f, 0.88f);
        l.intensity = 2.4f;
        l.range = 16f;
        l.shadows = LightShadows.None;

        BoxCollider bc = p.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 3.10f, 0f);
        bc.size = new Vector3(5.3f, 2.7f, 0.3f);
        return p;
    }

    /// <summary>
    /// Triciclo de reciclador: en Huancayo son ellos los que recuperan de verdad
    /// el plástico y el cartón. Va estacionado en la vereda, con sus sacos.
    /// </summary>
    private static GameObject CrearTricicloReciclador(string nombre)
    {
        GameObject t = new GameObject(nombre);

        Material metal  = MatColor("Tri_metal",  new Color(0.68f, 0.24f, 0.18f), 0.45f);
        Material negro  = MatColor("Tri_negro",  new Color(0.11f, 0.11f, 0.12f), 0.25f);
        Material madera = MatColor("Tri_madera", new Color(0.56f, 0.41f, 0.26f), 0.10f);
        Material malla  = MatColor("Tri_malla",  new Color(0.36f, 0.38f, 0.40f), 0.40f);
        Material saco   = MatColor("Tri_saco",   new Color(0.90f, 0.88f, 0.76f), 0.08f);
        Material azul   = MatColor("Tri_azul",   new Color(0.18f, 0.46f, 0.80f), 0.20f);

        // Cajón de carga (atrás).
        Pieza(t, PrimitiveType.Cube, new Vector3(0f, 0.62f, -0.95f), new Vector3(1.45f, 0.14f, 1.75f), Vector3.zero, madera, "Piso");
        Pieza(t, PrimitiveType.Cube, new Vector3(-0.70f, 1.05f, -0.95f), new Vector3(0.08f, 0.85f, 1.75f), Vector3.zero, malla, "ParedIzq");
        Pieza(t, PrimitiveType.Cube, new Vector3( 0.70f, 1.05f, -0.95f), new Vector3(0.08f, 0.85f, 1.75f), Vector3.zero, malla, "ParedDer");
        Pieza(t, PrimitiveType.Cube, new Vector3(0f, 1.05f, -1.80f), new Vector3(1.45f, 0.85f, 0.08f), Vector3.zero, malla, "ParedAtras");
        Pieza(t, PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.10f), new Vector3(1.45f, 0.85f, 0.08f), Vector3.zero, malla, "ParedFrente");

        // Sacos de botellas y cartón asomando del cajón.
        Pieza(t, PrimitiveType.Cube,     new Vector3(-0.30f, 1.20f, -1.30f), new Vector3(0.62f, 0.85f, 0.60f), new Vector3(0f, 12f, 0f), saco,   "SacoA");
        Pieza(t, PrimitiveType.Cube,     new Vector3( 0.32f, 1.12f, -0.90f), new Vector3(0.58f, 0.72f, 0.56f), new Vector3(0f, -9f, 0f), saco,   "SacoB");
        Pieza(t, PrimitiveType.Cube,     new Vector3(-0.10f, 1.02f, -0.45f), new Vector3(0.90f, 0.55f, 0.35f), new Vector3(6f, 3f, 0f),  madera, "Carton");
        Pieza(t, PrimitiveType.Cylinder, new Vector3( 0.36f, 1.62f, -1.34f), new Vector3(0.16f, 0.16f, 0.16f), new Vector3(20f, 0f, 0f), azul,   "Botella");

        // Cuadro, sillín y manubrio.
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 0.72f, 0.42f), new Vector3(0.07f, 0.55f, 0.07f), new Vector3(70f, 0f, 0f), metal, "Cuadro");
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 0.95f, 0.98f), new Vector3(0.07f, 0.52f, 0.07f), new Vector3(18f, 0f, 0f), metal, "Horquilla");
        Pieza(t, PrimitiveType.Cube,     new Vector3(0f, 1.00f, 0.18f), new Vector3(0.26f, 0.10f, 0.42f), Vector3.zero,            negro, "Sillin");
        Pieza(t, PrimitiveType.Cylinder, new Vector3(0f, 1.42f, 0.86f), new Vector3(0.05f, 0.34f, 0.05f), new Vector3(0f, 0f, 90f), negro, "Manubrio");

        // Ruedas: una adelante, dos atrás.
        Pieza(t, PrimitiveType.Cylinder, new Vector3( 0.00f, 0.36f,  1.22f), new Vector3(0.72f, 0.06f, 0.72f), new Vector3(0f, 0f, 90f), negro, "RuedaDelantera");
        Pieza(t, PrimitiveType.Cylinder, new Vector3(-0.66f, 0.34f, -1.25f), new Vector3(0.66f, 0.06f, 0.66f), new Vector3(0f, 0f, 90f), negro, "RuedaTrasIzq");
        Pieza(t, PrimitiveType.Cylinder, new Vector3( 0.66f, 0.34f, -1.25f), new Vector3(0.66f, 0.06f, 0.66f), new Vector3(0f, 0f, 90f), negro, "RuedaTrasDer");

        // Cartel del reciclador.
        Pieza(t, PrimitiveType.Cube, new Vector3(0f, 1.66f, -1.83f), new Vector3(1.30f, 0.34f, 0.05f), Vector3.zero, azul, "Cartel");
        TextoPanel(t, "RECICLADOR\nAUTORIZADO", new Vector3(0f, 1.66f, -1.87f), 0.0150f,
                   Color.white, TextAnchor.MiddleCenter, "CartelTexto", 0f);

        BoxCollider bc = t.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.85f, -0.6f);
        bc.size = new Vector3(1.6f, 1.7f, 3.2f);
        return t;
    }

    /// <summary>Coloca un reciclador de pie al costado de su triciclo, mirándolo.</summary>
    private static void RecicladorJuntoAlTriciclo(GameObject tri, int z)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            BASE + "worker_Male_constructor_A.prefab");
        if (prefab == null)
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                BASE + "worker_Male_constructor_B.prefab");
        if (prefab == null)
        {
            List<GameObject> mods = ModelosCiudadanos();
            if (mods.Count > 0) prefab = mods[z % mods.Count];
        }
        if (prefab == null) return;

        GameObject rec = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        rec.name = "Reciclador_z" + z + "_0";

        // Va COLGADO del triciclo, justo detrás: se mueve con él como si lo empujara.
        rec.transform.SetParent(tri.transform, false);
        rec.transform.localPosition = new Vector3(0f, 0f, -2.35f);
        rec.transform.localRotation = Quaternion.identity;

        // Apagar la IA del pack: a este lo mueve el triciclo.
        foreach (MonoBehaviour mb in rec.GetComponentsInChildren<MonoBehaviour>())
            if (mb != null && mb.GetType().Name == "CityPeople") mb.enabled = false;

        Animator an = rec.GetComponentInChildren<Animator>();
        RuntimeAnimatorController ctrl = CargarControlador(false);
        if (an != null && ctrl != null) an.runtimeAnimatorController = ctrl;

        rec.AddComponent<AnimarCaminar>();      // mueve las piernas mientras avanza
        Undo.RegisterCreatedObjectUndo(rec, "Crear reciclador");
    }

    /// <summary>
    /// Residuos de reserva (desactivados) que los vecinos van botando mientras
    /// se juega. Así el jugador entiende que la basura no se acaba sola: si
    /// nadie segrega en la fuente, la calle se vuelve a ensuciar.
    /// </summary>
    private static List<TrashItem> CrearReservaBasura(int cuantos)
    {
        List<TrashItem> reserva = new List<TrashItem>();

        Transform grupo = GrupoRaiz("=== GUARDIAN DE HUANCAYO ===");
        Transform sub = SubGrupo(grupo, "Residuos de reserva");

        for (int i = 0; i < cuantos; i++)
        {
            int tipo = i % PREFABS_POR_TIPO.Length;
            GameObject prefab = PrefabDeTipo(tipo);

            GameObject b;
            if (prefab != null) b = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            else
            {
                b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.transform.localScale = Vector3.one * 0.6f;
                Renderer rr = b.GetComponent<Renderer>();
                if (rr != null) rr.sharedMaterial = MatColor("Residuo_" + tipo, COLOR_NTP[tipo], 0.1f);
            }
            b.name = "BasuraReserva_" + i;
            b.transform.position = new Vector3(0f, -40f, 0f);   // guardada bajo el suelo

            foreach (Collider viejoCol in b.GetComponentsInChildren<Collider>())
                viejoCol.enabled = false;

            SphereCollider trig = b.AddComponent<SphereCollider>();
            trig.isTrigger = true;
            trig.radius = 0.9f;
            Rigidbody rb = b.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            ArreglarMaterialesURP(b);

            TrashItem ti = b.AddComponent<TrashItem>();
            ti.valor = 10;
            ti.zona = -1;
            ti.deReserva = true;
            ti.tipo = (TipoResiduo)tipo;

            if (sub != null) b.transform.SetParent(sub, true);
            b.SetActive(false);

            Undo.RegisterCreatedObjectUndo(b, "Crear residuo de reserva");
            reserva.Add(ti);
        }
        return reserva;
    }

    /// <summary>Reparte la reserva entre algunos peatones: ellos serán los que ensucien.</summary>
    private static int RepartirEnsuciadores(List<TrashItem> reserva, int cuantosPeatones)
    {
        foreach (Ensuciador viejo in Object.FindObjectsOfType<Ensuciador>())
            Undo.DestroyObjectImmediate(viejo);

        Peaton[] peatones = Object.FindObjectsOfType<Peaton>();
        if (peatones.Length == 0 || reserva.Count == 0) return 0;

        int n = Mathf.Min(cuantosPeatones, peatones.Length);
        int porCabeza = Mathf.Max(1, reserva.Count / Mathf.Max(1, n));
        int k = 0, puestos = 0;

        for (int i = 0; i < peatones.Length && puestos < n; i++)
        {
            // Uno de cada tantos, repartidos por la lista para que no sean vecinos.
            if (i % Mathf.Max(1, peatones.Length / n) != 0) continue;

            Ensuciador e = Undo.AddComponent<Ensuciador>(peatones[i].gameObject);
            e.bolsa = new List<TrashItem>();
            for (int j = 0; j < porCabeza && k < reserva.Count; j++, k++)
                e.bolsa.Add(reserva[k]);
            puestos++;
        }
        return puestos;
    }

    /// <summary>
    /// Coloca en la vereda el panel del ODS 11 (zona 0, la plaza) y un triciclo
    /// de reciclador en cada zona. Nada de esto va sobre el asfalto.
    /// </summary>
    private static void DetallesEducativos(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && (t.name.StartsWith("PanelODS") || t.name.StartsWith("TricicloReciclador")
                           || t.name.StartsWith("Reciclador_")))
                Undo.DestroyObjectImmediate(t.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);

        // Postes, semáforos, bancas y demás props del pack: el panel grande no
        // debe quedar justo detrás de uno (no se leería).
        List<Vector3> props = new List<Vector3>();
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("Props_")) props.Add(t.position);

        for (int z = 0; z < zonas.Length; z++)
        {
            List<Vector3> dirs = new List<Vector3>();
            List<Vector3> vereda = PuntosDeVereda(tramos, zonas[z], 42f, dirs);
            List<Vector3> usados = OcupadosDeZona(z);

            // Triciclo del reciclador, estacionado pegado al borde de la vereda.
            int it = TomarVeredaSuave(vereda, dirs, usados, zonas[z], 2.4f, 8f, 34f, 7f);
            if (it >= 0)
            {
                GameObject tri = CrearTricicloReciclador("TricicloReciclador_z" + z + "_0");
                Vector3 pt = vereda[it];
                tri.transform.position = new Vector3(pt.x, SueloY(pt.x, pt.z, 0f), pt.z);
                // Estacionado en paralelo a la pista, no de frente.
                Vector3 lado = Vector3.Cross(Vector3.up, dirs[it]);
                tri.transform.rotation = Quaternion.LookRotation(lado);

                // Ya no está estacionado: recorre la vereda empujado por su dueño.
                Rigidbody rbt = tri.AddComponent<Rigidbody>();
                rbt.isKinematic = true;
                rbt.useGravity = false;

                Peaton rueda = tri.AddComponent<Peaton>();
                rueda.velocidad = Random.Range(0.95f, 1.20f);
                rueda.ruta = RutaDeVereda(vereda, tri.transform.position, 6);

                Undo.RegisterCreatedObjectUndo(tri, "Crear triciclo del reciclador");

                // Su dueño, empujando desde atrás: son los recicladores los que de
                // verdad recuperan el plástico y el cartón de la ciudad.
                RecicladorJuntoAlTriciclo(tri, z);
            }

            if (z != 0) continue;   // el panel grande va en la plaza

            int ip = -1;
            Vector3 q = Vector3.zero, haciaCalle = Vector3.forward;
            for (int intento = 0; intento < 6; intento++)
            {
                int k = TomarVeredaSuave(vereda, dirs, usados, zonas[z], 0.6f, 10f, 38f, 10f);
                if (k < 0) break;
                Vector3 cand = vereda[k] - dirs[k] * 0.9f;   // un paso adentro de la vereda
                if (intento < 5 && !Separado(cand, props, 3.6f)) continue;
                ip = k; q = cand; haciaCalle = dirs[k];
                break;
            }
            if (ip < 0) continue;

            GameObject panel = PanelODS("PanelODS_z0_0");
            panel.transform.position = new Vector3(q.x, SueloY(q.x, q.z, 0f), q.z);
            panel.transform.rotation = Quaternion.LookRotation(haciaCalle);
            LimpiarVehiculos(panel.transform.position, 8f);
            Undo.RegisterCreatedObjectUndo(panel, "Crear panel del ODS 11");
        }
    }

    // ============================================================
    //  VIDA EN LA CALLE (palomas, perro) Y PUNTO DE ACOPIO MUNICIPAL
    // ============================================================

    /// <summary>Paloma de plaza, hecha con primitivas. Las alas van aparte para poder aletear.</summary>
    private static GameObject CrearPaloma(string nombre)
    {
        GameObject p = new GameObject(nombre);

        Material gris  = MatColor("Paloma_gris",  new Color(0.60f, 0.63f, 0.70f), 0.18f);
        Material claro = MatColor("Paloma_claro", new Color(0.86f, 0.87f, 0.90f), 0.18f);
        Material pico  = MatColor("Paloma_pico",  new Color(0.92f, 0.62f, 0.28f), 0.25f);
        Material pata  = MatColor("Paloma_pata",  new Color(0.84f, 0.42f, 0.35f), 0.20f);

        Pieza(p, PrimitiveType.Sphere, new Vector3(0f, 0.135f,  0.00f),  new Vector3(0.17f, 0.15f, 0.27f), Vector3.zero,              gris,  "Cuerpo");
        Pieza(p, PrimitiveType.Sphere, new Vector3(0f, 0.225f,  0.115f), new Vector3(0.11f, 0.11f, 0.11f), Vector3.zero,              claro, "Cabeza");
        Pieza(p, PrimitiveType.Cube,   new Vector3(0f, 0.218f,  0.180f), new Vector3(0.025f, 0.02f, 0.05f), Vector3.zero,             pico,  "Pico");
        Pieza(p, PrimitiveType.Cube,   new Vector3(0f, 0.125f, -0.175f), new Vector3(0.09f, 0.02f, 0.14f), new Vector3(18f, 0f, 0f),  gris,  "Cola");
        Pieza(p, PrimitiveType.Cube,   new Vector3(-0.025f, 0.05f, 0.04f), new Vector3(0.015f, 0.09f, 0.015f), Vector3.zero,          pata,  "PataIzq");
        Pieza(p, PrimitiveType.Cube,   new Vector3( 0.025f, 0.05f, 0.04f), new Vector3(0.015f, 0.09f, 0.015f), Vector3.zero,          pata,  "PataDer");

        AlaDePaloma(p, "AlaIzq", -1f, gris);
        AlaDePaloma(p, "AlaDer",  1f, gris);
        return p;
    }

    /// <summary>El ala cuelga de un pivote en el lomo: el script Paloma lo hace girar.</summary>
    private static void AlaDePaloma(GameObject padre, string nombre, float lado, Material mat)
    {
        GameObject piv = new GameObject(nombre);
        piv.transform.SetParent(padre.transform, false);
        piv.transform.localPosition = new Vector3(lado * 0.055f, 0.175f, 0f);
        Pieza(piv, PrimitiveType.Cube, new Vector3(lado * 0.075f, 0f, 0f),
              new Vector3(0.14f, 0.022f, 0.21f), Vector3.zero, mat, "Pluma");
    }

    /// <summary>Perro callejero: no falta en ninguna esquina de Huancayo.</summary>
    private static GameObject CrearPerro(string nombre)
    {
        GameObject d = new GameObject(nombre);

        Material pelo   = MatColor("Perro_pelo",   new Color(0.56f, 0.41f, 0.25f), 0.12f);
        Material claro  = MatColor("Perro_claro",  new Color(0.80f, 0.70f, 0.52f), 0.12f);
        Material hocico = MatColor("Perro_hocico", new Color(0.18f, 0.15f, 0.13f), 0.25f);

        Pieza(d, PrimitiveType.Cube, new Vector3(0f, 0.44f,  0.00f), new Vector3(0.26f, 0.28f, 0.66f), Vector3.zero,               pelo,   "Cuerpo");
        Pieza(d, PrimitiveType.Cube, new Vector3(0f, 0.32f,  0.02f), new Vector3(0.24f, 0.16f, 0.56f), Vector3.zero,               claro,  "Panza");
        Pieza(d, PrimitiveType.Cube, new Vector3(0f, 0.58f,  0.38f), new Vector3(0.22f, 0.22f, 0.24f), Vector3.zero,               pelo,   "Cabeza");
        Pieza(d, PrimitiveType.Cube, new Vector3(0f, 0.54f,  0.53f), new Vector3(0.13f, 0.12f, 0.16f), Vector3.zero,               hocico, "Hocico");
        Pieza(d, PrimitiveType.Cube, new Vector3(-0.09f, 0.70f, 0.34f), new Vector3(0.05f, 0.13f, 0.09f), new Vector3(0f, 0f,  14f), pelo, "OrejaIzq");
        Pieza(d, PrimitiveType.Cube, new Vector3( 0.09f, 0.70f, 0.34f), new Vector3(0.05f, 0.13f, 0.09f), new Vector3(0f, 0f, -14f), pelo, "OrejaDer");
        Pieza(d, PrimitiveType.Cube, new Vector3(0f, 0.56f, -0.36f), new Vector3(0.05f, 0.05f, 0.28f), new Vector3(-42f, 0f, 0f),  pelo,   "Cola");

        float[] px = { -0.10f, 0.10f };
        float[] pz = {  0.22f, -0.22f };
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                Pieza(d, PrimitiveType.Cube, new Vector3(px[i], 0.15f, pz[j]),
                      new Vector3(0.07f, 0.30f, 0.08f), Vector3.zero, pelo, "Pata_" + i + j);

        return d;   // sin colisionador: no vale la pena que el perro tape la vereda
    }

    /// <summary>
    /// Punto de acopio municipal: la caseta donde termina lo que el Guardián
    /// segregó. Cierra el mensaje del ODS 11 — segregar sirve porque hay a dónde
    /// llevarlo — y de paso explica el color de cada caja.
    /// </summary>
    private static GameObject PuntoDeAcopio(string nombre)
    {
        GameObject a = new GameObject(nombre);

        Material verde  = MatColor("Acopio_verde",  new Color(0.12f, 0.44f, 0.29f), 0.15f);
        Material crema  = MatColor("Acopio_crema",  new Color(0.92f, 0.90f, 0.82f), 0.10f);
        Material metal  = MatColor("Acopio_metal",  new Color(0.55f, 0.57f, 0.60f), 0.55f);
        Material piso   = MatColor("Acopio_piso",   new Color(0.42f, 0.43f, 0.45f), 0.08f);
        Material madera = MatColor("Acopio_madera", new Color(0.56f, 0.41f, 0.26f), 0.10f);
        Material saco   = MatColor("Acopio_saco",   new Color(0.90f, 0.88f, 0.76f), 0.08f);

        Pieza(a, PrimitiveType.Cube, new Vector3(0f, 0.08f, -0.20f), new Vector3(5.20f, 0.16f, 3.20f), Vector3.zero, piso, "Tarima");

        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0) ? -2.35f : 2.35f;
            float z = (i < 2) ? -1.55f : 1.15f;
            Pieza(a, PrimitiveType.Cylinder, new Vector3(x, 1.30f, z),
                  new Vector3(0.12f, 1.30f, 0.12f), Vector3.zero, metal, "Poste_" + i);
        }

        // Techo a dos aguas.
        Pieza(a, PrimitiveType.Cube, new Vector3(-1.32f, 2.86f, -0.20f), new Vector3(2.90f, 0.10f, 3.70f), new Vector3(0f, 0f,  16f), verde, "TechoIzq");
        Pieza(a, PrimitiveType.Cube, new Vector3( 1.32f, 2.86f, -0.20f), new Vector3(2.90f, 0.10f, 3.70f), new Vector3(0f, 0f, -16f), verde, "TechoDer");
        Pieza(a, PrimitiveType.Cube, new Vector3(0f, 3.26f, -0.20f), new Vector3(0.24f, 0.16f, 3.80f), Vector3.zero, metal, "Cumbrera");

        Pieza(a, PrimitiveType.Cube, new Vector3(0f, 1.35f, -1.62f), new Vector3(5.00f, 2.40f, 0.14f), Vector3.zero, crema,  "Fondo");
        Pieza(a, PrimitiveType.Cube, new Vector3(0f, 0.95f,  1.12f), new Vector3(5.00f, 0.90f, 0.22f), Vector3.zero, madera, "Mostrador");
        Pieza(a, PrimitiveType.Cube, new Vector3(0f, 1.44f,  1.12f), new Vector3(5.20f, 0.10f, 0.42f), Vector3.zero, crema,  "Tablero");

        // Cajas con el color de la norma, SOBRE el mostrador: desde la vereda se
        // ve de un vistazo a qué caja va cada residuo (antes el mostrador las tapaba).
        for (int i = 0; i < COLOR_NTP.Length; i++)
        {
            float x = 1.80f - i * 0.90f;   // +X queda a la izquierda visto desde la calle
            Material mc = MatColor("AcopioCaja_" + i, COLOR_NTP[i], 0.18f);
            Pieza(a, PrimitiveType.Cube, new Vector3(x, 1.76f, 1.05f), new Vector3(0.76f, 0.52f, 0.50f), Vector3.zero, mc,    "Caja_" + i);
            Pieza(a, PrimitiveType.Cube, new Vector3(x, 2.03f, 1.05f), new Vector3(0.82f, 0.06f, 0.56f), Vector3.zero, metal, "CajaBorde_" + i);
        }

        // Y una pila de material ya clasificado en la tarima, detrás del mostrador.
        Pieza(a, PrimitiveType.Cube, new Vector3(-0.70f, 0.48f, -0.70f), new Vector3(0.86f, 0.64f, 0.74f), new Vector3(0f,  9f, 0f), madera, "PilaA");
        Pieza(a, PrimitiveType.Cube, new Vector3( 0.55f, 0.44f, -0.75f), new Vector3(0.80f, 0.56f, 0.70f), new Vector3(0f, -6f, 0f), madera, "PilaB");

        Pieza(a, PrimitiveType.Cube, new Vector3(-2.05f, 0.52f, 0.45f), new Vector3(0.70f, 0.72f, 0.60f), new Vector3(0f,  14f, 0f), saco, "SacoA");
        Pieza(a, PrimitiveType.Cube, new Vector3(-2.15f, 1.12f, 0.35f), new Vector3(0.62f, 0.52f, 0.56f), new Vector3(0f,  -8f, 0f), saco, "SacoB");
        Pieza(a, PrimitiveType.Cube, new Vector3( 2.05f, 0.50f, 0.50f), new Vector3(0.92f, 0.60f, 0.42f), new Vector3(5f,   6f, 0f), madera, "Carton");

        // Cartel del punto de acopio.
        Pieza(a, PrimitiveType.Cube, new Vector3(0f, 2.42f, 1.30f), new Vector3(4.60f, 0.62f, 0.10f), Vector3.zero, verde, "Cartel");
        TextoPanel(a, "PUNTO DE ACOPIO MUNICIPAL", new Vector3(0f, 2.53f, 1.37f), 0.0170f,
                   Color.white, TextAnchor.MiddleCenter, "CartelTitulo");
        TextoPanel(a, "Residuos aprovechables · aquí llega lo que segregas",
                   new Vector3(0f, 2.31f, 1.37f), 0.0100f,
                   new Color(0.88f, 0.96f, 0.90f), TextAnchor.MiddleCenter, "CartelSub");

        // Luz para que el cartel se lea también a contraluz.
        GameObject luz = new GameObject("LuzAcopio");
        luz.transform.SetParent(a.transform, false);
        luz.transform.localPosition = new Vector3(0f, 2.60f, 2.40f);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.97f, 0.90f);
        l.intensity = 2.0f;
        l.range = 14f;
        l.shadows = LightShadows.None;

        BoxCollider bc = a.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 1.20f, -0.80f);
        bc.size = new Vector3(5.2f, 2.4f, 2.6f);

        // Zona de entrega: al terminar de limpiar la zona hay que venir hasta acá.
        BoxCollider entrega = a.AddComponent<BoxCollider>();
        entrega.isTrigger = true;
        entrega.center = new Vector3(0f, 1.30f, 2.20f);
        entrega.size = new Vector3(6.4f, 3.2f, 4.6f);

        return a;
    }

    /// <summary>Un punto de acopio por zona, sobre la vereda y mirando a la calle.</summary>
    private static void PuntosDeAcopio(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("PuntoAcopio"))
                Undo.DestroyObjectImmediate(t.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);

        List<Vector3> props = new List<Vector3>();
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("Props_")) props.Add(t.position);

        for (int z = 0; z < zonas.Length; z++)
        {
            List<Vector3> dirs = new List<Vector3>();
            List<Vector3> vereda = PuntosDeVereda(tramos, zonas[z], 44f, dirs);
            List<Vector3> usados = OcupadosDeZona(z);

            int ip = -1;
            Vector3 q = Vector3.zero, haciaCalle = Vector3.forward;
            for (int intento = 0; intento < 6; intento++)
            {
                int k = TomarVeredaSuave(vereda, dirs, usados, zonas[z], -1f, 14f, 40f, 11f);
                if (k < 0) break;
                Vector3 cand = vereda[k] - dirs[k] * 0.25f;  // un paso adentro de la vereda
                if (intento < 5 && !Separado(cand, props, 3.4f)) continue;
                if (!LugarLibre(cand, 7f)) continue;
                ip = k; q = cand; haciaCalle = dirs[k];
                break;
            }
            if (ip < 0) continue;

            GameObject ac = PuntoDeAcopio("PuntoAcopio_z" + z + "_0");
            ac.transform.position = new Vector3(q.x, SueloY(q.x, q.z, 0f), q.z);
            ac.transform.rotation = Quaternion.LookRotation(haciaCalle);

            PuntoAcopioZona pa = ac.AddComponent<PuntoAcopioZona>();
            pa.zona = z;
            LimpiarVehiculos(ac.transform.position, 9f);
            Undo.RegisterCreatedObjectUndo(ac, "Crear punto de acopio");
        }
    }

    /// <summary>
    /// Palomas en el atrio de la iglesia y un perro callejero por zona.
    /// No cambian la jugabilidad, pero son lo que hace que la ciudad se sienta
    /// habitada y no un maquetón vacío.
    /// </summary>
    private static void VidaEnLaCalle(Vector3 centro)
    {
        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && (t.name.StartsWith("Paloma_") || t.name.StartsWith("PerroCallejero_")))
                Undo.DestroyObjectImmediate(t.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);

        // --- Palomas: delante de la iglesia si existe; si no, en la plaza. ---
        Transform ig = BuscarPorNombre("Iglesia_PlazaConstitucion");
        Vector3 plaza = ig != null ? ig.position + ig.forward * 9f : zonas[0];

        for (int i = 0; i < 9; i++)
        {
            float ang = (i / 9f) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float rad = Random.Range(1.5f, 5.5f);
            Vector3 p = plaza + new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);

            GameObject pa = CrearPaloma("Paloma_" + i);
            pa.transform.position = new Vector3(p.x, SueloY(p.x, p.z, 0f) + 0.02f, p.z);
            pa.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            Paloma comp = pa.AddComponent<Paloma>();
            comp.radio = 4.5f;
            comp.velocidad = Random.Range(0.85f, 1.25f);

            Undo.RegisterCreatedObjectUndo(pa, "Crear paloma");
        }

        // --- Un perro callejero por zona, trotando por la vereda. ---
        for (int z = 0; z < zonas.Length; z++)
        {
            List<Vector3> dirs = new List<Vector3>();
            List<Vector3> vereda = PuntosDeVereda(tramos, zonas[z], 40f, dirs);
            if (vereda.Count == 0) continue;

            Vector3 p = vereda[(z * 7 + 3) % vereda.Count];

            GameObject pe = CrearPerro("PerroCallejero_z" + z + "_0");
            // Girar y DESPUES apoyar: poner el pivote a la altura del suelo dejaba
            // al perro flotando, porque su pivote no esta en las patas.
            pe.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            ApoyarEn(pe, new Vector3(p.x, SueloY(p.x, p.z, 0f), p.z), 0.02f);

            Peaton comp = pe.AddComponent<Peaton>();
            comp.velocidad = Random.Range(1.5f, 2.1f);
            comp.ruta = RutaDeVereda(vereda, pe.transform.position, 6);

            Undo.RegisterCreatedObjectUndo(pe, "Crear perro callejero");
        }
    }

    /// <summary>
    /// Camión recolector de la municipalidad: cabina naranja, compactadora blanca
    /// con franja verde y tolva atrás. Circula como cualquier vehículo, pero al
    /// pasar junto a un contenedor lo recoge (script CamionRecolector).
    /// </summary>
    /// <summary>
    /// Camion recolector de la municipalidad.
    ///
    /// Antes estaba armado a mano con dieciocho cubos y cilindros. Funcionaba,
    /// pero al lado de los carros del pack —que comparten paleta, proporciones y
    /// el mismo atlas de textura— cantaba a kilometros: era el unico vehiculo de
    /// la ciudad con otro lenguaje visual. Ahora se parte del camion de caja del
    /// pack y solo se le agrega lo que lo vuelve municipal: el rotulo en los dos
    /// costados y la baliza ambar. Si el pack no estuviera, se cae al camion
    /// hecho a mano y el juego sigue teniendo su recolector.
    /// </summary>
    private static GameObject CrearCamionRecolector(string nombre)
    {
        GameObject prefab = null;
        for (int i = 0; i < PREFABS_CAMION.Length && prefab == null; i++)
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS_CAMION[i]);

        if (prefab == null) return CamionAMano(nombre);

        GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        c.name = nombre;
        c.transform.localScale = Vector3.one;
        ArreglarMaterialesURP(c);

        // El pack trae sus propios colliders, que pelean con el trigger de golpe
        // y con el cuerpo solido que le pone el trafico.
        foreach (Collider col in c.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(col);

        // Medir la caja real: cada camion del pack tiene su tamano y el rotulo
        // tiene que caer en el costado, no en el aire.
        Bounds b = CajaDe(c);
        Vector3 loc = c.transform.InverseTransformPoint(b.center);
        float medioAncho = b.size.x * 0.5f;
        float alto = b.size.y;

        Material ambar = MatColor("Camion_ambar", new Color(0.98f, 0.70f, 0.12f), 0.60f);

        // Baliza en el techo de la cabina.
        Pieza(c, PrimitiveType.Cube,
              new Vector3(loc.x, loc.y + alto * 0.52f, loc.z + b.size.z * 0.28f),
              new Vector3(0.95f, 0.16f, 0.26f), Vector3.zero, ambar, "Baliza");

        // Rotulo de la municipalidad en los dos costados de la caja.
        float yRotulo = loc.y + alto * 0.10f;
        float zRotulo = loc.z - b.size.z * 0.10f;
        TextoPanel(c, "MUNICIPALIDAD DE HUANCAYO\nRecolecci\u00f3n de residuos s\u00f3lidos",
                   new Vector3(loc.x - medioAncho - 0.03f, yRotulo, zRotulo), 0.0165f,
                   new Color(0.10f, 0.28f, 0.18f), TextAnchor.MiddleCenter, "RotuloIzq", 90f);
        TextoPanel(c, "MUNICIPALIDAD DE HUANCAYO\nRecolecci\u00f3n de residuos s\u00f3lidos",
                   new Vector3(loc.x + medioAncho + 0.03f, yRotulo, zRotulo), 0.0165f,
                   new Color(0.10f, 0.28f, 0.18f), TextAnchor.MiddleCenter, "RotuloDer", 270f);

        return c;
    }

    private static GameObject CamionAMano(string nombre)
    {
        GameObject c = new GameObject(nombre);

        Material naranja = MatColor("Camion_naranja", new Color(0.93f, 0.52f, 0.10f), 0.35f);
        Material blanco  = MatColor("Camion_blanco",  new Color(0.90f, 0.91f, 0.89f), 0.30f);
        Material verde   = MatColor("Camion_verde",   new Color(0.12f, 0.46f, 0.28f), 0.25f);
        Material gris    = MatColor("Camion_gris",    new Color(0.42f, 0.44f, 0.47f), 0.55f);
        Material negro   = MatColor("Camion_negro",   new Color(0.10f, 0.10f, 0.11f), 0.25f);
        Material vidrio  = MatColor("Camion_vidrio",  new Color(0.34f, 0.44f, 0.52f), 0.85f);
        Material ambar   = MatColor("Camion_ambar",   new Color(0.98f, 0.70f, 0.12f), 0.60f);

        // Chasis y cabina.
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 0.62f, -0.20f), new Vector3(2.15f, 0.35f, 6.90f), Vector3.zero,               gris,    "Chasis");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 1.56f,  2.35f), new Vector3(2.20f, 1.55f, 2.00f), Vector3.zero,               naranja, "Cabina");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 1.88f,  3.32f), new Vector3(1.92f, 0.78f, 0.10f), new Vector3(-12f, 0f, 0f),  vidrio,  "Parabrisas");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 2.40f,  2.35f), new Vector3(0.95f, 0.14f, 0.24f), Vector3.zero,               ambar,   "Baliza");

        // Caja compactadora con la franja de la municipalidad.
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 1.85f, -0.90f), new Vector3(2.25f, 1.90f, 4.20f), Vector3.zero,               blanco,  "Compactadora");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 1.52f, -0.90f), new Vector3(2.29f, 0.44f, 4.24f), Vector3.zero,               verde,   "Franja");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 2.82f, -0.90f), new Vector3(2.28f, 0.10f, 4.24f), Vector3.zero,               gris,    "TechoCaja");

        // Tolva trasera (donde se vacían los contenedores).
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 1.22f, -3.32f), new Vector3(2.20f, 1.35f, 0.95f), new Vector3(10f, 0f, 0f),   gris,    "Tolva");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 0.46f, -3.60f), new Vector3(2.24f, 0.22f, 0.50f), Vector3.zero,               ambar,   "Estribo");

        // Faros y guardabarros.
        Pieza(c, PrimitiveType.Cube, new Vector3(-0.76f, 1.02f, 3.37f), new Vector3(0.36f, 0.22f, 0.08f), Vector3.zero, blanco, "FaroIzq");
        Pieza(c, PrimitiveType.Cube, new Vector3( 0.76f, 1.02f, 3.37f), new Vector3(0.36f, 0.22f, 0.08f), Vector3.zero, blanco, "FaroDer");
        Pieza(c, PrimitiveType.Cube, new Vector3(0f, 0.86f, 3.34f), new Vector3(2.20f, 0.34f, 0.16f), Vector3.zero, gris, "Paragolpes");

        // Seis ruedas.
        float[] zr = { 2.40f, -1.15f, -2.35f };
        for (int i = 0; i < zr.Length; i++)
        {
            Pieza(c, PrimitiveType.Cylinder, new Vector3(-1.06f, 0.45f, zr[i]),
                  new Vector3(0.90f, 0.11f, 0.90f), new Vector3(0f, 0f, 90f), negro, "RuedaIzq_" + i);
            Pieza(c, PrimitiveType.Cylinder, new Vector3( 1.06f, 0.45f, zr[i]),
                  new Vector3(0.90f, 0.11f, 0.90f), new Vector3(0f, 0f, 90f), negro, "RuedaDer_" + i);
        }

        // Rótulo en los dos costados de la compactadora.
        TextoPanel(c, "MUNICIPALIDAD DE HUANCAYO\nRecolección de residuos sólidos",
                   new Vector3(-1.17f, 2.00f, -0.90f), 0.0175f,
                   new Color(0.10f, 0.28f, 0.18f), TextAnchor.MiddleCenter, "RotuloIzq", 90f);
        TextoPanel(c, "MUNICIPALIDAD DE HUANCAYO\nRecolección de residuos sólidos",
                   new Vector3( 1.17f, 2.00f, -0.90f), 0.0175f,
                   new Color(0.10f, 0.28f, 0.18f), TextAnchor.MiddleCenter, "RotuloDer", 270f);

        return c;
    }

    private static void GenerarBotes(Vector3 centro)
    {
        AsegurarGameManager();

        // Quitar botes anteriores.
        foreach (RecycleBin viejo in Object.FindObjectsOfType<RecycleBin>())
            Undo.DestroyObjectImmediate(viejo.gameObject);

        foreach (Transform t in Object.FindObjectsOfType<Transform>())
            if (t != null && t.name.StartsWith("EtiquetaBote_"))
                Undo.DestroyObjectImmediate(t.gameObject);

        List<Vector3> vias = RecolectarVias();
        List<Tramo> tramos = AnalizarVias(vias);
        Vector3[] zonas = ElegirZonas(vias, centro);

        int n = PREFABS_BOTE_TIPO.Length;   // 5 tipos = 5 contenedores por zona

        for (int z = 0; z < zonas.Length; z++)
        {
            Vector3 pz = zonas[z];

            // Los contenedores van EN LA VEREDA (nunca sobre el asfalto), repartidos
            // alrededor de la zona: hay que reconocer el color y caminar hasta el correcto.
            List<Vector3> dirs = new List<Vector3>();
            List<Vector3> vereda = PuntosDeVereda(tramos, pz, 46f, dirs);
            List<Vector3> usados = OcupadosDeZona(z);

            for (int t = 0; t < n; t++)
            {
                float ang = (t / (float)n) * Mathf.PI * 2f + z * 0.7f;
                int iv = TomarVereda(vereda, dirs, usados, pz, ang, 10f, 34f, 9f);

                Vector3 p, haciaCalle;
                if (iv >= 0) { p = vereda[iv]; haciaCalle = dirs[iv]; }
                else
                {
                    p = pz + new Vector3(Mathf.Cos(ang) * 12f, 0f, Mathf.Sin(ang) * 12f);
                    haciaCalle = (pz - p).normalized;
                }

                GameObject bote = ContenedorNTP(t,
                    "ContenedorReciclaje_z" + z + "_" + NOMBRE_NTP[t].Replace(" ", ""));

                bote.transform.position = new Vector3(p.x, SueloY(p.x, p.z, 0f), p.z);

                // Mira hacia la calle: así se ven el pictograma y el cartel desde lejos.
                Vector3 mira = haciaCalle; mira.y = 0f;
                if (mira.sqrMagnitude < 0.01f) mira = Vector3.forward;
                mira.Normalize();
                bote.transform.rotation = Quaternion.LookRotation(mira);

                EtiquetaBote(bote, t, z, mira);

                // Cuerpo SÓLIDO: el contenedor ya no se atraviesa.
                BoxCollider cuerpo = bote.AddComponent<BoxCollider>();
                cuerpo.center = new Vector3(0f, 0.76f, 0f);
                cuerpo.size = new Vector3(1.18f, 1.48f, 0.98f);

                // Y encima, la zona de entrega (trigger) más amplia que el cuerpo.
                BoxCollider zonaTrig = bote.AddComponent<BoxCollider>();
                zonaTrig.isTrigger = true;
                zonaTrig.size = new Vector3(3.6f, 3.2f, 3.6f);
                zonaTrig.center = new Vector3(0f, 1.1f, 0f);

                Rigidbody rb = bote.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                RecycleBin bin = bote.AddComponent<RecycleBin>();
                bin.acepta = (TipoResiduo)t;
                bin.zona = z;

                Undo.RegisterCreatedObjectUndo(bote, "Crear contenedor");
            }
        }
    }

    /// <summary>Pinta todo el contenedor con el color exacto de la NTP 900.058-2019.</summary>
    private static void PintarNTP(GameObject bote, int tipo)
    {
        Material m = MatColor("Contenedor_" + NOMBRE_NTP[tipo].Replace(" ", "_"),
                              COLOR_NTP[tipo], 0.22f);
        foreach (Renderer r in bote.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = m;
            r.sharedMaterials = mats;
        }
    }

    /// <summary>Cartel encima del contenedor: color de la norma + qué se echa ahí.</summary>
    private static void EtiquetaBote(GameObject bote, int tipo, int zona, Vector3 mira)
    {
        string[] colorNombre = { "BLANCO", "VERDE", "AZUL", "AMARILLO", "MARRON" };

        GameObject raiz = new GameObject("EtiquetaBote_z" + zona + "_" + tipo);
        raiz.transform.position = bote.transform.position;
        raiz.transform.rotation = Quaternion.LookRotation(mira);

        Material matPoste = MaterialSimple("Assets/Guardian/Materiales/Poste.mat",
                                           new Color(0.55f, 0.56f, 0.59f), 0.55f, null, Vector2.one);
        for (int lado = 0; lado < 2; lado++)
        {
            GameObject poste = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poste.name = lado == 0 ? "PosteIzq" : "PosteDer";
            poste.transform.SetParent(raiz.transform, false);
            poste.transform.localPosition = new Vector3(lado == 0 ? -0.62f : 0.62f, 1.28f, -0.56f);
            poste.transform.localScale = new Vector3(0.07f, 1.28f, 0.07f);
            Object.DestroyImmediate(poste.GetComponent<Collider>());
            poste.GetComponent<Renderer>().sharedMaterial = matPoste;
        }

        GameObject tablero = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tablero.name = "Tablero";
        tablero.transform.SetParent(raiz.transform, false);
        tablero.transform.localPosition = new Vector3(0f, 2.56f, -0.56f);
        tablero.transform.localScale = new Vector3(1.58f, 0.56f, 0.07f);
        Object.DestroyImmediate(tablero.GetComponent<Collider>());
        tablero.GetComponent<Renderer>().sharedMaterial = MatColor(
            "Etiqueta_" + NOMBRE_NTP[tipo].Replace(" ", "_"), COLOR_NTP[tipo], 0.1f);

        Font fuente = FuenteLetrero();
        if (fuente != null)
        {
            Color c = COLOR_NTP[tipo];
            float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            Color tinta = lum > 0.6f ? new Color(0.08f, 0.09f, 0.11f) : Color.white;

            GameObject t = new GameObject("Texto");
            t.transform.SetParent(raiz.transform, false);
            t.transform.localPosition = new Vector3(0f, 2.56f, -0.505f);
            t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            TextMesh tm = t.AddComponent<TextMesh>();
            tm.text = colorNombre[tipo] + "\n" + NOMBRE_NTP[tipo];
            tm.font = fuente;
            tm.fontSize = 90;
            tm.characterSize = 0.029f;
            tm.lineSpacing = 0.9f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = tinta;

            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = fuente.material;
        }

        // Colgar el cartel del contenedor conservando su tamaño real, para que
        // se oculte junto con él cuando se juega otra zona.
        raiz.transform.SetParent(bote.transform, true);

        Undo.RegisterCreatedObjectUndo(raiz, "Crear etiqueta de contenedor");
    }

    // ============================================================
    //  ORDEN Y LIMPIEZA
    //  - Carpetas del proyecto (Assets/Guardian/...)
    //  - Jerarquía de la escena agrupada en "carpetas" (objetos vacíos)
    // ============================================================

    private const string RAIZ = "Assets/Guardian";

    /// <summary>Crea las carpetas del proyecto si no existen.</summary>
    private static void AsegurarCarpetas()
    {
        if (!AssetDatabase.IsValidFolder(RAIZ))
            AssetDatabase.CreateFolder("Assets", "Guardian");

        string[] carpetas = { "Scripts", "Editor", "Prefabs", "Materiales", "Mallas", "Imagenes", "Documentacion" };
        foreach (string c in carpetas)
            if (!AssetDatabase.IsValidFolder(RAIZ + "/" + c))
                AssetDatabase.CreateFolder(RAIZ, c);
    }

    [MenuItem("Tools/Guardián de Huancayo/📁 Crear carpetas del proyecto", false, 2)]
    private static void CrearCarpetasMenu()
    {
        AsegurarCarpetas();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Guardián de Huancayo",
            "Carpetas listas en Assets/Guardian:\n\n" +
            "• Scripts        (código del juego)\n" +
            "• Editor         (herramientas del menú Tools)\n" +
            "• Prefabs        (objetos reutilizables)\n" +
            "• Materiales     (materiales propios, ej. agua del río)\n" +
            "• Imagenes       (capturas, íconos, texturas propias)\n" +
            "• Documentacion  (informe del proyecto)", "Listo");
    }

    // ---- Helpers de jerarquía ----

    private static Transform GrupoRaiz(string nombre)
    {
        Scene esc = SceneManager.GetActiveScene();
        foreach (GameObject r in esc.GetRootGameObjects())
            if (r.name == nombre) return r.transform;

        GameObject go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, "Crear grupo");
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    private static Transform SubGrupo(Transform padre, string nombre)
    {
        Transform t = padre.Find(nombre);
        if (t != null) return t;

        GameObject go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, "Crear grupo");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        return go.transform;
    }

    private static bool EsAncestro(Transform ancestro, Transform t)
    {
        Transform p = t;
        while (p != null) { if (p == ancestro) return true; p = p.parent; }
        return false;
    }

    /// <summary>Mueve el objeto dentro del grupo CONSERVANDO su posición en el mundo.</summary>
    private static void MoverA(Transform obj, Transform destino)
    {
        if (obj == null || destino == null) return;
        if (obj == destino || obj.parent == destino) return;
        if (EsAncestro(obj, destino)) return; // nunca meter un padre dentro de su hijo
        Undo.SetTransformParent(obj, destino, "Ordenar escena");
    }

    /// <summary>Lee la zona de nombres tipo "Basura_z1_4" -> 1. Devuelve -1 si no tiene.</summary>
    private static int ZonaDelNombre(string n)
    {
        int i = n.IndexOf("_z");
        if (i < 0 || i + 2 >= n.Length) return -1;
        char c = n[i + 2];
        return (c >= '0' && c <= '9') ? c - '0' : -1;
    }

    [MenuItem("Tools/Guardián de Huancayo/★ ORDENAR ESCENA (limpiar jerarquía)", false, 1)]
    private static void OrdenarEscenaMenu()
    {
        int movidos = OrdenarEscena();
        EditorUtility.DisplayDialog("Guardián de Huancayo",
            "¡Jerarquía ordenada!  (" + movidos + " objetos agrupados)\n\n" +
            "=== GUARDIAN DE HUANCAYO ===\n" +
            "   Sistema · Jugador · Zona 0/1/2 · Peatones · Aliados · Escenario\n\n" +
            "=== CIUDAD (SimplePoly) ===\n" +
            "   Vias · Edificios · Props · Vehiculos · Naturaleza\n\n" +
            "La cámara y la luz quedan en la raíz.\n" +
            "Todo es reversible con Ctrl+Z. No olvides guardar (Ctrl+S).", "Listo");
    }

    /// <summary>Agrupa todos los objetos sueltos de la escena en grupos con nombre.</summary>
    private static int OrdenarEscena()
    {
        Undo.SetCurrentGroupName("Ordenar escena");
        int grupoUndo = Undo.GetCurrentGroup();
        int movidos = 0;

        // Fotografía de la raíz ANTES de mover nada.
        Scene esc = SceneManager.GetActiveScene();
        GameObject[] raiz = esc.GetRootGameObjects();

        // Estructura del juego
        Transform juego     = GrupoRaiz("=== GUARDIAN DE HUANCAYO ===");
        Transform sistema   = SubGrupo(juego, "Sistema");
        Transform gJugador  = SubGrupo(juego, "Jugador");
        Transform[] zonas = {
            SubGrupo(juego, "Zona 0 - Centro (peatonal)"),
            SubGrupo(juego, "Zona 1 - Mercado (trafico)"),
            SubGrupo(juego, "Zona 2 - Ribera del Shullcas (trafico)"),
        };
        Transform gPeatones = SubGrupo(juego, "Peatones (NPC)");
        Transform gAliados  = SubGrupo(juego, "Aliados (Policias)");
        Transform gLetreros = SubGrupo(juego, "Letreros");
        Transform gEscenario= SubGrupo(juego, "Escenario");

        // Estructura de la ciudad
        Transform ciudad  = GrupoRaiz("=== CIUDAD (SimplePoly) ===");
        Transform gVias   = SubGrupo(ciudad, "Vias");
        Transform gEdif   = SubGrupo(ciudad, "Edificios");
        Transform gProps  = SubGrupo(ciudad, "Props");
        Transform gVeh    = SubGrupo(ciudad, "Vehiculos decorativos");
        Transform gNat    = SubGrupo(ciudad, "Naturaleza");
        Transform gOtros  = SubGrupo(ciudad, "Otros");

        try
        {
            for (int i = 0; i < raiz.Length; i++)
            {
                if (i % 40 == 0)
                    EditorUtility.DisplayProgressBar("Guardián de Huancayo",
                        "Ordenando la jerarquía...", (float)i / Mathf.Max(1, raiz.Length));

                GameObject go = raiz[i];
                if (go == null) continue;
                Transform t = go.transform;
                if (t == juego || t == ciudad) continue;

                string n = go.name;

                // La cámara y la luz se quedan en la raíz (se ubican fácil).
                if (go.GetComponent<Camera>() != null || go.GetComponent<Light>() != null) continue;

                Transform destino = null;

                // --- Objetos del juego ---
                if (go.CompareTag("Player") || n == "Guardian") destino = gJugador;
                else if (go.GetComponent<GameManager>() != null
                      || go.GetComponent<GuardianCameraHUD>() != null
                      || go.GetComponent<RoadNetwork>() != null
                      || n.StartsWith("Guardian") || n.StartsWith("RoadNetwork")) destino = sistema;
                else if (n.StartsWith("Letrero_")) destino = gLetreros;
                else if (n == "Piso_Seguridad" || n == "Rio_Shullcas" || n == "Ribera_Shullcas"
                      || n.StartsWith("Cerro_") || n.StartsWith("Iglesia_")
                      || n.StartsWith("Arena_Shullcas") || n == "SueloValle"
                      || n.StartsWith("Plaza_")
                      || n.StartsWith("VegetacionRio_")) destino = gEscenario;
                else if (n.StartsWith("Peaton")) destino = gPeatones;
                else if (n.StartsWith("PoliciaAliado")) destino = gAliados;
                else
                {
                    int z = ZonaDelNombre(n);
                    if (z >= 0 && z < zonas.Length)
                    {
                        string sub = n.StartsWith("Basura") ? "Basura"
                                   : n.StartsWith("Contenedor") ? "Contenedores"
                                   : n.StartsWith("Carro") ? "Carros"
                                   : n.StartsWith("Puesto") ? "Puestos del mercado" : "Otros";
                        destino = SubGrupo(zonas[z], sub);
                    }
                    // --- Ciudad del pack ---
                    else if (n.StartsWith("Road")) destino = gVias;
                    else if (n.StartsWith("Building")) destino = gEdif;
                    else if (n.StartsWith("Props")) destino = gProps;
                    else if (n.StartsWith("Vehicle")) destino = gVeh;
                    else if (n.StartsWith("Natures")) destino = gNat;
                    else destino = gOtros;
                }

                if (destino != null && t.parent != destino) { MoverA(t, destino); movidos++; }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Undo.CollapseUndoOperations(grupoUndo);
        return movidos;
    }
}
#endif
