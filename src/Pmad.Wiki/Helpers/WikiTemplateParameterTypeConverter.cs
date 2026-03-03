using Pmad.Wiki.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Pmad.Wiki.Helpers;

internal sealed class WikiTemplateParameterTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(WikiTemplateParameterType);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (parser.Current is Scalar scalar)
        {
            var value = scalar.Value;
            parser.MoveNext();
            
            return value.ToLowerInvariant() switch
            {
                "number" => WikiTemplateParameterType.Number,
                "date" => WikiTemplateParameterType.Date,
                "datetime" => WikiTemplateParameterType.DateTime,
                _ => WikiTemplateParameterType.Text
            };
        }

        parser.MoveNext();
        return WikiTemplateParameterType.Text;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        if (value is WikiTemplateParameterType paramType)
        {
            var stringValue = paramType switch
            {
                WikiTemplateParameterType.Number => "number",
                WikiTemplateParameterType.Date => "date",
                WikiTemplateParameterType.DateTime => "datetime",
                _ => "text"
            };
            emitter.Emit(new Scalar(stringValue));
        }
    }
}
