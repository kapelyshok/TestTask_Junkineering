using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Junkineering.Gameplay
{
    public class TargetView : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer targetRenderer;
        [SerializeField, Min(0f)] private float negativeFlashDuration = 0.25f;
        [SerializeField] private Color negativeFlashColor = Color.red;

        private CancellationTokenSource _lifetimeCancellation;
        private CancellationTokenSource _negativeFlashCancellation;
        private MaterialPropertyBlock _propertyBlock;
        private Color _defaultColor = Color.white;

        public Texture CurrentTexture { get; private set; }

        private void Awake()
        {
            _lifetimeCancellation = new CancellationTokenSource();
            ResolveRenderer();
            _propertyBlock = new MaterialPropertyBlock();
            _defaultColor = ReadMaterialColor();
        }

        private void Reset()
        {
            ResolveRenderer();
        }

        private void OnDisable()
        {
            CancelNegativeFeedback();
        }

        private void OnDestroy()
        {
            _lifetimeCancellation?.Cancel();
            CancelNegativeFeedback();

            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }

        public void ApplyTexture(Texture texture)
        {
            if (targetRenderer == null)
            {
                Debug.LogError("TargetView requires a Renderer.", this);
                return;
            }

            CurrentTexture = texture;
            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetTexture(BaseMapId, texture);
            _propertyBlock.SetTexture(MainTextureId, texture);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        public bool Contains(Collider hitCollider)
        {
            return hitCollider != null && hitCollider.transform.IsChildOf(transform);
        }

        public void PlayNegativeFeedback()
        {
            if (!isActiveAndEnabled || targetRenderer == null)
            {
                return;
            }

            CancelNegativeFeedback();
            _negativeFlashCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCancellation.Token);

            CancellationTokenSource flashCancellation = _negativeFlashCancellation;
            CancellationToken cancellationToken = flashCancellation.Token;
            PlayNegativeFeedbackAsync(flashCancellation, cancellationToken).Forget();
        }

        private async UniTask PlayNegativeFeedbackAsync(
            CancellationTokenSource flashCancellation,
            CancellationToken cancellationToken)
        {
            try
            {
                SetColor(negativeFlashColor);
                await UniTask.WaitForSeconds(
                        negativeFlashDuration,
                        cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();
            }
            finally
            {
                if (ReferenceEquals(_negativeFlashCancellation, flashCancellation))
                {
                    _negativeFlashCancellation = null;
                    SetColor(_defaultColor);
                    flashCancellation.Dispose();
                }
            }
        }

        private void SetColor(Color color)
        {
            if (targetRenderer == null || _propertyBlock == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Color ReadMaterialColor()
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            {
                return Color.white;
            }

            Material material = targetRenderer.sharedMaterial;
            if (material.HasProperty(BaseColorId))
            {
                return material.GetColor(BaseColorId);
            }

            return material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
        }

        private void ResolveRenderer()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>(true);
            }
        }

        private void CancelNegativeFeedback()
        {
            if (_negativeFlashCancellation == null)
            {
                SetColor(_defaultColor);
                return;
            }

            CancellationTokenSource flashCancellation = _negativeFlashCancellation;
            _negativeFlashCancellation = null;
            flashCancellation.Cancel();
            flashCancellation.Dispose();
            SetColor(_defaultColor);
        }
    }
}
