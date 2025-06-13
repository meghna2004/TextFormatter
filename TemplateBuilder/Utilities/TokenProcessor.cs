using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        public string ReplaceTokens(string text, Entity entity)
        {
            _tracing.Trace("Start of Replace Token Functions");

            // Accept format strings in the format
            // {attributeLogicalName} or {attribtueLogicalName:formatstring}
            // Where formatstring is a standard String.format format string e.g. {course.date:dd MMM yyyy}
            var pattern = @"(?<!{){{([\w .]*)(:[^}]+)*}}(?!})";
            var result = Regex.Replace(text, pattern, (match) =>
            {
                _tracing.Trace("Inside var result");
                _tracing.Trace("Text: "+text);
                var fulltoken = match.Groups[1].Value;
                // Try get the query
                var format = match.Groups[2].Value;
                _tracing.Trace("Format: "+format);
                var attributeName = match.Groups[1].Value.Trim();
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
                    if (!String.IsNullOrEmpty(format))
                    {
                        // 'format' is match.Groups[2].Value, which contains the colon and format specifier (e.g., ": dd/MMM/yyyy")
                        // Construct the standard .NET format string: "{0: dd/MMM/yyyy}"
                        string formatPattern = "{0" + format + "}";
                        _tracing.Trace("Format Pattern for String.Format: " + formatPattern);

                        var formattedResult = string.Format(formatPattern, value);
                        _tracing.Trace("Formatted Result: " + formattedResult);
                        return formattedResult;
                    }
                    string replacementValue = value?.ToString() ?? "";
                    return replacementValue;
                }
                return "";
            });
            _tracing.Trace("Result: "+result);
            return ReplaceFormatLogic(result);
        }
        private static string ReplaceFormatLogic(string result)
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
    }
}
