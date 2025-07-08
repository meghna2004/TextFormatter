using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using TemplateBuilder.Enum;

namespace TemplateBuilder.OutputStrategies
{
    public class OutputStrategyFactory
    {
        private readonly IPluginExecutionContext _context;
        private readonly IOrganizationService _service;
        public OutputStrategyFactory(IPluginExecutionContext context, IOrganizationService service)
        {
            _context = context;
            _service = service;
        }
        public IOutputStrategy GetOutputStrategy(TemplateType templateType)
        {
            switch(templateType)
            {
                case TemplateType.Email:
                    return new EmailOutputStrategy(_context,_service);
                case TemplateType.DocumentTemplate:
                    return new DocumentOutputStrategy(_context,_service);
                default:
                    throw new ArgumentOutOfRangeException(nameof(templateType), $"Unsupported template type: {templateType}");
            }
            
        }
    }
}
