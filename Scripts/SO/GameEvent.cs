using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEvent", menuName = "Scriptable Objects/GameEvent")]
public class GameEvent : ScriptableObject
{
    public event Action OnRaised;

    public void Raise()
    {
        OnRaised?.Invoke();
    }
}

public abstract class GameEvent<T> : ScriptableObject
{
    public event Action<T> OnRaised;

    public void Raise(T value)
    {
        OnRaised?.Invoke(value);
    }
}
