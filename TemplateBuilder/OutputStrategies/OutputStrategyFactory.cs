using System;
using TemplateBuilder.Enum;

namespace TemplateBuilder.OutputStrategies
{
    public class OutputStrategyFactory
    {
        public IOutputStrategy GetOutputStrategy(TemplateType templateType)
        {
            switch(templateType)
            {
                case TemplateType.Email:
                    return new EmailOutputStrategy();
                case TemplateType.DocumentTemplate:
                    return new DocumentOutputStrategy();
                default:
                    throw new ArgumentOutOfRangeException(nameof(templateType), $"Unsupported template type: {templateType}");
            }
            
        }
    }
}
