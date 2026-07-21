using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Junkineering.Content;
using Junkineering.Input;
using Junkineering.UI;
using Junkineering.UI.Feedback;
using UnityEngine;
using Zenject;

namespace Junkineering.Gameplay
{
    public class ContentLoaderTaskController : MonoBehaviour
    {
        public enum ContentLoaderTaskState
        {
            Initializing,
            Loading,
            Ready,
            Error
        }

        [Header("Scene References")]
        [SerializeField] private HudView hudView;
        [SerializeField] private PointerSelectionInput selectionInput;
        [SerializeField] private Transform actorSpawnPoint;
        [SerializeField] private MissFeedbackEmitter missFeedback;

        private CancellationTokenSource _lifetimeCancellation;
        private CancellationTokenSource _roundCancellation;
        private IContentService _contentService;
        private GameObject _targetInstance;
        private TargetView _targetView;
        private int _roundId;
        private bool _hasStarted;

        public event Action IncorrectSelection;

        public ContentLoaderTaskState State { get; private set; } = ContentLoaderTaskState.Initializing;
        public int Score { get; private set; }

        [Inject]
        private void Construct(IContentService contentService)
        {
            _contentService = contentService;
        }

        private void Awake()
        {
            _lifetimeCancellation = new CancellationTokenSource();
            hudView?.SetScore(Score);
            SetInputEnabled(false);
        }

        private void OnEnable()
        {
            if (selectionInput != null)
            {
                selectionInput.WorldHit += OnWorldHit;
                selectionInput.Missed += OnSelectionMissed;
            }

            if (_hasStarted && State == ContentLoaderTaskState.Loading)
            {
                BeginRoundLoading(_roundId);
            }
        }

        private void Start()
        {
            _hasStarted = true;
            RequestNextRound();
        }

        private void OnDisable()
        {
            if (selectionInput != null)
            {
                selectionInput.WorldHit -= OnWorldHit;
                selectionInput.Missed -= OnSelectionMissed;
            }

            CancelCurrentRound();
        }

        private void OnDestroy()
        {
            _lifetimeCancellation?.Cancel();
            CancelCurrentRound();

            if (_targetInstance != null)
            {
                Destroy(_targetInstance);
            }

            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }

        public int RequestNextRound()
        {
            _roundId++;
            State = ContentLoaderTaskState.Loading;
            SetInputEnabled(false);
            hudView?.ShowLoading();

            if (isActiveAndEnabled)
            {
                BeginRoundLoading(_roundId);
            }

            return _roundId;
        }

        private bool CompleteRound(int roundId, Texture texture)
        {
            if (!IsCurrentRound(roundId) || texture == null || _targetView == null)
            {
                return false;
            }

            _targetView.ApplyTexture(texture);
            State = ContentLoaderTaskState.Ready;
            hudView?.ShowReady();
            SetInputEnabled(true);
            return true;
        }

        private bool FailRound(int roundId, string message)
        {
            if (!IsCurrentRound(roundId))
            {
                return false;
            }

            State = ContentLoaderTaskState.Error;
            SetInputEnabled(false);
            hudView?.ShowError(message);
            return true;
        }

        private void BeginRoundLoading(int roundId)
        {
            CancelCurrentRound();
            _roundCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCancellation.Token);

            CancellationTokenSource roundCancellation = _roundCancellation;
            CancellationToken cancellationToken = roundCancellation.Token;
            PrepareRoundAsync(roundId, roundCancellation, cancellationToken).Forget();
        }

        private async UniTask PrepareRoundAsync(
            int roundId,
            CancellationTokenSource roundCancellation,
            CancellationToken cancellationToken)
        {
            try
            {
                if (_contentService == null)
                {
                    throw new InvalidOperationException(
                        "ContentLoaderTaskController requires an IContentService.");
                }

                await EnsureTargetAsync(cancellationToken);
                Texture2D texture = await _contentService.LoadRoundTextureAsync(
                    roundId,
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                CompleteRound(roundId, texture);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                FailRound(roundId, "Content initialization failed.");
            }
            finally
            {
                if (ReferenceEquals(_roundCancellation, roundCancellation))
                {
                    _roundCancellation = null;
                    roundCancellation.Dispose();
                }
            }
        }

        private async UniTask EnsureTargetAsync(CancellationToken cancellationToken)
        {
            if (_targetView != null)
            {
                return;
            }

            GameObject targetPrefab = await _contentService.LoadTargetPrefabAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            Vector3 position = actorSpawnPoint != null ? actorSpawnPoint.position : Vector3.zero;
            Quaternion rotation = actorSpawnPoint != null
                ? actorSpawnPoint.rotation
                : Quaternion.identity;

            _targetInstance = Instantiate(targetPrefab, position, rotation);
            if (!_targetInstance.TryGetComponent(out TargetView targetView))
            {
                Destroy(_targetInstance);
                _targetInstance = null;
                throw new InvalidOperationException(
                    "The target prefab must contain a TargetView component.");
            }

            _targetView = targetView;
        }

        private void OnWorldHit(RaycastHit hit, Vector2 screenPosition)
        {
            if (State != ContentLoaderTaskState.Ready)
            {
                return;
            }

            if (_targetView != null && _targetView.Contains(hit.collider))
            {
                Score++;
                hudView?.SetScore(Score);
                RequestNextRound();
                return;
            }

            OnSelectionMissed(screenPosition);
        }

        private void OnSelectionMissed(Vector2 screenPosition)
        {
            if (State != ContentLoaderTaskState.Ready)
            {
                return;
            }

            _targetView?.PlayNegativeFeedback();
            IncorrectSelection?.Invoke();
            missFeedback?.Show(screenPosition);
        }

        private bool IsCurrentRound(int roundId)
        {
            return roundId == _roundId && State == ContentLoaderTaskState.Loading;
        }

        private void SetInputEnabled(bool isEnabled)
        {
            selectionInput?.SetInputEnabled(isEnabled);
        }

        private void CancelCurrentRound()
        {
            if (_roundCancellation == null)
            {
                return;
            }

            CancellationTokenSource roundCancellation = _roundCancellation;
            _roundCancellation = null;
            roundCancellation.Cancel();
            roundCancellation.Dispose();
        }
    }
}
