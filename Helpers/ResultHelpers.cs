using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

using Survey_Backend.Data;
using Survey_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Survey_Backend.Helpers
{
    using JsonTable = Dictionary<string, object>;
    public static class ResultHelpers
    {
        public class ExpandedResponseData
        {
            public Guid responderId;
            public int questionId;              // Index of the question, not the unique row ID
            public int dimensionId;
            public int roleId;
            public int departmentId;
            public List<int> departmentChain;   // Top depts chain, ordered from bottom most -> top most
            public int departmentTop;
            public int generationId;
            public int serviceMonths;
            public int yearBorn;
            public int age;
            public int gender;
            public int score;
        }
        /// <summary>
        /// Returns the base survey answers for manipulations
        /// </summary>
        /// <param name="dataContext"></param>
        /// <param name="surveySetId"></param>
        /// <param name="responses"></param>
        /// <returns></returns>
        public static IQueryable<ExpandedResponseData> GetExpandedResponseQuery(
            DataContext dataContext, int surveySetId, IQueryable<QuestionResponse> responses)
        {

            // Join with ResponderInfos
            return responses
                .Where(x => x.SurveySetId == surveySetId)
                .Join(dataContext.ResponderInfos,
                    qr => qr.ResponderId,
                    ri => ri.ResponderId,
                    (qr, ri) => new ExpandedResponseData
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
        public static async Task<List<ExpandedResponseData>> GetExpandedResponseList(
            DataContext dataContext, int surveySetId, IQueryable<QuestionResponse> responses)
        {

            var expandedQuery = GetExpandedResponseQuery(
                dataContext, surveySetId, responses);
            return await GetExpandedResponseListEx(dataContext, expandedQuery);
        }
        public static async Task<List<ExpandedResponseData>> GetExpandedResponseListEx(
            DataContext dataContext, IQueryable<ExpandedResponseData> exresponses)
        {

            var listRes = await exresponses.ToListAsync();

            var mapTopDepartmentChain = await dataContext.GetParentDepartmentsChain()
                .ToDictionaryAsync(k => k.Id, x => ValueHelpers.SplitIntString(x.ChainDept));
            var mapSubDepartmentChain = await dataContext.Departments
                .Select(x => new {
                    x.Id,
                    Chain = ValueHelpers.SplitIntString(dataContext.GetAllSubDepartmentsAsString(x.Id)).ToList(),
                })
                .ToDictionaryAsync(x => x.Id, x => x.Chain);

            /// as the first 65 response will have identical department chains , we can use the first one as the template to cut the iterations down by 64
            foreach (var i in listRes)
            {
                i.departmentChain = mapTopDepartmentChain[i.departmentId].ToList();
                i.departmentTop = i.departmentChain.Last();
            }

            return listRes;
        }

        // -----------------------------------------------------

        public struct SurveyStatisticsResult
        {
            public int count;           // Number of total responses
            public int countUnique;     // Number of unique responders
            public float avgScoreRate;  // Average score rate (0~1)
            public int[] scoreAsMode;   // Number of ratings for each score units (1~5)
            public Int64 totalServiceM; // Total months of service

            public SurveyStatisticsResult()
            {
                count = 0;
                countUnique = 0;
                avgScoreRate = 0;
                scoreAsMode = new int[5];
                totalServiceM = 0;
            }

            public void ComputeAverageScore()
            {
                int count = scoreAsMode.Sum();
                if (count > 0)
                {
                    int maxScore = count * 5;
                    int score = scoreAsMode[0] * 1 + scoreAsMode[1] * 2
                        + scoreAsMode[2] * 3 + scoreAsMode[3] * 4 + scoreAsMode[4] * 5;
                    this.avgScoreRate = score / (float)maxScore;
                }
                else
                {
                    this.avgScoreRate = 0;
                }
            }
        }
        public static SurveyStatisticsResult CalculateSurveyStatistics(
            List<ExpandedResponseData> responses)
        {

            if (responses.Count == 0)
                return new();

            var ssr = new SurveyStatisticsResult();
            var responderData = new Dictionary<Guid, int>();

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
                responderData[iResponse.responderId] = iResponse.serviceMonths;
            }

            ssr.ComputeAverageScore();

            ssr.countUnique = responderData.Count;
            ssr.totalServiceM = responderData.Select(x => x.Value).Sum(x => (Int64)x);

            return ssr;
        }

        // -----------------------------------------------------

        public static async Task<List<ExpandedResponseData>> GetSurveyExpandedResponses(
            DataContext dataContext, int sid)
        {

            var queryResponse = Queries.GetQuantitativeResponses(dataContext, sid);
            return await GetExpandedResponseList(dataContext, sid, queryResponse);
        }
        public static async Task<List<SurveyHelpers.OverViewHelper>> GetSurveyExpandedResponseLevel(
            DataContext dataContext, int sid)
        {

            var queryResponse = Queries.GetQuantitativeResponses(dataContext, sid);
            return await SurveyHelpers.GetExpandedResponseLev(dataContext, sid, queryResponse);
        }
        public static JsonTable CreateResultTable(SurveyStatisticsResult stats,
            params (string, object)[] addValues)
        {

            return CreateResultTable(stats, addValues.ToDictionary(k => k.Item1, x => x.Item2));
        }
        public static JsonTable CreateResultTable(SurveyStatisticsResult stats, JsonTable addTable)
        {
            var res = new JsonTable()
            {
                ["responder_count"] = stats.countUnique,
                ["service_avg"] = stats.countUnique > 0
                    ? ((stats.totalServiceM / (float)stats.countUnique) / 12.0f) : 0,
                ["score_avg"] = stats.avgScoreRate * 5,
                ["score_avg_percent"] = stats.avgScoreRate * 100,
                ["score_modes"] = stats.scoreAsMode.ToList(),
            };
            foreach (var (k, v) in addTable)
                res[k] = v;
            return res;
        }

        public static async Task<List<JsonTable>> CreateResultTableByDimension(
            DataContext dataContext, string projectName, List<ExpandedResponseData> responses)
        {

            var dimensions = await Queries.GetDimensionsInSurvey(dataContext, projectName);
            return CreateResultTableByDimension(responses, dimensions);
        }
        public static List<JsonTable> CreateResultTableByDimension(
            List<ExpandedResponseData> responses, Dictionary<int, Dimension> dimensionMap)
        {

            var tables = new List<JsonTable>();

            var responseStatsById = responses
                .GroupBy(x => x.dimensionId)
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => CalculateSurveyStatistics(x.ToList()));

            foreach (var (dimensionId, stats) in responseStatsById)
            {
                Dimension? dimension = null;
                dimensionMap.TryGetValue(dimensionId, out dimension);

                tables.Add(CreateResultTable(stats, dimension != null ? dimension.ToTable() :
                    (new Dimension() { Id = dimensionId }).ToTable()));
            }

            return tables;
        }

        public static async Task<List<JsonTable>> CreateResultTableByDepartment(
            DataContext dataContext, Survey survey, List<ExpandedResponseData> responses, bool bTopOnly = false)
        {

            var tables = new List<JsonTable>();

            var listDept = await SurveyHelpers.GetAllDepartmentsInSurveySet(dataContext, survey);
            var listTopDeptIds = listDept.Where(x => x.ParentDepartmentId == null)
                .Select(x => x.Id).ToList();

            var mapDeptResults = await CalculateDepartmentsResults(
                dataContext, responses, listDept.Select(x => x.Id).ToList());

            foreach (var (departmentId, stats) in mapDeptResults.OrderBy(x => x.Key))
            {
                if (bTopOnly && !listTopDeptIds.Contains(departmentId)) continue;

                Department? department = listDept.Find(x => x.Id == departmentId);

                tables.Add(CreateResultTable(stats, department != null ? department.ToTable() :
                    (new Department() { Id = departmentId }).ToTable()));
            }

            return tables;
        }

        public static async Task<Dictionary<int, SurveyStatisticsResult>> CalculateDepartmentsResults(
            DataContext dataContext, List<ExpandedResponseData> responses, List<int> includeDeptIds)
        {

            // Create map of subdepts, don't care about skipped levels
            var subDepartmentsMap = await dataContext.Departments
                .Where(x => includeDeptIds.Any(y => y == x.Id))
                .Select(x => new {
                    x.Id,
                    Children = dataContext.Departments
                        .Where(y => y.ParentDepartmentId == x.Id)
                        .Select(y => y.Id)
                        .ToList(),
                    Result = (SurveyStatisticsResult?)null,
                })
                .OrderBy(x => x.Id)
                .ToDictionaryAsync(k => k.Id, x => x);

            int countDistinct = responses.DistinctBy(x => x.responderId).Count();

            Dictionary<int, List<ExpandedResponseData>> responsesByDept = responses
                .GroupBy(x => x.departmentId)
                .ToDictionary(k => k.Key, x => x.ToList());

            Dictionary<int, SurveyStatisticsResult?> statsByDept = includeDeptIds
                .Select(x => x)
                .ToDictionary(k => k, _ => (SurveyStatisticsResult?)null);

            // Blank definition for lambda recursion
            Func<int, SurveyStatisticsResult> _CreateResult = null!;
            _CreateResult = (int dept) => {
                var stats = statsByDept[dept];
                if (stats == null)
                {
                    // Process own results
                    SurveyStatisticsResult newStats = new();
                    {
                        List<ExpandedResponseData> resp = new();
                        if (responsesByDept.TryGetValue(dept, out resp!))
                            newStats = CalculateSurveyStatistics(resp);
                    }

                    var childrenIds = subDepartmentsMap[dept].Children;
                    if (childrenIds.Count > 0)
                    {
                        // Process results of subdepts (recursive)
                        var childrenStats = childrenIds
                            .Select(x => _CreateResult(x))
                            .ToList();

                        // Aggregate subdepts results into own result
                        newStats.count += childrenStats.Select(x => x.count).Sum();
                        newStats.countUnique += childrenStats.Select(x => x.countUnique).Sum();
                        newStats.totalServiceM += childrenStats.Select(x => x.totalServiceM).Sum();

                        for (int i = 0; i < 5; ++i)
                            newStats.scoreAsMode[i] += childrenStats.Select(x => x.scoreAsMode[i]).Sum();

                        newStats.ComputeAverageScore();
                    }

                    stats = newStats;
                    statsByDept[dept] = stats;
                }
                return stats.Value;
            };

            var resMap = statsByDept.ToDictionary(k => k.Key, x => _CreateResult(x.Key));

            return resMap;
        }

        public static async Task<List<JsonTable>> CreateResultTableByRole(
            DataContext dataContext, Survey survey, List<ExpandedResponseData> responses)
        {

            var tables = new List<JsonTable>();

            var responseStatsById = responses
                .GroupBy(x => x.roleId)
                .ToDictionary(x => x.Key, x => CalculateSurveyStatistics(x.ToList()));

            var roles = await Queries.GetRolesInSurvey(dataContext, survey.ProjectName);
            foreach (var (roleId, stats) in responseStatsById)
            {
                Role? role = null;
                roles.TryGetValue(roleId, out role);

                tables.Add(CreateResultTable(stats, role != null ? role.ToTable() :
                    (new Role() { Id = roleId }).ToTable()));
            }

            return tables;
        }

        public static async Task<List<JsonTable>> CreateResultTableByGeneration(
            DataContext dataContext, Survey survey, List<ExpandedResponseData> responses)
        {

            var tables = new List<JsonTable>();

            var responseStatsById = responses
                .GroupBy(x => x.generationId)
                .ToDictionary(x => x.Key, x => CalculateSurveyStatistics(x.ToList()));

            var generations = await Queries.GetGenerationsInSurvey(dataContext);
            foreach (var (genId, stats) in responseStatsById)
            {
                Generation? generation = null;
                generations.TryGetValue(genId, out generation);

                tables.Add(CreateResultTable(stats, generation != null ? generation.ToTable() :
                    (new Generation() { Id = genId }).ToTable()));
            }

            return tables;
        }

        public static List<JsonTable> CreateResultTableByGender(
            List<ExpandedResponseData> responses)
        {

            var tables = new List<JsonTable>();

            var responseStatsById = responses
                .GroupBy(x => x.gender)
                .ToDictionary(x => x.Key, x => CalculateSurveyStatistics(x.ToList()));

            foreach (var (genId, stats) in responseStatsById)
            {
                tables.Add(CreateResultTable(stats,
                    ("gender", genId)));
            }

            return tables;
        }

        // -----------------------------------------------------

        public static async Task<List<JsonTable>> CreateOVTableByDepartment(
            DataContext dataContext, List<ExpandedResponseData> responses)
        {

            var tables = new List<JsonTable>();

            var responseStatsById = responses
                  // .Where(x => x.departmentChain.Count != 1 && x.departmentChain.Count != 2 && x.departmentChain.Count != 3)
                  .Where(x => x.departmentChain.Count == 0)

                .GroupBy(x => x.departmentId)
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => CalculateSurveyStatistics(x.ToList()));

            foreach (var (dept, stats) in responseStatsById)
            {
                Department? department = await Queries.GetDepartmentFromId(dataContext, dept);

                tables.Add(CreateResultTable(stats, department != null ? department.ToTable() :
                    (new Department() { Id = dept }).ToTable()));
            }

            return tables;
        }
    }
}
