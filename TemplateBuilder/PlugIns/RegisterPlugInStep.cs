using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using TextFormatter.TemplateBuilder;
using TextFormatter;
using TemplateBuilder.Services;

namespace TemplateBuilder.PlugIns
{
    [CrmPluginRegistration("Update",
  "vig_templateconfigurationsetting", StageEnum.PreOperation, ExecutionModeEnum.Synchronous, "statuscode"
  , "TextFormatter.TemplateBuilder.RegisterPlugInStep: Update of Template Config Setting", 1000,
  IsolationModeEnum.Sandbox
  , Image1Type = ImageTypeEnum.PreImage
  , Image1Name = "PreImage"
  , Image1Attributes = "statuscode, vig_triggertype, vig_triggerentity, vig_filterattributes, vig_executionmode, vig_processstage, vig_sdkmessageprocessingstepid"
  , Description = "TextFormatter.TemplateBuilder.RegisterPlugInStep: Update of Template Config Setting"
  , Id = "309f1217-34f2-48c5-a7f7-3ecea99ba7ef"
  )]
    public class RegisterPlugInStep:PluginBase
    {
        protected override void ExecuteCDSPlugin(LocalPluginContext localcontext)
        {
            var context = localcontext.PluginExecutionContext;
            var service = localcontext.OrganizationService;
            PluginConfigService pluginConfigService = new PluginConfigService(service, localcontext);

            try
            {
                // Ensure we are in Update Message
                localcontext.Trace("Start Of Plugin");
                if (context.MessageName.ToLower() != "update")
                    return;
                localcontext.Trace("Checking primaryentity name");
                // Ensure the entity is 'vig_templateconfigurationsetting'
                if (context.PrimaryEntityName.ToLower() != "vig_templateconfigurationsetting")
                    return;
                //if pluginstep already registered check if all config values are the same, if not then update otherwise return.

                // Retrieve the updated record details
                localcontext.Trace("Getting configRecord");
                vig_templateconfigurationsetting configRecord = localcontext.MergedPreTarget.ToEntity<vig_templateconfigurationsetting>();
                vig_templateconfigurationsetting configTargetRecord = ((Entity)localcontext.PluginExecutionContext.InputParameters["Target"]).ToEntity<vig_templateconfigurationsetting>();
                if (configRecord.statuscode.Value != vig_templateconfigurationsetting_statuscode.On)
                    return;

                if (configRecord == null)
                    throw new InvalidPluginExecutionException("Configuration record not found in Post Image.");

                // Retrieve plugin configuration details
                localcontext.Trace("Get trigger type");
                string messageName = configRecord.vig_triggertype.Value.ToString();
                localcontext.Trace("Getting trigger entity");
                string primaryEntity = configRecord.vig_triggerentity;
                localcontext.Trace("Getting execution mode");
                string executionMode = configRecord.vig_executionmode.Value.ToString(); // Synchronous or Asynchronous
                localcontext.Trace("Getting filtering attributes");
                string filterAttributes = configRecord.vig_filterattributes; // Comma-separated
                localcontext.Trace("Getting process stage");
                string stage = configRecord.vig_processstage.Value.ToString();
               
                localcontext.Trace("All config values retrieved");
                Guid existingStepId = pluginConfigService.FindExistingPluginStep(messageName, primaryEntity);
                if (existingStepId!=Guid.Empty)
                {
                    localcontext.Trace("Plugin step already exists. No action taken.");
                    configTargetRecord.vig_sdkmessageprocessingstepid = new EntityReference("sdkmessageprocessingstep", existingStepId);
                    return;
                }

                // Fully Qualified Name (FQN) of the plugin class containing the business logic
                Guid sdkMessageProcessingStepID = pluginConfigService.CreatePluginStep(primaryEntity, messageName,stage, executionMode,filterAttributes);

                configTargetRecord.vig_sdkmessageprocessingstepid = new EntityReference("sdkmessageprocessingstep", sdkMessageProcessingStepID);
                configTargetRecord.vig_name = $"{messageName} of {primaryEntity} Step"; 
                localcontext.Trace("Plugin step successfully registered.");
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException($"Error in DynamicPluginRegistration: {ex.Message}");
            }
        }      
    }
}

