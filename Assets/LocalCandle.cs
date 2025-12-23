using System.Linq;
using UnityEngine;

public class LocalCandle : MonoBehaviour
{
  [Header("Flame children (leave empty to auto-discover)")]
  [SerializeField] private GameObject[] flameObjects;

  public bool IsHeldByAnyone => IsHeld;
  public bool IsLit;

  // Local states
  [SerializeField] private bool Lit = false;
  [SerializeField] private bool IsHeld = false;

  // local cache so we only toggle visuals when state actually changes
  private bool _lastAppliedLit;

  void Awake()
  {
    if (flameObjects == null || flameObjects.Length == 0) CacheFlames();
  }

  void Start()
  {
    // ensure correct visuals
    CacheFlames();

    // mimic original: if object name starts with "glow", default lit
    if (gameObject.name.StartsWith("glow"))
      Lit = true;

    ApplyLit(Lit);
  }

  void Update()
  {
    // cheap polling like your Render() logic
    if (_lastAppliedLit != Lit) ApplyLit(Lit);
  }

  // ------- Interaction hooks --------

  // Bind this to your Grab Begin
  public void Grab()
  {
    IsHeld = true;
  }

  // Bind this to your Grab End
  public void UnGrab()
  {
    IsHeld = false;
    CandleUnGrabLocal(); // parity with RPC_CandleUnGrab
  }

  // Bind this to your Ray "Select/Click" (NOT grab)
  public void ToggleSelected()
  {
    Lit = !Lit;
    // visuals will update via Update() polling, or you can call ApplyLit(Lit) immediately:
    ApplyLit(Lit);
  }

  // ------- Internals --------

  private void CacheFlames()
  {
    if (flameObjects != null && flameObjects.Length > 0) return;

    flameObjects = GetComponentsInChildren<Transform>(true)
      .Where(t => t != transform && (t.name.StartsWith("Flame") || t.name.StartsWith("flame")))
      .Select(t => t.gameObject)
      .Distinct()
      .ToArray();
  }

  private void ApplyLit(bool value)
  {
    if (flameObjects == null || flameObjects.Length == 0) CacheFlames();
    foreach (var go in flameObjects) if (go) go.SetActive(value);

    _lastAppliedLit = value;
    IsLit = value;
    Lit = value;
  }

  // Keep parity with your previous ungrab behavior (e.g., re-enable physics)
  private void CandleUnGrabLocal()
  {
    var rb = GetComponent<Rigidbody>();
    if (rb) rb.isKinematic = false;
  }
}
