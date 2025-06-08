using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TemplateBuilder.DTO;
namespace TextFormatter.TemplateBuilder
{
    public class EmailFormatterFunctions:PluginBase
    {
        //function to run the fetchqueries retrieved from the template and get the personalised information from it to replace the placeholders in the template with.
        public static object RetrieveColumnValues(string fetchQuery, IOrganizationService service,List<SubSections> DC, LocalPluginContext localcontext)
        {
            var getQueries = EmailFormatterFunctions.GetFetchQuery(fetchQuery);
            localcontext.Trace("EmailFormatterFunctions: Test 1");           
            string runQuery = string.Format(getQueries);                       
            localcontext.Trace("EmailFormatterFunctions: Test 2");
            EntityCollection retreivedEntities = service.RetrieveMultiple(new FetchExpression(runQuery));
            localcontext.Trace("EmailFormatterFunctions: Test 3");
            string value = string.Empty;
            string format= string.Empty;
            localcontext.Trace("EmailFormatterFunctions: Test 4");
            foreach (Entity entity in retreivedEntities.Entities)
            {                              
                localcontext.Trace("EmailFormatterFunctions: Test 5");
                foreach (var dc in DC)
                {
                    localcontext.Trace("EmailFormatterFunctions: Test 6");
                    if (!string.IsNullOrEmpty(dc.format))
                    {
                         format = dc.format;
                    }                                       
                    localcontext.Trace("Dc Format"+ format);
                    foreach (var c in dc.content)
                    {
                        localcontext.Trace("EmailFormatterFunctions: Test 7"+c.colName);
                        if (entity.Contains(c.colName))
                        {
                            if (!string.IsNullOrEmpty(format))
                            {
                                format = format.Replace(c.colName, (entity[c.colName].ToString()));
                            }
                            else
                            {
                             //   format += dc.defaultFormat.Replace("placeholder", entity[c.colName].ToString());
                            }
                        }
                        else
                        {
                            throw new InvalidPluginExecutionException("Column not present in query");
                        }                                        
                    }
                    value += format;
                    dc.contentValue = value;
                    localcontext.Trace("EmailFormatterFunctions: Test 8");
                }
            }
            return DC;
        }
        // takes the fetchquery text as input and reads the XML in it and out puts the query as a string.
        public static string GetFetchQuery(string queryXml)
        {
            try
            {
                XmlReader reader = XmlReader.Create(new StringReader(queryXml));
              
                while (reader.ReadToFollowing("fetch"))
                {                  
                    queryXml = reader.ReadOuterXml();
                }               
                return queryXml;
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Invalid Layout Query Format. Must be in the fetch query format", ex);
            }
        }
        // This function is to get all the necessary information from the template and recreate the data into code how it is retrieved from the databas for better data manipulation.
        public static TextDescriptionBodies RetrieveTemplateInfo(Guid descriptionID,IOrganizationService service,LocalPluginContext localcontext)
        {
            string getNecessaryInfo = string.Format(@"<fetch>
                                                          <entity name='vig_section'>
                                                            <attribute name='vig_format' />
                                                            <attribute name='vig_sequence' />
                                                            <filter>
                                                              <condition attribute='vig_emaildescriptionid' operator='eq' value='{0}' />
                                                            </filter>
                                                            <order attribute='vig_sequence' />
                                                            <link-entity name='vig_query' from='vig_sectionid' to='vig_sectionid' link-type='inner' alias='Q'>
                                                              <attribute name='vig_fetchquery' />
                                                              <attribute name='vig_fetchsequence' />
                                                              <order attribute='vig_fetchsequence' />
                                                              <link-entity name='vig_dynamiccontent' from='vig_queryid' to='vig_queryid' link-type='inner' alias='DC'>
                                                                <attribute name='vig_name' />
                                                                <attribute name='vig_contentsequence' />
                                                                <attribute name='vig_format' />
                                                                <order attribute='vig_contentsequence' />
                                                                <link-entity name='vig_column' from='vig_dynamiccontentid' to='vig_dynamiccontentid' link-type='inner' alias='Col'>
                                                                  <attribute name='vig_columnlogicalname' />
                                                                  <attribute name='vig_sequence' />
                                                                  <order attribute='vig_sequence' />
                                                                </link-entity>
                                                              </link-entity>
                                                            </link-entity>
                                                          </entity>
                                                        </fetch>", descriptionID.ToString());
            EntityCollection sectionsEntity = service.RetrieveMultiple(new FetchExpression(getNecessaryInfo));
            var emailDescription = new TextDescriptionBodies
            {
                sectionClasses = new List<Sections>()
            };
            foreach (Entity entity in sectionsEntity.Entities)
            {
                localcontext.Trace("CustomEmails: Test 1.5");
                var sectionSeq = entity.GetAttributeValue<int>("vig_sequence");
                localcontext.Trace("CustomEmails: Test 1.6");
                int querySeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("Q.vig_fetchsequence").Value);
                localcontext.Trace("CustomEmails: Test 1.7");
                int dcSeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("DC.vig_contentsequence").Value);
                localcontext.Trace("CustomEmails: Test 1.8");
                int columnSeq = Convert.ToInt32(entity.GetAttributeValue<AliasedValue>("Col.vig_sequence").Value);
                localcontext.Trace("CustomEmails: Test 1.9");
                var fetchQuery = string.Format(entity.GetAttributeValue<AliasedValue>("Q.vig_fetchquery").Value.ToString());
                localcontext.Trace("CustomEmails: Test 2");
                var colLogicalName = entity.GetAttributeValue<AliasedValue>("Col.vig_columnlogicalname").Value.ToString();
                localcontext.Trace("CustomEmails: Test 2.1");
                var secFormat = entity.GetAttributeValue<string>("vig_format");
                var dcFormat = string.Empty;
                var dcName = entity.GetAttributeValue<AliasedValue>("DC.vig_name").Value.ToString();
                if (entity.Contains("DC.vig_format"))
                {
                     dcFormat = entity.GetAttributeValue<AliasedValue>("DC.vig_format").Value.ToString();
                }                
                localcontext.Trace("CustomEmails: Test 2.2");
                var section = emailDescription.sectionClasses.FirstOrDefault(s => s.sequence == sectionSeq);
                localcontext.Trace("Section Sequence" + entity["vig_sequence"].ToString());
                if (section == null)
                {
                    section = new Sections
                    {
                        sequence = sectionSeq,
                        format = secFormat,
                        queryClasses = new List<Queries>()

                    };
                    emailDescription.sectionClasses.Add(section);
                }
                var query = section.queryClasses.FirstOrDefault(q => q.sequence == querySeq);
                if (query == null)
                {
                    query = new Queries
                    {
                        sequence = querySeq,
                        contentClasses = new List<SubSections>(),
                        queryText = fetchQuery
                    };
                    section.queryClasses.Add(query);
                }
                var dynamicContent = query.contentClasses.FirstOrDefault(c => c.sequence == dcSeq);
                if (dynamicContent == null)
                {
                    dynamicContent = new SubSections
                    {
                        sequence = dcSeq,
                        content = new List<Columns>(),
                        name = dcName,
                       format= dcFormat
                    };
                    query.contentClasses.Add(dynamicContent);
                }
                dynamicContent.content.Add(new Columns
                {
                    sequence = columnSeq,
                    colName = colLogicalName
                });
                localcontext.Trace("DC Name:"+dynamicContent.name);
            }
            return emailDescription;                                          
        }
        // The objects of the classes made to recreate the data structure then used to generate the email descrition text with the replaced value.
        public static string GenerateDescriptionBody(Guid descriptionID, IOrganizationService service, LocalPluginContext localcontext)
        {
            TextDescriptionBodies descriptionFormatInfo = RetrieveTemplateInfo(descriptionID, service, localcontext);
            Dictionary<string, string> columnValues = new Dictionary<string, string>();
            string emailBodyHtml = string.Empty;
            string emailDes = string.Empty;
            foreach (var s in descriptionFormatInfo.sectionClasses)
            {
                emailBodyHtml += s.format;
                localcontext.TracingService.Trace(s.format);
                foreach (var q in s.queryClasses)
                {
                    string query = q.queryText;
                    localcontext.Trace("Query:" + query);
                    localcontext.Trace("CustomEmails fetchqueries retrieved");
                    //retrieve all query placeholders and replace in query text
                    EmailFormatterFunctions.RetrieveColumnValues(query, service, q.contentClasses, localcontext);
                    foreach (var dc in q.contentClasses)
                    {
                        localcontext.Trace("Valeus:" + dc.contentValue);
                        localcontext.Trace("CustomEmails: Test 2.3");

                        if (!columnValues.ContainsKey(dc.name))
                        {
                            columnValues.Add(dc.name, dc.contentValue);
                        }
                        else
                        {
                            columnValues[dc.name] += dc.contentValue;
                        }
                    }
                }
            }
            try
            {
                foreach (var column in columnValues)
                {
                    emailBodyHtml = emailBodyHtml.Replace(column.Key, column.Value);
                }
                return emailBodyHtml;
            }
            catch
            {
                throw new InvalidPluginExecutionException("Placeholder does not match the name of value to be inserted");
            }
        }
    }
}
