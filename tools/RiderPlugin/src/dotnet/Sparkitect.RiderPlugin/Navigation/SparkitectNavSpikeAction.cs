using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.TextControl.DataContext;
using JetBrains.Util;

namespace Sparkitect.RiderPlugin.Navigation;

/// <summary>
/// No-op backend action proving the frontend-id routing rail: a matching frontend RiderAnAction with
/// backendActionId "SparkitectNavSpike" reaches this handler over the existing RD protocol, with no
/// rdgen model. Carries no PSI work — only a trace line confirming the backend fired.
/// </summary>
[Action("SparkitectNavSpike", "Sparkitect Nav Spike")]
public sealed class SparkitectNavSpikeAction : IExecutableAction
{
    private static readonly ILogger Logger =
        JetBrains.Util.Logging.Logger.GetLogger(typeof(SparkitectNavSpikeAction));

    public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
    {
        return context.GetData(TextControlDataConstants.TEXT_CONTROL) != null;
    }

    public void Execute(IDataContext context, DelegateExecute nextExecute)
    {
        Logger.Trace("SparkitectNavSpike backend action fired");
    }
}
