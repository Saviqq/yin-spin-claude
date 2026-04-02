using UnityEngine;

[CreateAssetMenu(fileName = "ExpandPlayAreaEffect", menuName = "Scriptable Objects/Powerup Effects/ExpandPlayArea")]
public class ExpandPlayAreaEffect : PowerupEffect
{
    [SerializeField] private GameEvent expandPlayAreaEvent;

    public override void Apply()
    {
        expandPlayAreaEvent.Raise();
        RaiseMessage();
    }
}
