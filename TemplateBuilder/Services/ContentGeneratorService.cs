using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using TemplateBuilder.DTO;
using TemplateBuilder.Repositories;
using TemplateBuilder.Utilities;

namespace TemplateBuilder.Services
{
    //Service for generating the formatted text.
    public class ContentGeneratorService
    {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;
        private readonly ITracingService _tracing;
        private readonly TemplateRepository _templateRepo;
        private TextDescriptionBodies _descriptionFormatInfo;
        private Dictionary<string, Entity> _queryDictionary = new Dictionary<string, Entity>();
        Dictionary<string, string> _nestedValues = new Dictionary<string, string>();

        public ContentGeneratorService(IOrganizationService service, IPluginExecutionContext context,ITracingService tracingService, TemplateRepository templateRepo, Guid descriptionID)
        {
            _service = service;
            _context = context;
            _tracing = tracingService;
            _templateRepo = templateRepo;
            _descriptionFormatInfo = _templateRepo.CreateTemplateModel(descriptionID);
        }
        public string BuildContent()
        {
            TokenProcessor processor = new TokenProcessor(_tracing, _service, _context,null);
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            string emailDes = string.Empty;
            foreach (var q in _descriptionFormatInfo.queries)
            {
                string query = processor.ReplaceTokens(q.queryText);
                object results = ExecuteFetchAndPopulateValues(q.name,query, q.repeatingGroups);
                q.repeatingGroups = results as List<RepeatingGroups>;
                if(q.repeatingGroups != null)
                {
                    foreach (var rg in q.repeatingGroups)
                    {
                        if (!columnValues.ContainsKey(rg.name))
                        {
                            columnValues.Add(rg.name, rg.contentValue);
                        }
                        else
                        {
                            columnValues[rg.name] += rg.contentValue;
                        }
                    }
                }
            }
            TokenProcessor processToken = new TokenProcessor(_tracing, _service, _context, _queryDictionary,columnValues);
            _descriptionFormatInfo.structuredValue = processToken.ReplaceTokens(_descriptionFormatInfo.structure);

            try
            {
                return _descriptionFormatInfo.structuredValue;
            }
            catch
            {
                throw new InvalidPluginExecutionException("Placeholder does not match the name of value to be inserted");
            }
        }
        public object ExecuteFetchAndPopulateValues(string queryName,string fetchXml, List<RepeatingGroups> repeatingGroups)
        {            
            XmlHelper xmlHelper = new XmlHelper();
            string formattedQuery = xmlHelper.ExtractFetchQuery(fetchXml);
            try
            {

                EntityCollection retrievedEntities = _service.RetrieveMultiple(new FetchExpression(formattedQuery));

                string format = string.Empty;
                if (retrievedEntities.Entities.Count > 0)
                {
                    TokenProcessor processToken = new TokenProcessor(_tracing, _service, _context, retrievedEntities.Entities[0],null);

                    if (!_queryDictionary.ContainsKey(queryName))
                    {
                        _queryDictionary.Add(queryName, retrievedEntities.Entities[0]);
                    }

                    foreach (Entity entity in retrievedEntities.Entities)
                    {
                        if(repeatingGroups!=null)
                        {
                            foreach (var dc in repeatingGroups)
                            {
                                if (!string.IsNullOrEmpty(dc.format))
                                {
                                    format = dc.format;
                                }

                                if (dc.nestedRepeatingGroups!=null&&dc.nestedRepeatingGroups.Count>0)
                                {
                                    processToken = new TokenProcessor(_tracing,_service, _context, entity);
                                    string nestedQuery = processToken.ReplaceTokens(dc.query.queryText);
                                    object nestedResult = ExecuteFetchAndPopulateValues(dc.query.name, nestedQuery,dc.nestedRepeatingGroups);
                                    dc.nestedRepeatingGroups = nestedResult as List<RepeatingGroups>;

                                    foreach (var nested in dc.nestedRepeatingGroups)
                                    {                                    
                                        if (!_nestedValues.ContainsKey(nested.name))
                                        {

                                            _nestedValues.Add(nested.name, nested.contentValue);
                                        }
                                        else
                                        {
                                            _nestedValues[nested.name] = nested.contentValue;
                                        }
                                        nested.contentValue = string.Empty;
                                    }                                 
                                }                               
                                processToken = new TokenProcessor(_tracing, _service, _context, entity,_nestedValues);
                                dc.contentValue += processToken.ReplaceTokens(format);
                            }
                        }
                    }
                }

                return repeatingGroups;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Failed to execute fetch query: " + ex.Message);
            }
        } 
    }
}
