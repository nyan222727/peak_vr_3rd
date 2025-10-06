using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerNetworkManager : NetworkBehaviour
{
    public GameObject selfCamera;
    public List<UnityEngine.Behaviour> selfComponents = new List<UnityEngine.Behaviour>();
    public List<GameObject> selfObjects = new List<GameObject>();
    // Start is called before the first frame update
    
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            selfCamera.SetActive(true);
            
            foreach (var comp in selfComponents)
            {
                if (comp != null)
                    comp.enabled = true;
            }

            foreach (var obj in selfObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
    }
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
