using CoreEngine.HostServices.Interop;

namespace CoreEngine.HostServices
{
    public readonly struct HostPlatform
    {
        public GraphicsService GraphicsService { get; }
        public InputsService InputsService { get; }
    }
}