using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.IO;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;

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
        public void OutputContent(string content,string subject)
        {
            //byte[] documentBytes = Encoding.UTF8.GetBytes(content);
            //string documentBodyBase64 = Convert.ToBase64String(documentBytes);
            byte[] documentBytes;
            string fileName = subject + ".html";
            string mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            using (MemoryStream stream = new MemoryStream())
            {
                using(WordprocessingDocument wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());
                    Paragraph subjectParagraph = new Paragraph(new Run(new Text(subject)));
                    Paragraph separatorParagraph = new Paragraph(new Run(new Text("---")));
                    body.AppendChild(separatorParagraph);

                    // Add the raw HTML content as a paragraph.
                    // IMPORTANT: This will not render the HTML as rich text.
                    // The HTML tags themselves will be visible as text in the DOCX.
                    Paragraph contentParagraph = new Paragraph(new Run(new Text(content)));
                    body.AppendChild(contentParagraph);

                }
            }
           // string mimeType = "text/html";

            // Example: Create Note (Annotation) with the document
            Entity annotation = new Entity("annotation");
            annotation["subject"] = subject;
            annotation["notetext"] = "Generated Document";
           // annotation["documentbody"] = Convert.ToBase64String(documentBytes);
            annotation["objectid"] = _primaryEntity.ToEntityReference();
            annotation["objecttypecode"] = _primaryEntity.LogicalName;
            annotation["filename"] = fileName;
            annotation["mimetype"] = mimeType;
            _service.Create(annotation);
        }       
    }
}

