// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.OData.Edm;
using Microsoft.OpenApi.OData.Vocabulary;

namespace Microsoft.OpenApi.OData.Common
{
    /// <summary>
    /// Utilities methods
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Get the term qualified name when using the type of <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type of the term.</typeparam>
        /// <returns>The qualified name.</returns>
        public static string GetTermQualifiedName<T>()
        {
            object[] attributes = typeof(T).GetCustomAttributes(typeof(TermAttribute), false);
            if (attributes == null && attributes.Length == 0)
            {
                return null;
            }

            TermAttribute term = (TermAttribute)attributes[0];
            return term.QualifiedName;
        }

        /// <summary>
        /// Upper the first character of the string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The changed string.</returns>
        public static string UpperFirstChar(string input)
        {
            if (input == null)
            {
                return input;
            }

            char first = char.ToUpper(input[0]);
            return first + input.Substring(1);
        }

        /// <summary>
        /// Get an unique name.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="set">The input set.</param>
        /// <returns>The changed string.</returns>
        public static string GetUniqueName(string input, HashSet<string> set)
        {
            if (!set.Contains(input))
            {
                set.Add(input);
                return input;
            }

            int index = 1;
            string newInput;
            do
            {
                newInput = input + index.ToString();
                index++;
            }
            while (set.Contains(newInput));

            set.Add(newInput);
            return newInput;
        }

        /// <summary>
        /// Check the input argument whether its value is null or not.
        /// </summary>
        /// <typeparam name="T">The input value type.</typeparam>
        /// <param name="value">The input value</param>
        /// <param name="parameterName">The input parameter name.</param>
        /// <returns>The input value.</returns>
        internal static T CheckArgumentNull<T>(T value, string parameterName) where T : class
        {
            if (null == value)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        /// <summary>
        /// Check the input string null or empty.
        /// </summary>
        /// <param name="value">The input string</param>
        /// <param name="parameterName">The input parameter name.</param>
        /// <returns>The input value.</returns>
        internal static string CheckArgumentNullOrEmpty(string value, string parameterName)
        {
            if (String.IsNullOrEmpty(value))
            {
                throw Error.ArgumentNullOrEmpty(parameterName);
            }

            return value;
        }

        /// <summary>
        /// Lowers the first character of the string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The changed string.</returns>
        internal static string ToFirstCharacterLowerCase(this string input)
            => string.IsNullOrEmpty(input) ? input : $"{char.ToLowerInvariant(input.FirstOrDefault())}{input.Substring(1)}";

        /// <summary>
        /// Gets the navigation path.
        /// </summary>
        /// <param name="path">The <see cref="ODataPath"/>.</param>
        /// <param name="navigationPropertyName">Optional: The navigation property name.</param>
        internal static string NavigationPropertyPath(this ODataPath path, string navigationPropertyName = null)
        {
            string value = string.Join("/",
                path.Segments.OfType<ODataNavigationPropertySegment>().Select(e => e.Identifier));
            return navigationPropertyName == null ? value : $"{value}/{navigationPropertyName}";
        }

        /// <summary>
        /// Attempts to add the specified key and value to the dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
        /// <param name="dictionary">A dictionary with keys of type TKey and values of type TValue.</param>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>true when the key and value are successfully added to the dictionary; 
        /// false when the dictionary already contains the specified key, 
        /// in which case nothing gets added.</returns>
        /// <exception cref="System.ArgumentNullException">dictionary is null.</exception>
        internal static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
            TKey key, TValue value)
        {
            CheckArgumentNull(dictionary, nameof(dictionary));

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a mapping of custom extension values against custom attribute values for a given element to the provided
        /// extensions object.
        /// </summary>
        /// <param name="extensions">The target extensions object in which the mapped extensions and custom attribute
        /// values will be added to.</param>
        /// <param name="context">The OData context.</param>
        /// <param name="element">The target element.</param>
        internal static void AddCustomAttributesToExtensions(this IDictionary<string, IOpenApiExtension> extensions, ODataContext context, IEdmElement element)
        {
            if (extensions  == null ||
                context == null ||
                element == null)
            {
                return;
            }

            Dictionary<string, string> atrributesValueMap = GetCustomXMLAttributesValueMapping(context.Model, element, context.Settings.CustomXMLAttributesMapping);

            if (atrributesValueMap?.Any() ?? false)
            {
                foreach (var item in atrributesValueMap)
                {
                    extensions.TryAdd(item.Key, new OpenApiString(item.Value));
                }
            }
        }

        /// <summary>
        /// Correlates and retrieves custom attribute values for a given element in an Edm model
        /// from a provided dictionary mapping of attribute names and extension names.
        /// </summary>
        /// <param name="model">The Edm model.</param>
        /// <param name="element">The target element.</param>
        /// <param name="customXMLAttributesMapping">The dictionary mapping of attribute names and extension names.</param>
        /// <returns>A dictionary of extension names mapped to the custom attribute values.</returns>
        private static Dictionary<string, string> GetCustomXMLAttributesValueMapping(IEdmModel model, IEdmElement element, Dictionary<string, string> customXMLAttributesMapping)
        {
            Dictionary<string, string> atrributesValueMap = new();

            if ((!customXMLAttributesMapping?.Any() ?? true) ||
                model == null ||
                element == null)
            {
                return atrributesValueMap;
            }

            foreach (var item in customXMLAttributesMapping)
            {
                string attributeName = item.Key.Split(':').Last(); // example, 'ags:IsHidden' --> 'IsHidden'
                string extensionName = item.Value;
                EdmStringConstant customXMLAttribute = model.DirectValueAnnotationsManager.GetDirectValueAnnotations(element)?
                                .Where(x => x.Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase))?
                                .FirstOrDefault()?.Value as EdmStringConstant;
                string attributeValue = customXMLAttribute?.Value;

                if (!string.IsNullOrEmpty(attributeValue))
                {
                    atrributesValueMap.TryAdd(extensionName, attributeValue);
                }
            }

            return atrributesValueMap;
        }

