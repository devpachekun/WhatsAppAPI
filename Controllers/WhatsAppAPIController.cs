using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WhatsApp_API.Controllers
{
	[Route("api/meta/webhook")]
	[ApiController]
	public class WebhookController : ControllerBase
	{
		private readonly string _verifyToken;
		private readonly string _accessToken;
		private readonly string _phoneNumberId;

		public WebhookController(IConfiguration configuration)
		{
			_verifyToken = configuration["WhatsAppSettings:VerifyToken"];
			_accessToken = configuration["WhatsAppSettings:AccessToken"];
			_phoneNumberId = configuration["WhatsAppSettings:PhoneNumberId"];
		}

		[HttpGet]
		public IActionResult Verify([FromQuery(Name = "hub.mode")] string hubMode, [FromQuery(Name = "hub.verify_token")] string hubVerifyToken, [FromQuery(Name = "hub.challenge")] string hubChallenge)
		{
			if (hubMode == "subscribe" && hubVerifyToken == _verifyToken)
			{
				return Ok(hubChallenge);
			}
			return Forbid();
		}

		[HttpPost]
		public async Task<IActionResult> ReceiveMessage([FromBody] JsonElement data)
		{
			try
			{
				if (data.TryGetProperty("entry", out JsonElement entryElement) && entryElement.ValueKind == JsonValueKind.Array)
				{
					foreach (var entry in entryElement.EnumerateArray())
					{
						if (entry.TryGetProperty("changes", out JsonElement changesElement) && changesElement.ValueKind == JsonValueKind.Array)
						{
							foreach (var change in changesElement.EnumerateArray())
							{
								if (change.TryGetProperty("value", out JsonElement valueElement))
								{
									if (valueElement.TryGetProperty("messages", out JsonElement messagesElement) && messagesElement.ValueKind == JsonValueKind.Array)
									{
										foreach (var message in messagesElement.EnumerateArray())
										{
											if (message.TryGetProperty("from", out JsonElement fromElement) &&
												message.TryGetProperty("text", out JsonElement textElement) &&
												textElement.TryGetProperty("body", out JsonElement bodyElement))
											{
												string from = fromElement.GetString();
												string messageBody = bodyElement.GetString();

												// Responde al mensaje recibido
												await SendMessage(from, "Este es un mensaje automático de respuesta.");
											}
										}
									}
								}
							}
						}
					}
				}

				return Ok();
			}
			catch (Exception ex)
			{
				// Maneja cualquier excepción que ocurra durante el procesamiento
				return StatusCode(500, $"Internal server error: {ex.Message}");
			}
		}

		private async Task SendMessage(string to, string message)
		{
			var client = new HttpClient();
			var url = $"https://graph.facebook.com/v20.0/{_phoneNumberId}/messages";
			var payload = new
			{
				messaging_product = "whatsapp",
				to = to,
				type = "text",
				text = new
				{
					body = message
				}
			};

			var request = new HttpRequestMessage(HttpMethod.Post, url);
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
			request.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

			var response = await client.SendAsync(request);
			response.EnsureSuccessStatusCode();
		}
	}
}
