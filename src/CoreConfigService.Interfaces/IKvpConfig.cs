using System;
using System.Runtime.Serialization;

namespace ConfigService.Interfaces
{
    /// <summary>
    /// Represents a string Kvp config with a single value
    /// </summary>
    public interface IKvpConfig : IConfig
    {
        string Value { get; set; }
        string Type { get; set; }
    }

    [DataContract]
    [Serializable]
    //[KnownType(typeof(BaseTypes))]
    public class KvpConfig : IKvpConfig
    {
        [DataMember]
        public string Value { get; set; }
        //[BsonRepresentation(BsonType.String)]
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Version { get; set; }
    }

    [DataContract]
    public enum BaseTypes
    {
        Bool,
        Byte,
        Sbyte,
        Char,
        Decimal,
        Double,
        Float,
        Int,
        Uint,
        Long,
        Ulong,
        Object,
        Short,
        Ushort,
        String
    }
    public static class BaseTypesExtensions
    {
        public static string ToFriendlyString(this BaseTypes src)
        {
            switch (src)
            {
                case BaseTypes.Bool:
                    return "Bool";
                case BaseTypes.Byte:
                    return "Byte";
                case BaseTypes.Sbyte:
                    return "Sbyte";
                case BaseTypes.Char:
                    return "Char";
                case BaseTypes.Decimal:
                    return "Decimal";
                case BaseTypes.Double:
                    return "Double";
                case BaseTypes.Float:
                    return "Float";
                case BaseTypes.Int:
                    return "Int";
                case BaseTypes.Uint:
                    return "Uint";
                case BaseTypes.Long:
                    return "Long";
                case BaseTypes.Ulong:
                    return "Ulong";
                case BaseTypes.Object:
                    return "Object";
                case BaseTypes.Short:
                    return "Short";
                case BaseTypes.Ushort:
                    return "Ushort";
                case BaseTypes.String:
                    return "String";
                default:
                    return "wtf";
            }

        }
        public static BaseTypes FromString(string src)
        {
            switch (src)
            {
                case "Bool":
                    return BaseTypes.Bool;
                case "Byte":
                    return BaseTypes.Byte;
                case "Sbyte":
                    return BaseTypes.Sbyte;
                case "Char":
                    return BaseTypes.Char;
                case "Decimal":
                    return BaseTypes.Decimal;
                case "Double":
                    return BaseTypes.Double;
                case "Float":
                    return BaseTypes.Float;
                case "Int":
                    return BaseTypes.Int;
                case "Uint":
                    return BaseTypes.Uint;
                case "Long":
                    return BaseTypes.Long;
                case "Ulong":
                    return BaseTypes.Ulong;
                case "Object":
                    return BaseTypes.Object;
                case "Short":
                    return BaseTypes.Short;
                case "Ushort":
                    return BaseTypes.Ushort;
                case "String":
                    return BaseTypes.String;
                default:
                    return BaseTypes.String;
            }

        }
    }

}
