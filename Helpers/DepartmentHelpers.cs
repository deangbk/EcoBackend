using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

using Survey_Backend.Data;
using Survey_Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Survey_Backend.Helpers {
	using JsonTable = Dictionary<string, object>;

	public class DepartmentHelpers {
		/// <summary>
		/// Gets list of all top-level departments in the given list
		/// </summary>
		public static List<Department> GetTopDepartments(List<Department> departments) {
			var result = departments
				.Where(x => x.ParentDepartmentId == null)
				.ToList();
			return result;
		}

		/// <summary>
		/// Gets list of all top-level departments in the given list
		/// </summary>
		public static async Task<List<Department>> GetTopDepartments(DataContext dataContext, List<int> departments) {
			var result = await dataContext.Departments
				.Where(x => x.ParentDepartmentId == null)
				.Where(x => departments.Any(y => y == x.Id))
				.ToListAsync();
			return result;
		}

		/// <summary>
		/// Gets list of all non-top-level departments in the given list
		/// </summary>
		public static List<Department> GetSubDepartments(List<Department> departments) {
			var result = departments
				.Where(x => x.ParentDepartmentId != null)
				.ToList();
			return result;
		}
		/// <summary>
		/// Gets list of all non-top-level departments in the given list
		/// </summary>
		public static async Task<List<Department>> GetSubDepartments(DataContext dataContext, List<int> departments) {
			var result = await dataContext.Departments
				.Where(x => x.ParentDepartmentId != null)
				.Where(x => departments.Any(y => y == x.Id))
				.ToListAsync();
			return result;
		}

		// -----------------------------------------------------

		[DebuggerDisplay("{DebuggerDisplay,nq}")]
		public class DepartmentTree {
			public Department department;
			public Department? parent;
			public List<DepartmentTree> children;
			public int level;
			public int populationTotal;

			public DepartmentTree(Department dept) {
				this.department = dept;
				this.parent = null;
				this.children = new();
				this.level = 0;
				this.populationTotal = 0;
			}
			public DepartmentTree(Department dept, List<DepartmentTree> children) : this(dept) {
				this.children = children;
			}

			private string DebuggerDisplay {
				get => string.Format("{0} -> Children: {1}", department.ToString(), children.Count);
			}

			public JsonTable ToTable() {
				return new JsonTable(department.ToTable()) {
					["parent_id"] = parent != null ? parent.Id : null!,
					["children"] = children.Select(x => x.ToTable()).ToList(),
					["level"] = level,
					["population"] = department.Population,
					["population_sum"] = populationTotal,
				};
			}
		}

		/// <summary>
		/// Organizes given department list into a hierarchical tree structure
		/// </summary>
		public static List<DepartmentTree> CreateDepartmentTree(
			List<Department> departments) {

			var listAllTopDepts = GetTopDepartments(departments);
			var listAllSubDepts = GetSubDepartments(departments);

			List<DepartmentTree> res = new();

			// Blank definition for lambda recursion
			Action<DepartmentTree, int> _TraverseNode = null!;
			_TraverseNode = (node, level) => {
				Department nodeDept = node.department;
				var listSubDepts = GetSubDepartmentsOfDepartment(listAllSubDepts, nodeDept.Id)
					.Select(x => new DepartmentTree(x) { 
						parent = nodeDept,
						level = level + 1,
					})
					.ToList();

				node.children = listSubDepts;
				foreach (var i in listSubDepts)
					_TraverseNode(i, level + 1);

				node.populationTotal = nodeDept.Population
					+ node.children.Select(x => x.populationTotal).Sum();
			};

			foreach (var iDept in listAllTopDepts) {
				var node = new DepartmentTree(iDept) {
					level = 0,
				};
				_TraverseNode(node, 0);

				res.Add(node);
			}

			return res;
		}

		// -----------------------------------------------------
		public class DepartmentTree2 {
			public Department department;
			public Department? parent;
			public List<Department> children;
			public int level;
			public int populationTotal;

			public DepartmentTree2(Department dept, Department? parent, List<Department> children) {
				this.department = dept;
				this.parent = parent;
				this.children = children;
				this.level = 0;
			}

			public JsonTable ToTable() {
				return new JsonTable(department.ToTable()) {
					["parent_id"] = parent != null ? parent.Id : null!,
					["children_ids"] = children.Select(x => x.Id).ToArray(),
					["level"] = level,
					["population"] = department.Population,
					["population_sum"] = populationTotal,
				};
			}
		}

		/// <summary>
		/// Organizes given department list into a flattened hierarchy structure
		/// </summary>
		public static List<DepartmentTree2> CreateDepartmentTree2(
			List<Department> departments) {

			List<DepartmentTree2> resDeptList = new();

			var deptTree = CreateDepartmentTree(departments);

			// Blank definition for lambda recursion
			Func<DepartmentTree, DepartmentTree2> _TraverseNode = null!;
			_TraverseNode = (node) => {
				foreach (var i in node.children)
					_TraverseNode(i);

				List<Department> childrenAsDept = node.children.Select(x => x.department).ToList();

				var res = new DepartmentTree2(node.department, node.parent, childrenAsDept) {
					level = node.level,
					populationTotal = node.populationTotal,
				};

				resDeptList.Add(res);
				return res;
			};

			foreach (var i in deptTree)
				_TraverseNode(i);

			return resDeptList.OrderBy(x => x.department.Id).ToList();
		}

		// -----------------------------------------------------

		/// <summary>
		/// Gets the department's parent department
		/// </summary>
		public static async Task<Department?> GetParentDepartmentOfDepartment(
			DataContext dataContext, Department subDepartment) {

			if (subDepartment == null) return null;

			var result = await dataContext.Departments
				.FirstOrDefaultAsync(x => x.Id == subDepartment.ParentDepartmentId);

			return result;
		}
		/// <summary>
		/// Gets the department's parent department
		/// </summary>
		public static Department? GetParentDepartmentOfDepartment(
			List<Department> departments, Department subDepartment) {

			var result = departments
				.FirstOrDefault(x => x.Id == subDepartment.ParentDepartmentId);
			
			return result;
		}

		/// <summary>
		/// Gets the department's top department
		/// </summary>
		public static async Task<Department?> GetTopDepartmentOfDepartment(
			DataContext dataContext, Department subDepartment) {

			if (subDepartment == null) return null;

			Department? result = subDepartment;
			do {
				Department? parent = await dataContext.Departments
					.FirstOrDefaultAsync(x => x.Id == result.ParentDepartmentId);
				if (parent == null) return result;
				result = parent;
			} while (result!.ParentDepartmentId != null);

			return result;
		}
		/// <summary>
		/// Gets the department's top department
		/// </summary>
		public static Department? GetTopDepartmentOfDepartment(
			List<Department> departments, Department subDepartment) {

			Department? result = subDepartment;
			do {
				Department? parent = departments
					.FirstOrDefault(x => x.Id == result.ParentDepartmentId);
				if (parent == null) return result;
				result = parent;
			} while (result!.ParentDepartmentId != null);

			return result;
		}

		/// <summary>
		/// Gets all sub departments that belong under the given department
		/// </summary>
		public static async Task<List<Department>> GetSubDepartmentsOfDepartment(
			DataContext dataContext, int parentDepartmentId) {

			var result = await dataContext.Departments
				.Where(x => x.ParentDepartmentId == parentDepartmentId)
				.ToListAsync();

			return result;
		}
		/// <summary>
		/// Gets all sub departments that belong under the given department
		/// </summary>
		public static List<Department> GetSubDepartmentsOfDepartment(
			List<Department> departments, int departmentId) {

			var result = departments
				.Where(x => x.ParentDepartmentId == departmentId)
				.ToList();

			return result;
		}
	}
}
