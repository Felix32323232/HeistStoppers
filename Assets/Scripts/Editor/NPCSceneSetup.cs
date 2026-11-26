using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Monta BankLayout / MetalDetector / mostrador a partir de los objetos que ya hay en la escena:
//   BankLayout (con hijos "Entrada/Salida" y P1..Pn)  -> BankLayout.entrance/exit + wanderPoints
//   ServicePoint (ServicePointGroup)                  -> BankLayout.counter
//   MetalDetector (con hijos "In" y "Out")            -> componente MetalDetector + su cola
// Se puede volver a ejecutar sin romper nada.
public static class NPCSceneSetup
{
    [MenuItem("HeistStoppers/Configurar NPCs en la escena")]
    static void Setup()
    {
        var log = new List<string>();

        // ---------- BankLayout ----------
        GameObject layoutGo = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name == "BankLayout" && t.childCount > 0)
            .Select(t => t.gameObject)
            .FirstOrDefault();

        if (layoutGo == null)
        {
            EditorUtility.DisplayDialog("NPCs", "No encuentro un objeto \"BankLayout\" con hijos (Entrada/Salida, P1, P2...).", "OK");
            return;
        }

        BankLayout layout = layoutGo.GetComponent<BankLayout>();
        if (layout == null)
        {
            layout = Undo.AddComponent<BankLayout>(layoutGo);
            log.Add("Añadido componente BankLayout.");
        }
        Undo.RecordObject(layout, "Configurar BankLayout");

        Transform door = layoutGo.transform.Find("Entrada/Salida");
        if (door == null)
        {
            foreach (Transform c in layoutGo.transform)
                if (c.name.ToLower().Contains("entrada")) door = c;
        }
        if (door != null)
        {
            if (layout.entrance == null) layout.entrance = door;
            if (layout.exit == null) layout.exit = door;
        }
        else log.Add("AVISO: no hay hijo \"Entrada/Salida\"; asigna Entrance y Exit a mano.");

        var wander = new List<Transform>();
        foreach (Transform c in layoutGo.transform)
            if (c != door && c.name.StartsWith("P")) wander.Add(c);
        if (layout.wanderPoints == null || layout.wanderPoints.Length == 0)
        {
            layout.wanderPoints = wander.ToArray();
            log.Add($"Wander points: {wander.Count}.");
        }

        // ---------- Detector de metales ----------
        GameObject detectorGo = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.name == "MetalDetector" && t.Find("In") != null && t.Find("Out") != null)
            .Select(t => t.gameObject)
            .FirstOrDefault();

        if (detectorGo != null)
        {
            Transform inT = detectorGo.transform.Find("In");
            Transform outT = detectorGo.transform.Find("Out");

            // El ServicePointGroup que estaba directamente en el arco lo tenía a él mismo como spot: se quita.
            ServicePointGroup wrongGroup = detectorGo.GetComponent<ServicePointGroup>();
            if (wrongGroup != null)
            {
                Undo.DestroyObjectImmediate(wrongGroup);
                log.Add("Quitado ServicePointGroup mal puesto en MetalDetector.");
            }

            MetalDetector detector = detectorGo.GetComponent<MetalDetector>();
            if (detector == null)
            {
                detector = Undo.AddComponent<MetalDetector>(detectorGo);
                log.Add("Añadido componente MetalDetector.");
            }
            Undo.RecordObject(detector, "Configurar MetalDetector");

            Vector3 through = outT.position - inT.position;
            through.y = 0f;
            if (through.sqrMagnitude < 0.0001f) through = detectorGo.transform.forward;
            through.Normalize();

            Transform center = GetOrCreateChild(detectorGo.transform, "Center", (inT.position + outT.position) * 0.5f, Quaternion.LookRotation(through));
            Transform queueGo = GetOrCreateChild(detectorGo.transform, "Queue", inT.position, Quaternion.LookRotation(through));
            // La cola crece hacia el forward de QueueStart: hacia atrás, lejos del arco.
            Transform queueStart = GetOrCreateChild(queueGo, "QueueStart", inT.position - through * 1.1f, Quaternion.LookRotation(-through));

            Undo.RecordObject(inT, "Orientar In");
            inT.rotation = Quaternion.LookRotation(through);

            ServicePointGroup queue = queueGo.GetComponent<ServicePointGroup>();
            if (queue == null) queue = Undo.AddComponent<ServicePointGroup>(queueGo.gameObject);
            Undo.RecordObject(queue, "Configurar cola detector");
            queue.spots = new[] { inT };
            queue.queueStart = queueStart;

            detector.queue = queue;
            detector.center = center;
            detector.exitPoint = outT;
            if (layout.metalDetector == null) layout.metalDetector = detector;
            EditorUtility.SetDirty(detector);
            EditorUtility.SetDirty(queue);
        }
        else log.Add("AVISO: no hay objeto \"MetalDetector\" con hijos In/Out; el NPC del detector se irá nada más entrar.");

        // ---------- Mostrador ----------
        if (layout.counter == null)
        {
            ServicePointGroup counter = Object.FindObjectsByType<ServicePointGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(g => g.GetComponentInParent<MetalDetector>() == null);
            if (counter != null)
            {
                layout.counter = counter;
                log.Add($"Mostrador: {counter.name}.");
            }
            else log.Add("AVISO: no hay ServicePointGroup para el mostrador.");
        }

        EditorUtility.SetDirty(layout);
        EditorSceneManager.MarkSceneDirty(layoutGo.scene);

        // ---------- Comprobaciones ----------
        CheckOnNavMesh(layout.entrance, "Entrada", log);
        if (layout.wanderPoints != null)
            foreach (Transform p in layout.wanderPoints) CheckOnNavMesh(p, p != null ? p.name : "?", log);

        NPCSpawner spawner = Object.FindFirstObjectByType<NPCSpawner>();
        if (spawner == null) log.Add("AVISO: no hay NPCSpawner en la escena.");

        LobbyManager lobby = Object.FindFirstObjectByType<LobbyManager>();
        if (lobby == null) log.Add("AVISO: no hay LobbyManager; el NPCSpawner se queda esperando para siempre.");

        string msg = "Hecho. Guarda la escena.\n\n" + string.Join("\n", log);
        Debug.Log("[NPCSceneSetup] " + msg, layoutGo);
        EditorUtility.DisplayDialog("NPCs", msg, "OK");
    }

    static Transform GetOrCreateChild(Transform parent, string name, Vector3 position, Quaternion rotation)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + name);
            Undo.SetTransformParent(go.transform, parent, "Crear " + name);
            t = go.transform;
        }
        Undo.RecordObject(t, "Colocar " + name);
        t.SetPositionAndRotation(position, rotation);
        return t;
    }

    static void CheckOnNavMesh(Transform t, string label, List<string> log)
    {
        if (t == null) return;
        if (!NavMesh.SamplePosition(t.position, out _, 3f, NavMesh.AllAreas))
            log.Add($"AVISO: \"{label}\" no está cerca del NavMesh (¿está horneado?).");
    }
}
