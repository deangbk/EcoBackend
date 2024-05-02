using Microsoft.EntityFrameworkCore;
using Survey_Backend.Models;
using System.Security.Cryptography;

namespace Survey_Backend.Data {
	public class DataContext : DbContext {
		public DataContext(DbContextOptions<DataContext> options) : base(options) {
		}

		public DbSet<Survey> Surveys { get; set; }
		public DbSet<SurveyQuestions> SurveyQuestions { get; set; }

		public DbSet<Question> Questions { get; set; }
		public DbSet<Dimension> Dimensions { get; set; }

		public DbSet<QuestionResponse> QuestionResponses { get; set; }

		public DbSet<ResponderInfo> ResponderInfos { get; set; }
		public DbSet<Role> Roles { get; set; }
		public DbSet<Department> Departments { get; set; }
		public DbSet<Generation> Generations { get; set; }
        public DbSet<Old_Scores> Old_Scores { get; set; }

        //--------------------------------------------------------------------------

        // Scalar DB functions
        public int GetResponderAge(Guid responderId) => throw new NotSupportedException();
		public int GetGenerationID(int birthYear) => throw new NotSupportedException();
		public string GetAllSubDepartmentsAsString(int deptId) => throw new NotSupportedException();

		// Table DB functions
		public IQueryable<Department_ParentChain> GetParentDepartmentsChain() 
			=> FromExpression(() => GetParentDepartmentsChain());
		public IQueryable<Department_Parent_LvRel> GetAllSubDepartments(int deptId)
			=> FromExpression(() => GetAllSubDepartments(deptId));

		//--------------------------------------------------------------------------

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			// -----------------------------------------------------
			// Scalar DB functions

			// Register interface for dbo.ufnGetResponderAge
			modelBuilder.HasDbFunction(typeof(DataContext)
			  .GetMethod(nameof(GetResponderAge), new[] { typeof(Guid) })!)
			  .HasName("ufnGetResponderAge");

			// Register interface for dbo.ufnGetGenerationID
			modelBuilder.HasDbFunction(typeof(DataContext)
			  .GetMethod(nameof(GetGenerationID), new[] { typeof(int) })!)
			  .HasName("ufnGetGenerationID");

			// Register interface for dbo.ufnGetAllSubDeptsAsStr
			modelBuilder.HasDbFunction(typeof(DataContext)
			  .GetMethod(nameof(GetAllSubDepartmentsAsString), new[] { typeof(int) })!)
			  .HasName("ufnGetAllSubDeptsAsStr");

			// -----------------------------------------------------
			// Table DB functions

			// Register interface for dbo.ufnGetDepartmentsChain
			modelBuilder.HasDbFunction(typeof(DataContext)
			  .GetMethod(nameof(GetParentDepartmentsChain))!)
			  .HasName("ufnGetParentDeptsChain");
			modelBuilder.Entity<Department_ParentChain>().HasNoKey();

			// Register interface for dbo.ufnGetAllSubDepts
			modelBuilder.HasDbFunction(typeof(DataContext)
			  .GetMethod(nameof(GetAllSubDepartments), new[] { typeof(int) })!)
			  .HasName("ufnGetAllSubDepts");
			modelBuilder.Entity<Department_Parent_LvRel>().HasNoKey();
		}

		//--------------------------------------------------------------------------

		public async Task<int> SetIdentityInsert(string table, bool on) {
			string dboTable = $"[dbo].[{table}]";
			string setArg = on ? "on" : "off";
			return await Database.ExecuteSqlRawAsync($"set IDENTITY_INSERT {dboTable} {setArg}");
		}
	}
}
