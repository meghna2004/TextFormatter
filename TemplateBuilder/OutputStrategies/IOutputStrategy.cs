using Microsoft.Xrm.Sdk;

namespace TemplateBuilder.OutputStrategies
{
    public interface IOutputStrategy
    {
        void OutputContent(string content, IPluginExecutionContext context,IOrganizationService service);
    }
}
