using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Junkineering.Content.Providers
{
    public abstract class AContentProvider : MonoBehaviour
    {
        public virtual UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public abstract UniTask<ContentHandle<T>> LoadAsync<T>(
            string contentKey,
            CancellationToken cancellationToken) where T : UnityEngine.Object;

        public virtual void Shutdown()
        {
        }
    }
}
