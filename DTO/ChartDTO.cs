namespace Survey_Backend.DTO 
{
	public class ChartDTO 
	{
		public int DimensionId { get; set; }
		public string? DimensionName { get; set; }
		public List<percentageChart>? questions { get; set; }

	}
	public class questionDTO 
	{
		public int QuestionId { get; set; }
		public string? DimensionName { get; set; }
		public List<percentageChart>? DimChartData { get; set; }
	}
	public class percentageChart 
	{
		public int Score { get; set; }
		public int QuestionId { get; set; }
		public int ColumnName { get; set; }
		public int DimensionId { get; set; }
		public string? DimensionName { get; set; }
		public int Score1 { get; set; }
		public int Score2 { get; set; }
		public int Score3 { get; set; }
		public int Score4 { get; set; }
		public int Score5 { get; set; }
		public int[] scores { get; set; } = new int[5];
	}
}
