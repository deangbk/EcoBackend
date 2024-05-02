using System;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Survey_Backend.Models {
	[PrimaryKey(nameof(SurveySetId))]
	public class Survey {
		[Required]
		public int SurveySetId { get; set; }
		[Required]
		public DateTime SurveyCreatedDate { get; set; }
		[Required]
		public string ProjectName { get; set; } = string.Empty;
		[Required]
		public string SurveyDescription { get; set; } = string.Empty;
	}

	[PrimaryKey(nameof(SurveySetId), nameof(QuestionIndex))]
	public class SurveyQuestions {
		[Required]
		public int SurveySetId { get; set; }

		[Required]
		public int QuestionIndex { get; set; }

		[Required]
		public int QuestionId { get; set; }
	}
}
