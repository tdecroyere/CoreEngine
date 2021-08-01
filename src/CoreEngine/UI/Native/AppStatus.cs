using CoreEngine.HostServices;

namespace CoreEngine.UI.Native
{
    public readonly record struct AppStatus
    {
        public AppStatus(NativeAppStatus nativeAppStatus)
        {
            this.IsRunning = nativeAppStatus.IsRunning;
            this.IsActive = nativeAppStatus.IsActive;
        }

        public bool IsRunning { get; init; }
        public bool IsActive { get; init; }
    }
}