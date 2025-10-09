using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web.UI;
using TextFormatter;

namespace TemplateBuilder.Services
{
    //Service class containing methods required to register a pluginstep on dataverse.
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
        public Guid FindExistingPluginStep(string messageName, string primaryEntity, string filterAttributes)
        {
            Guid sdkMessageId = GetSdkMessageId(messageName);
            Guid sdkMessageFilterId = GetSdkMessageFilter(primaryEntity,sdkMessageId);
            Guid pluginTypeId = GetPluginTypeId();
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
                                new ConditionExpression("plugintypeid",ConditionOperator.Equal, pluginTypeId),
                                new ConditionExpression("filteringattributes",ConditionOperator.Equal,filterAttributes)
                            }
                        }
                    }
                }
            };
            var result = _service.RetrieveMultiple(queryExpression);
            if (result.Entities.Count > 0)
            {
                result[0]["statecode"] = new OptionSetValue(0);
                result[0]["statuscode"] = new OptionSetValue(1);
            }
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
            Guid sdkmessageId = GetSdkMessageId(messageName);
            Guid sdkMessageFilterId = GetSdkMessageFilter(primaryEntity, sdkmessageId);
            _context.Trace("Creating the sdk message processing step record");
            _context.Trace("sdkmessageID: "+sdkmessageId.ToString());
            _context.Trace("sdkmessageFilterID: " + sdkMessageFilterId.ToString());
            _context.Trace("pluginTypeId: " + pluginTypeId.ToString());
            _context.Trace("filterattribute: " + filterAttributes);
            _context.Trace("PrimaryEntity: " + primaryEntity);
            _context.Trace("MessageName: " + messageName);
            _context.Trace("Stage: " + stage);
            _context.Trace("Execution: " + executionMode);

            Entity sdkMessageProcessingStep = new Entity("sdkmessageprocessingstep")
            {
                ["name"] = $"{messageName} of {primaryEntity} Step",
                ["sdkmessageid"] = new EntityReference("sdkmessage",sdkmessageId),
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
        //Query Expression to retrive the plugin type (the actual plugin code that is registered on this step
            QueryExpression query = new QueryExpression("plugintype");
            query.ColumnSet = new ColumnSet("plugintypeid");
            query.Criteria.AddCondition("typename", ConditionOperator.Equal, pluginTypeName);
            _context.Trace("Query Expression executed");
            var result = _service.RetrieveMultiple(query);
            if (result.Entities.Count > 0)
            {
                _context.Trace("Query Expression executed");
                return result.Entities[0].Id;
            }
            return Guid.Empty;
        }
    }
}
