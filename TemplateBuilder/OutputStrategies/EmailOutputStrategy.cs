using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
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
        private Entity _primaryEntity;
        private readonly IPluginExecutionContext _context;
        private readonly IOrganizationService _service;
        public EmailOutputStrategy(IPluginExecutionContext context, IOrganizationService service)
        {
            _primaryEntity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet(true));
            _context = context;
            _service = service;
        }
        public void OutputContent(string content,string subject)
        {
            Email email = new Email()
            {
                Description = content,
                Subject = subject,
                RegardingObjectId = _primaryEntity.ToEntityReference()
            };
            _service.Create(email);
        }
    }
}
