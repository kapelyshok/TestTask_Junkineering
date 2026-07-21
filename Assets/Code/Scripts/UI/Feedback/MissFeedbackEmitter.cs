using System;
using Cysharp.Threading.Tasks;
using Unavinar.Pooling;
using UnityEngine;
using Zenject;

namespace Junkineering.UI.Feedback
{
    public class MissFeedbackEmitter : MonoBehaviour
    {
        [SerializeField] private MissFeedbackView feedbackPrefab;
        [SerializeField] private Canvas canvas;
        [SerializeField, Min(0f)] private float animationDuration = 1f;
        [SerializeField, Min(0f)] private float riseDistance = 100f;
        [SerializeField, Min(0)] private int warmupCount = 3;

        private RectTransform _rectTransform;
        private IObjectPool _objectPool;

        [Inject]
        private void Construct(IObjectPool objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            WarmupAsync().Forget();
        }

        public void Show(Vector2 screenPosition)
        {
            SpawnAsync(screenPosition).Forget();
        }

        private async UniTask SpawnAsync(Vector2 screenPosition)
        {
            if (_objectPool == null)
            {
                throw new InvalidOperationException("MissFeedbackEmitter requires an IObjectPool.");
            }

            if (feedbackPrefab == null)
            {
                Debug.LogError("MissFeedbackEmitter requires a feedback prefab.", this);
                return;
            }

            MissFeedbackView feedback = await _objectPool.GetObjectAsync(feedbackPrefab);
            if (feedback == null)
            {
                return;
            }

            if (this == null || !isActiveAndEnabled)
            {
                feedback.Release();
                return;
            }

            Camera eventCamera = canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPosition))
            {
                feedback.Release();
                return;
            }

            feedback.transform.SetParent(_rectTransform, false);
            feedback.Play(localPosition, animationDuration, riseDistance);
        }

        private async UniTask WarmupAsync()
        {
            if (warmupCount <= 0 || feedbackPrefab == null)
            {
                return;
            }

            if (_objectPool == null)
            {
                throw new InvalidOperationException("MissFeedbackEmitter requires an IObjectPool.");
            }

            await _objectPool.WarmupPoolAsync(feedbackPrefab, warmupCount);
        }
    }
}
