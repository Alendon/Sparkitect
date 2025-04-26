using System.Collections.Generic;
using System.IO;
using Fluid;
using JetBrains.Annotations;

namespace Sparkitect.Generator;

public class FluidHelper
{
    private static readonly FluidParser _fluidParser = new FluidParser();
    private static Dictionary<string, IFluidTemplate> _templateCache = new();


    
    public static bool TryRenderTemplate(string templateName, object model, out string result, [CanBeNull] TemplateOptions options = null)
    {
        result = string.Empty;
        
        if(!templateName.StartsWith("Sparkitect.Generator."))
            templateName = $"Sparkitect.Generator.{templateName}";

        if (!_templateCache.TryGetValue(templateName, out var template))
        {
            using var templateFileStream = typeof(FluidHelper).Assembly.GetManifestResourceStream(templateName);
            if (templateFileStream == null) return false;
            
            var templateString = new StreamReader(templateFileStream).ReadToEnd();

            if (!_fluidParser.TryParse(templateString, out template)) return false;
            
            _templateCache[templateName] = template;
        }

        var context = new TemplateContext(model, options ?? TemplateOptions.Default);

        result = template.Render(context);
        
        return true;
    }
}