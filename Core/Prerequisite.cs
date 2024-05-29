using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Diagnostics.CodeAnalysis;

namespace MCEPatcher.Core
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PrerequisiteType
    {
        Basic,
        Conditional
    }

    public abstract class Prerequisite
    {
        public abstract PrerequisiteType Type { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patches">Patches to apply before this one</param>
        /// <returns>
        /// <see href="true"/> when the condition for this patch is satisfied<br></br>
        /// <see href="false"/> if not
        /// </returns>
        public abstract bool Check(Patcher.PatchContext context, [NotNullWhen(false)] out List<string>? patches);

        public class Converter : JsonConverter<Prerequisite>
        {
            public override bool CanRead => true;
            public override bool CanWrite => false;

            public override Prerequisite? ReadJson(JsonReader reader, Type objectType, Prerequisite? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                JObject jo = JObject.Load(reader);

                if (!jo.TryGetValue("Type", out JToken? typeToken)) throw new JsonSerializationException("Invalid json");
                var type = typeToken.ToObject<PrerequisiteType>();

                Prerequisite prerequisite;
                switch (type)
                {
                    case PrerequisiteType.Basic:
                        prerequisite = new BasicPrerequisite();
                        break;
                    case PrerequisiteType.Conditional:
                        prerequisite = new ConditionalPrerequisite();
                        break;
                    default:
                        throw new InvalidDataException($"Invalid {nameof(PrerequisiteType)} '{type}'");
                }

                serializer.Populate(jo.CreateReader(), prerequisite);

                return prerequisite;
            }

            public override void WriteJson(JsonWriter writer, Prerequisite? value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class BasicPrerequisite : Prerequisite
    {
        public override PrerequisiteType Type => PrerequisiteType.Basic;

        public string RequiredPatch { get; set; }

        public BasicPrerequisite()
            : this(string.Empty)
        {
        }
        public BasicPrerequisite(string _requiredPatch)
        {
            RequiredPatch = _requiredPatch;
        }

        public override bool Check(Patcher.PatchContext context, [NotNullWhen(false)] out List<string>? patches)
        {
            patches = null;

            if (context.AppliedPatches.ContainsKey(RequiredPatch))
                return true;

            patches = new List<string>()
            {
                RequiredPatch
            };
            return false;
        }
    }

    /// <summary>
    /// <see cref="RequiredPatch"/> is required if <see cref="VariableName"/> == <see cref="VariableValue"/>
    /// </summary>
    public class ConditionalPrerequisite : Prerequisite
    {
        public override PrerequisiteType Type => PrerequisiteType.Conditional;

        public string RequiredPatch { get; set; }

        public string VariableName { get; set; }
        public string VariableValue { get; set; }

        public ConditionalPrerequisite()
            : this(string.Empty, string.Empty, string.Empty)
        { }
        public ConditionalPrerequisite(string _requiredPatch, string _variableName, string _variableValue)
        {
            RequiredPatch = _requiredPatch;
            VariableName = _variableName;
            VariableValue = _variableValue;
        }

        public override bool Check(Patcher.PatchContext context, [NotNullWhen(false)] out List<string>? patches)
        {
            patches = null;

            if (
                context.AppliedPatches.ContainsKey(RequiredPatch) ||
                !context.Variables.TryGetValue(VariableName, out string? val) ||
                val != VariableValue
                )
                return true;

            patches = new List<string>()
            {
                RequiredPatch
            };
            return false;
        }
    }
}
