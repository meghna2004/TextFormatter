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
        public ContentGeneratorService(IOrganizationService service, IPluginExecutionContext context,ITracingService tracingService, TemplateRepository templateRepo)
        {
            _service = service;
            _context = context;
            _tracing = tracingService;
            _templateRepo = templateRepo;
        }
        public string BuildContent(Guid descriptionID)
        {
            QueryBuilder queryBuilder = new QueryBuilder(_service,_context,_tracing);            
            TextDescriptionBodies descriptionFormatInfo =_templateRepo.CreateTemplateModel(descriptionID);
            _tracing.Trace("Template Model Created");
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            string emailBodyHtml = string.Empty;
            string emailDes = string.Empty;
            foreach (var s in descriptionFormatInfo.sectionClasses)
            {
                _tracing.Trace("Inside for each for text desriptionbody in template model");
                //retrieve the format of the whole section
                emailBodyHtml += s.format;
                foreach (var q in s.queryClasses)
                {
                    //foreach queries inside sections retrieve the placeholders
                    string query = queryBuilder.FormatQueryWithPlaceholders(q.queryText,q.placeholders);
                    //retrieve all query placeholders and replace in query text
                   
                    ExecuteFetchAndPopulateValues(query,q.contentClasses);
                    foreach (var dc in q.contentClasses)
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
                }
            }
            try
            {
                //this is to replace the placeholders that are in the same line. such as in a table row.
                foreach (var column in columnValues)
                {
                    emailBodyHtml = emailBodyHtml.Replace(column.Key, column.Value);
                }
                return emailBodyHtml;
            }
            catch
            {
                throw new InvalidPluginExecutionException("Placeholder does not match the name of value to be inserted");
            }
        }
        public object ExecuteFetchAndPopulateValues(string fetchXml, List<SubSections> subSection)
        {
            XmlHelper xmlHelper = new XmlHelper();
            TokenProcessor processToken = new TokenProcessor(_tracing);
            //_tracing.Trace("Format the query using XML Helper");
            string formattedQuery = xmlHelper.ExtractFetchQuery(fetchXml);
            try
            {
               // _tracing.Trace("Retrieve records from fetchQuery");
                EntityCollection retrievedEntities = _service.RetrieveMultiple(new FetchExpression(formattedQuery));
                string format = string.Empty;
                _tracing.Trace("EmailFormatterFunctions: Test 4");
                foreach (Entity entity in retrievedEntities.Entities)
                {
                    //foreach records retrieved from the query
                    //redo this bit to use tokens.
                    //_tracing.Trace("EmailFormatterFunctions: Test 5");
                    foreach (var dc in subSection)
                    {
                        //_tracing.Trace("EmailFormatterFunctions: Test 6");
                        if (!string.IsNullOrEmpty(dc.format))
                        {
                            format = dc.format;
                        }
                        //get the format of each subsection
                        //_tracing.Trace("Dc Format" + format);
                        foreach (var c in dc.content)
                        {
                            //call tokenprocessor here
                            //_tracing.Trace("EmailFormatterFunctions: Test 7" + c.colName);                      
                            format = processToken.ReplaceTokens(c.colName, format, entity);

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
                        }
                        //populate the text of the subsection with the replaced values in.
                        dc.contentValue += format;
                        _tracing.Trace("EmailFormatterFunctions: Test 8");
                    }
                }
                return subSection;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Failed to execute fetch query: " + ex.Message);
            }
        } 
    }
}
