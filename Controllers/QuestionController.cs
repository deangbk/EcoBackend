using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Survey_Backend.Data;
using Survey_Backend.DTO;
using Survey_Backend.Helpers;
using Survey_Backend.Models;
using System.Net;

namespace Survey_Backend.Controllers {
	[Route("api/[controller]")]
	[ApiController]
	public class QuestionController : ControllerBase {
		private readonly DataContext _dataContext;
		private readonly ILogger<QuestionController> _logger;

		public QuestionController(DataContext dataContext, ILogger<QuestionController> logger) {
			_dataContext = dataContext;
			_logger = logger;
		}

		// -----------------------------------------------------

		// Gets all groups/dimension in the database
		[HttpGet("getallgroups")]
		public async Task<IActionResult> GetAllGroups() {
			var listNames = await _dataContext.Dimensions
				.Select(x => x.ToTable(true))
				.ToListAsync();

			return Ok(listNames);
		}

		// -----------------------------------------------------

		[HttpPost("createresponse")]
		public async Task<IActionResult> CreateResponse() {
			return NoContent();
		}

		[HttpGet("getresponse")]
		public async Task<IActionResult> GetResponse([FromQuery] GetResponse dto) {
			QuestionResponse? response = await _dataContext.QuestionResponses
				.FirstOrDefaultAsync(x => x.QuestionId == dto.questionId && x.ResponderId == dto.responderId);
			if (response == null)
				return NotFound(string.Format("No response found for question {0} from responder {1}",
					dto.questionId.ToString("N"), dto.responderId.ToString("N")));

			return Ok(new Dictionary<string, object>() {
				["answer-score"] = response.AnswerScore ?? 0,
				["answer-text"] = response.AnswerText ?? "",
				["response-date"] = response.ResponseDate,
			});
		}
	}
}
