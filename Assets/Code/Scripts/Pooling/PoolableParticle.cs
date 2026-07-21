using System.Linq;
using UnityEngine;

namespace Unavinar.Pooling
{
    [RequireComponent(typeof(ParticleSystem))]
    public class PoolableParticle : PoolableMonoBehaviour
    {
        private ParticleSystem[] _particleSystems;

        private float _maxDuration;

        private void Awake()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>();
            _maxDuration = GetMaxDuration();
        }

        private void OnEnable()
        {
            Invoke(nameof(Release), _maxDuration);
        }

        private float GetMaxDuration()
        {
            if (_particleSystems == null || _particleSystems.Length == 0)
                return 0f;

            return _particleSystems.Max(ps => ps.main.duration + ps.main.startDelay.constant);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Release));
        }
    }
}
