using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateBuilder.OutputStrategies
{
    public class DocumentOutputStrategy : IOutputStrategy
    {
        public void OutputContent(string content, IPluginExecutionContext context, IOrganizationService service)
        {

            // Example: Create Note (Annotation) with the document
            var annotation = new Entity("annotation");
            annotation["notetext"] = content;

            service.Create(annotation);
        }
    }
}

