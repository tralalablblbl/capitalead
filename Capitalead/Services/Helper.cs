using System.Text.Json.Nodes;

namespace Capitalead.Services;

public static class Helper
{
    public static JsonNode[] TransformJSON(JsonNode[] jsonArray) {
        if (!jsonArray.Any())
            return Array.Empty<JsonNode>();
    
        var transformed = new List<JsonNode>();
        foreach (var json in jsonArray)
        {
            if (json == null)
                continue;
            var apartments = new JsonArray()
            {
                json["neighborhood"]?.DeepClone() ?? json["city"]?.DeepClone() ?? string.Empty,
                DateTime.TryParse(json["scraping_time"]?.GetValue<string>().Substring(0, 10) ?? string.Empty, out var date) ? date.ToUniversalTime().ToString("dd/MM/yyyy") : "",
                json["breadcrumb"]?.DeepClone() ?? json["real_estate_type"]?.DeepClone() ?? string.Empty,
                GetPhone(json["phone"]?.GetValue<string>()),
                json["rooms"]?.DeepClone() ?? json["room_count"]?.DeepClone() ?? string.Empty,
                json["size"]?.DeepClone() ?? json["area"]?.DeepClone() ?? string.Empty,
                json["energy"]?.DeepClone() ?? json["DPE_string"]?.DeepClone() ?? string.Empty
            };
    
            transformed.Add(apartments);
        }
    
        return transformed.ToArray();
    }

    public static string GetPhone(string? plainPhone) => plainPhone?.Replace(".", "").Trim() ?? string.Empty;

    public static JsonObject BuildJsonBodyForCreatingProspList(string listTitle, string[] tags, string nocrmUserEmail, JsonNode[]? prospects) {
        JsonObject jsonObject = new JsonObject();
        jsonObject["title"] = listTitle;

        JsonArray jsonArray = new JsonArray();
        jsonArray.Add(new JsonArray("Neighborhood", "Parsing Date", "Type", "Téléphone", "Rooms", "Size", "Energy"));
        if (prospects != null)
            foreach (var prospect in prospects) 
            {
                jsonArray.Add(prospect.AsArray());
            }

        jsonObject["content"] = jsonArray;
        jsonObject["description"] = listTitle;
        jsonObject["tags"] = new JsonArray(tags.Select(t => (JsonNode)t).ToArray());
        jsonObject["user_id"] = nocrmUserEmail;

        return jsonObject;
    }
}