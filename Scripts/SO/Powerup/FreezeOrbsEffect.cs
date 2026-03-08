using UnityEngine;

[CreateAssetMenu(fileName = "FreezeOrbsEffect", menuName = "Scriptable Objects/Powerup Effects/FreezeOrbs")]
public class FreezeOrbsEffect : PowerupEffect
{
    [SerializeField] private GameEvent freezeOrbsEvent;

    public override void Apply() => freezeOrbsEvent.Raise();
}