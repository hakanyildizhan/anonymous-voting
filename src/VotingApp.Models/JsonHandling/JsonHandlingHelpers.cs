using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace VotingApp.Models.JsonHandling
{
    public static class JsonHandlingHelpers
    {
        public static Action<NewtonsoftJsonHubProtocolOptions> GetJsonHandlerOptions()
        {
            return options =>
            {
                options.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                options.PayloadSerializerSettings.MaxDepth = 3;
                options.PayloadSerializerSettings.Converters = new List<JsonConverter>()
                {
                    new BouncyCastleBigIntegerConverter()
                };
            };
        }
    }
}
