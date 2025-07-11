using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;
using DevHabit.Api.DTOs.Common;

namespace DevHabit.Api.Services;

public sealed class DataShapingService
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertiesCache  = new();

    public ExpandoObject ShapeData<T>(T entity, string? fields)
    {
        List<ExpandoObject> shaped = ShapeCollectionData([entity], fields);

        return shaped.FirstOrDefault();
    }
    
    public List<ExpandoObject> ShapeCollectionData<T>(IEnumerable<T> entities, string? fields,
        Func<T, List<LinkDto>>? linksFactory = null)
    {
        HashSet<string> fieldsSet = fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        PropertyInfo[] propertyInfos = _propertiesCache.GetOrAdd(typeof(T),
            t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        if (fieldsSet.Any())
        {
            propertyInfos = propertyInfos
                .Where(p => fieldsSet.Contains(p.Name))
                .ToArray();
        }

        List<ExpandoObject> shapedObjects = [];
        foreach (T entity in entities)
        {
            IDictionary<string, object?> shapedObject = new ExpandoObject();
            
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
            }

            if (linksFactory != null)
            {
                shapedObject["links"] = linksFactory(entity);
            }
            
            shapedObjects.Add((ExpandoObject)shapedObject);
        }
        
        return shapedObjects;
    }

    public bool Validate<T>(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return true;
        }
        
        var fieldsSet = fields
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        PropertyInfo[] propertyInfos = _propertiesCache.GetOrAdd(typeof(T),
            t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        return fieldsSet.All(
            field => propertyInfos.Any(pi => pi.Name.Equals(field, StringComparison.OrdinalIgnoreCase)));
    }
}
