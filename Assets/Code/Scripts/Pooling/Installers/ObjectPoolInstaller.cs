using UnityEngine;
using Zenject;

namespace Unavinar.Pooling
{
    public class ObjectPoolInstaller : MonoInstaller
    {
        [SerializeField]
        private ObjectPool objectPool;

        public override void InstallBindings()
        {
            Container.Bind<IObjectPool>().FromInstance(objectPool).AsSingle().NonLazy();
        }
    }
}