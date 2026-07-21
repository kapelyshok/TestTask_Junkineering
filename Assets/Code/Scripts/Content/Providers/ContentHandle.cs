using System;
using UnityEngine;

namespace Junkineering.Content.Providers
{
    public class ContentHandle<T> : IDisposable where T : UnityEngine.Object
    {
        private Action _release;

        internal ContentHandle(T asset, Action release)
        {
            Asset = asset != null ? asset : throw new ArgumentNullException(nameof(asset));
            _release = release ?? throw new ArgumentNullException(nameof(release));
        }

        public T Asset { get; }
        public bool IsReleased => _release == null;

        public void Dispose()
        {
            Action release = _release;
            if (release == null)
            {
                return;
            }

            _release = null;
            release.Invoke();
        }
    }
}
