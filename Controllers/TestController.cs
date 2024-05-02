using System;
using System.IO;
using System.Collections.Generic;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Survey_Backend.Data;
using Survey_Backend.Helpers;
using Survey_Backend.Models;
using Survey_Backend.DTO;

// This controller is for testing/debugging only, remove in production

namespace Survey_Backend.Controllers {
	using JsonTable = Dictionary<string, object>;

	[Route("api/debug")]
	[ApiController]
	public class TestController : Controller {
		private readonly DataContext _dataContext;
		private readonly ILogger<QuestionController> _logger;

		public TestController(DataContext dataContext, ILogger<QuestionController> logger) {
			_dataContext = dataContext;
			_logger = logger;
		}

		// -----------------------------------------------------

		[HttpPut("make_depts")]
		public async Task<IActionResult> MakeDepartments([FromQuery] string project, [FromQuery] int baseId = 0) {
			if (!Initialize.IsDevelopment)
				return Forbid();

			var _CreateTopDept = (int id, string name, int population) => {
				return new Department() {
					Id = id,
					DepartmentName = name,
					ProjectName = project,
					TreeLevel = 0,
					Population = population,
				};
			};
			var _CreateSubDeptWithLv = (int id, int parentId, string name, int population, int level) => {
				var dept = _CreateTopDept(id, name, population);
				dept.ParentDepartmentId = parentId;
				dept.TreeLevel = level;
				return dept;
			};
			var _CreateSubDept = (int id, int parentId, string name, int population) => {
				return _CreateSubDeptWithLv(id, parentId, name, population, -1);
			};

			List<Department> listAddDepts = new() {
				_CreateTopDept(1, "PSD", 2),
							_CreateSubDeptWithLv(2, 1, "Corporate Secretary Division", 7, 3),
							_CreateSubDeptWithLv(3, 1, "Internal Audit Division", 7, 3),

						_CreateSubDeptWithLv(10, 1, "Corporate Management Department", 2, 2),
							_CreateSubDept(11, 10, "Human Resources Division", 8),
							_CreateSubDept(12, 10, "Business Solutions and Development Division", 9),
							_CreateSubDept(13, 10, "Infrastructure and Information Security Division", 4),
							_CreateSubDept(14, 10, "Legal Division", 6),
							_CreateSubDept(15, 10, "Power Plant Procurement Division", 15),
							_CreateSubDept(16, 10, "Procurement and Administration Division", 18),

						_CreateSubDeptWithLv(20, 1, "Corporate Strategy Department", 1, 2),
							_CreateSubDept(21, 20, "Corporate Planning Division", 6),
							_CreateSubDept(22, 20, "Risk Assessment Division", 4),
							_CreateSubDept(23, 20, "Corporate Communications Division", 12),

					_CreateSubDept(100, 1, "Business Development (International) Group", 2),
						_CreateSubDept(110, 100, "Business Development (International) 1", 1),
							_CreateSubDept(111, 110, "Business Development (International) Division 1", 1),
							_CreateSubDept(112, 110, "Business Development (International) Division 2", 3),
						_CreateSubDept(120, 100, "Business Development (International) 2", 0),
							_CreateSubDept(121, 120, "Business Development (International) Division 3", 3),
							_CreateSubDept(122, 120, "Business Development (International) Division 4", 0),

					_CreateSubDept(200, 1, "Business Development (Domestic) Group", 2),
						_CreateSubDept(210, 200, "Business Development (Domestic) Department 1", 1),
							_CreateSubDept(211, 210, "Business Development (Domestic) Division 1", 2),
							_CreateSubDept(212, 210, "Business Development (Domestic) Division 2", 3),
					
						_CreateSubDept(220, 200, "Business Development (Domestic) Department 2", 1),
							_CreateSubDept(221, 220, "Business Development (Domestic) Division 3", 3),
							_CreateSubDept(222, 220, "Business Development (Domestic) Division 4", 3),

					_CreateSubDept(300, 1, "Chief Financial Officer Group (CFO)", 2),
							_CreateSubDeptWithLv(301, 300, "Investor Relations Division", 4, 3),
					
						_CreateSubDept(310, 300, "Accounting Department", 1),
							_CreateSubDept(311, 310, "Accounting Division", 9),
							_CreateSubDept(312, 310, "Management and Analysis Accounting Division", 7),
					
						_CreateSubDept(320, 300, "Finance Department", 1),
							_CreateSubDept(321, 320, "Finance Division", 3),
							_CreateSubDept(322, 320, "Treasury Division", 4),
					
						_CreateSubDept(330, 300, "Subsidiaries Accounting & Finance Department", 2),
							_CreateSubDept(331, 330, "International Accounting Division", 5),
							_CreateSubDept(332, 330, "IPP Accounting Division", 7),
							_CreateSubDept(333, 330, "IPP Finance Division", 7),
							_CreateSubDept(334, 330, "Subsidiaries Accounting  Division", 17),
							_CreateSubDept(335, 330, "Subsidiaries Finance Division", 12),

					_CreateSubDept(400, 1, "Operation Management (OM) Group", 2),
						_CreateSubDept(410, 400, "Asset Management Department", 1),
							_CreateSubDept(411, 410, "Asset Management Division", 11),
					
						_CreateSubDept(420, 400, "Power Plant Management Department", 6),
							_CreateSubDept(421, 420, "Power Plant Business 1 Division", 4),
							_CreateSubDept(422, 420, "Power Plant Business 2 Division", 4),
							_CreateSubDept(423, 420, "Power Plant Business 3 Division", 4),
							_CreateSubDept(424, 420, "Power Plant Management Division", 18),
					
						_CreateSubDept(430, 400, "Project Management Department", 0),
							_CreateSubDept(431, 430, "Community Relations Division", 5),
							_CreateSubDept(432, 430, "Engineering Division", 4),
							_CreateSubDept(433, 430, "Project Management Division", 7),
				
				_CreateTopDept(500, "EGCO Plus", 10),
			};

			// Blank definition for lambda recursion
			Func<Department, int> _SetDeptLevel = null!;
			_SetDeptLevel = (Department dept) => {
				if (dept.TreeLevel == -1) {
					// Is some kind of sub dept

					if (dept.ParentDepartmentId == null) {
						return 0;
					}
					else {
						int parentId = dept.ParentDepartmentId.Value;

						Department deptParent = listAddDepts.Find(x => x.Id == parentId)!;
						int parentLevel = _SetDeptLevel(deptParent);

						dept.TreeLevel = parentLevel + 1;
					}
				}
				
				return dept.TreeLevel;
			};

			{
				int maxLevel = -1;

				foreach (var dept in listAddDepts) {
					if (dept.ParentDepartmentId != null) {
						var parent = listAddDepts.Find(x => x.Id == dept.ParentDepartmentId);
						dept.ParentDepartmentName = parent != null ? parent.DepartmentName : "";
					}
					int thisLevel = _SetDeptLevel(dept);
					maxLevel = Math.Max(maxLevel, thisLevel);

					dept.Id += baseId;
				}
			}

			using (var transaction = _dataContext.Database.BeginTransaction()) {
				// Clear existing data
				_dataContext.Departments
					.Where(x => x.ProjectName == project)
					.ExecuteDelete();
				await _dataContext.SaveChangesAsync();

				// Insert new data
				_dataContext.Departments.AddRange(listAddDepts);

				// Shut up the identity insert error
				await _dataContext.SetIdentityInsert("Departments", true);
				await _dataContext.SaveChangesAsync();
				await _dataContext.SetIdentityInsert("Departments", false);

				transaction.Commit();
			}

			return Ok(listAddDepts.Count);
		}

