using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using NotImplementedException = System.NotImplementedException;

namespace Unavinar.Pooling
{
    public class PoolTask
    {
        private readonly Queue<IPoolable> _freeObjects;
        private readonly List<IPoolable> _objectsInUse;
        private readonly Transform _container;
        private DiContainer _diContainer;

        [Inject]
        public PoolTask(Transform container, DiContainer diContainer)
        {
            _container = container;
            _diContainer = diContainer;
            _freeObjects = new Queue<IPoolable>();
            _objectsInUse = new List<IPoolable>();
        }

        public void UpdateDIContainer(DiContainer container)
        {
            _diContainer = container;
        }

        public UniTask<T> GetFreeObject<T>(T prefab) where T : Component, IPoolable
        {
            T poolable;
            if (_freeObjects.Count > 0)
            {
                poolable = (T)_freeObjects.Dequeue();
            }
            else
            {
                poolable = _diContainer.InstantiatePrefabForComponent<T>(prefab, _container);
            }

            poolable.Destroyed += ReturnToPool;
            poolable.ResetState();
            poolable.GameObject.SetActive(true);

            _objectsInUse.Add(poolable);

            return UniTask.FromResult(poolable);
        }

        public UniTask PrepareObject<T>(T prefab) where T : Component, IPoolable
        {
            T poolable = _diContainer.InstantiatePrefabForComponent<T>(prefab, _container);
            poolable.ResetState();
            poolable.GameObject.transform.SetParent(_container);
            _freeObjects.Enqueue(poolable);
            return UniTask.CompletedTask;
        }

        private void ReturnToPool(IPoolable poolable)
        {
            if (_objectsInUse.Remove(poolable))
            {
                poolable.Destroyed -= ReturnToPool;

                poolable.ResetState();
                poolable.GameObject.transform.SetParent(_container);

                _freeObjects.Enqueue(poolable);
            }
        }

        private void ReturnAllObjectsToPool()
        {
            for (int i = _objectsInUse.Count - 1; i >= 0; i--)
                _objectsInUse[i].Release();

            _objectsInUse.Clear();
        }

        private void DestroyAllObjects()
        {
            while (_freeObjects.Count > 0)
            {
                var poolable = _freeObjects.Dequeue();

                if (poolable.GameObject.activeSelf)
                    poolable.GameObject.SetActive(false);

                Object.Destroy(poolable.GameObject);
            }

            _freeObjects.Clear();
        }

        public void Dispose()
        {
            ReturnAllObjectsToPool();
            DestroyAllObjects();
        }
    }
}
