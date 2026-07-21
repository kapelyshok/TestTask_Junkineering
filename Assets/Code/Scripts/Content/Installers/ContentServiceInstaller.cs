using UnityEngine;
using Zenject;

namespace Junkineering.Content.Installers
{
    public class ContentServiceInstaller : MonoInstaller
    {
        [SerializeField] private ContentService contentService;

        public override void InstallBindings()
        {
            if (contentService == null)
            {
                throw new ZenjectException($"{nameof(ContentServiceInstaller)} requires a ContentService reference.");
            }

            Container.Bind<IContentService>().FromInstance(contentService).AsSingle();
        }
    }
}
