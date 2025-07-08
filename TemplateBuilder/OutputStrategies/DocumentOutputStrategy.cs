using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;
namespace TemplateBuilder.OutputStrategies
{
    public class DocumentOutputStrategy : IOutputStrategy
    {
        private Entity _primaryEntity;
        private readonly IPluginExecutionContext _context;
        private readonly IOrganizationService _service;
        public DocumentOutputStrategy(IPluginExecutionContext context, IOrganizationService service)
        {
            _primaryEntity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet(true));
            _context = context;
            _service = service;
        }
        public void OutputContent(string content, string subject)
        {
            byte[] documentBytes = Encoding.UTF8.GetBytes(content);

            string fileName = subject;

            Entity annotation = new Entity("annotation");
            annotation["subject"] = subject;
            annotation["notetext"] = "Generated Document";
            annotation["documentbody"] = Convert.ToBase64String(documentBytes);
            annotation["objectid"] = _primaryEntity.ToEntityReference();
            annotation["objecttypecode"] = _primaryEntity.LogicalName;
            annotation["filename"] = fileName;

            _service.Create(annotation);
        }
    }
}

