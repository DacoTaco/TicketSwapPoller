using System.Text.Json;

namespace TicketSwapPoller.Json;

public class LowerCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        return $"{name[0].ToString().ToLower()}{name[1..]}";
    }
}
