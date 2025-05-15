using TextFormatter;
using TemplateBuilder.Repositories;
using TextFormatter.TemplateBuilder;
using Microsoft.Xrm.Sdk;
using TemplateBuilder.Services;
using TemplateBuilder.OutputStrategies;

namespace TemplateBuilder.PlugIns
{
    public class GenerateContentPlugIn: PluginBase
    {
        protected override void ExecuteCDSPlugin(LocalPluginContext localcontext)
        {
            var context = localcontext.PluginExecutionContext;
            var service = localcontext.OrganizationService;
            var templateRepo = new TemplateRepository(service,context);
            var queryRepo = new QueryRepository();
            var outputStrategy = new OutputStrategyFactory();
            var contentGenService = new ContentGeneratorService(service, context,templateRepo,queryRepo,outputStrategy);
            //Retrieve config table
            //retrive all active and ready templates
            //execute
            var messageName = context.MessageName;
            var entityName = context.PrimaryEntityName;
            var stage = context.Stage.ToString();
            var mode = context.Mode.ToString();
            vig_templateconfigurationsetting configSetting =  templateRepo.GetTemplateConfig(messageName, entityName, mode, stage);
            if(configSetting!=null)
            {
                EntityCollection templatesToProcess = templateRepo.GetTemplates(configSetting.Id);
                foreach( var template in templatesToProcess.Entities)
                {
                    vig_customtemplate customTemplate = template.ToEntity<vig_customtemplate>();
                    contentGenService.BuildContent(customTemplate.vig_textdescriptionbodyid.Id);
                }
            }
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
