using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class OVRCameraRigWatchdog : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        Dump("OnEnable");
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m) => Dump($"sceneLoaded {s.name} mode={m}");
    void OnActiveSceneChanged(Scene a, Scene b) => Dump($"activeSceneChanged {a.name} -> {b.name}");

    [ContextMenu("Dump Rigs Now")]
    public void DumpNow() => Dump("Manual");

    static void Dump(string tag)
    {
        var rigs = FindObjectsOfType<OVRCameraRig>(true);
        Debug.Log($"[RigWatchdog] {tag} rigs={rigs.Length}\n" +
                  string.Join("\n", rigs.Select(r =>
                      $" - {r.gameObject.name} id={r.GetInstanceID()} active={r.gameObject.activeInHierarchy} scene={r.gameObject.scene.name}")));
    }
}
