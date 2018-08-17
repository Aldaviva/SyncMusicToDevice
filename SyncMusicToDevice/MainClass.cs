using System.Threading.Tasks;
using Autofac;
using SyncMusicToDevice.Injection;
using SyncMusicToDevice.Service;

namespace SyncMusicToDevice
{
    public static class MainClass
    {
        public static async Task Main()
        {
            const string desktopMusicDirectory = @"D:\Music";
            const string deviceMusicDirectory = @"Music";

            IContainer dependencyInjectionContainer = ContainerFactory.CreateContainer();
            using (ILifetimeScope scope = dependencyInjectionContainer.BeginLifetimeScope())
            {
                var synchronizer = scope.Resolve<Synchronizer>();
                synchronizer.DesktopMusicDirectory = desktopMusicDirectory;
                synchronizer.DeviceMusicDirectory = deviceMusicDirectory;
                await synchronizer.Synchronize();
            }
        }
    }
}