using UnityEngine;

public class OrbSpawner
{
    private readonly Orb orbPrefab;
    private readonly FloatValue halfHeightPlayArea;
    private readonly FloatValue halfWidthPlayArea;

    public OrbSpawner(Orb orbPrefab, FloatValue halfHeightPlayArea, FloatValue halfWidthPlayArea)
    {
        this.orbPrefab = orbPrefab;
        this.halfHeightPlayArea = halfHeightPlayArea;
        this.halfWidthPlayArea = halfWidthPlayArea;
    }

    public Orb Spawn(float orbSpeed)
    {
        float hw = halfWidthPlayArea.Value;
        float hh = halfHeightPlayArea.Value;

        int edge = Random.Range(0, 4);
        Vector2 spawnPos;
        float angleMin, angleMax;

        switch (edge)
        {
            case 0: // top
                spawnPos = new Vector2(Random.Range(-hw, hw), hh + Constants.SPAWN_MARGIN);
                angleMin = 210f; angleMax = 330f;
                break;
            case 1: // right
                spawnPos = new Vector2(hw + Constants.SPAWN_MARGIN, Random.Range(-hh, hh));
                angleMin = 120f; angleMax = 240f;
                break;
            case 2: // bottom
                spawnPos = new Vector2(Random.Range(-hw, hw), -hh - Constants.SPAWN_MARGIN);
                angleMin = 30f; angleMax = 150f;
                break;
            default: // left
                spawnPos = new Vector2(-hw - Constants.SPAWN_MARGIN, Random.Range(-hh, hh));
                angleMin = -60f; angleMax = 60f;
                break;
        }

        float angleDeg = Random.Range(angleMin, angleMax);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        bool isWhite = Random.value > 0.5f;
        Orb orb = Object.Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        orb.Initialize(isWhite, direction, orbSpeed);
        return orb;
    }
}
