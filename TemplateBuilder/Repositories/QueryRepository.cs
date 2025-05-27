using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TemplateBuilder.DTO;

namespace TemplateBuilder.Repositories
{
    public class QueryRepository
    {
        private readonly IOrganizationService _service;
        private readonly ITracingService _tracing;

        public QueryRepository(IOrganizationService service, ITracingService tracing)
        {
            _service = service;
            _tracing = tracing;
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
        public string FormatQueryWithPlaceholders(string fetchXML, List<QueryPlaceholders> placeholders)
        {
            //Check placeholder name field with placeholder text inside fetchXMLtext inside {}.
            //Retrive values as specified.
            /*
             * if(qp.Value ==null)
             *   if(qp.valueFrom == primaryEntity) -->pass in the trigger entity
             *     try
             *       if(triggerEntity.Contains("{qp.name}")
             *          qp.Value = triggerEntity["qp.name"].Value.ToString()
             *            
             */
            //Need to use tokens here as well.
            string formattedQuery = string.Empty;
           foreach(var qp in placeholders)
            {
               fetchXML.Replace(qp.name, qp.value);
            }
            return fetchXML;
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
