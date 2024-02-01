using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;

namespace FunctionApp1
{
    public class DurableModel
    {
        public DateTime? Value { get; set; }

        public DateTime? Get() => Value;

        public void Set(DateTime value)
        {
            if (value > DateTime.Now) value = DateTime.Now;
            if (value > Value) Value = value;
        }

        [Function(nameof(DurableModel))]
        public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
        {
            return dispatcher.DispatchAsync<DurableModel>();
        }
    }

    public class Function1(ILoggerFactory loggerFactory)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<Function1>();

        [Function("Trigger")]
        public async Task<HttpResponseData> Trigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient starter
        )
        {
            await starter.ScheduleNewOrchestrationInstanceAsync(nameof(Orchestrator));

            var response = req.CreateResponse(HttpStatusCode.OK);

            return response;
        }

        [Function(nameof(Orchestrator))]
        public async Task Orchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context,
            FunctionContext functionContext)
        {
            var entityId = new EntityInstanceId(nameof(DurableModel), "test:issue-246");
            // it throws here
            var value = await context.Entities.CallEntityAsync<DateTime?>(entityId, "Get");

        }
    }
}
