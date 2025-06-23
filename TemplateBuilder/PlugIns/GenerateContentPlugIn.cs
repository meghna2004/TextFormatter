using TextFormatter;
using TemplateBuilder.Repositories;
using TextFormatter.TemplateBuilder;
using Microsoft.Xrm.Sdk;
using TemplateBuilder.Services;
using TemplateBuilder.OutputStrategies;
using TemplateBuilder.Enum;
using System;
using System.Drawing.Imaging;
using Microsoft.Xrm.Sdk.Query;

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
            TemplateRepository templateRepo = new TemplateRepository(service,context,tracingService,pluginConfigService);
            OutputStrategyFactory outputStrategy = new OutputStrategyFactory(context,service);
            ContentGeneratorService contentGenService = null;
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
                    contentGenService = new ContentGeneratorService(service, context, tracingService, templateRepo, customTemplate.vig_textdescriptionbodyid.Id);
                    vig_textdescriptionbody tbd = service.Retrieve("vig_textdescriptionbody", customTemplate.vig_textdescriptionbodyid.Id, columnSet:new ColumnSet(true)).ToEntity<vig_textdescriptionbody>();
                    string content = contentGenService.BuildContent();
                    //Get enum type
                    tracingService.Trace("Enum value is: ");
                    var tempType = (TemplateType)customTemplate.vig_outputtype.Value;
                    tracingService.Trace("Enum value is: " + tempType.ToString());
                    string subject = customTemplate.vig_subject;
                    tracingService.Trace("Subject: "+subject);
                    var strategy = outputStrategy.GetOutputStrategy(tempType);
                    strategy.OutputContent(content, subject);
                    tbd.vig_preview = content;
                    service.Update(tbd);
                    /*if (customTemplate.vig_outputtype != null *//*&&
                        System.Enum.IsDefined(typeof(TemplateType), customTemplate.vig_outputtype.Value)*//*)
                    {
                        var tempType = (TemplateType)customTemplate.vig_outputtype.Value;
                        Console.WriteLine("Enum value is: " + tempType);

                        var strategy = outputStrategyFactory.GetOutputStrategy(tempType);
                        strategy.OutputContent(content, context, service);
                    }
                    else
                    {
                        Console.WriteLine("Invalid choice value.");
                    }*/



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
