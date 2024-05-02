using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Survey_Backend.Models {
	[PrimaryKey(nameof(Id), nameof(ResponderId))]
	public class QuestionResponse {
		[Required]
		public int Id { get; set; }                 // Row ID
		[Required]
		public Guid ResponderId { get; set; }

		[Required]
		public int SurveySetId { get; set; }        // Survey set ID

		public int? AnswerScore { get; set; }		// Response if the question was score rating
		public string? AnswerText { get; set; }     // Response if the question was free text

		[Required]
		public DateTime ResponseDate { get; set; }

		public int QuestionId { get; set; }			// Index of the question in the survey
		public int? DimensionId { get; set; }
	}
}
