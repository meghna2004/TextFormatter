using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using TemplateBuilder.OutputStrategies;
using TemplateBuilder.Repositories;
using TextFormatter.TemplateBuilder;

namespace TemplateBuilder.Services
{
    public class ContentGeneratorService
    {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;
        private readonly ITracingService _tracingService;
        private readonly TemplateRepository _templateRepo;
        private readonly QueryRepository _queryRepo;
        private readonly OutputStrategyFactory _outputStrategyFactory;

        public ContentGeneratorService(IOrganizationService service, IPluginExecutionContext context,ITracingService tracingService, TemplateRepository templateRepo, QueryRepository queryRepo, OutputStrategyFactory outputStrategyFactory)
        {
            _service = service;
            _context = context;
            _tracingService = tracingService;
            _templateRepo = templateRepo;
            _queryRepo = queryRepo;
            _outputStrategyFactory = outputStrategyFactory;
        }
        public string BuildContent(Guid descriptionID)
        {
            TextDescriptionBodies descriptionFormatInfo =_templateRepo.CreateTemplateModel(descriptionID);
            _tracingService.Trace("Template Model Created");
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            string emailBodyHtml = string.Empty;
            string emailDes = string.Empty;
            foreach (var s in descriptionFormatInfo.sectionClasses)
            {
                _tracingService.Trace("Inside for each for text desriptionbody in template model");
                emailBodyHtml += s.format;
                foreach (var q in s.queryClasses)
                {
                    string query = q.queryText;
                    //retrieve all query placeholders and replace in query text
                   // EmailFormatterFunctions.RetrieveColumnValues(query, _service, q.contentClasses, _context);
                    _queryRepo.ExecuteFetchAndPopulateValues(query,q.contentClasses);
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
                _tracingService.Trace("HTML Body Result: "+ emailBodyHtml);
                return emailBodyHtml;
            }
            catch
            {
                throw new InvalidPluginExecutionException("Placeholder does not match the name of value to be inserted");
            }
        }
    }
}
