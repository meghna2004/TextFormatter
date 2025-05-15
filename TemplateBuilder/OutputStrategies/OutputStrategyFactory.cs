using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateBuilder.Enum;
using TextFormatter.TemplateBuilder;

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
                case TemplateType.Document:
                    return new DocumentOutputStrategy();
                default:
                    throw new ArgumentOutOfRangeException(nameof(templateType), $"Unsupported template type: {templateType}");
            }
            
        }
    }
}
