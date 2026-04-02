using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IntegerValue", menuName = "Scriptable Objects/IntegerValue")]
public class IntegerValue : ScriptableObject
{
    [SerializeField] private int DefaultValue;

    public int Value { get; private set; }

    public event Action<int> OnChange;

    void OnEnable() => Value = DefaultValue;

    public void Set(int value)
    {
        Value = value;
        OnChange?.Invoke(Value);
    }

    public void Reset()
    {
        Value = DefaultValue;
        OnChange?.Invoke(Value);
    }
}