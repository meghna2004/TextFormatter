using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using TemplateBuilder.DTO;
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
            //string emailBodyHtml1 = string.Empty;
            string emailDes = string.Empty;
            foreach (var q in _descriptionFormatInfo.queries)
            {
                _tracing.Trace("Inside for each for text desriptionbody in template model");
                string query = processor.ReplaceTokens(q.queryText);
                //retrieve all query placeholders and replace in query text
                _tracing.Trace("Query: " + query);
                (object,string) results = ExecuteFetchAndPopulateValues(q.name,query, q.subSections, q.format);
                q.formatValues = results.Item2;
               // emailBodyHtml1 += q.formatValues;  
                _tracing.Trace("After Execute Fetch and populate values: "+q.formatValues);
                q.subSections = results.Item1 as List<SubSections>;
                foreach (var dc in q.subSections)
                {
                    if (!columnValues.ContainsKey(dc.name))
                    {
                        columnValues.Add(dc.name, dc.contentValue);
                    }
                    else
                    {
                        columnValues[dc.name] += dc.contentValue;
                    }
                }
                /*if (!sectionValues.ContainsKey(q.name))
                {
                    sectionValues.Add(q.name, q.formatValues);
                }
                else
                {
                    sectionValues[q.name] = q.formatValues;
                }*/
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
                //this is to replace the placeholders that are in the same line. such as in a table row.
                /*foreach (var column in columnValues)
                {
                    emailBodyHtml1 = emailBodyHtml1.Replace(column.Key, column.Value);
                }*/
               /* foreach(var section in sectionValues)
                {
                    emailDes = _descriptionFormatInfo.structuredValue.Replace(section.Key, section.Value);
                }*/
                return _descriptionFormatInfo.structuredValue;
            }
            catch
            {
                throw new InvalidPluginExecutionException("Placeholder does not match the name of value to be inserted");
            }
        }
        public (object,string) ExecuteFetchAndPopulateValues(string queryName,string fetchXml, List<SubSections> subSection,string qFormat)
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
                string replacedValues = string.Empty;
                if (retrievedEntities.Entities.Count > 0)
                {
                    _tracing.Trace("Entities count more than 0");
                    TokenProcessor processToken = new TokenProcessor(_tracing, _service, _context, retrievedEntities.Entities[0]);

                    if (!_queryDictionary.ContainsKey(queryName))
                    {
                        _tracing.Trace("Add query and entity to dictionary: "+ queryName+ "ID: "+ retrievedEntities.Entities[0].Id);
                        _queryDictionary.Add(queryName, retrievedEntities.Entities[0]);
                    }
                    //replacedValues = processToken.ReplaceTokens(qFormat);
                    _tracing.Trace("EmailFormatterFunctions: Test 4 "+ _descriptionFormatInfo.structuredValue);
                    //use for loop for (int i=0; i<=entities.Count; i++)
                    foreach (Entity entity in retrievedEntities.Entities)
                    {
                        //Build entity to Dictionary here
                      //  Dictionary<string, List<Dictionary<string, List<Dictionary<string, object>>>>> entityDataModel = new Dictionary<string, List<Dictionary<string, List<Dictionary<string, object>>>>>();
                        processToken = new TokenProcessor(_tracing, _service, _context, entity);
                        foreach (var dc in subSection)
                        {
                            //_tracing.Trace("EmailFormatterFunctions: Test 6");
                            if (!string.IsNullOrEmpty(dc.format))
                            {
                                format = dc.format;
                            }

                            //if nested == true
                                //get groupby field
                                //if(entity.Contains[groupby])
                                   //groupbyValue = entity["groupbyfieldname"];
                                     


                            //get the format of each subsection
                            //_tracing.Trace("Dc Format" + format);
                            /*foreach (var c in dc.content)
                            {
                               */ //call tokenprocessor here
                                  //_tracing.Trace("EmailFormatterFunctions: Test 7" + c.colName);                      
                                  //format = processToken.ReplaceTokens(format);

                            //get the value of the column name for each column inside the sub section
                            /*if (entity.Contains(c.colName))
                            {
                                //replace the text/format with the placeholder (colName) with the actual value
                                if (!string.IsNullOrEmpty(format))
                                {
                                    format = format.Replace(c.colName, (entity[c.colName].ToString()));
                                }
                                else
                                {
                                    format += dc.defaultFormat.Replace("placeholder", entity[c.colName].ToString());
                                }
                            }
                            else
                            {
                                throw new InvalidPluginExecutionException("Column not present in query");
                            }*/
                            //  }
                            //populate the text of the subsection with the replaced values in.
                            dc.contentValue += processToken.ReplaceTokens(format);
                            _tracing.Trace("EmailFormatterFunctions: Test 8");
                        }
                    }
                    //do for each loop for subsections here and pass in the entitydictionary
                }

                return (subSection,replacedValues);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Failed to execute fetch query: " + ex.Message);
            }
        } 
    }
}
