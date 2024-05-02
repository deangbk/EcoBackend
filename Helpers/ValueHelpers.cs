using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Survey_Backend.Helpers {
	public static class ValueHelpers {
		public static List<object> TupleToList(ITuple tuple) {
			if (tuple == null)
				throw new ArgumentNullException(nameof(tuple));

			var result = new List<object>(tuple.Length);
			for (int i = 0; i < tuple.Length; i++) {
				result.Add(tuple[i]!);
			}
			return result;
		}

		/// <summary>
		/// Sorts a given list into a list of lists, segregated by ranges according to the given comparator function
		/// </summary>
		public static List<List<T>> SortIntoRanges<T, U>(List<T> source, 
			List<(U lower, U upper)> ranges, Func<T, U, int> comparer) {

			int count = ranges.Count;
			var result = Enumerable.Range(0, count)
				.Select(x => new List<T>()).ToList();

			foreach (var iValue in source) {
				int placement = ranges.FindIndex(
					x => (comparer(iValue, x.lower) >= 0 && comparer(iValue, x.upper) < 0));

				if (placement >= 0)
					result[placement].Add(iValue);
			}

			return result;
		}

		public static IEnumerable<int> SplitIntString(string str) {
			return str.Split(',')
				.Select(x => int.Parse(x.Trim()));
		}
	}
}
