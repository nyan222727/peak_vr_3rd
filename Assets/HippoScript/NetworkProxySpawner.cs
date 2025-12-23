using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class NetworkProxySpawner : NetworkBehaviour
{
    public NetworkPrefabRef proxyPrefab; // 指向 ProxyPrefab（含 NetworkItemProxy + NetworkTransform）
    private readonly Dictionary<string, NetworkItemProxy> _proxies = new();

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            SpawnAllProxies();
        }
    }

    private void SpawnAllProxies()
    {
        // XR端才有這些物件；手機端會找不到也沒關係，因為手機端不是StateAuthority不會跑這段
        var tags = FindObjectsOfType<XRInteractableTag>(true);

        foreach (var tag in tags)
        {
            if (string.IsNullOrEmpty(tag.ItemId)) continue;
            if (_proxies.ContainsKey(tag.ItemId)) continue;

            var no = Runner.Spawn(proxyPrefab, tag.transform.position, tag.transform.rotation);
            var proxy = no.GetComponent<NetworkItemProxy>();

            proxy.ItemId = tag.ItemId;
            proxy.VisualKey = tag.VisualKey;

            // XR端可以立刻綁定source（手機端 proxy.xrSource 會保持null，不影響）
            proxy.xrSource = tag.transform;
            proxy.followXRSource = true;

            no.name = $"Proxy_{tag.ItemId}";
            _proxies.Add(tag.ItemId, proxy);
        }
    }
}
