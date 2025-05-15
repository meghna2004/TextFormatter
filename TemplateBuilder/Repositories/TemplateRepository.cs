using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using TemplateBuilder.Services;
using TextFormatter;
using TextFormatter.TemplateBuilder;

namespace TemplateBuilder.Repositories
{
    public class TemplateRepository : PluginBase
    {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;
        private readonly PluginConfigService _pluginConfigService;
        public TemplateRepository(IOrganizationService service, IPluginExecutionContext context)
        {
            _service = service;
            _context = context;
        }
        public vig_templateconfigurationsetting GetTemplateConfig(string messageName, string triggerEntity, string executionMode, string stage)
        {
            messageName = _context.MessageName;
            triggerEntity = _context.PrimaryEntityName;
            vig_templateconfigurationsetting templateConfig = null;
            // Get the plugin step id
            (Guid,string) pluginStepId = _pluginConfigService.GetPluginStepId(messageName, triggerEntity, executionMode, stage);

            QueryExpression getTemplateConfig = new QueryExpression("vig_templateconfigurationsetting")
            {
                ColumnSet = new ColumnSet("vig_templateconfigurationsettingid"),
                Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("vig_sdkmessageprocessingstepid", ConditionOperator.Equal, pluginStepId.Item1),
                    new ConditionExpression("vig_mode",ConditionOperator.Equal, executionMode),
                    new ConditionExpression("vig_filteringattributes",ConditionOperator.Equal, pluginStepId.Item2),
                }
            }
            };
            EntityCollection templateConfigsRetrieved = _service.RetrieveMultiple(getTemplateConfig);
            if (templateConfigsRetrieved.Entities.Count > 0)
            {
                templateConfig = templateConfigsRetrieved.Entities[0].ToEntity<vig_templateconfigurationsetting>();
            }
            return templateConfig;
        }
        public EntityCollection GetTemplates(Guid configID)
        {
            QueryExpression getTemplate = new QueryExpression("vig_customtemplate")
            {
                ColumnSet = new ColumnSet("vig_customtemplateid", "vig_textdescriptionbodyid"),
                Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("vig_templateconfigurationsettingid", ConditionOperator.Equal, configID)
                }
            }
            };
            EntityCollection templatesRetrieved = _service.RetrieveMultiple(getTemplate);
            if (templatesRetrieved.Entities.Count > 0)
            {
                return templatesRetrieved;
            }
            return null;
        }
        public TextDescriptionBodies CreateTemplateModel(Guid templateId)
        {
            //Change Query
            string retrieveTemplateSections = string.Format(@"<fetch>
                                                          <entity name='vig_section'>
                                                            <attribute name='vig_format' />
                                                            <attribute name='vig_sequence' />
                                                            <filter>
                                                              <condition attribute='vig_textdescriptionbody' operator='eq' value='{0}' />
                                                            </filter>
                                                            <order attribute='vig_sequence' />
                                                            <link-entity name='vig_query' from='vig_sectionid' to='vig_sectionid' link-type='inner' alias='Q'>
                                                              <attribute name='vig_fetchquery' />
                                                              <attribute name='vig_fetchsequence' />
                                                              <order attribute='vig_fetchsequence' />
                                                              <link-entity name='vig_subsection' from='vig_queryid' to='vig_queryid' link-type='inner' alias='DC'>
                                                                <attribute name='vig_name' />
                                                                <attribute name='vig_contentsequence' />
                                                                <attribute name='vig_format' />
                                                                <order attribute='vig_contentsequence' />
                                                                <link-entity name='vig_column' from='vig_dynamiccontentid' to='vig_subsection' link-type='inner' alias='Col'>
                                                                  <attribute name='vig_columnlogicalname' />
                                                                  <attribute name='vig_sequence' />
                                                                  <order attribute='vig_sequence' />
                                                                </link-entity>
                                                              </link-entity>
                                                            </link-entity>
                                                          </entity>
                                                        </fetch>");
            EntityCollection sectionsEntity = _service.RetrieveMultiple(new FetchExpression(retrieveTemplateSections));
            var descriptionBody = new TextDescriptionBodies
            {
                sectionClasses = new List<Sections>()
            };
            foreach (Entity entity in sectionsEntity.Entities)
            {
                var sectionSeq = entity.GetAttributeValue<int>("vig_sequence");
                int querySeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("Q.vig_fetchsequence").Value);
                int dcSeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("DC.vig_contentsequence").Value);
                int columnSeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("Col.vig_sequence").Value);
                var fetchQuery = string.Format(entity.GetAttributeValue<AliasedValue>("Q.vig_fetchquery").Value.ToString());
                var colLogicalName = entity.GetAttributeValue<AliasedValue>("Col.vig_columnlogicalname").Value.ToString();
                var secFormat = entity.GetAttributeValue<string>("vig_format");
                var dcFormat = string.Empty;
                var dcName = entity.GetAttributeValue<AliasedValue>("DC.vig_name").Value.ToString();
                if (entity.Contains("DC.vig_format"))
                {
                    dcFormat = entity.GetAttributeValue<AliasedValue>("DC.vig_format").Value.ToString();
                }
                var section = descriptionBody.sectionClasses.FirstOrDefault(s => s.sequence == sectionSeq);
                if (section == null)
                {
                    section = new Sections
                    {
                        sequence = sectionSeq,
                        format = secFormat,
                        queryClasses = new List<Queries>()

                    };
                    descriptionBody.sectionClasses.Add(section);
                }
                var query = section.queryClasses.FirstOrDefault(q => q.sequence == querySeq);
                if (query == null)
                {
                    query = new Queries
                    {
                        sequence = querySeq,
                        contentClasses = new List<SubSections>(),
                        queryText = fetchQuery
                    };
                    section.queryClasses.Add(query);
                }
                var subSections = query.contentClasses.FirstOrDefault(c => c.sequence == dcSeq);
                if (subSections == null)
                {
                    subSections = new SubSections
                    {
                        sequence = dcSeq,
                        content = new List<Columns>(),
                        name = dcName,
                        format = dcFormat
                    };
                    query.contentClasses.Add(subSections);
                }
                subSections.content.Add(new Columns
                {
                    sequence = columnSeq,
                    colName = colLogicalName
                });
            }
            return descriptionBody;
        }
    }
}


