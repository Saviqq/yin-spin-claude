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
