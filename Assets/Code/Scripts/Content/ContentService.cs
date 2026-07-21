using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Junkineering.Content.Configuration;
using Junkineering.Content.Providers;
using UnityEngine;

namespace Junkineering.Content
{
    public class ContentService : MonoBehaviour, IContentService
    {
        [Header("Dependencies")]
        [SerializeField] private AContentProvider contentProvider;
        [SerializeField] private AGameContentConfig contentConfig;

        private CancellationTokenSource _lifetimeCancellation;
        private UniTask _initializationTask;
        private ContentHandle<GameObject> _targetPrefabHandle;
        private ContentHandle<Texture2D> _fallbackTextureHandle;
        private ContentHandle<Texture2D> _currentTextureHandle;
        private int _initializationVersion;
        private bool _hasInitializationTask;

        private void Awake()
        {
            _lifetimeCancellation = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _lifetimeCancellation?.Cancel();

            _currentTextureHandle?.Dispose();
            _fallbackTextureHandle?.Dispose();
            _targetPrefabHandle?.Dispose();
            contentProvider?.Shutdown();

            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }

        public async UniTask<GameObject> LoadTargetPrefabAsync(CancellationToken cancellationToken)
        {
            await EnsureInitializedAsync().AttachExternalCancellation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return _targetPrefabHandle.Asset;
        }

        public async UniTask<Texture2D> LoadRoundTextureAsync(
            int roundId,
            CancellationToken cancellationToken)
        {
            await EnsureInitializedAsync().AttachExternalCancellation(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            string textureKey = contentConfig.GetRoundTextureKey(roundId);
            if (string.IsNullOrWhiteSpace(textureKey))
            {
                return UseFallbackTexture();
            }

            ContentHandle<Texture2D> loadedTexture = null;

            try
            {
                loadedTexture = await contentProvider.LoadAsync<Texture2D>(
                    textureKey,
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                ContentHandle<Texture2D> previousTexture = _currentTextureHandle;
                _currentTextureHandle = loadedTexture;
                loadedTexture = null;
                previousTexture?.Dispose();

                return _currentTextureHandle.Asset;
            }
            catch (OperationCanceledException)
            {
                loadedTexture?.Dispose();
                throw;
            }
            catch (Exception exception)
            {
                loadedTexture?.Dispose();
                Debug.LogWarning(
                    $"Failed to load round texture '{textureKey}'. Applying fallback.\n{exception}",
                    this);
                return UseFallbackTexture();
            }
        }

        private async UniTask EnsureInitializedAsync()
        {
            if (contentProvider == null)
            {
                throw new InvalidOperationException("ContentService requires an AContentProvider.");
            }

            if (!_hasInitializationTask)
            {
                _initializationTask = InitializeContentAsync(_lifetimeCancellation.Token).Preserve();
                _hasInitializationTask = true;
                _initializationVersion++;
            }

            UniTask initializationTask = _initializationTask;
            int initializationVersion = _initializationVersion;

            try
            {
                await initializationTask;
            }
            catch
            {
                if (_initializationVersion == initializationVersion)
                {
                    _initializationTask = default;
                    _hasInitializationTask = false;
                }

                throw;
            }
        }

        private async UniTask InitializeContentAsync(CancellationToken cancellationToken)
        {
            ValidateConfiguration();
            await contentProvider.InitializeAsync(cancellationToken);

            _targetPrefabHandle = await contentProvider.LoadAsync<GameObject>(
                contentConfig.TargetPrefabKey,
                cancellationToken);

            try
            {
                _fallbackTextureHandle = await contentProvider.LoadAsync<Texture2D>(
                    contentConfig.FallbackTextureKey,
                    cancellationToken);
            }
            catch
            {
                _targetPrefabHandle.Dispose();
                _targetPrefabHandle = null;
                throw;
            }
        }

        private Texture2D UseFallbackTexture()
        {
            _currentTextureHandle?.Dispose();
            _currentTextureHandle = null;
            return _fallbackTextureHandle.Asset;
        }

        private void ValidateConfiguration()
        {
            if (contentConfig == null)
            {
                throw new InvalidOperationException("Content config is not assigned.");
            }

            if (string.IsNullOrWhiteSpace(contentConfig.TargetPrefabKey))
            {
                throw new InvalidOperationException("Target prefab reference is not configured.");
            }

            if (string.IsNullOrWhiteSpace(contentConfig.FallbackTextureKey))
            {
                throw new InvalidOperationException("Fallback texture reference is not configured.");
            }
        }
    }
}
