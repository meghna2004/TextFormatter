using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextFormatter.TemplateBuilder;

namespace TemplateBuilder.OutputStrategies
{
    public class EmailOutputStrategy : IOutputStrategy
    {
         public void OutputContent(string content, IPluginExecutionContext context,IOrganizationService service)
        {
            Email email = new Email()
            {
                Description = content,
            };
            service.Create(email);
        }
    }
}
