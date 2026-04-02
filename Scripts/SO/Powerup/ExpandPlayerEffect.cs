using UnityEngine;

[CreateAssetMenu(fileName = "ExpandPlayerEffect", menuName = "Scriptable Objects/Powerup Effects/ExpandPlayer")]
public class ExpandPlayerEffect : PowerupEffect
{
    [SerializeField] private GameEvent expandPlayerEvent;

    public override void Apply()
    {
        expandPlayerEvent.Raise();
        RaiseMessage();
    }
}
