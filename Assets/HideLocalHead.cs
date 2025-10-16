using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class HideLocalHead : Fusion.NetworkBehaviour {
  public Renderer[] headRenderers;
  public override void Spawned() {
    foreach (var r in headRenderers) r.enabled = !Object.HasInputAuthority;
  }
}

