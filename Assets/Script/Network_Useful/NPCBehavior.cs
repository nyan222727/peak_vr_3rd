using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class NPCBehavior : NetworkBehaviour, IStateAuthorityChanged
{
    [SerializeField] private float moveSpeed = 1.0f;
    private Vector3 dir = Vector3.right;
    private bool active;

    public override void Spawned()
    {
        NPCRegistry.Register(this);
        active = Object.HasStateAuthority;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        NPCRegistry.Unregister(this);
    }

    public void StateAuthorityChanged()
    {
        active = Object.HasStateAuthority;
    }

    private void Update()
    {
        if (!active) return;

        // transform.position += dir * (moveSpeed * Time.deltaTime);
        // if (Mathf.Abs(transform.position.x) > 6f) dir = -dir;
    }
}
