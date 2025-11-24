using System.Collections.Generic;
using System.IO;
using Fluid;

namespace Sparkitect.Generator;

public class FluidHelper
{
    private static FluidParser FluidParser { get; set; } = null!;
    private static Dictionary<string, IFluidTemplate> TemplateCache { get; set; } = null!;


    public static TemplateOptions DefaultUnsafeAccess { get; private set; } = null!;

    static bool _isSetup = false;
    public static void Setup()
    {
        if (_isSetup) return;
        
        DefaultUnsafeAccess = new TemplateOptions
        {
            MemberAccessStrategy = new UnsafeMemberAccessStrategy()
        };
        FluidParser = new FluidParser();
        TemplateCache = new();
        
        _isSetup = true;
    }
    
    public static bool TryRenderTemplate(string templateName, object model, out string result, TemplateOptions? options = null)
    {
        Setup();
        
        result = string.Empty;
        
        if(!templateName.StartsWith("Sparkitect.Generator."))
            templateName = $"Sparkitect.Generator.{templateName}";

        if (!TemplateCache.TryGetValue(templateName, out var template))
        {
            using var templateFileStream = typeof(FluidHelper).Assembly.GetManifestResourceStream(templateName);
            if (templateFileStream == null) return false;
            
            var templateString = new StreamReader(templateFileStream).ReadToEnd();

            if (!FluidParser.TryParse(templateString, out template)) return false;
            
            TemplateCache[templateName] = template;
        }

        var context = new TemplateContext(model, options ?? DefaultUnsafeAccess);

        result = template.Render(context);
        
        return true;
    }
}