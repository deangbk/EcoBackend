using System.Net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Survey_Backend.Data;
using Survey_Backend.Helpers;
using Survey_Backend.Migrations;
using Survey_Backend.Models;

namespace Survey_Backend.Controllers {
	[Route("api/[controller]")]
	//[Authorize]
	[ApiController]
	public class ResponderController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<QuestionController> _logger;

		public ResponderController(DataContext dataContext, ILogger<QuestionController> logger) {
			_dataContext = dataContext;
			_logger = logger;
		}

		// -----------------------------------------------------

		// Gets all roles in the database
		[HttpGet("getallroles")]
		public async Task<IActionResult> GetAllRoles() {
			var listNames = await _dataContext.Roles
				.Select(x => x.ToTable(true))
				.ToListAsync();

			return Ok(listNames);
		}

		// Gets all departments in the database
		[HttpGet("getalldepts")]
		public async Task<IActionResult> GetAllDepts() {
			var listNames = await _dataContext.Departments
				.Select(x => x.ToTable(true))
				.ToListAsync();

			return Ok(listNames);
		}

		// Gets all generations in the database
		[HttpGet("getallgenerations")]
		public async Task<IActionResult> GetAllGenerations() {
			var listNames = await _dataContext.Generations
				.Select(x => x.ToTable(true))
				.ToListAsync();

			return Ok(listNames);
		}

		// -----------------------------------------------------

		[HttpGet("getrole/{id}")]
		public async Task<IActionResult> GetRole([FromRoute] int id) {
			Role? role = await _dataContext.Roles.FindAsync(id);
			if (role == null)
				return NotFound(string.Format("No role found with ID = {0}", id));

			return Ok(role.ToTable(true));
		}

		[HttpPost("createrole")]
		public async Task<IActionResult> CreateRole() {
			return NoContent();
		}

		[HttpDelete("deleterole")]
		public async Task<IActionResult> DeleteRole([FromRoute] int id) {
			Role? role = await _dataContext.Roles.FindAsync(id);
			if (role == null)
				return NotFound(string.Format("No role found with ID = {0}", id));

			_dataContext.Roles.Remove(role);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		[HttpGet("getdept/{id}")]
		public async Task<IActionResult> GetDepartment([FromRoute] int id) {
			Department? department = await _dataContext.Departments.FindAsync(id);
			if (department == null)
				return NotFound(string.Format("No department found with ID = {0}", id));

			return Ok(department.ToTable(true));
		}

		[HttpPost("createdept")]
		public async Task<IActionResult> CreateDepartment() {
			return NoContent();
		}

		[HttpDelete("deletedept")]
		public async Task<IActionResult> DeleteDepartment([FromRoute] int id) {
			Department? department = await _dataContext.Departments.FindAsync(id);
			if (department == null)
				return NotFound(string.Format("No department found with ID = {0}", id));

			_dataContext.Departments.Remove(department);

			await _dataContext.SaveChangesAsync();
			return Ok();
		}

		// -----------------------------------------------------

		[AllowAnonymous]
		[HttpGet("getgeneration/{year}")]
		public async Task<IActionResult> GetGeneration([FromRoute] int year) {
			Generation? generation = await _dataContext.Generations.FirstOrDefaultAsync(x => 
				year >= x.YearRangeLower && year <= x.YearRangeUpper);
			if (generation == null)
				return NotFound(string.Format("No generation is associated with year {0}", year));

			return Ok(generation.ToTable(false));
		}

		// -----------------------------------------------------

		[HttpPost("createresponder")]
		public async Task<IActionResult> CreateResponder() {
			return NoContent();
		}

		[HttpGet("getresponder/{rid}")]
		public async Task<IActionResult> GetResponder([FromRoute] Guid rid) {
			ResponderInfo? responder = await _dataContext.ResponderInfos.FindAsync(rid);
			if (responder == null) {
				return NotFound(string.Format("No responder found with ID = {0}",
					rid.ToString("N")));
			}

			return Ok(new Dictionary<string, object>() {
				["id"] = rid.ToString("N"),
				["role_id"] = responder.RoleId,
				["role_name"] = responder.RoleName ?? "",
				["dept_id"] = responder.DepartmentId,
				["dept_name"] = responder.DepartmentName ?? "",

				["survey_id"] = responder.SurveySetId,
				["survey_completed"] = responder.SurveyCompletedTime != null,
				["survey_complete_time"] = responder.SurveyCompletedTime ?? new DateTime(),

				["responder_ip_addr"] = responder.IPAddress ?? IPAddress.None,
			});
		}
	}
}
