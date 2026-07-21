using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Unavinar.Pooling
{
    public interface IObjectPool
    {
        public UniTask<T> GetObjectAsync<T>(T prefab) where T : Component, IPoolable
        {
            return UniTask.FromResult<T>(null);
        }

        public UniTask WarmupPoolAsync<T>(T prefab, int count) where T : Component, IPoolable
        {
            return UniTask.CompletedTask;
        }
    }
}
