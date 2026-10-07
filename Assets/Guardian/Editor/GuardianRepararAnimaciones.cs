using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - Reparar animaciones de los personajes.
///
/// Si a los Animator (City M / City F / construction) les faltan los clips,
/// los personajes quedan en la pose por defecto de Unity: hundidos hasta la
/// cintura, inclinados y con los brazos adelante. Esta herramienta busca cada
/// estado sin animación y le vuelve a poner el clip del mismo nombre
/// (por ejemplo, el estado "locom_m_basicWalk_30f" recibe el clip de
/// locom_m_basicWalk_30f.fbx).
/// </summary>
public static class GuardianRepararAnimaciones
{
    [MenuItem("Tools/Guardián de Huancayo/Reparar animaciones de personajes")]
    public static void Reparar()
    {
        // Todos los clips del proyecto, por nombre.
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                AnimationClip c = o as AnimationClip;
                if (c == null || c.name.StartsWith("__preview__")) continue;
                if (!clips.ContainsKey(c.name)) clips[c.name] = c;
            }
        }

        int bien = 0, arreglados = 0, sinClip = 0;
        List<string> faltan = new List<string>();

        foreach (string guid in AssetDatabase.FindAssets("t:AnimatorController"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta);
            if (ac == null) continue;
            bool cambio = false;

            foreach (AnimatorControllerLayer capa in ac.layers)
                foreach (ChildAnimatorState cs in capa.stateMachine.states)
                {
                    AnimatorState st = cs.state;
                    if (st.motion != null) { bien++; continue; }

                    AnimationClip c;
                    if (clips.TryGetValue(st.name, out c))
                    {
                        st.motion = c;
                        EditorUtility.SetDirty(st);
                        cambio = true;
                        arreglados++;
                    }
                    else { sinClip++; faltan.Add(ac.name + " / " + st.name); }
                }

            if (cambio) EditorUtility.SetDirty(ac);
        }

        AssetDatabase.SaveAssets();
        string msg = "Estados con animación: " + bien + "\nReparados: " + arreglados + "\nSin clip encontrado: " + sinClip;
        if (faltan.Count > 0) msg += "\n\n" + string.Join("\n", faltan.ToArray());
        Debug.Log("[Guardián] Reparar animaciones → " + msg.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Reparar animaciones", msg, "OK");
    }
}
