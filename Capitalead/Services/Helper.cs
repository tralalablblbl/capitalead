using System.Globalization;
using System.Text.Json.Nodes;

namespace Capitalead.Services;

public static class Helper
{
    public static JsonArray TransformJSON(JsonArray jsonArray) {
        if (!jsonArray.Any()) return jsonArray;
    
        JsonArray transformed = new JsonArray();
        foreach (var json in jsonArray)
        {
            if (json == null)
                continue;
            var apartments = new List<JsonNode>()
            {
                json["neighborhood"] ?? json["city"] ?? string.Empty,
                DateTime.ParseExact(json["scraping_time"].GetValue<string>().Substring(0, 10), "dd/MM/yyyy", CultureInfo.InvariantCulture),
                json["breadcrumb"] ?? json["real_estate_type"] ?? string.Empty,
                json["phone"]?.GetValue<string>().Replace(".", "") ?? string.Empty,
                json["rooms"] ?? json["room_count"] ?? string.Empty,
                json["size"] ?? json["area"] ?? string.Empty,
                json["energy"] ??json["DPE_string"] ?? string.Empty
            };
    
            transformed.Add(apartments);
        }
    
        return transformed;
    }

    public static JsonObject BuildJsonBodyForCreatingProspList(string listTitle, string[] tags, string nocrmUserEmail) {
        JsonObject jsonObject = new JsonObject();
        jsonObject["title"] = listTitle;

        JsonArray jsonArray = new JsonArray();
        jsonArray.Add(new JsonArray("Neighborhood", "Parsing Date", "Type", "Téléphone", "Rooms", "Size", "Energy"));

        jsonObject["content"] = jsonArray;
        jsonObject["description"] = listTitle;
        jsonObject["tags"] = new JsonArray(tags.Select(t => (JsonNode)t).ToArray());
        jsonObject["user_id"] = nocrmUserEmail;

        return jsonObject;
    }
}