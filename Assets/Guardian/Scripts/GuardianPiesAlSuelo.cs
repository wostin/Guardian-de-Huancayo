using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardián de Huancayo - PIES AL SUELO.
///
/// Las animaciones de caminar/idle del pack CityPeople (City M/F Animator) están
/// hechas para otro esqueleto: al reproducirlas, la cadera queda más abajo de lo
/// que debería y el personaje se ve HUNDIDO hasta la cintura en la vereda,
/// aunque su cápsula esté bien parada encima.
///
/// Este componente corrige la pose DESPUÉS de que el Animator la calcula
/// (LateUpdate): mide el hueso más bajo (el pie) y, si quedó por debajo del
/// suelo, sube la cadera lo que falte. No toca la física ni el movimiento, solo
/// el dibujo. Se agrega solo al dar Play a todos los personajes animados.
/// </summary>
public class GuardianPiesAlSuelo : MonoBehaviour
{
    private const bool ACTIVO = false;

    private Transform cadera;
    private Transform[] huesos;
    private float alturaPie;      // altura del hueso más bajo sobre el pivote en la pose original
    private bool listo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Agregar()
    {
        // APAGADO: el hundimiento venía de que faltaban las animaciones del pack
        // CityPeople en el proyecto (los personajes quedaban en la pose por
        // defecto de Unity). Ya se reimportaron; esta corrección ya no hace falta.
        // Pon ACTIVO = true solo si vuelve a pasar con algún modelo.
        if (!ACTIVO) return;
        // AfterSceneLoad corre antes del primer cálculo del Animator: la pose
        // de la escena todavía es la original, con los pies sobre el suelo.
        foreach (Animator an in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (an == null || an.GetComponent<GuardianPiesAlSuelo>() != null) continue;
            if (an.GetComponentInChildren<SkinnedMeshRenderer>(true) == null) continue;
            an.gameObject.AddComponent<GuardianPiesAlSuelo>();
        }
    }

    void Awake()
    {
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr == null || smr.bones == null || smr.bones.Length == 0) return;

        List<Transform> lista = new List<Transform>();
        foreach (Transform b in smr.bones) if (b != null) lista.Add(b);
        if (lista.Count == 0) return;
        huesos = lista.ToArray();

        cadera = smr.rootBone != null ? smr.rootBone : huesos[0];
        if (cadera == transform) return;   // no se puede mover el propio objeto

        float min = float.MaxValue;
        foreach (Transform b in huesos) min = Mathf.Min(min, b.position.y);
        alturaPie = Mathf.Clamp(min - transform.position.y, 0f, 0.2f);
        listo = true;
    }

    private CharacterController cc;
    private float desfase;          // cuánto se sube (+) o baja (-) la cadera, suavizado
    private Vector3 baseLocal, puesto;
    private bool hayPuesto;

    void Start() { cc = GetComponent<CharacterController>(); }

    void LateUpdate()
    {
        if (!listo || cadera == null) return;

        // Si el Animator no reescribió la cadera este cuadro, se parte de la pose
        // sin corregir (así la corrección nunca se acumula).
        if (hayPuesto && (cadera.localPosition - puesto).sqrMagnitude < 1e-10f)
            cadera.localPosition = baseLocal;
        baseLocal = cadera.localPosition;

        float min = float.MaxValue;
        for (int i = 0; i < huesos.Length; i++)
            if (huesos[i] != null) min = Mathf.Min(min, huesos[i].position.y);
        if (min == float.MaxValue) return;

        // En el suelo: el pie más bajo queda APOYADO (ni hundido ni flotando).
        // En el aire (salto): se mantiene la última corrección y el cuerpo sube
        // con el salto normalmente.
        bool enSuelo = cc == null || !cc.enabled || cc.isGrounded;
        if (enSuelo)
        {
            float objetivo = Mathf.Clamp((transform.position.y + alturaPie) - min, -1.2f, 1.6f);
            desfase = Mathf.Lerp(desfase, objetivo, 1f - Mathf.Exp(-25f * Time.deltaTime));
        }

        if (Mathf.Abs(desfase) > 0.005f) cadera.position += Vector3.up * desfase;
        puesto = cadera.localPosition;
        hayPuesto = true;
    }
}
