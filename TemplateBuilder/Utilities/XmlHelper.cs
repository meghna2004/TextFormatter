using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TemplateBuilder.Utilities
{
    public static class XmlHelper
    {
        public static string ExtractFetchQuery(string xmlQuery)
        {
            try
            {
                XmlReader reader = XmlReader.Create(new StringReader(xmlQuery));

                while (reader.ReadToFollowing("fetch"))
                {
                    xmlQuery = reader.ReadOuterXml();
                }
                return xmlQuery;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Invalid Layout Query Format. Must be in the fetch query format", ex);
            }
        }
    }
}
