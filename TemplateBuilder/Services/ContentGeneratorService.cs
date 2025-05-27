using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TemplateBuilder.DTO;
using TemplateBuilder.OutputStrategies;
using TemplateBuilder.Repositories;

namespace TemplateBuilder.Services
{
    public class ContentGeneratorService
    {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;
        private readonly ITracingService _tracing;
        private readonly TemplateRepository _templateRepo;
        //private readonly QueryRepository _queryRepo;
        private readonly OutputStrategyFactory _outputStrategyFactory;

        public ContentGeneratorService(IOrganizationService service, IPluginExecutionContext context,ITracingService tracingService, TemplateRepository templateRepo,OutputStrategyFactory outputStrategyFactory)
        {
            _service = service;
            _context = context;
            _tracing = tracingService;
            _templateRepo = templateRepo;
            //_queryRepo = queryRepo;
            _outputStrategyFactory = outputStrategyFactory;
        }
        public string BuildContent(Guid descriptionID)
        {
            TextDescriptionBodies descriptionFormatInfo =_templateRepo.CreateTemplateModel(descriptionID);
            _tracing.Trace("Template Model Created");
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            string emailBodyHtml = string.Empty;
            string emailDes = string.Empty;
            foreach (var s in descriptionFormatInfo.sectionClasses)
            {
                _tracing.Trace("Inside for each for text desriptionbody in template model");
                emailBodyHtml += s.format;
                foreach (var q in s.queryClasses)
                {
                    string query = q.queryText;
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
                foreach (var column in columnValues)
                {
                    emailBodyHtml = emailBodyHtml.Replace(column.Key, column.Value);
                }
                //_tracingService.Trace("HTML Body Result: "+ emailBodyHtml);
                return emailBodyHtml;
            }
            catch
            {
                throw new InvalidPluginExecutionException("Placeholder does not match the name of value to be inserted");
            }
        }
        public object ExecuteFetchAndPopulateValues(string fetchXml, List<SubSections> subSection)
        {
            string formattedQuery = GetFetchQuery(fetchXml);
            try
            {
                EntityCollection retrievedEntities = _service.RetrieveMultiple(new FetchExpression(formattedQuery));
                string value = string.Empty;
                string format = string.Empty;
                _tracing.Trace("EmailFormatterFunctions: Test 4");
                foreach (Entity entity in retrievedEntities.Entities)
                {
                    //redo this bit to use tokens.
                    _tracing.Trace("EmailFormatterFunctions: Test 5");
                    foreach (var dc in subSection)
                    {
                        _tracing.Trace("EmailFormatterFunctions: Test 6");
                        if (!string.IsNullOrEmpty(dc.format))
                        {
                            format = dc.format;
                        }
                        _tracing.Trace("Dc Format" + format);
                        foreach (var c in dc.content)
                        {
                            _tracing.Trace("EmailFormatterFunctions: Test 7" + c.colName);
                            if (entity.Contains(c.colName))
                            {
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
                            }
                        }
                        value += format;
                        dc.contentValue = value;
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
        public string GetFetchQuery(string queryXml)
        {
            try
            {
                XmlReader reader = XmlReader.Create(new StringReader(queryXml));
                while (reader.ReadToFollowing("fetch"))
                {
                    queryXml = reader.ReadOuterXml();
                }
                return queryXml;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Invalid Layout Query Format. Must be in the fetch query format", ex);
            }
        }
    }
}
