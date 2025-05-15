using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using TextFormatter;

namespace TemplateBuilder.Services
{
    public class PluginConfigService: PluginBase
    {
        private readonly IOrganizationService _service;
        private readonly LocalPluginContext _context;
        string assemblyName = "TemplateBuilder";
        string pluginTypeName = "TemplateBuilder.PlugIns.GenerateContentPlugIn";

        public PluginConfigService(IOrganizationService service, LocalPluginContext context)
        {
            _service = service;
            _context = context; 
        }
        public Guid FindExistingPluginStep(string messageName, string primaryEntity)
        {
            var sdkMessageId = GetSdkMessageId(messageName);
            var sdkMessageFilterId = GetSdkMessageFilter(primaryEntity,sdkMessageId);
            QueryExpression queryExpression = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet("sdkmessageprocessingstepid"),
                Criteria =
                {
                    Filters =
                    {
                        new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("sdkmessageid", ConditionOperator.Equal, sdkMessageId),
                                new ConditionExpression("sdkmessagefilterid", ConditionOperator.Equal, sdkMessageFilterId),
                                new ConditionExpression("plugintypeid.name",ConditionOperator.Equal, pluginTypeName)
                            }
                        }
                    }
                }
            };
            var result = _service.RetrieveMultiple(queryExpression);
            return result.Entities.Count > 0 ? result.Entities[0].Id: Guid.Empty;
        }
        public Guid CreatePluginStep(string primaryEntity, string messageName, string stage, string executionMode, string filterAttributes)
        {
           
            QueryExpression assemblyQuery = new QueryExpression("pluginassembly")
            {
                ColumnSet = new ColumnSet("pluginassemblyid"),
                Criteria =
                {
                      Conditions = { new ConditionExpression("name", ConditionOperator.Equal, assemblyName) }
                    }
            };
            _context.Trace("Plugin assembly retrieved");
            EntityCollection assemblies = _service.RetrieveMultiple(assemblyQuery);
            if (assemblies.Entities.Count == 0)
                throw new InvalidPluginExecutionException($"Plugin Assembly '{assemblyName}' not found in CRM. Please deploy it first.");

            Guid assemblyId = assemblies.Entities[0].Id;

            _context.Trace("Create PlugInType");
          
            Guid pluginTypeId = GetPluginTypeId();

            if (pluginTypeId == Guid.Empty || pluginTypeId == null)
            {
                Entity pluginType = new Entity("plugintype")
                {
                    ["pluginassemblyid"] = new EntityReference("pluginassembly", assemblyId),
                    ["typename"] = pluginTypeName,
                    ["friendlyname"] = "GenerateContentPlugIn",
                    ["name"] = "GenerateContentPlugIn"
                };
                pluginTypeId = _service.Create(pluginType);
            }           
            // Register the plugin step
            Guid sdkMessageFilterId = GetSdkMessageFilter(primaryEntity, GetSdkMessageId(messageName));

            Entity sdkMessageProcessingStep = new Entity("sdkmessageprocessingstep")
            {
                ["name"] = $"{messageName} of {primaryEntity} Step",
                ["sdkmessageid"] = new EntityReference("sdkmessage", GetSdkMessageId(messageName)),
                ["plugintypeid"] = new EntityReference("plugintype", pluginTypeId),
                ["mode"] = new OptionSetValue(executionMode.ToLower() == "asynchronous" ? 1 : 0), // 0 = Sync, 1 = Async
                ["stage"] = new OptionSetValue(stage.ToLower() == "postoperation" ? 40 : 20), // 20 = preoperation, 40 = postoperation
                ["rank"] = 1,
                ["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", sdkMessageFilterId),
                ["filteringattributes"] = filterAttributes
            };

            sdkMessageProcessingStep.Id = _service.Create(sdkMessageProcessingStep);
            return sdkMessageProcessingStep.Id;       
        }
        private Guid GetSdkMessageId(string messageName)
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

            EntityCollection messages = _service.RetrieveMultiple(query);
            return messages.Entities.Count > 0 ? messages.Entities[0].Id : Guid.Empty;
        }
        private Guid GetSdkMessageFilter(string primaryEntity, Guid sdkMessageId)
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

            EntityCollection results = _service.RetrieveMultiple(sdkMessageFilterQuery);
            return results.Entities.Count > 0 ? results.Entities[0].Id : Guid.Empty;
        }
        private Guid GetPluginTypeId()
        {
            QueryExpression query = new QueryExpression("plugintype");
            query.ColumnSet = new ColumnSet("plugintypeid");
            query.Criteria.AddCondition("typename", ConditionOperator.Equal, pluginTypeName);

            var result = _service.RetrieveMultiple(query);
            if (result.Entities.Count > 0)
            {
                return result.Entities[0].Id;
            }
            return Guid.Empty;
        }
        public (Guid,string) GetPluginStepId(string messageName, string triggerEntity, string executionMode, string stage)
        {
            Guid pluginTypeId = GetPluginTypeId();

            // Now try to query the SdkMessageProcessingStep entity
            QueryExpression query = new QueryExpression("sdkmessageprocessingstep");
            query.ColumnSet = new ColumnSet("sdkmessageprocessingstepid", "name","filteringattributes");
            query.Criteria.AddCondition("plugintypeid", ConditionOperator.Equal, pluginTypeId);
            query.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, GetSdkMessageId(messageName));
            query.Criteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, triggerEntity);
            query.Criteria.AddCondition("mode", ConditionOperator.Equal, executionMode);
            query.Criteria.AddCondition("stage", ConditionOperator.Equal, stage);

            EntityCollection steps = _service.RetrieveMultiple(query);

            if (steps.Entities.Count > 0)
            {
                var step = steps.Entities[0];   
                return (step.Id,step.GetAttributeValue<string>("filteringattributes"));
            }
            return (Guid.Empty,string.Empty);
        }
        public bool MatchFilterAttributes(Guid stepID, string filterAttributes)
        {
            var step = _service.Retrieve("sdkmessageprocessingstep", stepID, new ColumnSet("filteringattributes"));
            if (step.Contains("filteringattributes"))
            {
                if(step.GetAttributeValue<string>("filteringattributes").ToLower() == filterAttributes.ToLower())
                {
                    return true;
                }
            }
            return false;
        }
    }
}
