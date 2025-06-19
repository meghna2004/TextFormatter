using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Services;
using TemplateBuilder.DTO;
using TemplateBuilder.Enum;
using TemplateBuilder.Services;
using TextFormatter;
using TextFormatter.TemplateBuilder;

namespace TemplateBuilder.Repositories
{
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
            _tracing.Trace("MessageName: " +_context.MessageName);
            _tracing.Trace("PrimaryEntity: " + _context.PrimaryEntityName);
            _tracing.Trace("Mode: " + _context.Mode);
            _tracing.Trace("Stage: " + _context.Stage);
            vig_templateconfigurationsetting templateConfig = null;
            // Get the plugin step id
            _tracing.Trace("Getting PluginStepID");
            (Guid,string) pluginStepId = _pluginConfigService.GetPluginStepId(messageName, triggerEntity, executionMode, stage);
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
        public TextDescriptionBodies CreateTemplateModel(Guid templateDesId)
        {
            //Change Query to retrive only active records.
            _tracing.Trace("Create Template Model Query: "+ templateDesId.ToString());
            string retrieveTemplateSections = string.Format(@"<fetch>
                                                                <entity name='vig_query'>
                                                                    <attribute name='vig_name' />
                                                                    <attribute name='vig_fetchquery' />
                                                                    <attribute name='vig_format' />
                                                                    <attribute name='vig_fetchsequence' />
                                                                    <filter>
                                                                        <condition attribute='vig_textdescriptionbodyid' operator='eq' value='{0}' />
                                                                    </filter>
                                                                    <order attribute='vig_fetchsequence' />
                                                                    <link-entity name='vig_textdescriptionbody' from='vig_textdescriptionbodyid' to= 'vig_textdescriptionbodyid' link-type='outer' alias='TBD'>
                                                                        <attribute name='vig_textformat'/>
                                                                    </link-entity>
                                                                    <link-entity name='vig_subsection' from='vig_queryid' to='vig_queryid' link-type='outer' alias='DC'>
                                                                        <attribute name='vig_name' />
                                                                        <attribute name='vig_sequence' />
                                                                        <attribute name='vig_format' />
                                                                        <order attribute='vig_sequence' />                                                                        
                                                                    </link-entity>
                                                                    <link-entity name='vig_queryplaceholder' from= 'vig_queryid' to= 'vig_queryid' link-type= 'outer' alias= 'QP'>
                                                                        <attribute name= 'vig_datatype' />
                                                                        <attribute name= 'vig_name' />
                                                                        <attribute name= 'vig_sequence' />
                                                                        <attribute name= 'vig_value' />
                                                                        <attribute name= 'vig_valuefrom' />
                                                                        <order attribute='vig_sequence' />
                                                                    </link-entity>
                                                                </entity>
                                                              </fetch>", templateDesId.ToString());
            EntityCollection sectionsEntity = _service.RetrieveMultiple(new FetchExpression(retrieveTemplateSections));
            _tracing.Trace("Query Ran");

            var descriptionBody = new TextDescriptionBodies
            {
                queries = new List<Queries>()
            };
            foreach (Entity entity in sectionsEntity.Entities)
            {
                _tracing.Trace("Initilise Variables with Values retrieved");
                var sectionSeq = entity.GetAttributeValue<int>("vig_fetchsequence");
                _tracing.Trace("Variable 1 "+ sectionSeq);
               // int querySeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("vig_fetchsequence").Value);
                //_tracing.Trace("Variable 2 "+ querySeq);
                int dcSeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("DC.vig_sequence").Value);
                _tracing.Trace("Variable 3 "+ dcSeq);
                //int columnSeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("Col.vig_sequence").Value);
                //_tracing.Trace("Variable 4 "+ columnSeq);
                var fetchQuery = entity.GetAttributeValue<string>("vig_fetchquery");
                //_tracing.Trace("Variable 6 "+ fetchQuery);
               // var colLogicalName = entity.GetAttributeValue<AliasedValue>("Col.vig_columnlogicalname").Value.ToString();
               // _tracing.Trace("Variable 7 "+ colLogicalName);
                var textFormat = entity.GetAttributeValue<string>("vig_format");
                var queryName = entity.GetAttributeValue<string>("vig_name");

                //_tracing.Trace("Variable 8 "+ textFormat);
                var dcFormat = string.Empty;
                var dcName = entity.GetAttributeValue<AliasedValue>("DC.vig_name").Value.ToString();
                _tracing.Trace("Variable 9 "+ dcName);
                var qpName = string.Empty;
                _tracing.Trace("Variable 10 "+ qpName);
                if (entity.Contains("TBD.vig_textformat"))
                {
                    var tbdFormat = entity.GetAttributeValue<AliasedValue>("TBD.vig_textformat").Value.ToString();
                    descriptionBody.structure = tbdFormat;
                }
                var qpValue = string.Empty;
                int qpSeq = 0;
                var qpDataType = new OptionSetValue();
                var qpGuidType = new OptionSetValue();
                if (entity.Contains("QP.vig_name"))
                {
                     qpName = entity.GetAttributeValue<AliasedValue>("QP.vig_name").Value.ToString();
                    if (entity.Contains("QP.vig_value"))
                    {
                        qpValue = entity.GetAttributeValue<AliasedValue>("QP.vig_value").Value.ToString();
                        _tracing.Trace("Variable 11 " + qpValue);
                    }
                     qpSeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("QP.vig_sequence").Value);
                    _tracing.Trace("Variable 5 " + qpSeq);
                     qpDataType = (OptionSetValue)entity.GetAttributeValue<AliasedValue>("QP.vig_datatype").Value;
                     qpGuidType = (OptionSetValue)entity.GetAttributeValue<AliasedValue>("QP.vig_valuefrom").Value;
                    _tracing.Trace("Variable 12 " + qpDataType);

                    _tracing.Trace("Variable 13 " + qpGuidType);
                }

                _tracing.Trace("Getting Query placeholder values");
              
                
                _tracing.Trace("All Variables Initialised");
                if (entity.Contains("DC.vig_format"))
                {
                    dcFormat = entity.GetAttributeValue<AliasedValue>("DC.vig_format").Value.ToString();
                    //_tracing.Trace("Variable 14 "+ dcFormat);
                }
               /* var section = descriptionBody.sectionClasses.FirstOrDefault(s => s.sequence == sectionSeq);
                if (section == null)
                {
                    section = new Sections
                    {
                        sequence = sectionSeq,
                        format = secFormat,
                        queryClasses = new List<Queries>()

                    };
                    descriptionBody.sectionClasses.Add(section);
                }*/
                var query = descriptionBody.queries.FirstOrDefault(q => q.sequence == sectionSeq);
                if (query == null)
                {
                    query = new Queries
                    {
                        sequence = sectionSeq,
                        subSections = new List<SubSections>(),
                        //placeholders = new List<QueryPlaceholders>(),
                        queryText = fetchQuery,
                        format = textFormat,
                        name = queryName
                    };
                    descriptionBody.queries.Add(query);
                }
                var subSections = query.subSections.FirstOrDefault(c => c.sequence == dcSeq);
                if (subSections == null)
                {
                    subSections = new SubSections
                    {
                        sequence = dcSeq,
                       // content = new List<Columns>(),
                        name = dcName,
                        format = dcFormat
                    };
                    query.subSections.Add(subSections);
                }
               /* subSections.content.Add(new Columns
                {
                    sequence = columnSeq,
                    colName = colLogicalName
                });*/
                /*var qPlaceholder = query.placeholders.FirstOrDefault(p =>p.sequence==qpSeq);
                if(qPlaceholder == null)
                {
                    qPlaceholder = new QueryPlaceholders
                    {
                        sequence = qpSeq,
                        name = qpName,
                        value = qpValue,
                        dataType = (DataType)qpDataType.Value,
                        valueFrom = (ValueFrom)qpGuidType.Value
                    };
                    query.placeholders.Add(qPlaceholder);
                }*/
            }
            _tracing.Trace("Template Model Created");
            return descriptionBody;
        }
    }
}


