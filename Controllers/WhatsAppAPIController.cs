using Microsoft.AspNetCore.Mvc;
using System;

namespace WhatsApp_API.Controllers
{
	[Route("api/meta/webhook")]
	[ApiController]
	public class WebhookController : ControllerBase
	{
		private const string VerifyToken = "TOKEN";

		[HttpGet]
		public IActionResult Verify([FromQuery] string hub_mode, [FromQuery] string hub_verify_token, [FromQuery] string hub_challenge)
		{
			if (hub_mode == "subscribe" && hub_verify_token == VerifyToken)
			{
				return Ok(hub_challenge);
			}
			return Forbid();
		}
	}
}
