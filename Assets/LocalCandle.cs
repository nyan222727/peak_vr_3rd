using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCandle : MonoBehaviour
{
    [SerializeField] private bool startLit = true;
    public bool IsLit { get; private set; }

    private void Awake()
    {
        IsLit = startLit;
        Debug.Log($"[LocalCandle] {name} Awake. IsLit={IsLit}");
    }

    // optional, if later you want to toggle
    public void SetLit(bool lit)
    {
        IsLit = lit;
        Debug.Log($"[LocalCandle] {name} SetLit -> {IsLit}");
    }
}
