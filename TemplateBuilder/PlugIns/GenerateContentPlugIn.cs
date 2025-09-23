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
        //Plugin to create formatted text for emails or document templates.
        protected override void ExecuteCDSPlugin(LocalPluginContext localcontext)
        {
            var context = localcontext.PluginExecutionContext;
            var service = localcontext.OrganizationService;
            var tracingService = localcontext.TracingService;
            PluginConfigService pluginConfigService = new PluginConfigService(service, localcontext);
            TemplateRepository templateRepo = new TemplateRepository(service,context,tracingService,pluginConfigService);
            OutputStrategyFactory outputStrategy = new OutputStrategyFactory(context,service);
            ContentGeneratorService contentGenService = null;

            var messageName = context.MessageName;
            tracingService.Trace("Message Name: "+messageName);
            var entityName = context.PrimaryEntityName;
            tracingService.Trace("Primary Entity: " + entityName);
            var stage = context.Stage.ToString();
            tracingService.Trace("Stage: " + stage);
            var mode = context.Mode.ToString();
            tracingService.Trace("Mode: " + mode);
            vig_templateconfigurationsetting configSetting =  templateRepo.GetTemplateConfig(messageName, entityName, mode, stage);

            if (configSetting!=null)
            {
                tracingService.Trace("Retrieve templates from configId: " + configSetting.Id.ToString());
                EntityCollection templatesToProcess = templateRepo.GetTemplates(configSetting.Id);
                foreach ( var template in templatesToProcess.Entities)
                {
                    vig_customtemplate customTemplate = template.ToEntity<vig_customtemplate>();
                    contentGenService = new ContentGeneratorService(service, context, tracingService, templateRepo, customTemplate.vig_textdescriptionbodyid.Id);
                    vig_textdescriptionbody tbd = service.Retrieve("vig_textdescriptionbody", customTemplate.vig_textdescriptionbodyid.Id, columnSet:new ColumnSet(true)).ToEntity<vig_textdescriptionbody>();
                    string content = contentGenService.BuildContent();
                    var tempType = (TemplateType)customTemplate.vig_outputtype.Value;
                    tracingService.Trace("Enum value is: " + tempType.ToString());
                    string subject = customTemplate.vig_subject;
                    tracingService.Trace("Subject: "+subject);
                    var strategy = outputStrategy.GetOutputStrategy(tempType);
                    strategy.OutputContent(content, subject);
                    tbd.vig_preview = content;
                    service.Update(tbd);
                }
            }
        }
    }
}
