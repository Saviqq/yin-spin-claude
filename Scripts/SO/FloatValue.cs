using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FloatValue", menuName = "Scriptable Objects/FloatValue")]
public class FloatValue : ScriptableObject
{
    [SerializeField] private float DefaultValue;

    public float Value { get; private set; }

    public event Action<float> OnChange;

    void OnEnable() => Value = DefaultValue;

    public void Set(float value)
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