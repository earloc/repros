using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;

namespace Tests;

public class SerializationContextAwareJsonParseNodeFactory(KiotaJsonSerializationContext ctx) : IAsyncParseNodeFactory
{
    public string ValidContentType => "application/json";

    public IParseNode GetRootParseNode(string contentType, Stream content)
    {
        throw new NotImplementedException();
    }

    public async Task<IParseNode> GetRootParseNodeAsync(string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentNullException(nameof(contentType));
        }
        else if (!ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException($"expected a {ValidContentType} content type");
        }

        _ = content ?? throw new ArgumentNullException(nameof(content));

        using var jsonDocument = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new SerializationContextAwareJsonParseNode(jsonDocument.RootElement.Clone(), ctx);
    }
}

public class SerializationContextAwareJsonParseNode : IParseNode
{
    public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }
    public Action<IParsable>? OnAfterAssignFieldValues { get; set; }

    private readonly JsonParseNode node;
    private readonly JsonElement element;
    private readonly KiotaJsonSerializationContext ctx;

    public SerializationContextAwareJsonParseNode(JsonElement element, KiotaJsonSerializationContext ctx)
    {
        node = new(element, ctx);
        this.element = element;
        this.ctx = ctx;
    }

    public bool? GetBoolValue()
        => node.GetBoolValue();

    public byte[]? GetByteArrayValue()
        => node.GetByteArrayValue();

    public byte? GetByteValue()
       => node.GetByteValue();

    public IParseNode? GetChildNode(string identifier)
       => node.GetChildNode(identifier);

    public IEnumerable<T?> GetCollectionOfEnumValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum
       => node.GetCollectionOfEnumValues<T>();

    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable
       => node.GetCollectionOfObjectValues<T>(factory);

    public IEnumerable<T> GetCollectionOfPrimitiveValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>()
       => node.GetCollectionOfPrimitiveValues<T>();

    public DateTimeOffset? GetDateTimeOffsetValue()
       => node.GetDateTimeOffsetValue();

    public Date? GetDateValue()
       => node.GetDateValue();

    public decimal? GetDecimalValue()
       => node.GetDecimalValue();

    public double? GetDoubleValue() // modified
       => element.ValueKind == JsonValueKind.Number || (ctx.Options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString) && element.ValueKind == JsonValueKind.String)
       ? element.Deserialize(ctx.Double)
       : null
    ;

    public T? GetEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum
       => node.GetEnumValue<T>();

    public float? GetFloatValue()
       => node.GetFloatValue();

    public Guid? GetGuidValue()
       => node.GetGuidValue();

    public int? GetIntValue()
        => element.ValueKind == JsonValueKind.Number || (ctx.Options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString) && element.ValueKind == JsonValueKind.String)
       ? element.Deserialize(ctx.Int32)
       : null
    ;

    public long? GetLongValue()
        => element.ValueKind == JsonValueKind.Number || (ctx.Options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString) && element.ValueKind == JsonValueKind.String)
       ? element.Deserialize(ctx.Int64)
       : null
    ;


    private UntypedNode GetUntypedValue() => GetUntypedValue(element);

    public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable
    {
        // until interface exposes GetUntypedValue()
        var genericType = typeof(T);
        if (genericType == typeof(UntypedNode))
        {
            return (T)(object)GetUntypedValue();
        }
        var item = factory(this);
        OnBeforeAssignFieldValues?.Invoke(item);
        AssignFieldValues(item);
        OnAfterAssignFieldValues?.Invoke(item);
        return item;
    }

    private void AssignFieldValues<T>(T item) where T : IParsable
        {
            if(element.ValueKind != JsonValueKind.Object) return;
            IDictionary<string, object>? itemAdditionalData = null;
            if(item is IAdditionalDataHolder holder)
            {
                holder.AdditionalData ??= new Dictionary<string, object>();
                itemAdditionalData = holder.AdditionalData;
            }
            var fieldDeserializers = item.GetFieldDeserializers();

            foreach(var fieldValue in element.EnumerateObject())
            {
                if(fieldDeserializers.ContainsKey(fieldValue.Name))
                {
                    if(fieldValue.Value.ValueKind == JsonValueKind.Null)
                {
                    continue;// If the property is already null just continue. As calling functions like GetDouble,GetBoolValue do not process JsonValueKind.Null.
                }

                var fieldDeserializer = fieldDeserializers[fieldValue.Name];
                    Debug.WriteLine($"found property {fieldValue.Name} to deserialize");
                    fieldDeserializer.Invoke(new SerializationContextAwareJsonParseNode(fieldValue.Value, ctx) // added 'ctx', as otherwise, KiotaJsonSerializationContext.Default is used under the hood
                    {
                        OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
                        OnAfterAssignFieldValues = OnAfterAssignFieldValues
                    });
                }
                else if(itemAdditionalData != null)
                {
                    Debug.WriteLine($"found additional property {fieldValue.Name} to deserialize");
                    IDictionaryExtensions.TryAdd(itemAdditionalData, fieldValue.Name, TryGetAnything(fieldValue.Value)!);
                }
                else
                {
                    Debug.WriteLine($"found additional property {fieldValue.Name} to deserialize but the model doesn't support additional data");
                }
            }
        }
        private object? TryGetAnything(JsonElement element)
        {
            switch(element.ValueKind)
            {
                case JsonValueKind.Number:
                    if(element.TryGetDecimal(out var dec)) return dec;
                    else if(element.TryGetDouble(out var db)) return db;
                    else if(element.TryGetInt16(out var s)) return s;
                    else if(element.TryGetInt32(out var i)) return i;
                    else if(element.TryGetInt64(out var l)) return l;
                    else if(element.TryGetSingle(out var f)) return f;
                    else if(element.TryGetUInt16(out var us)) return us;
                    else if(element.TryGetUInt32(out var ui)) return ui;
                    else if(element.TryGetUInt64(out var ul)) return ul;
                    else throw new InvalidOperationException("unexpected additional value type during number deserialization");
                case JsonValueKind.String:
                    if(element.TryGetDateTime(out var dt)) return dt;
                    else if(element.TryGetDateTimeOffset(out var dto)) return dto;
                    else if(element.TryGetGuid(out var g)) return g;
                    else return element.GetString();
                case JsonValueKind.Array:
                case JsonValueKind.Object:
                    return GetUntypedValue(element);
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    throw new InvalidOperationException($"unexpected additional value type during deserialization json kind : {element.ValueKind}");
            }
        }

    private UntypedNode GetUntypedValue(JsonElement jsonNode) => jsonNode.ValueKind switch
    {
        JsonValueKind.Number when jsonNode.TryGetInt32(out var intValue) => new UntypedInteger(intValue),
        JsonValueKind.Number when jsonNode.TryGetInt64(out var longValue) => new UntypedLong(longValue),
        JsonValueKind.Number when jsonNode.TryGetDecimal(out var decimalValue) => new UntypedDecimal(decimalValue),
        JsonValueKind.Number when jsonNode.TryGetSingle(out var floatValue) => new UntypedFloat(floatValue),
        JsonValueKind.Number when jsonNode.TryGetDouble(out var doubleValue) => new UntypedDouble(doubleValue),
        JsonValueKind.String => new UntypedString(jsonNode.GetString()),
        JsonValueKind.True or JsonValueKind.False => new UntypedBoolean(jsonNode.GetBoolean()),
        JsonValueKind.Array => new UntypedArray(GetCollectionOfUntypedValues(jsonNode)),
        JsonValueKind.Object => new UntypedObject(GetPropertiesOfUntypedObject(jsonNode)),
        JsonValueKind.Null or JsonValueKind.Undefined => new UntypedNull(),
        _ => throw new InvalidOperationException($"unexpected additional value type during deserialization json kind : {jsonNode.ValueKind}")
    };

    /// <summary>
    /// Gets the collection of untyped values of the node.
    /// </summary>
    /// <returns>The collection of untyped values.</returns>
    private IEnumerable<UntypedNode> GetCollectionOfUntypedValues(JsonElement jsonNode)
    {
        if(jsonNode.ValueKind == JsonValueKind.Array)
        {
            foreach(var collectionValue in jsonNode.EnumerateArray())
            {
                var currentParseNode = new SerializationContextAwareJsonParseNode(collectionValue, ctx)
                {
                    OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
                    OnAfterAssignFieldValues = OnAfterAssignFieldValues
                };
                yield return currentParseNode.GetUntypedValue();
            }
        }
    }

    private IDictionary<string, UntypedNode> GetPropertiesOfUntypedObject(JsonElement jsonNode)
        {
            var properties = new Dictionary<string, UntypedNode>();
            if(jsonNode.ValueKind == JsonValueKind.Object)
            {
                foreach(var objectValue in jsonNode.EnumerateObject())
                {
                    JsonElement property = objectValue.Value;
                    if(objectValue.Value.ValueKind == JsonValueKind.Object)
                    {
                        var childNode = new SerializationContextAwareJsonParseNode(objectValue.Value, ctx)
                        {
                            OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
                            OnAfterAssignFieldValues = OnAfterAssignFieldValues
                        };
                        var objectVal = childNode.GetPropertiesOfUntypedObject(childNode.element);
                        properties[objectValue.Name] = new UntypedObject(objectVal);
                    }
                    else
                    {
                        properties[objectValue.Name] = GetUntypedValue(property);
                    }
                }
            }
            return properties;
        }

    public sbyte? GetSbyteValue()
       => node.GetSbyteValue();

    public string? GetStringValue()
       => node.GetStringValue();

    public TimeSpan? GetTimeSpanValue()
       => node.GetTimeSpanValue();

    public Time? GetTimeValue()
       => node.GetTimeValue();
}