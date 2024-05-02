using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Survey_Backend.Models {
	using JsonTable = Dictionary<string, object>;

	[PrimaryKey(nameof(Id))]
	public class Question {
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Required]
		public int Id { get; set; }                 // Question ID
		[Required]
		public int QuestionIndex { get; set; }      // Question index
		[Required]
		public int SurveySetId { get; set; }        // Survey set ID

		[Required]
		public QuestionType Type { get; set; }

		public int DimensionId { get; set; }		// Dimension ID
		public string? DimensionName { get; set; }      // Intentional redundancy

		[Required]
		public string QuestionText { get; set; } = string.Empty;
		public string? QuestionTextTH { get; set; }
		public string? QuestionDescription { get; set; }
	}

	public enum QuestionType : int {
		Rating,
		Textbox,
	};

	[PrimaryKey(nameof(Id))]
	public class Dimension {	// aka question groups
		[Required]
		public int Id { get; set; }

		[Required]
		public string DimensionName { get; set; } = string.Empty;

		[Required]
		public string ProjectName { get; set; } = string.Empty;

		public string? ShortName { get; set; }

		public Tuple<string, string> GetNames() => new(DimensionName, ShortName ?? "");
		public List<object> GetNames_List() => Helpers.ValueHelpers.TupleToList(GetNames());

		public JsonTable ToTable(bool bExtraInfo = false) {
			var res = new JsonTable() {
				["dimension_id"] = Id,
				["dimension_name"] = DimensionName,
				["dimension_name_short"] = ShortName ?? "",
			};
			if (bExtraInfo) {
				res["project_name"] = ProjectName;
			}
			return res;
		}
	}
}
