using Microsoft.AspNetCore.SignalR;

namespace VotingApp.Models.JsonHandling
{
    public static class JsonHandlingHelpers
    {
        public static Action<NewtonsoftJsonHubProtocolOptions> GetJsonHandlerOptions()
        {
            return options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Serialize;
                options.PayloadSerializerSettings.MaxDepth = 3;
                options.PayloadSerializerSettings.Converters = new List<Newtonsoft.Json.JsonConverter>()
                {
                    new BigIntegerConverter()
                };
            };
        }
    }
}
