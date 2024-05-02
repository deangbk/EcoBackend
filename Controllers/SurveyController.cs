using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Survey_Backend.Data;
using Survey_Backend.Helpers;
using Survey_Backend.Models;
using Survey_Backend.DTO;

using static Survey_Backend.Helpers.SurveyHelpers;

namespace Survey_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/[controller]")]
	[Authorize]
	[ApiController]
	public class SurveyController : ControllerBase {
		private readonly DataContext _dataContext;
		private readonly ILogger<QuestionController> _logger;

		public SurveyController(DataContext dataContext, ILogger<QuestionController> logger) {
			_dataContext = dataContext;
			_logger = logger;
		}

		// -----------------------------------------------------

		[HttpPost("create")]
		public async Task<IActionResult> CreateSurveySet([FromBody] SurveyCreate surveyCreate) {
			{
				bool bExist = await _dataContext.Surveys.AnyAsync(x => x.ProjectName == surveyCreate.ProjectName);
				if (bExist)
					_logger.LogWarning("Another survey set with the same project name was found");
			}

			Survey newSurvey = new() {
				SurveyCreatedDate = DateTime.Now,
				ProjectName = surveyCreate.ProjectName,
				SurveyDescription = surveyCreate.SurveyDescription,
			};

			_dataContext.Surveys.Add(newSurvey);

			await _dataContext.SaveChangesAsync();
			return Ok(newSurvey.SurveySetId);
		}

		[HttpDelete("delete/{sid}")]
		public async Task<IActionResult> DeleteSurveySet([FromRoute] int sid) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);

			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			_dataContext.Surveys.Remove(survey);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Gets survey basic info
		/// </summary>
		[HttpGet("get/{sid}")]
		public async Task<IActionResult> GetSurveyInfo([FromRoute] int sid) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var countResponder = await _dataContext.Departments
				.Select(x => x.Population)
				.SumAsync();

			return Ok(new JsonTable() {
				["date_created"] = survey.SurveyCreatedDate,
				["project_name"] = survey.ProjectName,
				["description"] = survey.SurveyDescription,
				["responder_count"] = countResponder,
			});
		}

		/// <summary>
		/// Gets survey question list
		/// </summary>
		[HttpGet("getquestions/{sid}")]
		public async Task<IActionResult> GetSurveyQuestions([FromRoute] int sid) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var listQuestion = await GetAllQuestionsInSurveySet(
				_dataContext, survey.SurveySetId);

			return Ok(listQuestion);
		}

		/// <summary>
		/// Gets survey responders
		/// </summary>
		[HttpGet("getresponders/{sid}")]
		public async Task<IActionResult> GetSurveyResponders([FromRoute] int sid) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);

			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var listResponder = await Queries.ResponsesBySID(_dataContext, sid)
				.Select(x => new object[] { x.ResponderId, x.ResponseDate })
				.ToListAsync();

			return Ok(listResponder);
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets the roles of all survey responders
		/// </summary>
		[HttpGet("getallroles/{sid}")]
		public async Task<IActionResult> GetAllRoles([FromRoute] int sid) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var listResponders = new HashSet<Guid>(
				await Queries.ResponsesBySID(_dataContext, sid)
					.Select(x => x.ResponderId)
					.Distinct()
					.ToListAsync());

			var listNames = await _dataContext.ResponderInfos
				.Where(x => x.SurveySetId == sid && listResponders.Contains(x.ResponderId))
				.Select(x => x.RoleName)
				.ToListAsync();

			// DistinctBy can't be translated to a SQL query
			return Ok(listNames.ToHashSet());
		}

		/// <summary>
		/// Gets the departments of all survey responders
		/// </summary>
		[HttpGet("getalldepts_tree/{sid}")]
		public async Task<IActionResult> GetAllDeptsTree([FromRoute] int sid) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var listDepts = await GetAllDepartmentsInSurveySet(_dataContext, survey);
			var deptTree = DepartmentHelpers.CreateDepartmentTree(listDepts);

			var resTables = deptTree.Select(x => x.ToTable()).ToList();

			return Ok(resTables);
		}

		/// <summary>
		/// Gets the departments of all survey responders
		/// </summary>
		[HttpGet("getalldepts_flat/{sid}")]
		public async Task<IActionResult> GetAllDeptsFlat([FromRoute] int sid) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var listDepts = await GetAllDepartmentsInSurveySet(_dataContext, survey);
			var deptTree = DepartmentHelpers.CreateDepartmentTree2(listDepts);

			var resTables = deptTree.Select(x => x.ToTable()).ToList();

			return Ok(resTables);
		}

		/// <summary>
		/// Gets all groups/dimension in the survey
		/// </summary>

		[HttpGet("getallgroups/{sid}")]
		public async Task<IActionResult> GetAllGroups([FromRoute] int sid) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var listGroupId = await Queries.QuestionsBySID(_dataContext, sid)
				.Select(x => x.DimensionId)
				.Distinct()
				.ToListAsync();

			var listGroups = await _dataContext.Dimensions
				.Where(x => listGroupId.Any(y => y == x.Id))
				.Select(x => x.ToTable(true))
				.ToListAsync();

			return Ok(listGroups);
		}
	}
}
