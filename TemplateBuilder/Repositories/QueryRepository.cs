using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TemplateBuilder.DTO;
using TemplateBuilder.Utilities;

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
