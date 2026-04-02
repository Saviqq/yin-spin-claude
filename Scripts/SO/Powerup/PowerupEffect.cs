using UnityEngine;

public abstract class PowerupEffect : ScriptableObject
{
    [SerializeField] private StringEvent powerupMessageEvent;
    [SerializeField] protected string message;

    public abstract void Apply();

    protected void RaiseMessage() => powerupMessageEvent?.Raise(message);
}