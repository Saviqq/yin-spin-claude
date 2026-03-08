using UnityEngine;

[CreateAssetMenu(fileName = "SwitchOrbColorsEffect", menuName = "Scriptable Objects/Powerup Effects/SwitchOrbColors")]
public class SwitchOrbColorsEffect : PowerupEffect
{
    [SerializeField] private GameEvent switchOrbColorsEvent;

    public override void Apply() => switchOrbColorsEvent.Raise();
}