		[HttpPut("make_questions/{sid}")]
		public async Task<IActionResult> MakeQuestions(int sid) {
			if (!Initialize.IsDevelopment)
				return Forbid();

			var dir = System.IO.Directory.GetCurrentDirectory();

			List<Question> listAddQuestions = new();

			// Read questions csv and turn them into Question instances
			var lines = System.IO.File.ReadAllLines("Resource/EngagementQuestion.csv");
			foreach (var line in lines) {
				var data = line.Split('ÿ');

				int dimensionId = int.Parse(data[0]);
				int questionNum = int.Parse(data[1].Substring(1));
				string textTH = data[2];
				string textEN = data[3];
				bool isText = textEN.Contains("Open-ended");

				listAddQuestions.Add(new Question() {
					QuestionIndex = questionNum,
					SurveySetId = sid,
					Type = isText ? QuestionType.Textbox : QuestionType.Rating,
					DimensionId = dimensionId,
					QuestionText = textEN,
					QuestionTextTH = textTH,
					QuestionDescription = "",
				});
			}

			using (var transaction = _dataContext.Database.BeginTransaction()) {
				// Clear existing data
				_dataContext.Surveys.Where(x => x.SurveySetId == sid)
					.ExecuteDelete();
				_dataContext.Questions.Where(x => x.SurveySetId == sid)
					.ExecuteDelete();
				_dataContext.SurveyQuestions.Where(x => x.SurveySetId == sid)
					.ExecuteDelete();
				await _dataContext.SaveChangesAsync();

				// Insert new data
				_dataContext.Surveys.Add(new Survey {
					SurveySetId = sid,
					SurveyCreatedDate = DateTime.Now,
					ProjectName = "EGCO",
					SurveyDescription = "Ph'nglui mglw'nafh Cthulhu R'lyeh wgah'nagl fhtagn",
				});

				_dataContext.Questions.AddRange(listAddQuestions);

				// Shut up the identity insert error
				await _dataContext.SetIdentityInsert("Surveys", true);
				await _dataContext.SaveChangesAsync();
				await _dataContext.SetIdentityInsert("Surveys", false);

				// That QuestionId is probably not the best solution but it'll do for now
				_dataContext.SurveyQuestions.AddRange(listAddQuestions.Select(x => new SurveyQuestions {
					SurveySetId = x.SurveySetId,
					QuestionIndex = x.QuestionIndex,
					QuestionId = _dataContext.Questions.First(y => y.SurveySetId == x.SurveySetId
						&& y.QuestionIndex == x.QuestionIndex).Id,
				}).ToList());
				await _dataContext.SaveChangesAsync();

				transaction.Commit();
			}

			return Ok(listAddQuestions.Count);
		}

