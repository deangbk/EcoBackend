using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Net;

namespace Survey_Backend.Models {
	using JsonTable = Dictionary<string, object>;

	[PrimaryKey(nameof(ResponderId), nameof(SurveySetId))]
	public class ResponderInfo {
		[Required]
		public Guid ResponderId { get; set; }				// Unique ID, not employee ID because responders are anonymous
		[Required]
		public int SurveySetId { get; set; }

		[Required]
		public int RoleId { get; set; }						// Role of the person; junior, management, director, etc
		public string? RoleName { get; set; }				// Intentional redundancy

		[Required]
		public int DepartmentId { get; set; }				// Department the person belongs to
		public string? DepartmentName { get; set; }			// Intentional redundancy

		public DateTime? SurveyCompletedTime { get; set; }
		
		public IPAddress? IPAddress { get; set; }			// Technically you could change IPs so they're still anonymous

		public int Department_Id { get; set; }				// Highest level department ID
		//public int Dept2 { get; set; }					// Second highest level department ID
		//public int Dept3 { get; set; }					// Third highest level department ID

		[Range(0, int.MaxValue)]
		public int ServiceMonths { get; set; }				// Number of months in service

		[Range(1900, int.MaxValue)]
		public int BirthYear { get; set; }                  // Year born

		[Range(18, 256)]
		public int Age { get; set; }						// Age

		public int Gender { get; set; } = 0;                // Gender (https://en.wikipedia.org/wiki/ISO/IEC_5218)
	}

	[PrimaryKey(nameof(Id))]
	public class Generation {
		[Required]
		public int Id { get; set; }

		[Required]
		[Range(1900, int.MaxValue, ErrorMessage = "Invalid year lower bound for generation")]
		public int YearRangeLower { get; set; }
		[Required]
		[Range(1900, int.MaxValue, ErrorMessage = "Invalid year upper bound for generation")]
		public int YearRangeUpper { get; set; }

		[Required]
		public string GenerationName { get; set; } = string.Empty;

		public JsonTable ToTable(bool bExtraInfo = false) {
			var res = new JsonTable() {
				["generation_id"] = Id,
				["generation_name"] = GenerationName,
			};
			if (bExtraInfo) {
				res["year_min"] = YearRangeLower;
				res["year_max"] = YearRangeUpper;
			}
			return res;
		}
	}

	[PrimaryKey(nameof(Id))]
	public class Role {
		[Required]
		public int Id { get; set; }

		[Required]
		public string RoleName { get; set; } = string.Empty;
		[Required]
		public string ProjectName { get; set; } = string.Empty;

		public JsonTable ToTable(bool bExtraInfo = false) {
			var res = new JsonTable() {
				["role_id"] = Id,
				["role_name"] = RoleName,
			};
			if (bExtraInfo) {
				res["project_name"] = ProjectName;
			}
			return res;
		}
	}
}
