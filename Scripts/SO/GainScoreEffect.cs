using UnityEngine;

[CreateAssetMenu(fileName = "GainScoreEffect", menuName = "Scriptable Objects/Powerup Effects/GainScore")]
public class GainScoreEffect : PowerupEffect
{
    [SerializeField] private IntegerValue score;
    [SerializeField] private int amount = 5;

    public override void Apply()
    {
        score.Set(score.Value + amount);
    }
}