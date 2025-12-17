using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Fluid;

namespace Sparkitect.Generator;

public class FluidHelper
{
    private static FluidParser? _fluidParser;
    private static ConcurrentDictionary<string, IFluidTemplate>? _templateCache;
    private static TemplateOptions? _defaultUnsafeAccess;
    private static int _isSetup;

    private static void EnsureSetup()
    {
        if (Interlocked.CompareExchange(ref _isSetup, 1, 0) == 0)
        {
            _defaultUnsafeAccess = new TemplateOptions
            {
                MemberAccessStrategy = new UnsafeMemberAccessStrategy()
            };
            _fluidParser = new FluidParser();
            _templateCache = new ConcurrentDictionary<string, IFluidTemplate>();
        }

        SpinWait.SpinUntil(() => _templateCache is not null);
    }
    
    public static bool TryRenderTemplate(string templateName, object model, out string result, TemplateOptions? options = null)
    {
        EnsureSetup();

        result = string.Empty;

        if(!templateName.StartsWith("Sparkitect.Generator."))
            templateName = $"Sparkitect.Generator.{templateName}";

        var template = _templateCache!.GetOrAdd(templateName, static name =>
        {
            using var templateFileStream = typeof(FluidHelper).Assembly.GetManifestResourceStream(name);
            if (templateFileStream == null) return null!;

            var templateString = new StreamReader(templateFileStream).ReadToEnd();

            if (!_fluidParser!.TryParse(templateString, out var parsed)) return null!;

            return parsed;
        });

        if (template is null) return false;

        var context = new TemplateContext(model, options ?? _defaultUnsafeAccess);

        result = template.Render(context);

        return true;
    }
}