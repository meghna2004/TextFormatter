using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TemplateBuilder.Utilities
{

    public class TokenProcessor
    {
        private readonly IOrganizationService _service;
        private readonly IPluginExecutionContext _context;
        private readonly ITracingService _tracing;
        private readonly Entity _primaryEntity;
        private readonly string _primaryEntityName;
        private  Entity _entity;
        private Dictionary<string, Entity> _entityDictionary;
        private Dictionary<string, string> _sectionDictionary;
        private bool _tdbOrQuery;

        private readonly string _pattern = @"(?<!{){{((?:[\w]+-)?[\w .]*)(:[^}]+)*}}(?!})";

        public TokenProcessor(ITracingService tracing, IOrganizationService service, IPluginExecutionContext context, Entity entity)
        {
            _service = service;
            _context = context;
            _tracing = tracing;
            _entity = entity;
        }
        public TokenProcessor(ITracingService tracing, IOrganizationService service, IPluginExecutionContext context, Dictionary<string,Entity> entityDictionary, Dictionary<string,string> sectionDictionary)
        {
            _service = service;
            _context = context;
            _tracing = tracing;
            _entityDictionary = entityDictionary;
            _sectionDictionary = sectionDictionary;
            _tdbOrQuery = true;
        }
        public TokenProcessor(ITracingService tracing, IOrganizationService service, IPluginExecutionContext context)
        {
            _service = service;
            _context = context;
            _tracing = tracing;
            _primaryEntity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet(true));
            _primaryEntityName = _primaryEntity.LogicalName;
            _tdbOrQuery = true;
        }
        /// <summary>              
        /// _entity = _primaryEntity;
        /// Replaces curly brace values with the attribute values from the entity results
        /// </summary>
        /// <param name="fetchXML"></param>
        /// <returns></returns>
        //Update ReplaceTokesn to take into Account the QueryDictionary.
        public string ReplaceTokens(string text)
        {
            _tracing.Trace("Start of Replace Token Functions");

            // Accept format strings in the format
            // {attributeLogicalName} or {attribtueLogicalName:formatstring}
            // Where formatstring is a standard String.format format string e.g. {course.date:dd MMM yyyy}
            var result = Regex.Replace(text, _pattern, (match) =>
            {
                _tracing.Trace("Inside var result");
                //_tracing.Trace("Text: "+text);
                var fulltoken = match.Groups[1].Value;
                var attributeName = string.Empty;
                var queryName = string.Empty;
                if(_tdbOrQuery)
                {
                    if (_entityDictionary != null)
                    {
                        _tracing.Trace("Replacing for TBD structure: "+ fulltoken);
                        var parts = fulltoken.Split('-');
                        _tracing.Trace("Parts: "+ parts);
                        if (parts.Length > 1)
                        {
                            _tracing.Trace("Parts received");
                            queryName = parts[0];
                            _tracing.Trace("Query: "+ queryName);
                            if (_entityDictionary.ContainsKey(queryName))
                            {
                                _tracing.Trace("Entity from querydictionary received");
                                _entity = _entityDictionary[queryName];
                                _tracing.Trace("Entity: "+_entity.Id);
                            }
                            else
                            {
                                _tracing.Trace("Entity not present in the Query");
                            }
                            attributeName = parts[1];
                        }                        
                    }
                    else
                    {
                        _tracing.Trace("Replacing for Query");
                        var parts = fulltoken.Split('.');
                        if (parts.Length > 1)
                        {
                            Entity er = GetEntityReferenceRecord(parts[0]);
                            if (er != null)
                            {
                                _entity = er;
                            }
                            attributeName = parts[1];
                        }
                        else
                        {
                            _entity = _primaryEntity;
                            attributeName = match.Groups[1].Value.Trim();
                        }
                    }
                }
                else
                {
                    _tracing.Trace("Replacing for normal attribute in subsection");
                    //Check if there is a group by and then call the replace token function again. 
                    attributeName = match.Groups[1].Value.Trim();
                }
                // Try get the query
                var format = match.Groups[2].Value;
                _tracing.Trace("Format: "+format);
                _tracing.Trace("Token: " + attributeName);

                // Check if there is an attribute value
                //Need to put this into its own function. this function should take in the attribute name and a bool called repeating. If the repeating is true it should call itself again. but we also need the group by entity to 
                if (_entity != null)
                {
                    if (_entity.Contains(attributeName))
                    {
                        _tracing.Trace("Getting valuetype of token");

                        var value = _entity[attributeName];
                        var type = value.GetType().Name;
                        // If aliased value, get the real value
                        if (type == "AliasedValue")
                        {
                            value = (value as AliasedValue).Value;
                            type = value.GetType().Name;
                        }

                        switch (type)
                        {
                            case "Decimal":
                            case "Integer":
                            case "DateTime":
                                // No need to change the value
                                break;
                            case "EntityReference":
                                value = (value as EntityReference).Id;
                                break;
                            case "Guid":
                                value = (Guid)value;
                                break;
                            default:
                                value = value.ToString();
                                break;
                        }


                        // Use String.Format to get the additional formatting options - e.g. dates and numeric formatting
                        _tracing.Trace("Replacing the token value: " + value);
                        if (!string.IsNullOrEmpty(format))
                        {
                            // 'format' is match.Groups[2].Value, which contains the colon and format specifier (e.g., ": dd/MMM/yyyy")
                            // Construct the standard .NET format string: "{0: dd/MMM/yyyy}"
                            string formatPattern = "{0" + format + "}";
                            // _tracing.Trace("Format Pattern for String.Format: " + formatPattern);

                            var formattedResult = string.Format(formatPattern, value);
                            //_tracing.Trace("Formatted Result: " + formattedResult);
                            return formattedResult;
                        }
                        string replacementValue = value?.ToString() ?? "";
                        return replacementValue;
                    }
                }

                if(_sectionDictionary.ContainsKey(attributeName.Trim()))
                {
                    _tracing.Trace("Getting the subsection from the Queryname");
                    return _sectionDictionary[attributeName];
                }
                else
                {
                    _tracing.Trace("SectionDictionary doesn't contain a key with attribute name: " + attributeName);
                }
                return "";
            });
            return ReplaceFormatLogic(result);
        }
        public string ReplaceFormatLogic(string result)
        {
            // Add additional template logic 
            // {{delimeter:, }} - Will only add the string ", " if there is a value before and after
            // {{suffix: Days}} - Will only add the string " Days" if there is a value before
            string pattern = @"\[\[(delimeter|suffix|blankif|usefirst):([\w\W]*?)\]\]";

            Match match = Regex.Match(result, pattern);
            while (match.Success)
            {
                int index = match.Index;
                int endIndex = match.Index + match.Length;
                var before = index > 0 ? result.Substring(0, index) : null;
                var after = endIndex < result.Length ? result.Substring(endIndex) : null;
                var replaceWith = "";
                switch (match.Groups[1].Value)
                {
                    case "delimeter":
                        // If the preceding or following text is blank then don't output the delimiter
                        if (before != null && !before.EndsWith("}}") && after != null && !after.StartsWith("{{"))
                        {
                            replaceWith = match.Groups[2].Value;
                        }
                        result = result.Substring(0, index) + replaceWith + result.Substring(endIndex);
                        break;
                    case "suffix":
                        // If the preceding text is blank, then don't output the suffix
                        if (before != null && !before.EndsWith("}}"))
                        {
                            replaceWith = match.Groups[2].Value;
                        }
                        result = result.Substring(0, index) + replaceWith + result.Substring(endIndex);
                        break;
                    case "blankif":
                        // If the following text matches the blank if text, then blank the whole string
                        // The values are pipe delimetered
                        var valuesToBlank = match.Groups[2].Value.Split('|');
                        result = result.Substring(0, index) + result.Substring(endIndex);
                        foreach (string value in valuesToBlank)
                        {
                            if (after.StartsWith(value))
                            {
                                result = after.Replace(value, "");
                            }
                        }

                        break;
                    case "usefirst":
                        if (after.IndexOf('|') == 0)
                            result = after.Substring(after.IndexOf('|') + 1);
                        else
                            result = after.Substring(0, after.IndexOf('|'));
                        break;
                }
                match = Regex.Match(result, pattern);
            }

            return result;
        }
        public virtual Entity GetEntityReferenceRecord(string entityName)
        {
            RetrieveEntityRequest retrieveEntityRequest = new RetrieveEntityRequest
            {
                LogicalName = _primaryEntityName,
                EntityFilters = EntityFilters.Attributes
            };
            RetrieveEntityResponse retrieveEntityResponse = (RetrieveEntityResponse)_service.Execute(retrieveEntityRequest);
            EntityMetadata primaryEntityMetadata = retrieveEntityResponse.EntityMetadata;
            _tracing.Trace("Getting primaryEntity Metadata");
            // Step 2: Identify lookup attributes that point to the desired referenced entity
            var lookupAttributes = primaryEntityMetadata.Attributes
                .Where(attr => attr.AttributeType == AttributeTypeCode.Lookup || attr.AttributeType == AttributeTypeCode.Customer || attr.AttributeType == AttributeTypeCode.Owner)
                .Cast<LookupAttributeMetadata>()
                .Where(lookupAttr => lookupAttr.Targets.Contains(entityName))
                .ToList();
            ColumnSet columnSet = new ColumnSet(lookupAttributes.Select(attr => attr.LogicalName).ToArray());
            Entity primaryEntityRecord = _service.Retrieve(_primaryEntityName, _primaryEntity.Id, columnSet);
            _tracing.Trace("LookupAttributes Retrieved");
            // Step 4: Iterate through the retrieved attributes to find the EntityReference
            foreach (var attributeName in columnSet.Columns)
            {
                if (primaryEntityRecord.Contains(attributeName) && primaryEntityRecord[attributeName] is EntityReference entityReference)
                {
                    if (entityReference.LogicalName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
                    {
                        _tracing.Trace("Getting the Entity Reference record.");
                        return _service.Retrieve(entityName, entityReference.Id, new ColumnSet(true));
                    }
                }
            }
            _tracing.Trace("Entity Reference Record not there");
            return null;
        }
    }
}
