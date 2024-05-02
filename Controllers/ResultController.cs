using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Survey_Backend.Data;
using Survey_Backend.Helpers;
using Survey_Backend.Models;
using Survey_Backend.DTO;

using static Survey_Backend.Helpers.SurveyHelpers;
using static Survey_Backend.Helpers.ResultHelpers;

namespace Survey_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/[controller]")]
	//[Authorize]
	[ApiController]
	public class ResultController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<QuestionController> _logger;

		public ResultController(DataContext dataContext, ILogger<QuestionController> logger) {
			_dataContext = dataContext;
			_logger = logger;
		}

		// -----------------------------------------------------

		private static ResultsFilter.Type _GetFilterTypeFromStringBasic(string type) => type switch {
			"dims" => ResultsFilter.Type.Dimension,
			"dept" => ResultsFilter.Type.Department,
			"role" => ResultsFilter.Type.Role,
			"gnra" => ResultsFilter.Type.Generation,
			"gender" => ResultsFilter.Type.Gender,
			_ => ResultsFilter.Type.Invalid,
		};
		private static ResultsFilter.Type _GetFilterTypeFromStringAll(string type) => type switch {
			"dims" => ResultsFilter.Type.Dimension,
			"dept" => ResultsFilter.Type.Department,
			"role" => ResultsFilter.Type.Role,
			"gnra" => ResultsFilter.Type.Generation,
			"age" => ResultsFilter.Type.Age,
			"birth" => ResultsFilter.Type.BirthYear,
			"service" => ResultsFilter.Type.ServiceMonth,
			"gender" => ResultsFilter.Type.Gender,
			_ => ResultsFilter.Type.Invalid,
		};

		/// <summary>
		/// Returns summary of survey results
		///	<para>- Overall score</para>
		///	<para>- Score by dimensions</para>
		/// </summary>
		[HttpGet("overview/{sid}")]
		public async Task<IActionResult> GetResultsOverview([FromRoute] int sid, [FromQuery] ResultsOverview dto) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			ResultsFilter.Type[] getTypes;
			try {
				getTypes = dto.gets.Split(',')
					.Where(x => x.Length > 0)
					.Select(x => _GetFilterTypeFromStringBasic(x))
					.ToArray();
				foreach (var i in getTypes) {
					if (i == ResultsFilter.Type.Invalid)
						throw new Exception();
				}
			}
			catch (Exception e) {
				return BadRequest("Invalid category get type");
			}

			var queryResponses = Queries.GetQuantitativeResponses(_dataContext, sid);
			var queryExpanded = GetExpandedResponseQuery(_dataContext, sid, queryResponses);

			var responses = await queryExpanded.ToListAsync();

			SurveyStatisticsResult statsAll = CalculateSurveyStatistics(responses);

			JsonTable addTable = new();
			if (getTypes.Contains(ResultsFilter.Type.Dimension)) {
				var table = await CreateResultTableByDimension(
					_dataContext, survey.ProjectName, responses);
				addTable["score_dimensions"] = table;
			}
			if (getTypes.Contains(ResultsFilter.Type.Department)) {
				var table = await CreateResultTableByDepartment(
					_dataContext, survey, responses, false);
				addTable["score_depts"] = table;
			}
			if (getTypes.Contains(ResultsFilter.Type.Role)) {
				var table = await CreateResultTableByRole(_dataContext, survey, responses);
				addTable["score_roles"] = table;
			}
			if (getTypes.Contains(ResultsFilter.Type.Generation)) {
				var table = await CreateResultTableByGeneration(_dataContext, survey, responses);
				addTable["score_generations"] = table;
			}
			if (getTypes.Contains(ResultsFilter.Type.Gender)) {
				var table = CreateResultTableByGender(responses);
				addTable["score_genders"] = table;
			}

			return Ok(CreateResultTable(statsAll, addTable));
		}

		private static Func<ExpandedResponseData, bool> _GetFilterPredicateByCategory(
			ResultsFilter.Type category, int filterId) {

			Func<ExpandedResponseData, bool> predicate = category switch {
				ResultsFilter.Type.Dimension => (x => x.dimensionId == filterId),
				ResultsFilter.Type.Department => (x => x.departmentChain.Contains(filterId)),
				ResultsFilter.Type.Role => (x => x.roleId == filterId),
				ResultsFilter.Type.Generation => (x => x.generationId == filterId),
				ResultsFilter.Type.Gender => (x => x.gender == filterId),
				_ => (x => false),
			};
			return predicate;
		}
		private static async Task<JsonTable> _CreateInfoTableFromCategory(
			DataContext dataContext, ResultsFilter.Type category, int filterId) {

			switch (category) {
				case ResultsFilter.Type.Dimension: {
					Dimension? dimension = await Queries.GetDimensionFromId(dataContext, filterId);
					return dimension!.ToTable(false);
				}
				case ResultsFilter.Type.Department: {
					Department? department = await Queries.GetDepartmentFromId(dataContext, filterId);
					return department!.ToTable(false);
				}
				case ResultsFilter.Type.Role: {
					Role? role = await Queries.GetRoleFromId(dataContext, filterId);
					return role!.ToTable(false);
				}
				case ResultsFilter.Type.Generation: {
					Generation? generation = await Queries.GetGenerationFromId(dataContext, filterId);
					return generation!.ToTable(false);
				}
				case ResultsFilter.Type.Gender: {
					return new JsonTable() {
						["gender"] = filterId,
					};
				}
			}
			return new();
		}

		/// <summary>
		/// Returns survey results by a specific category
		/// </summary>
		[HttpGet("by_category/{sid}")]
		public async Task<IActionResult> GetResultsByCategoryFilter(
			[FromRoute] int sid, [FromQuery] ResultsFilter filter) {

			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			ResultsFilter.Type filterType = _GetFilterTypeFromStringBasic(filter.category);
			if (filterType == ResultsFilter.Type.Invalid)
				return BadRequest("Invalid filter category");

			List<ExpandedResponseData> responses = (await GetSurveyExpandedResponses(_dataContext, sid))
				.Where(_GetFilterPredicateByCategory(filterType, filter.id))
				.ToList();

			JsonTable resTable;
			{
				JsonTable resTableHead = await _CreateInfoTableFromCategory(
					_dataContext, filterType, filter.id);

				// Overall responses for the group
				SurveyStatisticsResult statsGroup = CalculateSurveyStatistics(responses);

				List<(string, List<JsonTable>)> listSubTable = new();
				{
					if (filterType != ResultsFilter.Type.Dimension) {
						// Calculate response breakdown by dimension
						var table = await CreateResultTableByDimension(_dataContext, survey.ProjectName, responses);
						listSubTable.Add(("score_dimensions", table));
					}

					if (filterType != ResultsFilter.Type.Department) {
						// Calculate response breakdown by department
						var table = await CreateResultTableByDepartment(_dataContext, survey,
							responses, filter.top_depts_only);
						listSubTable.Add(("score_depts", table));
					}

					if (filterType != ResultsFilter.Type.Role) {
						// Calculate response breakdown by role
						var table = await CreateResultTableByRole(_dataContext, survey, responses);
						listSubTable.Add(("score_roles", table));
					}

					if (filterType != ResultsFilter.Type.Generation) {
						// Calculate response breakdown by generation
						var table = await CreateResultTableByGeneration(_dataContext, survey, responses);
						listSubTable.Add(("score_generations", table));
					}

					if (filterType != ResultsFilter.Type.Gender) {
						// Calculate response breakdown by gender
						var table = CreateResultTableByGender(responses);
						listSubTable.Add(("score_genders", table));
					}
				}

				resTable = CreateResultTable(statsGroup);
				resTable["group"] = resTableHead;
				foreach (var (k, v) in listSubTable)
					resTable.Add(k, v);
			}

			return Ok(resTable);
		}

		/// <summary>
		/// Returns survey results by a specific category, at a specific filter and id
		/// </summary>
		[HttpGet("by_category_sp/{sid}")]
		public async Task<IActionResult> GetResultsByCategoryFilter_Specific(
			[FromRoute] int sid, [FromQuery] ResultsFilterSpecific filter) {

			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			ResultsFilter.Type filterType = _GetFilterTypeFromStringBasic(filter.category);
			ResultsFilter.Type getType = _GetFilterTypeFromStringBasic(filter.category_get);
			if (filterType == ResultsFilter.Type.Invalid || getType == ResultsFilter.Type.Invalid)
				return BadRequest("Invalid filter category");

			List<ExpandedResponseData> responses = (await GetSurveyExpandedResponses(_dataContext, sid))
				.Where(_GetFilterPredicateByCategory(filterType, filter.id))
				.ToList();

			JsonTable resTable;
			{
				JsonTable resTableHead = await _CreateInfoTableFromCategory(
					_dataContext, filterType, filter.id);
				JsonTable? resTableData = null;
				string dataName = "";

				switch (getType) {
					case ResultsFilter.Type.Dimension: {
						var table = await CreateResultTableByDimension(_dataContext, survey.ProjectName, responses);
						resTableData = table.Find(x => (int)x["dimension_id"] == filter.id_get);
						dataName = "score_dimensions";
						break;
					}
					case ResultsFilter.Type.Department: {
						var table = await CreateResultTableByDepartment(_dataContext, survey, responses);
						resTableData = table.Find(x => (int)x["department_id"] == filter.id_get);
						dataName = "score_depts";
						break;
					}
					case ResultsFilter.Type.Role: {
						var table = await CreateResultTableByRole(_dataContext, survey, responses);
						resTableData = table.Find(x => (int)x["role_id"] == filter.id_get);
						dataName = "score_roles";
						break;
					}
					case ResultsFilter.Type.Generation: {
						var table = await CreateResultTableByGeneration(_dataContext, survey, responses);
						resTableData = table.Find(x => (int)x["generation_id"] == filter.id_get);
						dataName = "score_generations";
						break;
					}
					case ResultsFilter.Type.Gender: {
						var table = CreateResultTableByGender(responses);
						resTableData = table.Find(x => (int)x["gender"] == filter.id_get);
						dataName = "score_genders";
						break;
					}
				}

				resTable = new JsonTable();
				resTable["group"] = resTableHead;
				resTable[dataName] = resTableData!;
			}

			return Ok(resTable);
		}

		/// <summary>
		/// Returns survey results of direct subdepts, no extra filtering
		/// </summary>
		[HttpGet("by_subdepts/{sid}/{deptId}")]
		public async Task<IActionResult> GetResultsSubdepts([FromRoute] int sid, [FromRoute] int deptId) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			Department? department = await Queries.GetDepartmentFromId(_dataContext, deptId);
			if (department == null)
				return BadRequest(string.Format("No department found with ID = {0}", deptId));

			int deptLevel = department.TreeLevel;

			// All subdepts, including subdepts of those subdepts and so on
			var subDepartmentsAll = await _dataContext.GetAllSubDepartments(deptId)
				.Select(x => x.Id)
				.ToListAsync();

			// Map of all depts, for searching by ID
			var departmentRefs = await _dataContext.Departments
				.Where(x => subDepartmentsAll.Any(y => y == x.Id))
				.ToDictionaryAsync(k => k.Id, x => x);

			// Direct subdepts only
			var subDepartmentsDirect = await _dataContext.Departments
				.Where(x => x.ParentDepartmentId == deptId)
				.Where(x => x.TreeLevel == deptLevel + 1)
				.Select(x => x.Id)
				.ToListAsync();

			var responses = await GetExpandedResponseQuery(_dataContext, sid,
				Queries.GetQuantitativeResponses(_dataContext, sid))
				.Where(x => subDepartmentsAll.Any(y => y == x.departmentId))
				.ToListAsync();

			Dictionary<int, SurveyStatisticsResult> statsByDept = await CalculateDepartmentsResults(
				_dataContext, responses, subDepartmentsAll);

			SurveyStatisticsResult statsOwn = statsByDept[deptId];
			List<(int id, SurveyStatisticsResult stats)> statsSub = subDepartmentsDirect
				.Select(x => (x, statsByDept[x]))
				.ToList();

			List<JsonTable> tableStatsSub = statsSub.Select(x => {
				Department subDept = departmentRefs[x.id];
				return CreateResultTable(x.stats, subDept.ToTable());
			}).ToList();

			return Ok(CreateResultTable(statsOwn,
				("score_subdepts", tableStatsSub)));
		}

		/// <summary>
		/// Returns survey results by years of service or age, grouped into ranges
		/// </summary>
		[HttpGet("by_range/{sid}")]
		public async Task<IActionResult> GetResultsByRangeFilter(
			[FromRoute] int sid, [FromQuery] ResultsRangeFilter filter) {

			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			ResultsFilter.Type filterType = _GetFilterTypeFromStringBasic(filter.category);
			ResultsFilter.Type rangeType = filter.category_range switch {
				"age" => ResultsFilter.Type.Age,
				"birth" => ResultsFilter.Type.BirthYear,
				"service" => ResultsFilter.Type.ServiceMonth,
				_ => ResultsFilter.Type.Invalid,
			};
			if (rangeType == ResultsFilter.Type.Invalid)
				return BadRequest("Invalid filter category");

			Func<ExpandedResponseData, bool> responsesFilter;
			if (filterType != ResultsFilter.Type.Invalid && filter.id != -1)
				responsesFilter = _GetFilterPredicateByCategory(filterType, filter.id);
			else responsesFilter = (x => true);

			List<(int, int)> ranges = new();
			try {
				var boundaries = filter.ranges.Split(',')
					.Select(x => int.Parse(x))
					.ToList();

				if (boundaries.Count < 2)
					throw new Exception();
				for (int i = 0; i < boundaries.Count - 1; ++i) {
					ranges.Add((boundaries[i], boundaries[i + 1]));
				}
			}
			catch (Exception) {
				return BadRequest("Invalid filter range");
			}

			List<ExpandedResponseData> responses = (await GetSurveyExpandedResponses(_dataContext, sid))
				.Where(responsesFilter)
				.ToList();

			Func<ExpandedResponseData, int, int> comparator = rangeType switch {
				ResultsFilter.Type.Age			=> ((x, r) => x.age.CompareTo(r)),
				ResultsFilter.Type.BirthYear	=> ((x, r) => x.yearBorn.CompareTo(r)),
				ResultsFilter.Type.ServiceMonth	=> ((x, r) => x.serviceMonths.CompareTo(r)),
				_ => (_, _) => 0,
			};
			List<List<ExpandedResponseData>> responseBuckets = ValueHelpers.SortIntoRanges(
				responses, ranges, comparator);

			List<JsonTable> tables = new();
			foreach (var (iList, range) in responseBuckets.Zip(ranges)) {
				var stats = CalculateSurveyStatistics(iList);

				JsonTable addTable = new() {
					["range"] = ValueHelpers.TupleToList(range),
				};

				if (filter.more) {
					var tableDimension = await CreateResultTableByDimension(
						_dataContext, survey.ProjectName, iList);
					var tableRole = await CreateResultTableByRole(
						_dataContext, survey, iList);
					var tableGender = CreateResultTableByGender(iList);

					addTable["score_dimensions"] = tableDimension;
					addTable["score_roles"] = tableRole;
					addTable["score_genders"] = tableGender;
				}

				tables.Add(CreateResultTable(stats, addTable));
			}

			return Ok(tables);
		}

		/// <summary>
		/// Returns survey results grouped by question number, optional filters by department and dimension
		/// </summary>
		[HttpGet("by_question_n/{sid}")]
		public async Task<IActionResult> GetQuestionsPercentageMap([FromRoute] int sid,
			[FromQuery] QuestionsResultsFilter filter) {

			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var expandedRespQuery = GetExpandedResponseQuery(_dataContext, sid,
				Queries.GetQuantitativeResponses(_dataContext, sid));

			ResultsFilter.Type filterType = _GetFilterTypeFromStringAll(filter.category);

			bool hasFilter = filterType switch {
				ResultsFilter.Type.Age or
				ResultsFilter.Type.BirthYear or
				ResultsFilter.Type.ServiceMonth
					=> (filter.range_l != -1 || filter.range_u != -1),
				_ => (filter.id != -1),
			};

			List<int> listDeptIdMatch = new();

			IQueryable<ExpandedResponseData> queryByFilter = expandedRespQuery;
			if (hasFilter) {
				switch (filterType) {
					case ResultsFilter.Type.Department: {
						bool bDeptExist = await _dataContext.Departments
							.Where(x => x.ProjectName == survey.ProjectName)
							.AnyAsync(x => x.Id == filter.id);
						if (!bDeptExist) {
							return BadRequest(string.Format(
								"No department found with ID = {0}", filter.id));
						}

						listDeptIdMatch = await _dataContext.GetAllSubDepartments(filter.id)
							.Select(x => x.Id)
							.ToListAsync();
						queryByFilter = expandedRespQuery
							.Where(x => listDeptIdMatch.Any(y => y == x.departmentId));

						/*
						// Somehow EF Core shits itself trying to translate this into a query
						queryByFilter = expandedRespQuery
							.Where(x => _dataContext.GetAllSubDepartments(filter.id)
								.Any(y => y.Id == x.departmentId));
						*/

						break;
					}
					case ResultsFilter.Type.Role:
						queryByFilter = expandedRespQuery
							.Where(x => filter.id == x.roleId);
						break;
					case ResultsFilter.Type.Generation:
						queryByFilter = expandedRespQuery
							.Where(x => filter.id == x.generationId);
						break;
					case ResultsFilter.Type.Age:
						queryByFilter = expandedRespQuery
							.Where(x => x.age >= filter.range_l && x.age < filter.range_u);
						break;
					case ResultsFilter.Type.BirthYear:
						queryByFilter = expandedRespQuery
							.Where(x => x.yearBorn >= filter.range_l && x.yearBorn < filter.range_u);
						break;
					case ResultsFilter.Type.ServiceMonth:
						queryByFilter = expandedRespQuery
							.Where(x => x.serviceMonths >= filter.range_l && x.serviceMonths < filter.range_u);
						break;
					case ResultsFilter.Type.Gender:
						queryByFilter = expandedRespQuery
							.Where(x => filter.id == x.gender);
						break;
					default:
						return BadRequest("Invalid filter category");
				}
			}

			// Filter by dimension
			IQueryable<ExpandedResponseData> queryByDimension = queryByFilter;
			if (filter.dim_id != -1) {
				queryByDimension = queryByFilter
					.Where(x => x.dimensionId == filter.dim_id);
			}

			// Select result with a single nightmare-inducing query
			var responsesGroup = await queryByDimension
				.GroupBy(x => x.questionId)
				.OrderBy(x => x.Key)
				.Select(x => new {
					question_id = x.Key,
					dimension_id = x.First().dimensionId,
					score_count = x.Count(),
					score_avg = x.Average(x => x.score),
					score_modes = new int[] {
						x.Count(x => x.score == 1),
						x.Count(x => x.score == 2),
						x.Count(x => x.score == 3),
						x.Count(x => x.score == 4),
						x.Count(x => x.score == 5),
					},
				})
				.ToListAsync();

			// ISSUE?: If query result contains no entries, it outputs an empty array instead of
			//			formatted entries with score_count of 0, probably could just handle it in the frontend

			return Ok(responsesGroup);
		}

		/// <summary>
		/// All I want for Christmas is for the pain to end all I want for Christmas is for the pain to end all I want for Christmas is for the pain to end
		/// all I want for Christmas is for the pain to end all I want for Christmas is for the pain to end all I want for Christmas is for the pain to end
		/// </summary>
		[HttpGet("by_dept_then_genr/{sid}/{dept}")]
		public async Task<IActionResult> GetResultsByDeptThenGeneration(int sid, int dept) {
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			Department? department = await Queries.GetDepartmentFromId(_dataContext, dept);
			if (department == null)
				return BadRequest(string.Format("No department found with ID = {0}", dept));

			// All subdepts, including subdepts of those subdepts and so on
			var subDepartmentsAll = await _dataContext.GetAllSubDepartments(dept)
				.Select(x => x.Id)
				.ToListAsync();

			var responsesAll = await GetExpandedResponseQuery(_dataContext, sid,
					Queries.GetQuantitativeResponses(_dataContext, sid))
				.Where(x => subDepartmentsAll.Any(y => y == x.departmentId))
				.ToListAsync();

			JsonTable resTable = new() {
				["group"] = department.ToTable(false)
			};
			{
				var statsOverall = CalculateSurveyStatistics(responsesAll);

				var responsesByGenr = responsesAll
					.GroupBy(x => x.generationId)
					.ToDictionary(x => x.Key, x => x.ToList());

				List<JsonTable> tablesGenr = new();

				var generations = await Queries.GetGenerationsInSurvey(_dataContext);
				var dimensions = await Queries.GetDimensionsInSurvey(_dataContext, survey.ProjectName);
				foreach (var (genId, responses) in responsesByGenr) {
					Generation? generation = null;
					generations.TryGetValue(genId, out generation);

					var tableAdd = new JsonTable() {
						["group"] = generation!.ToTable(false),
					};

					var statsGeneration = CalculateSurveyStatistics(responses);
					tableAdd["score"] = CreateResultTable(statsGeneration);

					{
						var tableDimension = CreateResultTableByDimension(responses, dimensions);
						tableAdd["score_dimensions"] = tableDimension;
					}
					{
						var statsByQuestion = responses
							.GroupBy(x => x.questionId)
							.OrderBy(x => x.Key)
							.Select(x => new {
								question_id = x.Key,
								dimension_id = x.First().dimensionId,
								score_count = x.Count(),
								score_avg = x.Average(x => x.score),
								score_modes = new int[] {
									x.Count(x => x.score == 1),
									x.Count(x => x.score == 2),
									x.Count(x => x.score == 3),
									x.Count(x => x.score == 4),
									x.Count(x => x.score == 5),
								},
							})
							.ToList();
						tableAdd["score_questions"] = statsByQuestion;
					}

					tablesGenr.Add(tableAdd);
				}

				resTable["score"] = CreateResultTable(statsOverall);
				resTable["score_generations"] = tablesGenr;
			}

			return Ok(resTable);
		}

		// -----------------------------------------------------

		// New api method for getting survey results by questions, very simlple query.. can add department and group by that later
		[HttpGet("by_questions/{sid}")]
		public async Task<IActionResult> GetResultsByQuestions([FromRoute] int sid) 
		{
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var listResponseAll = await GetSurveyExpandedResponses(_dataContext, sid);
			//  SurveyStatisticsResult statsAll = CalculateSurveyStatistics(listResponseAll);

			var responseQuestionsByScores = listResponseAll
				.GroupBy(x => x.questionId)
				.OrderBy(x => x.Key)
				.Select(group => new 
				{
					dimensionId = group.First().dimensionId,
					QuestionId = group.Key,
					AverageAnswerScore = group.Average(response => response.score),
					PercentageAnswerScore = (group.Average(response => response.score) / 5) * 100
				}).ToList();

			return Ok(responseQuestionsByScores);

		}
      

        [HttpGet("by_dept/{sid}")]
		public async Task<IActionResult> GetResultsDepart([FromRoute] int sid) 
		{
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			var listResponseAll = await GetSurveyExpandedResponseLevel(_dataContext, sid);
			SurveyStatisticsResult statsAll = CalculateSurveyStatisticsOV(listResponseAll);
            var resTableDepartmentsz = await CreateOVTableByDepartmentL(
                _dataContext, listResponseAll, 0);
            var resTableDepartments = await CreateOVTableByDepartmentL(
				_dataContext, listResponseAll,1);
            var resTableDepartments2 = await CreateOVTableByDepartmentL(
                _dataContext, listResponseAll, 2);
            var resTableDepartments3 = await CreateOVTableByDepartmentL(
                _dataContext, listResponseAll, 3);

            return Ok(CreateResultTable(statsAll,
				("response_count", listResponseAll.Count),
                ("score_departments0", resTableDepartmentsz),
                ("score_departments1", resTableDepartments),
                ("score_departments2", resTableDepartments2),
                ("score_departments3", resTableDepartments3)
            ));
			//return Ok();
		}

		[HttpGet("quest_dim/{sid}")]
		public async Task<IActionResult> GetQuestionsByDimension([FromRoute] int sid) 
		{
			Survey? survey = await _dataContext.Surveys.FindAsync(sid);

			var query = from qr in _dataContext.QuestionResponses
						join ri in _dataContext.ResponderInfos on qr.ResponderId equals ri.ResponderId
						join d in _dataContext.Dimensions on qr.DimensionId equals d.Id
						where qr.AnswerScore != 0
						group qr by new { qr.DimensionId, qr.QuestionId, d.DimensionName } into g
						orderby g.Key.QuestionId, g.Key.DimensionId
						select new 
						{
							AnswerCount = g.Count(),
							Score = g.Average(qr => qr.AnswerScore),
							g.Key.QuestionId,
							g.Key.DimensionId,
							g.Key.DimensionName,
							Score1 = g.Count(qr => qr.AnswerScore == 1),
							Score2 = g.Count(qr => qr.AnswerScore == 2),
							Score3 = g.Count(qr => qr.AnswerScore == 3),
							Score4 = g.Count(qr => qr.AnswerScore == 4),
							Score5 = g.Count(qr => qr.AnswerScore == 5)
						};

			List<percentageChart> Results = new List<percentageChart>();
			foreach (var q in query) 
			{
				percentageChart pc = new percentageChart();
				// pc.AnswerCount = q.AnswerCount;
				//  pc.Score = q.Score;
				pc.QuestionId = q.QuestionId;
				pc.DimensionId = (int)q.DimensionId;
				pc.DimensionName = q.DimensionName;
				pc.Score1 = q.Score1;
				pc.Score2 = q.Score2;
				pc.Score3 = q.Score3;
				pc.Score4 = q.Score4;
				pc.Score5 = q.Score5;
				Results.Add(pc);
			}

			var dimList = Datahelper.MapPercentage(Results);

			return Ok(dimList);
		}
	}
}
