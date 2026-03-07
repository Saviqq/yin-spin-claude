using UnityEngine;

[CreateAssetMenu(fileName = "BalanceColorEffect", menuName = "Scriptable Objects/Powerup Effects/BalanceColor")]
public class BalanceColorEffect : PowerupEffect
{
    [SerializeField] private GameEvent balanceColorEvent;

    public override void Apply()
    {
        balanceColorEvent.Raise();
    }
}