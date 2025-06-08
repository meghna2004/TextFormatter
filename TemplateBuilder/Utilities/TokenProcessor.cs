using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TemplateBuilder.DTO;

namespace TemplateBuilder.Utilities
{

    public class TokenProcessor
    {
        private readonly ITracingService _tracing;
        public TokenProcessor(ITracingService tracing)
        {
            _tracing = tracing; 
        }
        /// <summary>
        /// Replaces curly brace values with the attribute values from the entity results
        /// </summary>
        /// <param name="fetchXML"></param>
        /// <returns></returns>
        public string ReplaceTokens(string token, string text, Entity entity)
        {
            _tracing.Trace("Start of Replace Token Functions");

            // Accept format strings in the format
            // {attributeLogicalName} or {attribtueLogicalName:formatstring}
            // Where formatstring is a standard String.format format string e.g. {course.date:dd MMM yyyy}
            var result = Regex.Replace(text, @"\{\{\s*([\w\.]+)\s*\}\}", (match) =>
            {
                _tracing.Trace("Inside var result");
                _tracing.Trace("Token: " + token);
                //_tracing.Trace("Text: "+text);

                var attributeName = match.Groups[1].Value;
                _tracing.Trace("Token: " + attributeName);

                // Check if there is an attribute value
                if (entity.Contains(attributeName))
                {
                    _tracing.Trace("Getting valuetype of token");

                    var value = entity[attributeName];
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
                    _tracing.Trace("Replacing the token value: "+value);
                    // return string.Format(match.Value.Replace(text, "0"), value);
                    string replacementValue = value?.ToString() ?? "";
                    return replacementValue;
                }
                return "";
            });
            _tracing.Trace("Result: "+result);
            return result;
        } 
    }
}
