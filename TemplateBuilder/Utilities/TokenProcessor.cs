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
            _primaryEntity = service.Retrieve(context.PrimaryEntityName, context.PrimaryEntityId, new ColumnSet(true));
            if(_primaryEntity != null)
            {
                _primaryEntityName = _primaryEntity.LogicalName;
            }
            _tdbOrQuery = true;
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
        
        /// <summary>              
        /// _entity = _primaryEntity;
        /// Replaces curly brace values with the attribute values from the entity results
        /// </summary>
        /// <param name="fetchXML"></param>
        /// <returns></returns>
        
        
        public string ReplaceTokens(string text)
        {

            var result = Regex.Replace(text, _pattern, match =>
            {

                string fullToken = match.Groups[1].Value;

                string format = match.Groups[2].Value;

                string attributeName = ResolveEntityAndAttribute(fullToken);

                if (_entity != null)
                {
                    return FormatEntityValue(attributeName, format);
                }
                return ResolveSectionValue(attributeName);       
            });

            return ReplaceFormatLogic(result);
        }

        private string  ResolveEntityAndAttribute(string fullToken)
        {
            string attributeName = string.Empty;

            if (_tdbOrQuery)
            {
                if (_entityDictionary != null)
                {

                    (_entity, attributeName) = ResolveFromEntityDictionary(fullToken);

                }
                else
                {
                    if (_entity != null)
                    {
                        _tracing.Trace("Replacing nested query");
                        attributeName = fullToken.Trim();
                        return attributeName;
                    }
                    (_entity, attributeName) = ResolveFromQuery(fullToken);

                }
            }
            else
            {
                _tracing.Trace("not tdbOrQuery"+ fullToken.Trim());

                attributeName = fullToken.Trim();
            }
            return attributeName;
        }
        private (Entity entity, string attributeName) ResolveFromEntityDictionary(string token)
        {

            var parts = token.Split('-');
            if (parts.Length > 1)
            {
                string queryName = parts[0];

                string attributeName = parts[1];

                if (_entityDictionary.ContainsKey(queryName))
                {
                    _tracing.Trace($"Entity found in dictionary for {queryName}");
                    return (_entityDictionary[queryName], attributeName);
                }

                return (null, attributeName);
            }
            return (null, token);
        }
        private (Entity entity, string attributeName) ResolveFromQuery(string token)
        {

            
            var parts = token.Split('.');

            if (parts.Length > 1)
            {

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

            if (!_entity.Contains(attributeName))
            {
                return ResolveSectionValue(attributeName);
            }


            var value = _entity[attributeName];
            var type = value.GetType().Name;
            // If aliased value, get the real value
            if (type == "AliasedValue")
            {
                value = (value as AliasedValue).Value;
                type = value.GetType().Name;
            }
            _tracing.Trace("Type: " + type);
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
                case "Money":
                    value = ((Money)value).Value;
                    break;
                default:
                    value = value.ToString();
                    break;
            }

            _tracing.Trace("Type: " + type);

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
            if (_sectionDictionary != null&&_sectionDictionary.Count > 0)
            {
                if (_sectionDictionary.ContainsKey(attributeName.Trim()))
                {
                    return _sectionDictionary[attributeName];
                }
            }
            if(_nestedDictionary != null&&_nestedDictionary.Count > 0)
            {
                if (_nestedDictionary.ContainsKey(attributeName.Trim()))
                {
                    return _nestedDictionary[attributeName];
                }
            }
            return "";

        }

        public string ReplaceFormatLogic(string result)
        {
            // Add additional template logic 
            // [[delimeter:, ]] - Will only add the string ", " if there is a value before and after
            // [[suffix: Days]] - Will only add the string " Days" if there is a value before
            string pattern = @"\[\[(delimeter|suffix|prefix|blankif|usefirst):([\w\W]*?)\]\]";
            _tracing.Trace("Starting Replace Format Logic");
            Match match = Regex.Match(result, pattern);
            while (match.Success)
            {
                int index = match.Index;
                int endIndex = match.Index + match.Length;
                
                var beforeTextOnly = GetImmediateTextBefore(result,index);

                var  afterTextOnly = GetImmediateTextAfter(result, endIndex);
                
                var replaceWith = "";
                _tracing.Trace("Before: " );
                _tracing.Trace("After: " + afterTextOnly);
                switch (match.Groups[1].Value)
                {
                    case "delimeter":
                        // If the preceding or following text is blank then don't output the delimiter
                        if (!string.IsNullOrEmpty(beforeTextOnly)&&!beforeTextOnly.EndsWith("}}") && !string.IsNullOrEmpty(afterTextOnly) && !afterTextOnly.StartsWith("{{"))
                        {
                            replaceWith = match.Groups[2].Value;
                        }
                        result = result.Substring(0, index) + replaceWith + result.Substring(endIndex);
                        break;
                    case "suffix":
                        // If the preceding text is blank, then don't output the suffix
                        if (!string.IsNullOrEmpty(beforeTextOnly) && !beforeTextOnly.EndsWith("}}"))
                        {
                            replaceWith = match.Groups[2].Value;
                        }
                        result = result.Substring(0, index) + replaceWith + result.Substring(endIndex);
                        break;
                    case "prefix":
                        if(!string.IsNullOrEmpty(afterTextOnly) && !afterTextOnly.StartsWith("{{"))
                        {
                            _tracing.Trace("After: " + afterTextOnly);
                            _tracing.Trace("Replace with the prefix value ");
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
                            if (afterTextOnly.StartsWith(value))
                            {
                                result = afterTextOnly.Replace(value, "");
                            }
                        }

                        break;
                    case "usefirst":
                        if (afterTextOnly.IndexOf('|') == 0)
                            result = afterTextOnly.Substring(afterTextOnly.IndexOf('|') + 1);
                        else
                            result = afterTextOnly.Substring(0, afterTextOnly.IndexOf('|'));
                        break;
                }
                match = Regex.Match(result, pattern);
            }

            return result;
        }
        private string GetImmediateTextBefore(string input, int startIndex)
        {
            if (startIndex <= 0) return "";

            // Look backwards for the *last opening tag* of a container
            var snippetBefore = input.Substring(0, startIndex);

            // Find the last opening tag <td>, <div>, <p>, etc.
            var lastTagMatch = Regex.Match(snippetBefore, @"<(td|div|p)[^>]*>$",
                RegexOptions.IgnoreCase | RegexOptions.RightToLeft);

            int searchStart = lastTagMatch.Success
                ? lastTagMatch.Index + lastTagMatch.Length // start right after the tag
                : 0;

            string snippet = snippetBefore.Substring(searchStart);

            // Remove tags & whitespace
            string clean = Regex.Replace(snippet, "<.*?>", "").Replace("&nbsp;", "").Trim();

            return clean;
        }

        private string GetImmediateTextAfter(string input, int startIndex)
        {
            if (startIndex >= input.Length) return "";

            // Find the closing tag of the container (<td>, <div>, <p>, etc.)
            var nextTagMatch = Regex.Match(input.Substring(startIndex), @"</(td|div|p)[^>]*>", RegexOptions.IgnoreCase);
            int searchEnd = nextTagMatch.Success
                ? startIndex + nextTagMatch.Index   // limit to before the closing tag
                : input.Length;

            string snippet = input.Substring(startIndex, searchEnd - startIndex);

            // Remove tags & whitespace
            string clean = Regex.Replace(snippet, "<.*?>", "").Replace("&nbsp;", "").Trim();

            return clean;
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
            //Identify lookup attributes that point to the desired referenced entity
            var lookupAttributes = primaryEntityMetadata.Attributes
                .Where(attr => attr.AttributeType == AttributeTypeCode.Lookup || attr.AttributeType == AttributeTypeCode.Customer || attr.AttributeType == AttributeTypeCode.Owner)
                .Cast<LookupAttributeMetadata>()
                .Where(lookupAttr => lookupAttr.Targets.Contains(entityName))
                .ToList();
            ColumnSet columnSet = new ColumnSet(lookupAttributes.Select(attr => attr.LogicalName).ToArray());
            Entity primaryEntityRecord = _service.Retrieve(_primaryEntityName, _primaryEntity.Id, columnSet);
            //Iterate through the retrieved attributes to find the EntityReference
            foreach (var attributeName in columnSet.Columns)
            {
                if (primaryEntityRecord.Contains(attributeName) && primaryEntityRecord[attributeName] is EntityReference entityReference)
                {
                    if (entityReference.LogicalName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
                    {
                        return _service.Retrieve(entityName, entityReference.Id, new ColumnSet(true));
                    }
                }
            }
            return null;
        }
    }
}
