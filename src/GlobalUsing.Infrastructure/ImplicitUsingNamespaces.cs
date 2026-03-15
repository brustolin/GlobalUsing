using System.Collections.Frozen;

namespace GlobalUsing.Infrastructure;

internal static class ImplicitUsingNamespaces
{
    private static readonly FrozenSet<string> BaseSdkNamespaces = FrozenSet.ToFrozenSet(
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    ], StringComparer.Ordinal);

    private static readonly FrozenSet<string> WebSdkNamespaces = FrozenSet.ToFrozenSet(
    [
        "Microsoft.AspNetCore.Builder",
        "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Http",
        "Microsoft.AspNetCore.Routing",
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting",
        "Microsoft.Extensions.Logging",
    ], StringComparer.Ordinal);

    private static readonly FrozenSet<string> WorkerSdkNamespaces = FrozenSet.ToFrozenSet(
    [
        "Microsoft.Extensions.Configuration",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Hosting",
        "Microsoft.Extensions.Logging",
    ], StringComparer.Ordinal);

    public static FrozenSet<string> ForSdk(string? sdkName)
    {
        if (string.IsNullOrWhiteSpace(sdkName))
        {
            return BaseSdkNamespaces;
        }

        var names = BaseSdkNamespaces.ToArray().ToList();

        if (sdkName.Contains("Web", StringComparison.OrdinalIgnoreCase))
        {
            names.AddRange(WebSdkNamespaces);
        }

        if (sdkName.Contains("Worker", StringComparison.OrdinalIgnoreCase))
        {
            names.AddRange(WorkerSdkNamespaces);
        }

        return names.ToFrozenSet(StringComparer.Ordinal);
    }
}
