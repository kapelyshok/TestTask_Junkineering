using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Junkineering.Content
{
    public interface IContentService
    {
        UniTask<GameObject> LoadTargetPrefabAsync(CancellationToken cancellationToken);

        UniTask<Texture2D> LoadRoundTextureAsync(
            int roundId,
            CancellationToken cancellationToken);
    }
}
