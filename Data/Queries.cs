using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Survey_Backend.Data;
using Survey_Backend.DTO;
using Survey_Backend.Helpers;
using Survey_Backend.Models;
using System.Runtime.Intrinsics.Arm;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Survey_Backend.Data {
	public static class Queries {
		public static IQueryable<QuestionResponse> ResponsesBySID(DataContext data, int sid) {
			return data.QuestionResponses
				.Where(x => x.SurveySetId == sid);
		}
		public static IQueryable<Question> QuestionsBySID(DataContext data, int sid) {
			return data.Questions
				.Where(x => x.SurveySetId == sid);
		}

		// -----------------------------------------------------

		public static async Task<Dimension?> GetDimensionFromId(DataContext dataContext, int id) {
			Dimension? dimension = await dataContext.Dimensions.FindAsync(id);
			return dimension;
		}
        public static async Task<Department?> GetDepartmentFromId(DataContext dataContext, int id) {
            Department? department = await dataContext.Departments.FindAsync(id);
            return department;
        }
        public static async Task<Role?> GetRoleFromId(DataContext dataContext, int id) {
			Role? role = await dataContext.Roles.FindAsync(id);
			return role;
		}
		public static async Task<Generation?> GetGenerationFromId(DataContext dataContext, int id) {
			Generation? gen = await dataContext.Generations.FindAsync(id);
			return gen;
		}

		// -----------------------------------------------------

		public static async Task<Dictionary<int, Dimension>?> GetDimensionsInSurvey(DataContext dataContext, int sid) {
			Survey? survey = await dataContext.Surveys.FindAsync(sid);
			if (survey != null)
				return await GetDimensionsInSurvey(dataContext, survey.ProjectName);
			return new();
		}
		public static async Task<Dictionary<int, Dimension>> GetDimensionsInSurvey(DataContext dataContext, string projectName) {
			return await dataContext.Dimensions
				.Where(x => x.ProjectName == projectName)
				.ToDictionaryAsync(k => k.Id, x => x);
		}

		public static async Task<Dictionary<int, Role>?> GetRolesInSurvey(DataContext dataContext, int sid) {
			Survey? survey = await dataContext.Surveys.FindAsync(sid);
			if (survey != null)
				return await GetRolesInSurvey(dataContext, survey.ProjectName);
			return new();
		}
		public static async Task<Dictionary<int, Role>> GetRolesInSurvey(DataContext dataContext, string projectName) {
			return await dataContext.Roles
				.Where(x => x.ProjectName == projectName)
				.ToDictionaryAsync(k => k.Id, x => x);
		}

		public static async Task<Dictionary<int, Generation>> GetGenerationsInSurvey(DataContext dataContext) {
			return await dataContext.Generations
				.ToDictionaryAsync(k => k.Id, x => x);
		}

		// -----------------------------------------------------

		public static async Task<Dictionary<int, Question>> GetQuestionsMap(DataContext dataContext, List<int> listIDs) {
			return await dataContext.Questions
				.Where(x => listIDs.Any(y => x.QuestionIndex == y))
				.ToDictionaryAsync(k => k.QuestionIndex, v => v);
		}
		public static async Task<Dictionary<Guid, ResponderInfo>> GetRespondersMap(DataContext dataContext, List<Guid> listIDs) {
			return await dataContext.ResponderInfos
				.Where(x => listIDs.Any(y => x.ResponderId == y))
				.ToDictionaryAsync(k => k.ResponderId, v => v);
		}

		// -----------------------------------------------------

		public static IQueryable<QuestionResponse> GetQuantitativeResponses(
			DataContext dataContext, int sid) {

			var query = ResponsesBySID(dataContext, sid)
				.Where(x => x.AnswerScore > 0 && x.AnswerText == null);
			return query;
		}

        public static IQueryable<Old_Scores> ResponsesDepartmentScores(DataContext data, int sid, int depId)
        {

            /// we add the join here so we can sort by department order
            var results = data.Old_Scores
      .Where(x => x.Project_id == sid && x.Dim == 0 && x.Role==0 && x.YearService==0 && x.Generation==0)
      .Join(
          data.Departments,
          score => score.Department_Id,
          department => department.Id,
          (score, department) => new
          {
              Score = score,
              DepartmentOrder = department.sortOrder,
              Year = score.Year,

              ParentDepartmentId = department.ParentDepartmentId
          }
            )
       .Where(joinedData => joinedData.ParentDepartmentId == depId)
      .OrderBy(joinedData => joinedData.DepartmentOrder)
      .ThenBy(joinedData => joinedData.Year)
      .Select(joinedData => joinedData.Score);



            return results;






        }
        /// <summary>
        /// Get old scores from dimensions to  add to charts, it should only get the last year
		/// old year has to be changed because survey is run more often
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sid"></param>
        /// <returns></returns>
        public static IQueryable<Old_Scores> OldDimensionScores(DataContext data, int sid, int dep)
        {
            var currentYear = DateTime.Now.Year;
            var lastYear = currentYear ;

			return data.Old_Scores
			.Where(x => x.Project_id == sid && x.Department_Id == dep && x.Dim != 0 && x.YearService == 0 && x.Role == 0 && x.Generation == 0)
			//.OrderBy(score => score.Year)
			//.ThenBy(score => score.Dim);
			.OrderBy(score => score.Dim);
        }

        public static IQueryable<Old_Scores> OldOverallDep(DataContext data, int sid, int dep)
        {

            return data.Old_Scores
            .Where(x => x.Project_id == sid && x.Department_Id == dep && x.Dim == 0 && x.YearService==0 && x.Role==0 && x.Generation==0)
            .OrderBy(score => score.Year);

        }

        public static IQueryable<Old_Scores> OldFilter(DataContext data, int sid, OldScoresFilter sF)
        {
            var query = data.Old_Scores.Where(x => x.Project_id == sid);

            switch (sF.Category)
            {
                case "Generation":
                    query = query.Where(x => x.Department_Id == sF.Dep_Id && x.Dim == sF.Dim_id && x.Role == sF.Role && x.YearService == sF.Years_Service && x.Generation!=0 );
                    break;

                case "Role":
                    query = query.Where(x => x.Department_Id == sF.Dep_Id && x.Dim == sF.Dim_id && x.YearService == sF.Years_Service && x.Generation == sF.Generation && x.Role!=0);

                    // Add more conditions for Type2 if needed
                    break;
                case "GenerationDep":
                    query = query.Where(x => x.Department_Id == sF.Dep_Id && x.Dim == sF.Dim_id && x.Role == sF.Role && x.YearService == sF.Years_Service && x.Generation != 0);
                    break;

                case "RoleDep":
                    query = query.Where(x => x.Department_Id == sF.Dep_Id && x.Dim == sF.Dim_id && x.YearService == sF.Years_Service && x.Generation == sF.Generation && x.Role != 0);
                    break;
                // Add more cases as needed

                default:
                    // Handle the default case, or don't do anything if no specific case matches
                    break;
            }

            // .Where(x => x.Project_id == sid && x.Department_Id == sF.Dep_Id && x.Dim == sF.Dim_id && x.Role==sF.Role && x.YearService==sF.Years_Service && x.Generation==sF.Generation)
            // .OrderBy(score => score.Year);

            return query;

        }
        public static IQueryable<Old_Scores> OldSubDep(DataContext data, int sid, List<int> subDeps)
        {
            return data.Old_Scores
           .Where(x => x.Project_id == sid &&  subDeps.Contains(x.Department_Id) && x.Dim == 0 && x.YearService == 0 && x.Role == 0 && x.Generation == 0)
           .OrderBy(score => score.Department_Id);
        }

        }
}
