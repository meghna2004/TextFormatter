using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

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
        private Dictionary<string, string> _nestedDictionary;

        private bool _tdbOrQuery;

        private readonly string _pattern = @"(?<!{){{((?:[\w]+-)?[\w .]*)(:[^}]+)*}}(?!})";

        public TokenProcessor(ITracingService tracing, IOrganizationService service, IPluginExecutionContext context, Entity entity)
        {
            _service = service;
            _context = context;
            _tracing = tracing;
            _entity = entity;
        }
        public TokenProcessor(ITracingService tracing, IOrganizationService service, IPluginExecutionContext context, Entity entity, Dictionary<string, string> nestedDictionary)
        {
            _service = service;
            _context = context;
            _tracing = tracing;
            _entity = entity;
            _nestedDictionary = nestedDictionary;
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
        
        //split into different methods with different patterns.
       /* public string ReplaceTokens(string text)
        {
            _tracing.Trace("Start of Replace Token Functions");

            // Accept format strings in the format
            // {{attributeLogicalName}} or {{attribtueLogicalName:formatstring}} or {{queryName-repeatingGroupname}} or {{queryName-attributeLogicalName}}
            // Where formatstring is a standard String.format format string e.g. {{course.date:dd MMM yyyy}}
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
                    attributeName = match.Groups[1].Value.Trim();
                }
                // Try get the query
                var format = match.Groups[2].Value;
                _tracing.Trace("Format: "+format);
                _tracing.Trace("Token: " + attributeName);

                // Check if there is an attribute value
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
                            var formattedResult = string.Format(formatPattern, value);
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
        }*/
        public string ReplaceTokens(string text)
        {
            _tracing.Trace("Start of Replace Token Functions");

            var result = Regex.Replace(text, _pattern, match =>
            {
                _tracing.Trace("Inside Regex match evaluation");

                string fullToken = match.Groups[1].Value;
                _tracing.Trace("Fulltoken retrieved");

                string format = match.Groups[2].Value;
                _tracing.Trace("format retrieved");

                // Decide what entity and attribute the token maps to
                string attributeName = ResolveEntityAndAttribute(fullToken);

                if (_entity != null)
                {
                    return FormatEntityValue(attributeName, format);
                }
                return ResolveSectionValue(attributeName);       
                // If not found in entity, check section dictionary
            });

            return ReplaceFormatLogic(result);
        }

        private string  ResolveEntityAndAttribute(string fullToken)
        {
            string attributeName = string.Empty;
            _tracing.Trace("Inside Resolve Entity and Attribute");

            if (_tdbOrQuery)
            {
                if (_entityDictionary != null)
                {
                    _tracing.Trace("entity Dictionary is not null");

                    // Case: {{queryName-attribute}}
                    (_entity, attributeName) = ResolveFromEntityDictionary(fullToken);
                    _tracing.Trace("Resolve From entity dictionary completed");

                }
                else
                {
                    _tracing.Trace("entity Dictionary is null");

                    // Case: {{entity.attribute}} or {{attribute}}
                    (_entity, attributeName) = ResolveFromQuery(fullToken);
                    _tracing.Trace("Resolve from query completed");

                }
            }
            else
            {
                _tracing.Trace("not tdbOrQuery"+ fullToken.Trim());

                // Case: plain {{attribute}}
               // entity = _primaryEntity;
                attributeName = fullToken.Trim();
            }

            return attributeName;
        }
        private (Entity entity, string attributeName) ResolveFromEntityDictionary(string token)
        {
            _tracing.Trace("Resolve From Entity Dictionary");

            var parts = token.Split('-');
            if (parts.Length > 1)
            {
                string queryName = parts[0];
                _tracing.Trace("Resolve from entity dictionary: query name retrieved");

                string attributeName = parts[1];
                _tracing.Trace("Resolve from entity dicitonary: attribute name retrieved.");

                if (_entityDictionary.ContainsKey(queryName))
                {
                    _tracing.Trace($"Entity found in dictionary for {queryName}");
                    return (_entityDictionary[queryName], attributeName);
                }

                _tracing.Trace($"Entity not found for query {queryName}");
                return (null, attributeName);
            }
            return (null, token);
        }
        private (Entity entity, string attributeName) ResolveFromQuery(string token)
        {
            _tracing.Trace("Resolve From Query");

            var parts = token.Split('.');
            if (parts.Length > 1)
            {
                _tracing.Trace("Resolve from query: parts from token is present.");

                Entity er = GetEntityReferenceRecord(parts[0]);
                if (er != null)
                {
                    _entity = er;
                }
                return (_entity, parts[1]);
            }

            // If just attribute name, default to primary entity
            return (_primaryEntity, token.Trim());
        }
        private string FormatEntityValue(string attributeName, string format)
        {
            _tracing.Trace("Format Entity Value " +attributeName);
            _tracing.Trace("Entity: " + _entity.Id.ToString());
            if (!_entity.Contains(attributeName))
            {
                _tracing.Trace("attribute not present in entity");
                return ResolveSectionValue(attributeName);
            }

            /*object value = _entity[attributeName];
            if (value is AliasedValue aliased) value = aliased.Value;

            // Normalize value types
            if (value is EntityReference er) value = er.Id;
            else if (value is Guid guid) value = guid;
            else if (!(value is DateTime) && !(value is decimal) && !(value is int))
                value = value.ToString();*/

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


            _tracing.Trace($"Replacing token {attributeName} with value {value}");

            // Apply formatting
            if (!string.IsNullOrEmpty(format))
            {
                string formatPattern = "{0" + format + "}";
                return string.Format(formatPattern, value);
            }

            return value?.ToString() ?? "";
        }
        private string ResolveSectionValue(string attributeName)
        {
            _tracing.Trace("Resolving Section Values");
            if (_sectionDictionary != null&&_sectionDictionary.Count > 0)
            {
                if (_sectionDictionary.ContainsKey(attributeName.Trim()))
                {
                    _tracing.Trace("Found subsection match for " + attributeName);
                    return _sectionDictionary[attributeName];
                }
            }
            _tracing.Trace("Resolving from nested dictionary");
            if(_nestedDictionary != null&&_nestedDictionary.Count > 0)
            {
                if (_nestedDictionary.ContainsKey(attributeName.Trim()))
                {
                    return _nestedDictionary[attributeName];
                }
            }

            _tracing.Trace("SectionDictionary missing key: " + attributeName);
            return "";

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
            //Identify lookup attributes that point to the desired referenced entity
            var lookupAttributes = primaryEntityMetadata.Attributes
                .Where(attr => attr.AttributeType == AttributeTypeCode.Lookup || attr.AttributeType == AttributeTypeCode.Customer || attr.AttributeType == AttributeTypeCode.Owner)
                .Cast<LookupAttributeMetadata>()
                .Where(lookupAttr => lookupAttr.Targets.Contains(entityName))
                .ToList();
            ColumnSet columnSet = new ColumnSet(lookupAttributes.Select(attr => attr.LogicalName).ToArray());
            Entity primaryEntityRecord = _service.Retrieve(_primaryEntityName, _primaryEntity.Id, columnSet);
            _tracing.Trace("LookupAttributes Retrieved");
            //Iterate through the retrieved attributes to find the EntityReference
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
