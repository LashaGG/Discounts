using Discounts.Application.Mapping;

namespace Discounts.Tests.Helpers;

internal static class MapsterSetup
{
    private static bool _configured;
    private static readonly object Lock = new();

    internal static void EnsureConfigured()
    {
        if (_configured) return;

        lock (Lock)
        {
            if (_configured) return;
            MappingConfig.Configure();
            _configured = true;
        }
    }
}
