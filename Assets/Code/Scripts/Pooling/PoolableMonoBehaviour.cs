using System;
using UnityEngine;

namespace Unavinar.Pooling
{
    public class PoolableMonoBehaviour : MonoBehaviour, IPoolable
    {
        public GameObject GameObject => gameObject;

        public event Action<IPoolable> Destroyed;

        public void Release()
        {
            if (this != null && gameObject != null)
                Destroyed?.Invoke(this);
        }

        /// <summary>
        /// If your object has unique logic to be done before it can be reused, override this method and implement it.
        /// </summary>
        public virtual void ResetState()
        {
            gameObject.SetActive(false);
        }
    }
}
