using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace Survey_Backend.Helpers {
	public static class AsyncHelpers {
		// Actually, don't use this
		// Task.WhenAll interacts badly with database accesses
		public static async Task<IEnumerable<TResult>> SelectAsync<TSource, TResult>(
			this IEnumerable<TSource> source, Func<TSource, Task<TResult>> method) {
			return await Task.WhenAll(source.Select(async s => await method(s)));
		}
	}
}
