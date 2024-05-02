using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Survey_Backend.Data;
using Survey_Backend.Helpers;
using Survey_Backend.Models;
using Survey_Backend.DTO;

using static Survey_Backend.Helpers.SurveyHelpers;
using static Survey_Backend.Helpers.ResultHelpers;
using System.Runtime.Intrinsics.Arm;

namespace Survey_Backend.Controllers
{

    using JsonTable = Dictionary<string, object>;

    [Route("api/[controller]")]
    //[Authorize]
    [ApiController]
    public class OldResults : Controller
    {
        private readonly DataContext _dataContext;
        public OldResults(DataContext dataContext)
        {
            _dataContext = dataContext;
        }
        // -------------------------------------------------

        /// <summary>
        /// Gets old scores by all sub departments of the one inputed
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="pId"></param>
        /// <returns></returns>
        [HttpGet("old_dep/{sid}/{pId}")]
        public IActionResult GetOldScoresByDepartment([FromRoute] int sid, int pId)
        {
            //  Department_Scores? dep_score = _dataContext.DepScores.FindAsync(sid);
            //var pId = 511;
            List<Old_Scores> department_score = Queries.ResponsesDepartmentScores(_dataContext, sid, pId).ToList();
            List<DepartmentScoresGrouped> depScoresG = new List<DepartmentScoresGrouped>();
            //	var depSr= new DepartmentScoresGrouped();
            var depId = 0;
            foreach (var department in department_score)
            {
                if (depId != department.Department_Id)
                {
                    var dScore = department_score.Where(x => x.Department_Id == department.Department_Id).ToList();
                    var depSr = new DepartmentScoresGrouped();
                    depSr.Department_Id = department.Department_Id;


                    depSr.ScoresByYear = dScore.ToList();
                    depScoresG.Add(depSr);

                }
                depId = department.Department_Id;
            }
            return Ok(depScoresG);
        }
        /// <summary>
        /// get old dimension scores by department, retrieves a list of scores for each dimension
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="dep"></param>
        /// <returns></returns>
        [HttpGet("old_dep_dim/{sid}/{dep}")]
        public IActionResult GetOldScoresByDepDim([FromRoute] int sid, int dep)
        {
            var results = Queries.OldDimensionScores(_dataContext, sid, dep);
            return Ok(results);
        }

        /// <summary>
        /// Gets old scores by that department only
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="dep"></param>
        /// <returns></returns>
        [HttpGet("old_dep_overall/{sid}/{dep}")]
        public IActionResult GetOldScoresByDepOverall([FromRoute] int sid, int dep)
        {
            var results = Queries.OldOverallDep(_dataContext, sid, dep);
            return Ok(results);
        }
        /// <summary>
        /// Get any old score based on inputs, empty will be assigned zero
        /// </summary>
        /// <param name="sid"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpGet("old_filter/{sid}")]
        public async Task<IActionResult> GetOldScoresFilter([FromRoute] int sid, [FromQuery] OldScoresFilter dto)
        {
            /// hard coded generation ids to fix a bug.. future projects may need it to be dynamic
            var results = Queries.OldFilter(_dataContext, sid, dto);
            var AlteredResults= new List<Old_Scores>();



            // add for loop to add zero to empty values
            if (dto.Category == "GenerationDep" && results.Count() != 4)
            {
                for (int generationId = 1; generationId <= 4; generationId++)
                {
                    var existingScore = results.FirstOrDefault(score => score.Generation_Id == generationId);
                    if (existingScore == null)
                    {
                        var newScore = new Old_Scores
                        {
                            Generation_Id = generationId,
                            Score = 0,
                            Generation = Datahelper.GetGenerationById(generationId)
                        };
                        AlteredResults.Add(newScore);
                    }
                    else
                    {
                        AlteredResults.Add(existingScore);
                    }   
               
                }
                return Ok(AlteredResults);
            }
            else
            {
                return Ok(results);
            }
               
        }
         /// <summary>
         /// gets the direct subdepartments of the inputed department and thier old scores
         /// </summary>
         /// <param name="sid"></param>
         /// <param name="depId"></param>
         /// <returns></returns>
        [HttpGet("old_sub/{sid}/{depId}")]
        public async Task<IActionResult> GetOldSubScores([FromRoute] int sid, int depId)
        {
            // call GetSubDepartmentsOfDepartment from DepartmentHelpers
            var subDeps = await DepartmentHelpers.GetSubDepartmentsOfDepartment(_dataContext, depId);
            // get list of sub department ids from subDeps using linq
            var depDetails= await _dataContext.Departments.FindAsync(depId);
            var subDepIds = subDeps.Where(x => x.TreeLevel==depDetails.TreeLevel+1).Select(x => x.Id).ToList();
             var results = Queries.OldSubDep(_dataContext, sid, subDepIds);
            return Ok(results);
        }
    }
}
