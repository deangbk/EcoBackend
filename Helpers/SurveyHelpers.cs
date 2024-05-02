using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

using Survey_Backend.Data;
using Survey_Backend.Models;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using static Survey_Backend.Helpers.ResultHelpers;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Survey_Backend.Helpers {
    using JsonTable = Dictionary<string, object>;

    public static class SurveyHelpers {
		/// <summary>
		/// Gets list of all question in a survey set
		/// </summary>
		public static async Task<List<Question>> GetAllQuestionsInSurveySet(
			DataContext dataContext, int surveySetId) {

			var listQuestionId = await dataContext.SurveyQuestions
				.Where(x => x.SurveySetId == surveySetId)
				.OrderBy(x => x.QuestionIndex)
				.Select(x => x.QuestionId)
				.ToListAsync();

			var listQuestion = new List<Question>();
			foreach (var id in listQuestionId) {
				Question? question = await dataContext.Questions.FindAsync(id);
				if (question != null)
					listQuestion.Add(question);
			}

			return listQuestion;
		}

		/// <summary>
		/// Gets list of all departments who participated in a survey set
		/// </summary>
		public static async Task<List<Department>> GetAllDepartmentsInSurveySet(
			DataContext dataContext, int surveySetId) {

			Survey? survey = await dataContext.Surveys.FindAsync(surveySetId);

			return await GetAllDepartmentsInSurveySet(dataContext, survey);
		}
		/// <summary>
		/// Gets list of all departments who participated in a survey set
		/// </summary>
		public static async Task<List<Department>> GetAllDepartmentsInSurveySet(
			DataContext dataContext, Survey? survey) {

			if (survey == null)
				return new();

			var projectName = survey.ProjectName;

			var result = await dataContext.Departments
				.Where(x => x.ProjectName == projectName)
				.ToListAsync();

			return result;
		}
        /// <summary>
        /// very similar to GetResponseQuery, but adds in all department levels. Separated it out to avoid breaking existing code.
        /// </summary>
        public class OverViewHelper : ExpandedResponseData
        {
            public int DeptL0;
            public int DeptL1;
            public int DeptL2;
            public int DeptL3;
            public List<int>? chainLevels;

        }
		/// <summary>
		/// gets response data for survey, adds in all department levels.
		/// </summary>
		/// <param name="dataContext"></param>
		/// <param name="surveySetId"></param>
		/// <param name="responses"></param>
		/// <returns></returns>
        public static IQueryable<OverViewHelper> GetResponseQueryOverview(
          DataContext dataContext, int surveySetId, IQueryable<QuestionResponse> responses)
        {

            return responses
                .Where(x => x.SurveySetId == surveySetId)
                .Join(dataContext.ResponderInfos,
                    qr => qr.ResponderId,
                    ri => ri.ResponderId,
                    (qr, ri) => new OverViewHelper
                    {
                        responderId = qr.ResponderId,
                        questionId = qr.QuestionId,
                        dimensionId = qr.DimensionId ?? -1,
                        roleId = ri.RoleId,
                        departmentId = ri.DepartmentId,
                        generationId = dataContext.GetGenerationID(ri.BirthYear),
                        serviceMonths = ri.ServiceMonths,
                        yearBorn = ri.BirthYear,
                        //age = dataContext.GetResponderAge(qr.ResponderId),
                        age = ri.Age,
                        gender = ri.Gender,
                        score = qr.AnswerScore ?? 0,
                    });
        }
        public static SurveyStatisticsResult CalculateSurveyStatisticsOV(
          List<OverViewHelper> responses)
        {

            if (responses.Count == 0)
                return new();

            var ssr = new SurveyStatisticsResult();
            var responders = new HashSet<Guid>();

            ssr.count = responses.Count;

            ssr.scoreAsMode = new int[5] { 0, 0, 0, 0, 0 };
            foreach (var iResponse in responses)
            {
                int score = iResponse.score;
                if (score > 0)
                {
                    score = Math.Clamp(score, 1, 5);
                    ssr.scoreAsMode[score - 1]++;
                }
                responders.Add(iResponse.responderId);
            }

            ssr.ComputeAverageScore();

            ssr.countUnique = responders.Count;

            return ssr;
        }
        public static async Task<List<JsonTable>> CreateOVTableByDepartmentL(
            DataContext dataContext, List<OverViewHelper> responses, int TreeLevel)
        {

            var tables = new List<JsonTable>();

            var responseStatsById = responses
            // .Where(x => x.departmentChain.Count != 1 && x.departmentChain.Count != 2 && x.departmentChain.Count != 3)
            //.Where(x => x.departmentChain.Count == 0)

            // .GroupBy(x => TreeLevel == 1 ? x.DeptL1 : x.DeptL2)
            .GroupBy(x =>
            {
                switch (TreeLevel)
            {
                    case 0:
                        return x.DeptL0;
                    case 1:
                    return x.DeptL1;
                case 2:
                    return x.DeptL2;
                case 3:
                    return x.DeptL3;
                default:
                    throw new ArgumentException($"Unsupported TreeLevel value: {TreeLevel}");
            }
            })
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => CalculateSurveyStatisticsOV(x.ToList()));

            foreach (var (dept, stats) in responseStatsById)
            {
                Department? department = await Queries.GetDepartmentFromId(dataContext, dept);

                tables.Add(CreateResultTable(stats, department != null ? department.ToTable() :
                    (new Department() { Id = dept }).ToTable()));
            }

            return tables;
        }
        public static async Task<List<OverViewHelper>> GetExpandedResponseLev(
            DataContext dataContext, int surveySetId, IQueryable<QuestionResponse> responses)
        {

            // Join with ResponderInfos
            var listRes = await GetResponseQueryOverview(
                dataContext, surveySetId, responses).ToListAsync();

            var mapTopDepartmentChain = await dataContext.GetParentDepartmentsChain()
                .ToDictionaryAsync(k => k.Id, x => ValueHelpers.SplitIntString(x.ChainDept));
            var mapTopDepartmentLevel = await dataContext.GetParentDepartmentsChain()
                .ToDictionaryAsync(k => k.Id, x => ValueHelpers.SplitIntString(x.ChainLevel));
            var mapSubDepartmentChain = await dataContext.Departments
                .Select(x => new {
                    x.Id,
                    Chain = ValueHelpers.SplitIntString(dataContext.GetAllSubDepartmentsAsString(x.Id)).ToList()
                      })
                .ToDictionaryAsync(x => x.Id, x => x.Chain);
          
            /// order groupRes by department ID and only need to run the following 50 times instead of thousands
            foreach (var i in listRes)
            {
                i.departmentChain = mapTopDepartmentChain[i.departmentId].ToList();
                i.departmentTop = i.departmentChain.Last();
               i.chainLevels= mapTopDepartmentLevel[i.departmentId].ToList();
                var indexD0 = i.chainLevels.IndexOf(0);
                var indexD1= i.chainLevels.IndexOf(1);
                var indexD2 = i.chainLevels.IndexOf(2);
                var indexD3 = i.chainLevels.IndexOf(3);
                i.DeptL0 = indexD0 != -1 ? i.departmentChain[indexD0] : 0;
                i.DeptL1 = indexD1 != -1 ? i.departmentChain[indexD1] : 0;
                i.DeptL2 = indexD2 != -1 ? i.departmentChain[indexD2] : 0;
                i.DeptL3 = indexD3 != -1 ? i.departmentChain[indexD3] : 0;
            }

            return listRes;
        }

    }
}
