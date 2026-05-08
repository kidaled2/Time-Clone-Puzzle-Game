using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    [System.Serializable]
    private sealed class PooledEffect
    {
        public GameObject prefab;
        public Queue<ParticleSystem> available = new Queue<ParticleSystem>();
    }

    public static VFXManager Instance { get; private set; }

    [Header("VFX Prefabs")]
    [SerializeField] private GameObject vfxPressurePlateActivate;
    [SerializeField] private GameObject vfxDoorOpen;
    [SerializeField] private GameObject vfxCloneSpawn;
    [SerializeField] private GameObject vfxBoxPush;
    [SerializeField] private GameObject vfxLevelComplete;

    [SerializeField] private Transform pooledRoot;

    private readonly Dictionary<GameObject, PooledEffect> pools = new Dictionary<GameObject, PooledEffect>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (pooledRoot == null)
        {
            pooledRoot = transform;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayPressurePlateActivate(Vector3 position)
    {
        PlayEffect(vfxPressurePlateActivate, position);
    }

    public void PlayDoorOpen(Vector3 position)
    {
        PlayEffect(vfxDoorOpen, position);
    }

    public void PlayCloneSpawn(Vector3 position)
    {
        PlayEffect(vfxCloneSpawn, position);
    }

    public void PlayBoxPush(Vector3 position)
    {
        PlayBoxPush(position, Vector3.forward);
    }

    public void PlayBoxPush(Vector3 position, Vector3 pushDirection)
    {
        Quaternion rotation = pushDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(pushDirection.normalized, Vector3.up)
            : Quaternion.identity;
        PlayEffect(vfxBoxPush, position, rotation);
    }

    public void PlayLevelComplete(Vector3 position)
    {
        PlayEffect(vfxLevelComplete, position);
    }

    private void PlayEffect(GameObject prefab, Vector3 position)
    {
        PlayEffect(prefab, position, Quaternion.identity);
    }

    private void PlayEffect(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return;
        }

        ParticleSystem particleSystem = GetOrCreateSystem(prefab);
        if (particleSystem == null)
        {
            return;
        }

        GameObject effectObject = particleSystem.gameObject;
        effectObject.transform.SetPositionAndRotation(position, rotation);
        effectObject.transform.SetParent(pooledRoot, true);
        effectObject.SetActive(true);

        particleSystem.Clear(true);
        particleSystem.Play(true);

        float effectDuration = GetEffectDuration(effectObject);
        StartCoroutine(ReturnToPool(prefab, particleSystem, effectDuration));
    }

    private ParticleSystem GetOrCreateSystem(GameObject prefab)
    {
        PooledEffect pool = GetOrCreatePool(prefab);
        while (pool.available.Count > 0)
        {
            ParticleSystem pooledSystem = pool.available.Dequeue();
            if (pooledSystem != null)
            {
                return pooledSystem;
            }
        }

        GameObject instance = Instantiate(prefab, pooledRoot);
        instance.name = prefab.name;
        instance.SetActive(false);

        ParticleSystem particleSystem = instance.GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            particleSystem = instance.GetComponentInChildren<ParticleSystem>(true);
        }

        return particleSystem;
    }

    private PooledEffect GetOrCreatePool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out PooledEffect pool))
        {
            pool = new PooledEffect { prefab = prefab };
            pools.Add(prefab, pool);
        }

        return pool;
    }

    private IEnumerator ReturnToPool(GameObject prefab, ParticleSystem particleSystem, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (particleSystem == null)
        {
            yield break;
        }

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        GameObject effectObject = particleSystem.gameObject;
        effectObject.SetActive(false);
        effectObject.transform.SetParent(pooledRoot, false);

        PooledEffect pool = GetOrCreatePool(prefab);
        pool.available.Enqueue(particleSystem);
    }

    private float GetEffectDuration(GameObject effectObject)
    {
        float maxDuration = 0f;
        ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            float lifetime = GetCurveMax(main.startLifetime);
            maxDuration = Mathf.Max(maxDuration, main.duration + lifetime);
        }

        return maxDuration + 0.1f;
    }

    private static float GetCurveMax(ParticleSystem.MinMaxCurve curve)
    {
        return curve.mode switch
        {
            ParticleSystemCurveMode.Constant => curve.constant,
            ParticleSystemCurveMode.TwoConstants => curve.constantMax,
            ParticleSystemCurveMode.Curve => GetAnimationCurveMax(curve.curveMax, curve.constant),
            ParticleSystemCurveMode.TwoCurves => GetAnimationCurveMax(curve.curveMax, curve.constantMax),
            _ => curve.constantMax,
        };
    }

    private static float GetAnimationCurveMax(AnimationCurve curve, float fallback)
    {
        if (curve == null || curve.length == 0)
        {
            return fallback;
        }

        float maxValue = curve.keys[0].value;
        for (int i = 1; i < curve.length; i++)
        {
            maxValue = Mathf.Max(maxValue, curve.keys[i].value);
        }

        return maxValue;
    }
}
