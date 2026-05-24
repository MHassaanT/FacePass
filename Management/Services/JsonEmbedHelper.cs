using System.Linq;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Services
{
    /// <summary>
    /// Safely reads PostgREST embedded resources (ROLE, COURSES, USER, etc.)
    /// without throwing when the embed is null, a scalar, or an array.
    /// </summary>
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

        public static string RoleNameFromUser(JToken user)
        {
            var fromEmbed = GetField(user, "ROLE", "role_name");
            if (!string.IsNullOrEmpty(fromEmbed)) return fromEmbed;

            return user["role_id"]?.ToString() switch
            {
                "1" => "admin",
                "2" => "teacher",
                "3" => "student",
                _ => ""
            };
        }

        /// <summary>Walks embed path (e.g. STUDENTS → USER) and returns the leaf field.</summary>
        public static string GetNestedField(JToken row, params string[] path)
        {
            if (path.Length == 0) return "";
            if (path.Length == 1)
                return row[path[0]]?.ToString() ?? "";

            JToken? cur = row;
            for (int i = 0; i < path.Length - 1; i++)
            {
                cur = GetChildToken(cur, path[i]);
                if (cur == null) return "";
            }

            var leaf = path[^1];
            return cur switch
            {
                JObject o => o[leaf]?.ToString() ?? "",
                _ => ""
            };
        }

        public static string FullName(JToken row, params string[] pathToUser)
        {
            var first = GetNestedField(row, pathToUser.Concat(new[] { "first_name" }).ToArray());
            var last = GetNestedField(row, pathToUser.Concat(new[] { "last_name" }).ToArray());
            var name = $"{first} {last}".Trim();
            return string.IsNullOrEmpty(name) ? "Unknown" : name;
        }

        private static JToken? GetChildToken(JToken? token, string key)
        {
            return token switch
            {
                JObject o => o[key],
                JArray a when a.Count > 0 => a[0] is JObject first ? first[key] : null,
                _ => null
            };
        }
    }
}
