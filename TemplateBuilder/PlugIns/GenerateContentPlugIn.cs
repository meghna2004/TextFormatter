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
            var tracingService = localcontext.TracingService;
            PluginConfigService pluginConfigService = new PluginConfigService(service, localcontext);
            var templateRepo = new TemplateRepository(service,context,tracingService,pluginConfigService);
            var queryRepo = new QueryRepository(service,tracingService);
            var outputStrategy = new OutputStrategyFactory();
            var contentGenService = new ContentGeneratorService(service, context,tracingService,templateRepo,queryRepo,outputStrategy);
            //Retrieve config table
            //retrive all active and ready templates
            //execute
            var messageName = context.MessageName;
            tracingService.Trace("Message Name: "+messageName);
            var entityName = context.PrimaryEntityName;
            tracingService.Trace("Primary Entity: " + entityName);
            var stage = context.Stage.ToString();
            tracingService.Trace("Stage: " + stage);
            var mode = context.Mode.ToString();
            tracingService.Trace("Mode: " + mode);
            tracingService.Trace("Getting Configuration Settings");
            vig_templateconfigurationsetting configSetting =  templateRepo.GetTemplateConfig(messageName, entityName, mode, stage);

            if (configSetting!=null)
            {
                tracingService.Trace("Retrieve templates from configId: " + configSetting.Id.ToString());

                EntityCollection templatesToProcess = templateRepo.GetTemplates(configSetting.Id);
                tracingService.Trace("Templates Retrieved");

                foreach ( var template in templatesToProcess.Entities)
                {
                    tracingService.Trace("Inside For each to process Templates");
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
