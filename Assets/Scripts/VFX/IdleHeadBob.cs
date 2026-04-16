using System.Collections;
using UnityEngine;

#if DOTWEEN
using DG.Tweening;
#endif

public class IdleHeadBob : MonoBehaviour
{
    [Header("Bob Settings")]
    [SerializeField] private Transform headTransform;
    [SerializeField] private float bobHeight = 0.04f;
    [SerializeField] private float bobSpeed = 1.8f;
    [SerializeField] private float returnSpeed = 8f;

    [Header("Idle Delay")]
    [SerializeField] private float idleDelay = 0.15f;

    private Vector3 headRestLocalPos;
    private bool isMoving;
    private bool isBobbing;
    private Coroutine bobCoroutine;
    private Coroutine returnCoroutine;
    private Coroutine delayedStartCoroutine;

#if DOTWEEN
    private Tween bobTween;
    private Tween returnTween;
#endif

    private void Awake()
    {
        if (headTransform == null)
        {
            Transform found = transform.Find("Character_Root/Head");
            if (found != null)
            {
                headTransform = found;
            }
        }

        if (headTransform != null)
        {
            headRestLocalPos = headTransform.localPosition;
        }
    }

    private void Start()
    {
        OnMovementEnd();
    }

    private void OnDisable()
    {
        StopAllHeadMotion();
        ResetHeadToRest();
    }

    private void OnDestroy()
    {
        StopAllHeadMotion();
    }

    public void OnMovementStart()
    {
        isMoving = true;

        if (delayedStartCoroutine != null)
        {
            StopCoroutine(delayedStartCoroutine);
            delayedStartCoroutine = null;
        }

        StopBob();
        StartReturn();
    }

    public void OnMovementEnd()
    {
        isMoving = false;

        if (delayedStartCoroutine != null)
        {
            StopCoroutine(delayedStartCoroutine);
        }

        delayedStartCoroutine = StartCoroutine(DelayedBobStart());
    }

    private IEnumerator DelayedBobStart()
    {
        yield return new WaitForSeconds(idleDelay);
        delayedStartCoroutine = null;

        if (!isMoving)
        {
            StartBob();
        }
    }

    private void StartBob()
    {
        if (isBobbing || headTransform == null)
        {
            return;
        }

        isBobbing = true;

#if DOTWEEN
        if (bobTween != null)
        {
            bobTween.Kill();
        }

        Vector3 upPos = headRestLocalPos + Vector3.up * bobHeight;
        Vector3 downPos = headRestLocalPos - Vector3.up * (bobHeight * 0.4f);
        float halfCycle = 0.5f / bobSpeed;

        bobTween = DOTween.Sequence()
            .SetLoops(-1, LoopType.Restart)
            .Append(headTransform.DOLocalMove(upPos, halfCycle).SetEase(Ease.InOutSine))
            .Append(headTransform.DOLocalMove(downPos, halfCycle).SetEase(Ease.InOutSine))
            .SetUpdate(false);
#else
        if (bobCoroutine != null)
        {
            StopCoroutine(bobCoroutine);
        }

        bobCoroutine = StartCoroutine(BobCoroutine());
#endif
    }

    private void StopBob()
    {
        isBobbing = false;

#if DOTWEEN
        if (bobTween != null && bobTween.IsActive())
        {
            bobTween.Kill();
        }

        bobTween = null;
#else
        if (bobCoroutine != null)
        {
            StopCoroutine(bobCoroutine);
            bobCoroutine = null;
        }
#endif
    }

    private void StartReturn()
    {
        if (headTransform == null)
        {
            return;
        }

#if DOTWEEN
        if (returnTween != null)
        {
            returnTween.Kill();
        }

        returnTween = headTransform
            .DOLocalMove(headRestLocalPos, 1f / returnSpeed)
            .SetEase(Ease.OutSine)
            .OnComplete(() => returnTween = null);
#else
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
        }

        returnCoroutine = StartCoroutine(ReturnCoroutine());
#endif
    }

    private void StopAllHeadMotion()
    {
        if (delayedStartCoroutine != null)
        {
            StopCoroutine(delayedStartCoroutine);
            delayedStartCoroutine = null;
        }

        StopBob();

#if DOTWEEN
        if (returnTween != null && returnTween.IsActive())
        {
            returnTween.Kill();
        }

        returnTween = null;
#else
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
#endif
    }

    private void ResetHeadToRest()
    {
        if (headTransform != null)
        {
            headTransform.localPosition = headRestLocalPos;
        }
    }

#if !DOTWEEN
    private IEnumerator BobCoroutine()
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * bobSpeed;
            float yOffset = Mathf.Sin(time * Mathf.PI * 2f) * bobHeight;
            if (headTransform != null)
            {
                headTransform.localPosition = headRestLocalPos + Vector3.up * yOffset;
            }

            yield return null;
        }
    }

    private IEnumerator ReturnCoroutine()
    {
        while (headTransform != null && Vector3.Distance(headTransform.localPosition, headRestLocalPos) > 0.001f)
        {
            headTransform.localPosition = Vector3.MoveTowards(
                headTransform.localPosition,
                headRestLocalPos,
                returnSpeed * Time.deltaTime);
            yield return null;
        }

        if (headTransform != null)
        {
            headTransform.localPosition = headRestLocalPos;
        }

        returnCoroutine = null;
    }
#endif
}
