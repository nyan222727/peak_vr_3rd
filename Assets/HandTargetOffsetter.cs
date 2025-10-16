using UnityEngine;
public class HandTargetOffsetter : MonoBehaviour {
  public Transform leftTracker, rightTracker;   // your HandL/HandR trackers
  public Transform leftTarget, rightTarget;     // assign these in VRIK Arm Targets
  public Vector3 leftEulerOffset  = new Vector3(0,-90,90);
  public Vector3 rightEulerOffset = new Vector3(0, 90,-90);

  void Awake() {
    if (!leftTarget)  leftTarget  = Mk(leftTracker,  "HandL_Target");
    if (!rightTarget) rightTarget = Mk(rightTracker, "HandR_Target");
  }
  Transform Mk(Transform parent, string n) { var t=new GameObject(n).transform; t.SetParent(parent,false); return t; }

  void LateUpdate() {
    if (leftTracker)  { leftTarget.position  = leftTracker.position;  leftTarget.rotation  = leftTracker.rotation  * Quaternion.Euler(leftEulerOffset); }
    if (rightTracker) { rightTarget.position = rightTracker.position; rightTarget.rotation = rightTracker.rotation * Quaternion.Euler(rightEulerOffset); }
  }
}
