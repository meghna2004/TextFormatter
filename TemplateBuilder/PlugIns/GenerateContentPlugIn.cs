using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using TextFormatter.TemplateBuilder;
using TextFormatter;

namespace TemplateBuilder.PlugIns
{
    public class GenerateContentPlugIn: PluginBase
    {
        protected override void ExecuteCDSPlugin(LocalPluginContext localcontext)
        {
            var context = localcontext.PluginExecutionContext;
            var service = localcontext.OrganizationService;
            //Retrieve config table
            //retrive all active and ready templates
            //execute
            var messageName = context.MessageName;
            var entityName = context.PrimaryEntityName;
            var stage = context.Stage;
            var mode = context.Mode;

           /* string fetchTemplateConfig = string.Format(@"");
            EntityCollection configsRetrieved = service.RetrieveMultiple(new FetchExpression(fetchTemplateConfig));
            foreach (Entity config in configsRetrieved.Entities)
            {
                string fetchTemplates = string.Format(@"");
                EntityCollection templatesRetrieved = service.RetrieveMultiple(new FetchExpression(fetchTemplates));
                foreach (Entity template in templatesRetrieved.Entities)
                {
                    CustomTemplates customTemplates = new CustomTemplates();

                }
            }*/
        }
    }
}
