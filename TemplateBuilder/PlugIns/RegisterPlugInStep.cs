using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using TextFormatter.TemplateBuilder;
using TextFormatter;

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
                string assemblyName = "TemplateBuilder";
                localcontext.Trace("All config values retrieved");

                // Fully Qualified Name (FQN) of the plugin class containing the business logic
                string pluginTypeName = "TemplateBuilder.PlugIns.GenerateContentPlugIn";
                QueryExpression assemblyQuery = new QueryExpression("pluginassembly")
                {
                    ColumnSet = new ColumnSet("pluginassemblyid"),
                    Criteria =
                    {
                      Conditions = { new ConditionExpression("name", ConditionOperator.Equal, assemblyName) }
                    }
                };
                localcontext.Trace("Plugin assembly retrieved");
                EntityCollection assemblies = service.RetrieveMultiple(assemblyQuery);
                if (assemblies.Entities.Count == 0)
                    throw new InvalidPluginExecutionException($"Plugin Assembly '{assemblyName}' not found in CRM. Please deploy it first.");

                Guid assemblyId = assemblies.Entities[0].Id;

                localcontext.Trace("Create PlugInType");
                // Create a PluginType (if not already created)
                QueryExpression pluginTypeQuery = new QueryExpression("plugintype")
                {
                    ColumnSet = new ColumnSet("plugintypeid"),
                    Criteria =
                    {
                        Conditions = { new ConditionExpression("typename", ConditionOperator.Equal, pluginTypeName) }
                    }
                };

                EntityCollection pluginTypes = service.RetrieveMultiple(pluginTypeQuery);
                Guid pluginTypeId;

                if (pluginTypes.Entities.Count > 0)
                {
                    pluginTypeId = pluginTypes.Entities[0].Id;
                }
                else
                {
                    // Register the plugin type
                    Entity pluginType = new Entity("plugintype")
                    {
                        ["pluginassemblyid"] = new EntityReference("pluginassembly", assemblyId),
                        ["typename"] = pluginTypeName,
                        ["friendlyname"] = "GenerateContentPlugIn",
                        ["name"] = "GenerateContentPlugIn"
                    };
                    pluginTypeId = service.Create(pluginType);
                }
                // Register the plugin step
                Guid sdkMessageFilterId = GetSdkMessageFilter(service,primaryEntity,GetSdkMessageId(service,messageName));/* new Entity("sdkmessagefilter")
                {
                    ["sdkmessageid"] = GetSdkMessageId(service, messageName),
                    ["primaryobjecttypecode"] = primaryEntity
                };*/
                Entity sdkMessageProcessingStep = new Entity("sdkmessageprocessingstep")
                {
                    ["name"] = $"{messageName} of {primaryEntity} Step",
                    ["sdkmessageid"] = new EntityReference("sdkmessage", GetSdkMessageId(service, messageName)),
                    ["plugintypeid"] = new EntityReference("plugintype", pluginTypeId),
                    ["mode"] = new OptionSetValue(executionMode.ToLower() == "asynchronous" ? 1 : 0), // 0 = Sync, 1 = Async
                    ["stage"] = new OptionSetValue(stage.ToLower() == "postoperation" ? 40 : 20), // 20 = preoperation, 40 = postoperation
                    ["rank"] = 1,
                    ["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter",sdkMessageFilterId),
                    ["filteringattributes"] = filterAttributes
                };

                sdkMessageProcessingStep.Id = service.Create(sdkMessageProcessingStep);
                configTargetRecord.vig_sdkmessageprocessingstepid = new EntityReference("sdkmessageprocessingstep", sdkMessageProcessingStep.Id);
                configTargetRecord.vig_name = $"{messageName} of {primaryEntity} Step"; 
                localcontext.Trace("Plugin step successfully registered.");
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException($"Error in DynamicPluginRegistration: {ex.Message}");
            }
        }

        private Guid GetSdkMessageId(IOrganizationService service, string messageName)
        {
            QueryExpression query = new QueryExpression("sdkmessage")
            {
                ColumnSet = new ColumnSet("sdkmessageid"),
                Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("name", ConditionOperator.Equal, messageName)
                }
            }
            };

            EntityCollection messages = service.RetrieveMultiple(query);
            return messages.Entities.Count > 0 ? messages.Entities[0].Id : Guid.Empty;
        }
        private Guid GetSdkMessageFilter(IOrganizationService service, string primaryEntity, Guid sdkMessageId)
        {
            // Get sdkmessagefilter where sdkmessage = "Update", and primaryobjecttypecode = "account"
            QueryExpression sdkMessageFilterQuery = new QueryExpression("sdkmessagefilter")
            {
                ColumnSet = new ColumnSet("sdkmessagefilterid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("sdkmessageid", ConditionOperator.Equal, sdkMessageId),
                        new ConditionExpression("primaryobjecttypecode", ConditionOperator.Equal, primaryEntity)
                    }
                }
            };

            EntityCollection results = service.RetrieveMultiple(sdkMessageFilterQuery);
            return results.Entities.Count > 0 ? results.Entities[0].Id:Guid.Empty;
            
        }
    }
}

