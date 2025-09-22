using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using TemplateBuilder.DTO;
using TemplateBuilder.Services;
using TextFormatter;
using TextFormatter.TemplateBuilder;

namespace TemplateBuilder.Repositories
{
    // Repo class to fetch records required for creating templates.
    public class TemplateRepository : PluginBase
    {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;
        private readonly ITracingService _tracing;
        private readonly PluginConfigService _pluginConfigService;
        public TemplateRepository(IOrganizationService service, IPluginExecutionContext context, ITracingService tracing, PluginConfigService pluginService)
        {
            _service = service;
            _context = context;
            _tracing = tracing;
            _pluginConfigService = pluginService;
        }
        public vig_templateconfigurationsetting GetTemplateConfig(string messageName, string triggerEntity, string executionMode, string stage)
        {
            _tracing.Trace("MessageName: " + _context.MessageName);
            _tracing.Trace("PrimaryEntity: " + _context.PrimaryEntityName);
            _tracing.Trace("Mode: " + _context.Mode);
            _tracing.Trace("Stage: " + _context.Stage);
            vig_templateconfigurationsetting templateConfig = null;
            // Get the plugin step id
            _tracing.Trace("Getting PluginStepID");
            (Guid, string) pluginStepId = _pluginConfigService.GetPluginStepId(messageName, triggerEntity, executionMode, stage);
            _tracing.Trace("PluginStepID retrieved");

            QueryExpression getTemplateConfig = new QueryExpression("vig_templateconfigurationsetting")
            {
                ColumnSet = new ColumnSet("vig_templateconfigurationsettingid"),
                Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("vig_sdkmessageprocessingstepid", ConditionOperator.Equal, pluginStepId.Item1),
                    new ConditionExpression("vig_executionmode",ConditionOperator.Equal, executionMode),
                    new ConditionExpression("vig_filterattributes",ConditionOperator.Equal, pluginStepId.Item2),
                }
            }
            };
            EntityCollection templateConfigsRetrieved = _service.RetrieveMultiple(getTemplateConfig);
            if (templateConfigsRetrieved.Entities.Count > 0)
            {
                _tracing.Trace("Template Config Retrieved");
                _tracing.Trace("---------------------------");

                templateConfig = templateConfigsRetrieved.Entities[0].ToEntity<vig_templateconfigurationsetting>();
            }
            return templateConfig;
        }
        public EntityCollection GetTemplates(Guid configID)
        {
            QueryExpression getTemplate = new QueryExpression("vig_customtemplate")
            {
                ColumnSet = new ColumnSet(true),
                Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("vig_templateconfigurationsettingid", ConditionOperator.Equal, configID),
                    new ConditionExpression("statuscode",ConditionOperator.Equal,949800000)
                }
            }
            };
            EntityCollection templatesRetrieved = _service.RetrieveMultiple(getTemplate);
            if (templatesRetrieved.Entities.Count > 0)
            {
                _tracing.Trace("Templates Found for Template config" + configID);
                _tracing.Trace("Template ID" + templatesRetrieved.Entities[0].Id.ToString());

                return templatesRetrieved;
            }
            _tracing.Trace("---------------------------");
            return null;
        }
        public virtual TextDescriptionBodies CreateTemplateModel(Guid templateDesId)
        {
            //Change Query to retrive only active records.
            _tracing.Trace("Create Template Model Query: " + templateDesId.ToString());
            string retrieveTemplateSections = string.Format(@"<fetch>
                                                                <entity name='vig_query'>
                                                                    <attribute name='vig_name' />
                                                                    <attribute name='vig_fetchquery' />
                                                                    <attribute name='vig_format' />
                                                                    <attribute name='vig_fetchsequence' />
                                                                    <filter type= 'and'>
                                                                        <condition attribute='vig_textdescriptionbodyid' operator='eq' value='{0}' />
                                                                        <condition attribute='statuscode' operator='eq' value='1' />
                                                                    </filter>
                                                                    <order attribute='vig_fetchsequence' />
                                                                    <link-entity name='vig_textdescriptionbody' from='vig_textdescriptionbodyid' to= 'vig_textdescriptionbodyid' link-type='outer' alias='TBD'>
                                                                        <attribute name='vig_textformat'/>
                                                                    </link-entity>
                                                                    <link-entity name='vig_repeatinggroup' from='vig_queryid' to='vig_queryid' link-type='outer' alias='RG'>
                                                                        <attribute name='vig_name' />
                                                                        <attribute name='vig_format' />
                                                                        <filter type= 'and'>
                                                                            <condition attribute='statuscode' operator='eq' value='1' />
                                                                        </filter>
                                                                        <link-entity name='vig_repeatinggroup' from='vig_nestedrepeatinggroupid' to='vig_repeatinggroupid' link-type='outer' alias='NRG'>
                                                                            <attribute name='vig_format' />
                                                                            <attribute name='vig_name' />
                                                                            <attribute name='vig_queryid' />
                                                                            <filter>
                                                                                <condition attribute='statuscode' operator='eq' value='1' />
                                                                            </filter>
                                                                            <link-entity name='vig_query' from='vig_queryid' to='vig_queryid' link-type='outer' alias='nQuery'>
                                                                                <attribute name='vig_fetchquery' />
                                                                                <attribute name='vig_fetchsequence' />
                                                                                <attribute name='vig_format' />
                                                                                <attribute name='vig_name' />
                                                                            </link-entity>
                                                                        </link-entity>
                                                                    </link-entity>
                                                                </entity>
                                                              </fetch>", templateDesId.ToString());
            EntityCollection sectionsEntity = _service.RetrieveMultiple(new FetchExpression(retrieveTemplateSections));
            //_tracing.Trace("Query Ran");

            var descriptionBody = new TextDescriptionBodies
            {
                queries = new List<Queries>()
            };
            foreach (Entity entity in sectionsEntity.Entities)
            {
                //_tracing.Trace("Initilise Variables with Values retrieved");
                var fetchSequence = 0;
                var fetchQuery = string.Empty;
                var queryName = string.Empty;
                var rgName = string.Empty;
                var rgFormat = string.Empty;
                var nrgName = string.Empty;
                var nrgFormat = string.Empty;
                var nFetchQuery = string.Empty;
                var nQueryName = string.Empty;

                if (entity.Contains("vig_fetchsequence"))
                {
                    fetchSequence = entity.GetAttributeValue<int>("vig_fetchsequence");
                    //_tracing.Trace("Variable 1 " + fetchSequence);
                }
                if (entity.Contains("vig_fetchquery"))
                {
                    fetchQuery = entity.GetAttributeValue<string>("vig_fetchquery");
                    //crete data model here.
                }
                if (entity.Contains("vig_name"))
                {
                    queryName = entity.GetAttributeValue<string>("vig_name");
                }
                if (entity.Contains("RG.vig_name"))
                {
                    rgName = entity.GetAttributeValue<AliasedValue>("RG.vig_name").Value.ToString();
                   // _tracing.Trace("Variable 9 " + rgName);
                }
                if (entity.Contains("TBD.vig_textformat"))
                {
                    var tbdFormat = entity.GetAttributeValue<AliasedValue>("TBD.vig_textformat").Value.ToString();
                    descriptionBody.structure = tbdFormat;
                }
                if (entity.Contains("RG.vig_format"))
                {
                    rgFormat = entity.GetAttributeValue<AliasedValue>("RG.vig_format").Value.ToString();
                }
                if (entity.Contains("NRG.vig_format"))
                {
                    //_tracing.Trace("NRG Format");

                    nrgFormat = entity.GetAttributeValue<AliasedValue>("NRG.vig_format").Value.ToString();
                }
                if (entity.Contains("NRG.vig_name"))
                {
                    //_tracing.Trace("NRG Name");

                    nrgName = entity.GetAttributeValue<AliasedValue>("NRG.vig_name").Value.ToString();
                }
                if (entity.Contains("nQuery.vig_name"))
                {
                    //_tracing.Trace("Nested Query Name");

                    nQueryName = entity.GetAttributeValue<AliasedValue>("nQuery.vig_name").Value.ToString();
                }
                if (entity.Contains("nQuery.vig_fetchquery"))
                {
                    //_tracing.Trace("Nested Query text");

                    nFetchQuery = entity.GetAttributeValue<AliasedValue>("nQuery.vig_fetchquery").Value.ToString();
                }
                _tracing.Trace("All Variables Initialised");
                var query = descriptionBody.queries.FirstOrDefault(q => q.sequence == fetchSequence);
                if (query == null)
                {
                    query = new Queries
                    {
                        sequence = fetchSequence,
                        repeatingGroups = new List<RepeatingGroups>(),
                        queryText = fetchQuery,
                        name = queryName
                    };
                    descriptionBody.queries.Add(query);
                }

                var subSections = new RepeatingGroups
                {
                    name = rgName,
                    format = rgFormat,
                    nestedRepeatingGroups = new List<RepeatingGroups>(),
                };
                if(nrgFormat!=string.Empty&&nrgFormat!=string.Empty)
                {
                    var nestedRepeatingGroup = new RepeatingGroups
                    {
                        name = nrgName,
                        format = nrgFormat
                    };
                    if (nQueryName != string.Empty && nFetchQuery != string.Empty)
                    {
                        var nestedQuery = new Queries
                        {
                            name = nQueryName,
                            queryText = nFetchQuery
                        };
                        subSections.query = nestedQuery;
                    }
                    subSections.nestedRepeatingGroups.Add(nestedRepeatingGroup);
                }
                query.repeatingGroups.Add(subSections);
            }
            _tracing.Trace("Template Model Created");
            return descriptionBody;
        }
    }
}


