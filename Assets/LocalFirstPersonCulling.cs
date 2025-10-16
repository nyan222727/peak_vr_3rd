using UnityEngine;
using Fusion;

public class LocalFirstPersonCulling : NetworkBehaviour {
  [Tooltip("Head + inside-shoulder renderers you want hidden only for the local camera")]
  public Renderer[] hideForFirstPerson;
  public string firstPersonLayerName = "FirstPersonHidden";

  public override void Spawned() {
    if (!Object.HasInputAuthority) { enabled = false; return; }

    int fpLayer = LayerMask.NameToLayer(firstPersonLayerName);
    if (fpLayer == -1) { Debug.LogWarning("Create layer 'FirstPersonHidden' first."); return; }

    // Put the selected renderers on that layer (ONLY local avatar)
    foreach (var r in hideForFirstPerson) if (r) SetLayerRecursive(r.gameObject, fpLayer);

    // Hide that layer from the local camera only
    var cam = Camera.main;
    if (cam) cam.cullingMask &= ~(1 << fpLayer);
  }

  static void SetLayerRecursive(GameObject go, int layer) {
    go.layer = layer;
    foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
  }
}
