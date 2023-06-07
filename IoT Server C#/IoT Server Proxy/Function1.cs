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
using Microsoft.Azure.Devices.Common.Exceptions;
using static Microsoft.Azure.Amqp.Serialization.SerializableType;
using Microsoft.Azure.Amqp.Framing;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net;
using System.Collections.Generic;
//using Newtonsoft.Json;

class C2DMessage
{
    public string message { get; set; }
}

class DeviceData
{
    public string deviceId { get; set; }
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
                return new BadRequestErrorMessageResult(ex.Message);
            }
        }

        [FunctionName("CreateDevice")]
        public static async Task<IActionResult> CreateDevice(
            [HttpTrigger(AuthorizationLevel.Function, "post", "put", Route = "devices/{deviceId}/create")] HttpRequest req,
            ILogger log, string deviceId)
        {
            var connectionString = GetConnectionString();
            var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

            try
            {
                // Create a new device
                var device = new Device(deviceId);
                device = await registryManager.AddDeviceAsync(device);

                log.LogInformation($"Device '{device.Id}' created successfully.");
                var response = new { status = "Created" };
                return new OkObjectResult(response);

            }
            catch (DeviceAlreadyExistsException)
            {
                log.LogInformation($"Device '{deviceId}' already exists.");
                var response = new { status = "Already Exists" };
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogInformation($"Error creating device: {ex.Message}");
                var error = new { Message = "An error occurred.", ErrorCode = 500 };
                // Return an HTTP 500 error with the error object as the response body
                return new OkObjectResult(error);
            }
            finally
            {
                await registryManager.CloseAsync();
            }
        }

        public static async Task<IActionResult> deletegetdevice(
            [HttpTrigger(AuthorizationLevel.Function, "delete", "get", Route = "devices/{deviceId}")] HttpRequest req,
            ILogger log, string deviceId)
        {
            var connectionString = GetConnectionString();

            if (req.Method == HttpMethods.Delete)
            {

                var registryManager = RegistryManager.CreateFromConnectionString(connectionString);

                try
                {
                    await registryManager.RemoveDeviceAsync(deviceId);
                    var response = new { status = "Device deleted" };
                    return new OkObjectResult(response) { StatusCode = 201 };
                }
                catch (Exception ex)
                {
                    bool isNotFound = Regex.IsMatch(ex.Message, @"\bErrorCode:DeviceNotFound\b", RegexOptions.IgnoreCase);
                    if (isNotFound)
                    {
                        /* return device not found */
                        var response = new { status = "Device not found" };
                        return new OkObjectResult(response) { StatusCode = 204 };
                    }
                    else
                    {
                        var response = new { status = "Error" };
                        return new BadRequestObjectResult(response) { StatusCode = 500 };
                    }

                }
                finally
                {
                    await registryManager.CloseAsync();
                }
            }
            else if (req.Method == HttpMethods.Get)
            {
                var registryManager = RegistryManager.CreateFromConnectionString(connectionString);
                try
                {
                    // Check if the device exists
                    Device device = await registryManager.GetDeviceAsync(deviceId);
                    Console.WriteLine(device);
                    if (device != null)
                    {
                        var idresponse = new { status = "Device exists" };
                        return new OkObjectResult(idresponse) { StatusCode = 203 };
                    }
                    else
                    {
                        var idresponse = new { status = "Device not found" };
                        return new OkObjectResult(idresponse) { StatusCode = 204 };
                    }

                    //return device != null;
                }
                catch (Exception ex)
                {
                    var idresponse = new { Status = ex.Message };
                    return new OkObjectResult(idresponse) { StatusCode = 500 };

                }
                finally
                {
                    // Close the registry manager connection
                    await registryManager.CloseAsync();
                }
            }
            else
            {
                // Return a 405 Method Not Allowed response for unsupported methods
                return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
            }
        }

        [FunctionName("GetDevices")]
        public static async Task<IActionResult> GetDevices(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "devices")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request. Get all devices");
            string connectionString = GetConnectionString();
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            try
            {
                var query = registryManager.CreateQuery("SELECT * FROM devices");
                var twins = await query.GetNextAsTwinAsync();
                var deviceList = new List<DeviceData>();
                foreach (var twin in twins)
                {
                    deviceList.Add(new DeviceData { deviceId = twin.DeviceId });
                }
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(deviceList, options);
                return new OkObjectResult(json) { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                var connectionstringResponse = new { Status = ex.Message };
                return new OkObjectResult(connectionstringResponse) { StatusCode = 500 };
            }
            finally
            {
                await registryManager.CloseAsync();
            }

        }

        [FunctionName("GetDeviceConnection")]
        public static async Task<IActionResult> GetDeviceConnection(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "devices/device/{deviceId}/connection/connectionstring")] HttpRequest req,
            ILogger log, string deviceId)
        {
            log.LogInformation("C# HTTP trigger function processed a request. get connection string");

            var connectionString = GetConnectionString();
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            try
            {
                var device = await registryManager.GetDeviceAsync(deviceId);
                if (device == null)
                {
                    var errorMessage = new { status = "Device not exist" };
                    return new OkObjectResult(errorMessage) { StatusCode = 204 };
                }

                int startIndex = connectionString.IndexOf('=') + 1;
                int endIndex = connectionString.IndexOf('.', startIndex);
                string Hostname = connectionString.Substring(startIndex, endIndex - startIndex);


                string primaryKey = device.Authentication.SymmetricKey.PrimaryKey;
                string secondaryKey = device.Authentication.SymmetricKey.SecondaryKey;
                var primaryString = "Hostname=" + Hostname + ".azure-devices.net;DeviceId=" + deviceId + ";SharedAccesKey=" + primaryKey;
                var secondaryString = "Hostname=" + Hostname + ".azure-devices.net;DeviceId=" + deviceId + ";SharedAccesKey=" + secondaryKey; ;

                var responseMessage = new { device = deviceId, primary = primaryString, secondary = secondaryString };
                return new OkObjectResult(responseMessage) { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                var errorMessage = new { Status = ex.Message };
                return new OkObjectResult(errorMessage) { StatusCode = 500 };
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