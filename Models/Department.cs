using System.Net;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace Survey_Backend.Models {
	using JsonTable = Dictionary<string, object>;

	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	[PrimaryKey(nameof(Id))]
	public class Department {
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		[Required]
		public int Id { get; set; }

		[Required]
		public string DepartmentName { get; set; } = string.Empty;
		[Required]
		public string ProjectName { get; set; } = string.Empty;

		public string? ShortName { get; set; }

		public int? ParentDepartmentId { get; set; }
		public string? ParentDepartmentName { get; set; }

		public int TreeLevel { get; set; }
		public int Population { get; set; }
        public int sortOrder { get; set; }

        public Tuple<string, string> GetNames() => new(DepartmentName, ShortName ?? "");
		public List<object> GetNames_List() => Helpers.ValueHelpers.TupleToList(GetNames());

		public override string ToString() {
			string s = string.Format("Id={0}: {1}", Id, DepartmentName);
			if (ParentDepartmentId != null)
				s += string.Format(" (Parent={0}: {1})", ParentDepartmentId, ParentDepartmentName);
			return s;
		}
		private string DebuggerDisplay {
			get => this.ToString();
		}

		public JsonTable ToTable(bool bExtraInfo = false) {
			var res = new JsonTable() {
				["department_id"] = Id,
				["department_name"] = DepartmentName,
				["department_name_short"] = ShortName ?? "",
			};
			if (bExtraInfo) {
				res["project_name"] = ProjectName;
				res["population"] = Population;
			}
			return res;
		}
	}

	public class Department_ParentChain {
		public int Id { get; set; }
		public int? ParentDepartmentId { get; set; }
		public string ChainDept { get; set; } = string.Empty;
        public string ChainLevel { get; set; } = string.Empty;
    }
	public class Department_Parent_LvRel {
		public int Id { get; set; }
		public int? ParentDepartmentId { get; set; }
		public int LevelRelative { get; set; }
	}

    [PrimaryKey(nameof(Id))]
    public class Old_Scores
    {
        [Required]
        
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int Department_Id { get; set; }
        [Required]
        public int Project_id { get; set; }
        [Required]
        public decimal Score { get; set; }
        [Required]
        public int Year { get; set; }
		public int? Generation { get; set; }
        public int? Generation_Id { get; set; }
        public int? Role { get; set; }
		public int? YearService { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Dim { get; set; }
        [NotMapped]
        public int Department_Order { get; set; }

    }

    public class DepartmentScoresGrouped
    {
        public int Department_Id { get; set; }
        public List<Old_Scores> ScoresByYear { get; set; }
    }
}


