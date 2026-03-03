using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IntegerValue", menuName = "Scriptable Objects/IntegerValue")]
public class IntegerValue : ScriptableObject
{
    [SerializeField] private int Value;

    public int Current { get; private set; }

    public event Action<int> OnChange;

    void OnEnable()
    {
        Current = Value;
    }

    public void Set(int value)
    {
        Current = value;
        OnChange?.Invoke(Current);
    }

    public void Reset()
    {
        Current = Value;
        OnChange?.Invoke(Current);
    }
}
