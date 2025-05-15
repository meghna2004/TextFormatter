using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateBuilder.OutputStrategies
{
    public interface IOutputStrategy
    {
        void OutputContent(string content, IPluginExecutionContext context);
    }
}