		[HttpPut("make_responders/{sid}")]
		public async Task<IActionResult> MakeResponders(int sid) {
			if (!Initialize.IsDevelopment)
				return Forbid();

			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			Random rnd = new Random(DateTime.Now.GetHashCode());
			int currentYear = DateTime.Now.Year;

			List<ResponderInfo> responders = new();

			{
				var _NewResponder = (int[] roleIds, int deptId) => {
					var completeTime = DateTime.Now.AddSeconds(rnd.NextDouble() * -400000);

					int age = rnd.Next(20, 64);
					int service = rnd.Next(1, (age - 19) * 12);

					return new ResponderInfo {
						ResponderId = Guid.NewGuid(),
						SurveySetId = survey.SurveySetId,
						RoleId = roleIds[rnd.Next() % roleIds.Length],
						DepartmentId = deptId,
						SurveyCompletedTime = completeTime,
						ServiceMonths = service,
						BirthYear = currentYear - age - 1,
						Age = age,
						Gender = rnd.NextDouble() > 0.5 ? 1 : 2,
					};
				};
				var _AddFromDept = async (int[] roleIds, int deptLevel) => {
					var depts = await _dataContext.Departments
						.Where(x => x.ProjectName == survey.ProjectName)
						.Where(x => x.TreeLevel == deptLevel)
						.ToListAsync();
					foreach (var iDept in depts) {
						if (iDept.Population == 0) continue;

						// Simulate non-response
						int responderMin = (int)Math.Ceiling(iDept.Population * 0.7);
						int responderCount = rnd.Next(responderMin, iDept.Population);

						responders.AddRange(Enumerable.Range(0, responderCount)
							.Select(x => _NewResponder(roleIds, iDept.Id)));
					}
				};

				// For multiple role levels
				await _AddFromDept(new int[] { 1, 2 }, 0);		// SEVP+
				await _AddFromDept(new int[] { 1, 2 }, 1);		// SEVP
				await _AddFromDept(new int[] { 2, 3, 4 }, 2);		// EVP
				await _AddFromDept(new int[] { 4, 5, 5, 5 }, 3);	// VP- 
			}

			using (var transaction = _dataContext.Database.BeginTransaction()) {
				// Clear existing data
				_dataContext.ResponderInfos.Where(x => x.SurveySetId == sid)
					.ExecuteDelete();
				await _dataContext.SaveChangesAsync();

				// Insert new data
				_dataContext.ResponderInfos.AddRange(responders);
				await _dataContext.SaveChangesAsync();

				transaction.Commit();
			}

			return Ok(responders.Count);
		}

