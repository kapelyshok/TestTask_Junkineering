using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using AddressablesApi = UnityEngine.AddressableAssets.Addressables;

namespace Junkineering.Content.Providers.Addressables
{
    public class AddressablesContentProvider : AContentProvider
    {
        private AsyncOperationHandle<IResourceLocator> _initializationHandle;
        private UniTask _initializationTask;
        private int _initializationVersion;
        private bool _hasInitializationTask;
        private bool _isInitialized;

        public override async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (_isInitialized)
            {
                return;
            }

            if (!_hasInitializationTask)
            {
                _initializationTask = InitializeInternalAsync(cancellationToken).Preserve();
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

        public override async UniTask<ContentHandle<T>> LoadAsync<T>(
            string contentKey,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(contentKey))
            {
                throw new ArgumentException("Content key cannot be empty.", nameof(contentKey));
            }

            await InitializeAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            AsyncOperationHandle<T> handle = AddressablesApi.LoadAssetAsync<T>(contentKey);

            try
            {
                T asset = await AwaitOperationAsync(handle, contentKey, cancellationToken);
                return new ContentHandle<T>(asset, () => ReleaseIfValid(handle));
            }
            catch
            {
                ReleaseIfValid(handle);
                throw;
            }
        }

        public override void Shutdown()
        {
            if (_initializationHandle.IsValid())
            {
                AddressablesApi.Release(_initializationHandle);
            }

            _initializationHandle = default;
            _initializationTask = default;
            _hasInitializationTask = false;
            _initializationVersion++;
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private async UniTask InitializeInternalAsync(CancellationToken cancellationToken)
        {
            AsyncOperationHandle<IResourceLocator> handle = AddressablesApi.InitializeAsync(false);

            try
            {
                await AwaitOperationAsync(handle, "Addressables initialization", cancellationToken);
                _initializationHandle = handle;
                _isInitialized = true;
            }
            catch
            {
                ReleaseIfValid(handle);
                throw;
            }
        }

        private static async UniTask<T> AwaitOperationAsync<T>(
            AsyncOperationHandle<T> handle,
            string operationName,
            CancellationToken cancellationToken)
        {
            try
            {
                T result = await handle.ToUniTask(cancellationToken: cancellationToken);
                if (result != null)
                {
                    return result;
                }

                throw new InvalidOperationException(
                    $"Content operation returned no result: {operationName}.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Content operation failed: {operationName}.",
                    exception);
            }
        }

        private static void ReleaseIfValid<T>(AsyncOperationHandle<T> handle)
        {
            if (handle.IsValid())
            {
                AddressablesApi.Release(handle);
            }
        }
    }
}
