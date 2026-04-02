using UnityEngine;

[CreateAssetMenu(fileName = "ClearOrbsEffect", menuName = "Scriptable Objects/Powerup Effects/ClearOrbs")]
public class ClearOrbsEffect : PowerupEffect
{
    [SerializeField] private OrbSet orbSet;

    public override void Apply()
    {
        for (int i = orbSet.Items.Count - 1; i >= 0; i--)
        {
            Destroy(orbSet.Items[i].gameObject);
        }
        RaiseMessage();
    }
}
