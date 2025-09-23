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
        private string _errorMessage;
        public ContentGeneratorService(IOrganizationService service, IPluginExecutionContext context,ITracingService tracingService, TemplateRepository templateRepo, Guid descriptionID)
        {
            _service = service ?? throw new InvalidPluginExecutionException("We couldn’t connect to Dynamics 365. Please try again later or contact your system administrator.");
            _context = context ?? throw new InvalidPluginExecutionException("We couldn’t process your request because the context information is missing. Please try again later or contact your system administrator.");
            _tracing = tracingService?? throw new InvalidPluginExecutionException("We couldn’t process your request due to a system error. Please try again later or contact your system administrator.");
            _templateRepo = templateRepo ?? throw new InvalidPluginExecutionException("We couldn’t load the email template. Please try again later or contact your system administrator.");
            try
            {
                _descriptionFormatInfo = _templateRepo.CreateTemplateModel(descriptionID);
                if(_descriptionFormatInfo == null)
                {
                    throw new InvalidPluginExecutionException("The requested template could not be found. Please check the template and try again.");
                }
            }
            catch (Exception ex)
            {
                _tracing.Trace($"[ContentGeneratorService] Error Loading template {descriptionID}: {ex}");
                throw new InvalidPluginExecutionException("We couldn’t load the requested template. Please try again later or contact your system administrator.");
            }
        }
        public string BuildContent()
        {
            TokenProcessor processor = new TokenProcessor(_tracing, _service, _context,null);
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            string emailDes = string.Empty;
            foreach (var q in _descriptionFormatInfo.queries)
            {
                if (string.IsNullOrWhiteSpace(q.queryText))
                {
                    _tracing.Trace($"[BuildContent] Query '{q.name}' has empty text.");
                    throw new InvalidPluginExecutionException("One of the queries used in the template is blank. Please check the template setup.");
                }
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
            catch(Exception ex) 
            {
                _tracing.Trace($"[BuildContent] Error occurred while returning structured value: {ex.Message}");
                throw new InvalidPluginExecutionException("There was error outputting the formatted Text. Please try again later or contact your system administrator.");
            }
        }
        public object ExecuteFetchAndPopulateValues(string queryName,string fetchXml, List<RepeatingGroups> repeatingGroups)
        {
            if (string.IsNullOrWhiteSpace(fetchXml))
            {
                _tracing.Trace($"[ExecuteFetchAndPopulateValues] Query '{queryName}' has null/empty FetchXML.");
                throw new InvalidPluginExecutionException("One of the data queries is missing. Please check the template setup.");
            }
            XmlHelper xmlHelper = new XmlHelper();
            string formattedQuery = xmlHelper.ExtractFetchQuery(fetchXml);
            try
            {
                EntityCollection retrievedEntities = _service.RetrieveMultiple(new FetchExpression(formattedQuery));
                if (retrievedEntities == null)
                {
                    _tracing.Trace($"[ExecuteFetchAndPopulateValues] RetrieveMultiple returned null for query '{queryName}'.");
                    throw new InvalidPluginExecutionException("One of the inputted queries did not return any records. Please check the query is valid.");
                }
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
                _tracing.Trace($"[ExecuteFetchAndPopulateValues] Error in query {queryName} Exception: {ex}");
                throw new InvalidPluginExecutionException("We couldn't process the data for the requested template. Please check the template setup and try again. If error persists, contact your system administrator.");
            }
        } 
    }
}
