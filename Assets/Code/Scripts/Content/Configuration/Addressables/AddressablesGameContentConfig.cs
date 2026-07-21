using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Junkineering.Content.Configuration.Addressables
{
    [CreateAssetMenu(
        fileName = "GameContentConfig",
        menuName = "Junkineering/Content/Addressables Game Content Config")]
    public class AddressablesGameContentConfig : AGameContentConfig
    {
        [SerializeField] private AssetReferenceGameObject targetPrefab;
        [SerializeField] private AssetReferenceTexture2D fallbackTexture;
        [SerializeField] private List<AssetReferenceTexture2D> roundTextures =
            new List<AssetReferenceTexture2D>();

        public override string TargetPrefabKey => GetKey(targetPrefab);
        public override string FallbackTextureKey => GetKey(fallbackTexture);

        public override string GetRoundTextureKey(int roundId)
        {
            if (roundTextures == null || roundTextures.Count == 0)
            {
                return null;
            }

            int index = roundId > 0 ? (roundId - 1) % roundTextures.Count : 0;
            return GetKey(roundTextures[index]);
        }

        private static string GetKey(AssetReference assetReference)
        {
            return assetReference != null && assetReference.RuntimeKeyIsValid()
                ? assetReference.RuntimeKey.ToString()
                : null;
        }
    }
}
