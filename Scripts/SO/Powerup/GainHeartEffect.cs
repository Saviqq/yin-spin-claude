using UnityEngine;

[CreateAssetMenu(fileName = "GainHeartEffect", menuName = "Scriptable Objects/Powerup Effects/GainHeart")]
public class GainHeartEffect : PowerupEffect
{
    [SerializeField] private IntegerValue health;

    public override void Apply()
    {
        health.Set(Mathf.Min(health.Value + 1, Constants.MAX_HEALTH));
    }
}
