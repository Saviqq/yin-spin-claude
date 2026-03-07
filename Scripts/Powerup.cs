using UnityEngine;

public class Powerup : MonoBehaviour
{
    [SerializeField] private PowerupSet powerupSet;

    private PowerupEffect effect;

    void OnEnable() => powerupSet.Add(this);

    void OnDisable() => powerupSet.Remove(this);

    public void SetEffect(PowerupEffect powerupEffect)
    {
        effect = powerupEffect;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Player>() == null) return;

        effect.Apply();
        Destroy(gameObject);
    }
}