        /// <summary>
        /// Checks whether the base type of an <see cref="IEdmStructuredType"/> is referenced as a type within the Edm model.
        /// </summary>
        /// <param name="model">The Edm model.</param>
        /// <param name="baseType">The base type of the target <see cref="IEdmStructuredType"/>.</param>
        /// <param name="structuredTypes">Optional: The IEnumerable of <see cref="IEdmStructuredType"/> to check against.</param>
        /// <param name="actions">Optional: The IEnumerable of <see cref="IEdmAction"/> to check against.</param>
        /// <returns>True if reference is found, otherwise False.</returns>
        internal static bool IsBaseTypeReferencedAsTypeInModel(
            this IEdmModel model,
            IEdmStructuredType baseType,
            IEnumerable<IEdmStructuredType> structuredTypes = null,
            IEnumerable<IEdmAction> actions = null)
        {
            string baseTypeName = baseType?.FullTypeName();
            bool isBaseTypeEntity = Constants.EntityName.Equals(baseTypeName?.Split('.').Last(), StringComparison.OrdinalIgnoreCase);

            if (!string.IsNullOrEmpty(baseTypeName) && !isBaseTypeEntity)
            {
                structuredTypes ??= model.GetAllElements()
                        .Where(static x => x.SchemaElementKind == EdmSchemaElementKind.TypeDefinition)
                        .Where(static y => !y.Name.Equals(Constants.EntityName, StringComparison.OrdinalIgnoreCase))
                        .OfType<IEdmStructuredType>();

                actions ??= model.GetAllElements()
                        .Where(static x => x.SchemaElementKind == EdmSchemaElementKind.Action)
                        .OfType<IEdmAction>();

                // Is base type referenced as a type in any property within a structured type
                bool isReferencedInStructuredType = structuredTypes
                    .Any(x => x.DeclaredProperties.Where(y => y.Type.TypeKind() == EdmTypeKind.Entity ||
                                                            y.Type.TypeKind() == EdmTypeKind.Collection ||
                                                            y.Type.TypeKind() == EdmTypeKind.Complex)
                    .Any(z => z.Type.FullName().Equals(baseTypeName, StringComparison.OrdinalIgnoreCase)));
                if (isReferencedInStructuredType) return true;

                // Is base type referenced as a type in any parameter in an action
                bool isReferencedInAction = actions.Any(x => x.Parameters.Any(x => x.Type.FullName().Equals(baseTypeName, StringComparison.OrdinalIgnoreCase)));
                if (isReferencedInAction) return true;

                // Recursively check the base type
                return model.IsBaseTypeReferencedAsTypeInModel(baseType.BaseType, structuredTypes, actions);
            }

            return false;
        }

        ///// <summary>
        ///// Removes the default namespace from a string value.
        ///// </summary>
        ///// <param name="value">The target string value.</param>
        ///// <param name="settings">Convert settings.</param>
        ///// <returns>The string value with the namespace removed.</returns>
        //internal static string RemoveDefaultNamespace(this string value, OpenApiConvertSettings settings)
        //{
        //    CheckArgumentNullOrEmpty(value, nameof(value));
        //    CheckArgumentNull(settings, nameof(settings));

        //    if (string.IsNullOrEmpty(settings.DefaultNamespace))
        //    {
        //        return value;
        //    }

        //    string defaultNamespace = settings.DefaultNamespace.EndsWith(".", StringComparison.OrdinalIgnoreCase)
        //        ? settings.DefaultNamespace
        //        : settings.DefaultNamespace + ".";

        //    return value.Replace(defaultNamespace, "");
        //}
    }
}