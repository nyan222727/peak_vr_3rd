using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using System.Linq;

[RequireComponent(typeof(NetworkObject))]
public class NetworkCandle : NetworkBehaviour
{
  [Header("Flame children (leave empty to auto-discover)")]
  [SerializeField] private GameObject[] flameObjects;

  // Replicated states (default = false → UNLIT, and not held)
  [Networked] private NetworkBool Lit   { get; set; }
  [Networked] private NetworkBool IsHeld{ get; set; }

  // local cache so we only toggle visuals when state actually changes
  private bool _lastAppliedLit;

  void Awake() {
    if (flameObjects == null || flameObjects.Length == 0) CacheFlames();
  }

  public override void Spawned() {
    // ensure correct visuals for late joiners
    CacheFlames();

    //comment oout this if default should be unlit
    if (Object.HasStateAuthority)
        Lit = true;
        

    ApplyLit(Lit);
  }

  // Called every render tick on all peers; safe & cheap for one bool
  public override void Render() {
    if (_lastAppliedLit != Lit) ApplyLit(Lit);
  }

  // ------- Interaction hooks --------

  // Bind this to your Grab Begin
  public void Grab() {
    if (!Object.HasStateAuthority) Object.RequestStateAuthority();
    IsHeld = true; // no spin needed
  }

  // Bind this to your Grab End
  public void UnGrab() {
    if (!Object.HasStateAuthority) Object.RequestStateAuthority();
    IsHeld = false;
    RPC_CandleUnGrab();
  }

  // Bind this to your Ray "Select/Click" (NOT grab)
  public void ToggleSelected() {
    if (Object.HasStateAuthority) {
      Lit = !Lit;
    } else {
      RPC_RequestToggle();
    }
  }

  // ------- Internals --------

  private void CacheFlames() {
    if (flameObjects != null && flameObjects.Length > 0) return;
    flameObjects = GetComponentsInChildren<Transform>(true)
      .Where(t => t != transform && (t.name.StartsWith("Flame") || t.name.StartsWith("flame")))
      .Select(t => t.gameObject).Distinct().ToArray();
  }

  private void ApplyLit(bool value) {
    if (flameObjects == null || flameObjects.Length == 0) CacheFlames();
    foreach (var go in flameObjects) if (go) go.SetActive(value);
    _lastAppliedLit = value;
  }

  // Clients request a toggle; StateAuthority flips the bit (replicated to all)
  [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
  private void RPC_RequestToggle() {
    Lit = !Lit;
  }

  // Keep parity with your previous ungrab behavior (e.g., re-enable physics)
  [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
  private void RPC_CandleUnGrab() {
    var rb = GetComponent<Rigidbody>();
    if (rb) rb.isKinematic = false;
  }
}
