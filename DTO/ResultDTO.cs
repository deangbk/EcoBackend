using System;
using System.Collections.Generic;

using Survey_Backend.Models;
using Survey_Backend.Controllers;

namespace Survey_Backend.DTO {
	/// <summary>
	/// For <see cref="ResultController.GetResultsOverview"/> 
	/// </summary>
	public class ResultsOverview {
		public string gets { get; set; } = string.Empty;
	}

	/// <summary>
	/// For <see cref="ResultController.GetResultsByCategoryFilter"/> 
	/// </summary>
	public class ResultsFilter {
		public string category { get; set; } = string.Empty;
		public int id { get; set; } = -1;
		public bool top_depts_only { get; set; } = false;

		public enum Type : int {
			Dimension,
			Department,
			Role,
			Generation,
			Age,
			BirthYear,
			ServiceMonth,
			Gender,
			Invalid,
		}
	}

	/// <summary>
	/// For <see cref="ResultController.GetResultsByCategoryFilter_Specific"/> 
	/// </summary>
	public class ResultsFilterSpecific {
		public string category { get; set; } = string.Empty;
		public int id { get; set; }

		public string category_get { get; set; } = string.Empty;
		public int id_get { get; set; }
	}

	/// <summary>
	/// For <see cref="ResultController.GetResultsByRangeFilter"/> 
	/// </summary>
	public class ResultsRangeFilter {
		public string category { get; set; } = string.Empty;
		public int id { get; set; } = -1;

		public string category_range { get; set; } = string.Empty;
		public string ranges { get; set; } = string.Empty;

		public bool more { get; set; } = false;
	}

	/// <summary>
	/// For <see cref="ResultController.QuestionsResultsFilter"/> 
	/// </summary>
	public class QuestionsResultsFilter {
		public string category { get; set; } = string.Empty;
		public int id { get; set; } = -1;

		public int dim_id { get; set; } = -1;

		public int range_l { get; set; } = -1;
		public int range_u { get; set; } = -1;
	}

	public class OldScoresFilter
	{
		
		public int Role { get; set; } = 0;

		public int Dim_id { get; set; } = 0;

		public int Generation { get; set; } = 0;
        public int Generation_Id { get; set; } = 0;
        public int Years_Service { get; set; } = 0;

		public int Dep_Id { get; set; } = 0;
        public string? OrderBy { get; set; } 
		public string? Category { get; set; } 
    }
}
