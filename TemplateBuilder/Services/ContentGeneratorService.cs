using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TemplateBuilder.DTO;
using TemplateBuilder.OutputStrategies;
using TemplateBuilder.Repositories;
using TemplateBuilder.Utilities;

namespace TemplateBuilder.Services
{
    public class ContentGeneratorService
    {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;
        private readonly ITracingService _tracing;
        private readonly TemplateRepository _templateRepo;
        private TextDescriptionBodies _descriptionFormatInfo;
        private Dictionary<string, Entity> _queryDictionary = new Dictionary<string, Entity>();
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
            TokenProcessor processor = new TokenProcessor(_tracing, _service, _context);
            _tracing.Trace("Template Model Created");
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            Dictionary<string, string> sectionValues = new Dictionary<string, string>();
            string emailDes = string.Empty;
            foreach (var q in _descriptionFormatInfo.queries)
            {
                _tracing.Trace("Inside for each for text desriptionbody in template model");
                string query = processor.ReplaceTokens(q.queryText);
                _tracing.Trace("Query: " + query);
                object results = ExecuteFetchAndPopulateValues(q.name,query, q.repeatingGroups);
                q.repeatingGroups = results as List<RepeatingGroups>;
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
            foreach(var e in _queryDictionary)
            {
                _tracing.Trace("e.Key: " + e.Key);
                _tracing.Trace("e.Value: " + e.Value.Id);
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
            _tracing.Trace("Format the query using XML Helper");
            string formattedQuery = xmlHelper.ExtractFetchQuery(fetchXml);
            try
            {
                _tracing.Trace("Retrieve records from fetchQuery");
                EntityCollection retrievedEntities = _service.RetrieveMultiple(new FetchExpression(formattedQuery));
                _tracing.Trace("Records Retrieved from Query");
                string format = string.Empty;
                if (retrievedEntities.Entities.Count > 0)
                {
                    _tracing.Trace("Entities count more than 0");
                    TokenProcessor processToken = new TokenProcessor(_tracing, _service, _context, retrievedEntities.Entities[0]);

                    if (!_queryDictionary.ContainsKey(queryName))
                    {
                        _tracing.Trace("Add query and entity to dictionary: "+ queryName+ "ID: "+ retrievedEntities.Entities[0].Id);
                        _queryDictionary.Add(queryName, retrievedEntities.Entities[0]);
                    }
                    _tracing.Trace("EmailFormatterFunctions: Test 4 "+ _descriptionFormatInfo.structuredValue);
                    foreach (Entity entity in retrievedEntities.Entities)
                    {
                        processToken = new TokenProcessor(_tracing, _service, _context, entity);
                        foreach (var dc in repeatingGroups)
                        {
                            if (!string.IsNullOrEmpty(dc.format))
                            {
                                format = dc.format;
                            }
                            dc.contentValue += processToken.ReplaceTokens(format);
                            _tracing.Trace("EmailFormatterFunctions: Test 8");
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
