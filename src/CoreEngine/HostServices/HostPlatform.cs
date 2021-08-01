using CoreEngine.HostServices.Interop;

namespace CoreEngine.HostServices
{
    public readonly ref struct HostPlatform
    {
        public NativeUIService NativeUIService { get; }
        public GraphicsService GraphicsService { get; }
        public InputsService InputsService { get; }
    }
}