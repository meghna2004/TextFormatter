using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GemBox.Document;
using HtmlToOpenXml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Net.Http;
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
            //string documentBodyBase64 = Convert.ToBase64String(documentBytes);
            //byte[] documentBytes;
            string fileName = subject;
            string mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            /*using (MemoryStream ms = new MemoryStream())
            {
                using(WordprocessingDocument document = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = document.AddMainDocumentPart();
                    mainPart.Document = new Document(new Body());
                    HtmlConverter converter = new HtmlConverter(mainPart);
                    var paragraphs = converter.Parse(content);
                    foreach (var p in paragraphs)
                    {
                        mainPart.Document.Body.Append(p);
                    }
                }
                documentBytes = ms.ToArray();
            }*/
           /* var doc = DocumentModel.Load(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)), LoadOptions.HtmlDefault);
            using (MemoryStream ms = new MemoryStream())
            {
                doc.Save(ms,SaveOptions.DocxDefault);
                documentBytes = ms.ToArray();
            }*/
            // string mimeType = "text/html";

            // Example: Create Note (Annotation) with the document
            Entity annotation = new Entity("annotation");
            annotation["subject"] = subject;
            annotation["notetext"] = "Generated Document";
            annotation["documentbody"] = Convert.ToBase64String(documentBytes);
            annotation["objectid"] = _primaryEntity.ToEntityReference();
            annotation["objecttypecode"] = _primaryEntity.LogicalName;
            annotation["filename"] = fileName;
            //annotation["mimetype"] = mimeType;
            _service.Create(annotation);
        }
    }
}

