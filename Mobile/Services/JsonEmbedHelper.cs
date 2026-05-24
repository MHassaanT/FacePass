using Newtonsoft.Json.Linq;

namespace FacePass.Mobile.Services
{
    public static class JsonEmbedHelper
    {
        public static string GetField(JToken row, string embedKey, string fieldName)
        {
            if (row is not JObject obj) return "";
            var embed = obj[embedKey];
            if (embed is JObject embedObj)
                return embedObj[fieldName]?.ToString() ?? "";
            if (embed is JArray arr && arr.Count > 0 && arr[0] is JObject first)
                return first[fieldName]?.ToString() ?? "";
            return "";
        }
    }
}
