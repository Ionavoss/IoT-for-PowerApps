using Microsoft.Azure.Devices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using System.Text;
using System.Web.Http;

class C2DMessage
{
    public string message { get; set; }
}

namespace IoTServer
{
    public static class IoTProxy
    {
        [FunctionName("SendMessageFunction")]
        public static async Task<IActionResult> SendMessageFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "c2d/{deviceId}")] HttpRequest req,
            ILogger log, string deviceId)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Extract the device ID and message from the request body
            C2DMessage cloudmessage = await JsonSerializer.DeserializeAsync<C2DMessage>(req.Body);

            if (string.IsNullOrEmpty(cloudmessage?.message))
            {
                return new BadRequestObjectResult("Invalid request body. Device and message must be provided.");
            }

            string connectionString = GetConnectionString();
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.UTF8.GetBytes(cloudmessage.message));

            commandMessage.Ack = Microsoft.Azure.Devices.DeliveryAcknowledgement.Full;
            commandMessage.MessageId = Guid.NewGuid().ToString();

            try
            {
                // Send the message to the specified device
                await serviceClient.SendAsync(deviceId, commandMessage);
                log.LogInformation($"Message sent to device '{deviceId}' successfully!");
                return new OkObjectResult("Message sent successfully!");
            }
            catch (Exception ex)
            {
                log.LogError($"Failed to send message to device '{deviceId}'. Exception: {ex.Message}");
                return new ObjectResult("Failed to send message to device.") { StatusCode = 500 };
            }
        }

        [FunctionName("GetDeviceStatus")]
        public static async Task<IActionResult> GetDeviceStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "devices/{deviceId}/status")] HttpRequest req,
            ILogger log, string deviceId)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Extract the device ID and message from the request body

            string connectionString = GetConnectionString();

            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);


            try
            {
                var twin = await registryManager.GetTwinAsync(deviceId);
                bool connected = (twin.Status.ToString() == "Enabled");
                var response = new { status = connected ? "Connected" : "Not Connected" };
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new BadRequestErrorMessageResult(ex.Message );
            }
        }

        // Helper
        private static string GetConnectionString()
        {
            // Retrieve the IoT Hub connection string from your configuration or secrets
            // You can store it in Azure Key Vault or any other secure location
            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string connectionString = config.GetConnectionString("MyConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = Environment.GetEnvironmentVariable("MyConnectionString");
            }

            return connectionString;
        }
    }
}
