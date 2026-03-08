using UnityEngine;

[CreateAssetMenu(fileName = "ShrinkPlayerEffect", menuName = "Scriptable Objects/Powerup Effects/ShrinkPlayer")]
public class ShrinkPlayerEffect : PowerupEffect
{
    [SerializeField] private GameEvent shrinkPlayerEvent;

    public override void Apply() => shrinkPlayerEvent.Raise();
}