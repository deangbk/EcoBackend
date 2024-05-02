using Survey_Backend.DTO;

namespace Survey_Backend.Helpers 
{
	public class Datahelper 
	{
		/// <summary>
		/// change to get from database in future
		/// </summary>
		/// <param name="genID"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static int GetGenerationById(int genID)
		{
            switch (genID)
            {
                case 1:
                    return 1964;
                case 2:
                    return 1980;
                case 3:
                    return 1997;
                case 4:
                    return 2010;
                default:
                    throw new ArgumentException("Invalid generation ID");
            }
        }


        public static List<ChartDTO> MapPercentage(List<percentageChart> Results) 
		{
			string previousDimensionName = "";
			var newDim = new ChartDTO();
			var dimList = new List<ChartDTO>();
			var qDTO = new List<questionDTO>();
			var qD = new questionDTO();
			var pcList = new List<percentageChart>();
			var pcD = new percentageChart();
			var i = 0;

			foreach (var result in Results) 
			{

				if (result.DimensionName != previousDimensionName) 
				{
					if (i != 0) { 
						dimList.Add(newDim); 
					}

					pcD = new percentageChart();
					newDim = new ChartDTO();

					newDim.DimensionName = result.DimensionName;
					newDim.DimensionId = (int)result.DimensionId;

					newDim.questions = new List<percentageChart>();

					pcD.QuestionId = result.QuestionId;
					pcD.DimensionName = result.DimensionName;

					pcD.Score1 = result.Score1;
					pcD.Score2 = result.Score2;
					pcD.Score3 = result.Score3;
					pcD.Score4 = result.Score4;
					pcD.Score5 = result.Score5;
					pcD.scores[0] = result.Score1;
					pcD.scores[1] = result.Score2;
					pcD.scores[2] = result.Score3;
					pcD.scores[3] = result.Score4;
					pcD.scores[4] = result.Score5;

					newDim.questions.Add(pcD);

					// DimensionName has changed
					// Perform desired actions or logic here

					// Update previousDimensionName to the current DimensionName
					previousDimensionName = result.DimensionName;
				}
				else 
				{
					pcD = new percentageChart();

					pcD.QuestionId = result.QuestionId;
					pcD.DimensionName = result.DimensionName;

					pcD.Score1 = result.Score1;
					pcD.Score2 = result.Score2;
					pcD.Score3 = result.Score3;
					pcD.Score4 = result.Score4;
					pcD.Score5 = result.Score5;
					pcD.scores[0] = result.Score1;
					pcD.scores[1] = result.Score2;
					pcD.scores[2] = result.Score3;
					pcD.scores[3] = result.Score4;
					pcD.scores[4] = result.Score5;

					newDim.questions.Add(pcD);
				}
				i++;
			}
			dimList.Add(newDim);
			return dimList;
		}
	}
}
