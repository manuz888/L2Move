using System;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace L2Move.Core.Json;

/// <summary>
/// Custom attribute to force property inclusion even if null
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AlwaysIncludeAttribute : Attribute { }

public class CustomContractResolver: DefaultContractResolver
{
    public CustomContractResolver()
    {
        NamingStrategy = new CamelCaseNamingStrategy(); // Apply CamelCase where necessary
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        // Check if the property has the AlwaysInclude attribute
        if (member.GetCustomAttribute<AlwaysIncludeAttribute>() != null)
        {
            property.NullValueHandling = NullValueHandling.Include;
        }
        
        // If the property has a JsonProperty attribute, keep its defined name
        var jsonProperty = member.GetCustomAttributes(typeof(JsonPropertyAttribute), true);
        if (jsonProperty != null && jsonProperty.Length > 0)
        {
            return property; // Keep the original name from JsonProperty
        }

        // Apply CamelCase for properties without JsonProperty
        property.PropertyName = base.ResolvePropertyName(property.PropertyName);

        return property;
    }
}