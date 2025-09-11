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
            _tracing.Trace("Template Model Created");
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            string emailDes = string.Empty;
            foreach (var q in _descriptionFormatInfo.queries)
            {
                _tracing.Trace("Inside for each for text desriptionbody in template model");
                string query = processor.ReplaceTokens(q.queryText);
                _tracing.Trace("Query: " + query);
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
            _tracing.Trace("Format the query using XML Helper: "+fetchXml);
            string formattedQuery = xmlHelper.ExtractFetchQuery(fetchXml);
            try
            {
                _tracing.Trace("Retrieve records from fetchQuery");
                EntityCollection retrievedEntities = _service.RetrieveMultiple(new FetchExpression(formattedQuery));
                _tracing.Trace("Records Retrieved from Query");
                string format = string.Empty;
                //string childContent = string.Empty;
                if (retrievedEntities.Entities.Count > 0)
                {
                    _tracing.Trace("Entities count more than 0");
                    TokenProcessor processToken = new TokenProcessor(_tracing, _service, _context, retrievedEntities.Entities[0],null);

                    if (!_queryDictionary.ContainsKey(queryName))
                    {
                        _tracing.Trace("Add query and entity to dictionary: "+ queryName+ "ID: "+ retrievedEntities.Entities[0].Id);
                        _queryDictionary.Add(queryName, retrievedEntities.Entities[0]);
                    }
                    _tracing.Trace("TextFormatter: Test 4 "+ _descriptionFormatInfo.structuredValue);
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
                                _tracing.Trace("Format of Repeating Group:"+ format);

                                if (dc.nestedRepeatingGroups!=null&&dc.nestedRepeatingGroups.Count>0)
                                {
                                    
                                    _tracing.Trace("Nested Repeating Group");
                                    _tracing.Trace("Starting Recursion "+ dc.name);
                                    //_tracing.Trace("Starting Recursion two " + dc.query.queryText);

                                    //replace tokens in querytext before calling the method.either primary entity or field from the parent query.
                                    processToken = new TokenProcessor(_tracing,_service, _context, entity);
                                    string nestedQuery = processToken.ReplaceTokens(dc.query.queryText);
                                    object nestedResult = ExecuteFetchAndPopulateValues(dc.query.name, nestedQuery,dc.nestedRepeatingGroups);
                                    dc.nestedRepeatingGroups = nestedResult as List<RepeatingGroups>;

                                    _tracing.Trace("Recursion Ended");
                                    foreach (var nested in dc.nestedRepeatingGroups)
                                    {
/*                                        childContent = nested.contentValue;
*/                                       
                                        if (!_nestedValues.ContainsKey(nested.name))
                                        {
                                            _tracing.Trace("Nested Group Name:"+ nested.name);
                                            //_tracing.Trace("Nested Group Name:" + childContent);
                                            _nestedValues.Add(nested.name, nested.contentValue);
                                        }
                                        else
                                        {
                                            _nestedValues[nested.name] = nested.contentValue;
                                        }
                                        nested.contentValue = string.Empty;
                                    }                                 
                                }                               
                               //_tracing.Trace($"Repeating Group: {dc.name} doesn't contain nested repeating group");
                                processToken = new TokenProcessor(_tracing, _service, _context, entity,_nestedValues);
                                //add the unique ID in the token on the format before replacing it to avoid duplicate values.
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
