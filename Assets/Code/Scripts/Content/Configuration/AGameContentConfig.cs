using UnityEngine;

namespace Junkineering.Content.Configuration
{
    public abstract class AGameContentConfig : ScriptableObject
    {
        public abstract string TargetPrefabKey { get; }
        public abstract string FallbackTextureKey { get; }

        public abstract string GetRoundTextureKey(int roundId);
    }
}
