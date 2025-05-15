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
        private readonly TemplateRepository _templateRepo;
        private readonly QueryRepository _queryRepo;
        private readonly OutputStrategyFactory _outputStrategyFactory;

        public ContentGeneratorService(IOrganizationService service, IPluginExecutionContext context, TemplateRepository templateRepo, QueryRepository queryRepo, OutputStrategyFactory outputStrategyFactory)
        {
            _service = service;
            _context = context;
            _templateRepo = templateRepo;
            _queryRepo = queryRepo;
            _outputStrategyFactory = outputStrategyFactory;
        }
        public string BuildContent(Guid descriptionID)
        {
            TextDescriptionBodies descriptionFormatInfo =_templateRepo.CreateTemplateModel(descriptionID);
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            string emailBodyHtml = string.Empty;
            string emailDes = string.Empty;
            foreach (var s in descriptionFormatInfo.sectionClasses)
            {
                emailBodyHtml += s.format;
                foreach (var q in s.queryClasses)
                {
                    string query = q.queryText;
                    //retrieve all query placeholders and replace in query text
                   // EmailFormatterFunctions.RetrieveColumnValues(query, _service, q.contentClasses, _context);
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
                return emailBodyHtml;
            }
            catch
            {
                throw new InvalidPluginExecutionException("Placeholder does not match the name of value to be inserted");
            }
        }
    }
}
