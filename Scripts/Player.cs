using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource)), RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(PlayerMovement))]
public class Player : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;
    [SerializeField] private float invulnerableDuration;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent balanceColorEvent;

    [Header("Audio")]
    [SerializeField] private AudioClip collectSFX;
    [SerializeField] private AudioClip damageSFX;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private MeshRenderer meshRenderer;
    private Material splitMaterial;
    private PlayerMovement playerMovement;

    private float colorRatio = Constants.DEFAULT_COLOR_RATIO;

    private Coroutine activeInvulnerabilityCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        meshRenderer = GetComponent<MeshRenderer>();
        splitMaterial = meshRenderer.material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
        playerMovement = GetComponent<PlayerMovement>();
    }

    void OnEnable()
    {
        gameStartEvent.OnRaised += OnGameStart;
        balanceColorEvent.OnRaised += OnBalanceColor;
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= OnGameStart;
        balanceColorEvent.OnRaised -= OnBalanceColor;
    }
    void FixedUpdate()
    {
        playerMovement.Handle();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Orb orb = other.GetComponent<Orb>();
        if (orb == null) return;

        bool hitWhiteHalf = IsWhiteHalf(other.transform.position);
        if (orb.IsWhite == hitWhiteHalf)
        {
            CollectOrb(orb.IsWhite);
        }
        else
        {
            TryTakeDamage();
        }

        Destroy(other.gameObject);
    }

    private void CollectOrb(bool orbIsWhite)
    {
        colorRatio += orbIsWhite ? Constants.COLLECT_DELTA : -Constants.COLLECT_DELTA;
        colorRatio = Mathf.Clamp01(colorRatio);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
        audioSource.PlayOneShot(collectSFX);
        score.Set(score.Value + 1);
    }

    private void TryTakeDamage()
    {
        if (activeInvulnerabilityCoroutine == null && health.Value > 0)
        {
            health.Set(health.Value - 1);
            audioSource.PlayOneShot(damageSFX);
            activeInvulnerabilityCoroutine = StartCoroutine(FlashAndBeInvulnerable());
        }
    }

    private IEnumerator FlashAndBeInvulnerable()
    {
        float elapsed = 0f;
        float flashInterval = 0.25f;

        while (elapsed < invulnerableDuration)
        {
            meshRenderer.enabled = false;
            yield return new WaitForSeconds(flashInterval);
            meshRenderer.enabled = true;
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval * 2f;
        }

        meshRenderer.enabled = true;
        activeInvulnerabilityCoroutine = null;
    }

    private bool IsWhiteHalf(Vector3 orbWorldPos)
    {
        Vector2 worldDir = (Vector2)(orbWorldPos - transform.position);
        float worldAngle = Mathf.Atan2(worldDir.y, worldDir.x);
        float localAngle = worldAngle - rb.rotation * Mathf.Deg2Rad;
        float t = localAngle / (2f * Mathf.PI) + 0.5f;
        t = t - Mathf.Floor(t);
        return t < colorRatio;
    }

    private void OnGameStart()
    {
        transform.position = Vector2.zero;
        meshRenderer.enabled = true;
        colorRatio = Mathf.Clamp01(Constants.DEFAULT_COLOR_RATIO);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);

        if (activeInvulnerabilityCoroutine != null)
        {
            StopCoroutine(activeInvulnerabilityCoroutine);
            activeInvulnerabilityCoroutine = null;
        }
    }

    private void OnBalanceColor()
    {
        if (colorRatio > 0.5f)
        {
            colorRatio -= Constants.COLLECT_DELTA;
        }
        else if (colorRatio < 0.5f)
        {
            colorRatio += Constants.COLLECT_DELTA;
        }
        colorRatio = Mathf.Clamp01(colorRatio);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }
}