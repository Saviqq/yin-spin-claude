using UnityEngine;

[CreateAssetMenu(fileName = "SpawnBurstEffect", menuName = "Scriptable Objects/Powerup Effects/SpawnBurst")]
public class SpawnBurstEffect : PowerupEffect
{
    [SerializeField] private IntegerEvent spawnOrbEvent;
    [SerializeField] private int minOrbs = 3;
    [SerializeField] private int maxOrbs = 6;

    public override void Apply()
    {
        spawnOrbEvent.Raise(Random.Range(minOrbs, maxOrbs + 1));
    }
}