		[HttpPost("set_responders_data/{sid}")]
		public async Task<IActionResult> SetRespondersData(int sid) {
			if (!Initialize.IsDevelopment)
				return Forbid();

			Survey? survey = await _dataContext.Surveys.FindAsync(sid);
			if (survey == null)
				return BadRequest(string.Format("No survey found with ID = {0}", sid));

			Random rnd = new Random(DateTime.Now.GetHashCode());
			int currentYear = DateTime.Now.Year;

			var responders = await _dataContext.ResponderInfos
				.Where(x => x.SurveySetId == sid)
				.ToListAsync();

			{
				foreach (var i in responders) {
					int age = rnd.Next(20, 64);
					int service = rnd.Next(1, (age - 19) * 12);

					i.Age = age;
					i.BirthYear = currentYear - age - 1;
					i.ServiceMonths = service;
				}
				await _dataContext.SaveChangesAsync();
			}

			return Ok(responders.Count);
		}

		[HttpPut("make_qresponses/{sid}")]
		public async Task<IActionResult> MakeResponses(int sid) {
			if (!Initialize.IsDevelopment)
				return Forbid();

			Random rnd = new Random(DateTime.Now.GetHashCode());

			var responders = await _dataContext.ResponderInfos
				.Where(x => x.SurveySetId == sid)
				.Select(x => new { 
					x.ResponderId, 
					CompleteTime = x.SurveyCompletedTime ?? DateTime.Now 
				})
				.ToListAsync();

			var questions = _dataContext.SurveyQuestions
				.Where(x => x.SurveySetId == sid)
				.Join(_dataContext.Questions,
					sq => sq.QuestionId,
					qu => qu.Id,
					(sq, qu) => new {
						sq.QuestionId,
						sq.QuestionIndex,
						qu.DimensionId,
						qu.Type,
					}
				);
			int questionCount = await questions.CountAsync();

			using (var transaction = _dataContext.Database.BeginTransaction()) {
				// Clear existing data
				_dataContext.QuestionResponses.Where(x => x.SurveySetId == sid)
					.ExecuteDelete();
				await _dataContext.SaveChangesAsync();

				// Insert new data
				foreach (var responder in responders) {
					var addResponses = questions.Select(x => new QuestionResponse {
						Id = x.QuestionId,
						ResponderId = responder.ResponderId,
						SurveySetId = sid,

						/*  Rands between 1 and 5 (inclusive)
						 *  Doesn't actually work properly, unknown reason (weird rand distribution)
						 * 		Must also execute 
						 * 			update QuestionResponses
						 * 			set AnswerScore = 1 + abs(checksum(newid())) % 5
						 * 			where SurveySetId = id and AnswerText is null
						 * 		afterwards
						 * 	(Actually I might just be hallucinating)
						 */
						AnswerScore = x.Type != QuestionType.Rating ? 
							0: (int)(1 + EF.Functions.Random() * 4.99),

						AnswerText = x.Type == QuestionType.Rating ? 
							null : ("Lorem ipsum dolor sit amet " + x.QuestionIndex),
						ResponseDate = responder.CompleteTime,
						QuestionId = x.QuestionIndex,
						DimensionId = x.DimensionId,
					});

					_dataContext.QuestionResponses.AddRange(addResponses);
				}
				await _dataContext.SaveChangesAsync();

				transaction.Commit();
			}

			return Ok(ValueHelpers.TupleToList((responders.Count, questionCount, 
				responders.Count * questionCount)));
		}
	}
}
