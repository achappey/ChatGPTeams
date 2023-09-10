using achappey.ChatGPTeams.Config.SharePoint;
using achappey.ChatGPTeams.Models;

namespace achappey.ChatGPTeams.Extensions
{
    public static class SharePointExtensions
    {

        public static string SharePointFieldToJson(this Microsoft.Graph.ColumnTypes? fieldType)
        {
            return fieldType switch
            {
                Microsoft.Graph.ColumnTypes.Text or Microsoft.Graph.ColumnTypes.Choice or Microsoft.Graph.ColumnTypes.Note => "string",
                Microsoft.Graph.ColumnTypes.DateTime => "string",
                Microsoft.Graph.ColumnTypes.Number => "number",
                _ => "",
            };
        }

        public static Conversation ToConversation(this string value) => new()
        {
            Id = value
        };

        public static Function ToFunction(this LookupField value)
        {
            return new Function()
            {
                Id = value.LookupId.ToString(),
                //Name = value.LookupValue
            };
        }
        
        public static Database.Models.Function ToDbFunction(this LookupField value)
        {
            return new Database.Models.Function()
            {
                Id = value.LookupValue,

                //Name = value.LookupValue
            };
        }

        public static Reaction ToReaction(this LookupField value)
        {
            return new Reaction()
            {
                Id = value.LookupId.ToString(),
                Title = value.LookupValue
            };
        }

        public static User GetOwner(this Microsoft.Graph.ListItem src)
        {
            if (!src.Fields.AdditionalData.ContainsKey(FieldNames.AIOwner.ToLookupField()))
            {
                return null;
            }

            return new User()
            {
                DisplayName = src.GetFieldValue(FieldNames.AIOwner),
                Id = src.GetFieldValue(FieldNames.AIOwner.ToLookupField()),
            };
        }

        public static User ToUser(this string value)
        {
            return !string.IsNullOrEmpty(value) ? new User()
            {
                Id = value
            } : null;
        }

        public static Department LookupToDepartment(this string value)
        {
            return !string.IsNullOrEmpty(value) ? new Department()
            {
                Id = value
            } : null;
        }

        public static Department NameToDepartment(this string value)
        {
            return !string.IsNullOrEmpty(value) ? new Department()
            {
                Name = value
            } : null;
        }

        public static FunctionCall GetFunctionCall(this Microsoft.Graph.ListItem src)
        {
            if (!src.Fields.AdditionalData.ContainsKey(FieldNames.AIArguments) || src.Fields.AdditionalData[FieldNames.AIArguments] == null)
                return null;

            return new FunctionCall()
            {
                Name = src.GetFieldValue(FieldNames.Title),
                Arguments = src.GetFieldValue(FieldNames.AIArguments)
            };
        }

        public static string ToMessageName(this Microsoft.Graph.ListItem src)
        {
            if (src.GetFieldValue(FieldNames.AIRole) == "assistant")
            {
                return null;
            }

            return src.Fields.AdditionalData.ContainsKey(FieldNames.Title) ? src.GetFieldValue(FieldNames.Title) : null;
        }

        public static Department GetDepartment(this Microsoft.Graph.ListItem src)
        {
            if (!src.Fields.AdditionalData.ContainsKey(FieldNames.AIDepartment.ToLookupField()))
            {
                return null;
            }

            return new Department()
            {
                Name = src.GetFieldValue(FieldNames.AIDepartment),
                Id = src.GetFieldValue(FieldNames.AIDepartment.ToLookupField()),
            };
        }

        public static Assistant GetAssistant(this Microsoft.Graph.ListItem src)
        {
            if (!src.Fields.AdditionalData.ContainsKey(FieldNames.AIAssistant.ToLookupField()))
            {
                return null;
            }

            return new Assistant()
            {
                Name = src.GetFieldValue(FieldNames.AIAssistant),
                Id = src.GetFieldValue(FieldNames.AIAssistant.ToLookupField()).ToInt().Value,
            };
        }

    }
}