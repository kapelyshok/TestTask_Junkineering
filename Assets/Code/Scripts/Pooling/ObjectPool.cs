using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Unavinar.Pooling
{
    public class ObjectPool : MonoBehaviour, IObjectPool
    {
        private readonly Dictionary<Component, PoolTask> _activePoolTasks = new();
        [Inject] private DiContainer _diContainer;
        
        public async UniTask<T> GetObjectAsync<T>(T prefab) where T : Component, IPoolable
        {
            if (prefab == null)
            {
                Debug.LogError("Object pool can't process request because prefab is null!");
                return null;
            }
            
            if (!_activePoolTasks.TryGetValue(prefab, out var poolTask))
                AddTaskToPool(prefab, out poolTask);

            return await poolTask.GetFreeObject(prefab);
        }
        
        public async UniTask WarmupPoolAsync<T>(T prefab, int count) where T : Component, IPoolable
        {
            if (prefab == null)
            {
                Debug.LogError("Object pool can't process request because prefab is null!");
                return;
            }
            
            if (!_activePoolTasks.TryGetValue(prefab, out var poolTask))
                AddTaskToPool(prefab, out poolTask);

            List<UniTask> tasks = new();

            for (int i = 0; i < count; i++)
            {
                tasks.Add(poolTask.PrepareObject(prefab));
            }

            await UniTask.WhenAll(tasks);
        }

        private void AddTaskToPool<T>(T prefab, out PoolTask poolTask) where T : Component, IPoolable
        {
            var taskContainer = new GameObject($"{prefab.name}_pool")
            {
                transform = { parent = transform }
            };

            poolTask = _diContainer.Instantiate<PoolTask>(new object[] { taskContainer.transform });
            _activePoolTasks.Add(prefab, poolTask);
        }

        private void DisposeAllTasks()
        {
            foreach (var poolTask in _activePoolTasks.Values)
                poolTask.Dispose();
        }

        private void OnDisable()
        {
            DisposeAllTasks();
        }
    }
}
