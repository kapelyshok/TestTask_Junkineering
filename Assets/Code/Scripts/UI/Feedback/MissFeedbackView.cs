using DG.Tweening;
using TMPro;
using Unavinar.Pooling;
using UnityEngine;

namespace Junkineering.UI.Feedback
{
    public class MissFeedbackView : PoolableMonoBehaviour
    {
        private const string MESSAGE = "Missed!";

        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;

        private Sequence _animation;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Reset()
        {
            ResolveReferences();
        }

        public void Play(Vector2 anchoredPosition, float duration, float riseDistance)
        {
            ResolveReferences();

            if (rectTransform == null || canvasGroup == null || messageText == null)
            {
                Debug.LogError("MissFeedbackView is missing required UI references.", this);
                Release();
                return;
            }

            KillAnimation();
            rectTransform.anchoredPosition = anchoredPosition;
            canvasGroup.alpha = 1f;
            messageText.text = MESSAGE;

            if (duration <= 0f)
            {
                Release();
                return;
            }

            _animation = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .Join(rectTransform
                    .DOAnchorPosY(anchoredPosition.y + riseDistance, duration)
                    .SetEase(Ease.OutCubic))
                .Join(canvasGroup
                    .DOFade(0f, duration)
                    .SetEase(Ease.InQuad))
                .OnComplete(OnAnimationCompleted);
        }

        public override void ResetState()
        {
            KillAnimation();
            ResolveReferences();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (messageText != null)
            {
                messageText.text = MESSAGE;
            }

            base.ResetState();
        }

        private void OnAnimationCompleted()
        {
            _animation = null;
            Release();
        }

        private void KillAnimation()
        {
            if (_animation == null)
            {
                return;
            }

            _animation.Kill();
            _animation = null;
        }

        private void ResolveReferences()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (messageText == null)
            {
                messageText = GetComponent<TMP_Text>();
            }
        }
    }
}
