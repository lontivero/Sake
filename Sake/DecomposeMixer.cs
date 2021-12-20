using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sake
{
	internal class DecomposeMixer : IMixer
	{
		public uint FeeRate { get; }
		public uint InputSize { get; }
		public uint OutputSize { get; }
		public ulong InputFee => InputSize * FeeRate;
		public ulong OutputFee => OutputSize * FeeRate;
		public IOrderedEnumerable<ulong> Denominations { get; } = Decomposer.StdDenoms.Select(x => (ulong)x).OrderBy(x => x);

		public DecomposeMixer(uint feeRate = 10, uint inputSize = 69, uint outputSize = 33)
		{
			FeeRate = feeRate;
			InputSize = inputSize;
			OutputSize = outputSize;
		}

		public IEnumerable<IEnumerable<ulong>> CompleteMix(IEnumerable<IEnumerable<ulong>> inputs)
		{
			var inputArray = inputs.ToArray();

			SetProbabilities(inputArray.Select(x => (long)x.Sum()));
//			SetProbabilities(inputArray.SelectMany(x => x.Select(y => (long)y)));

			for (int i = 0; i < inputArray.Length; i++)
			{
				var currentUser = inputArray[i];
				var others = new List<ulong>();
				for (int j = 0; j < inputArray.Length; j++)
				{
					if (i != j)
					{
						others.AddRange(inputArray[j]);
					}
				}
				yield return Mix(currentUser, others);
			}
		}

		public Dictionary<char, int> DenominationProbabilities { get; } = new();
		public IEnumerable<(long Sum, int Count, ulong Decomposition)> AllDecompositions { get; private set; }

		private void SetProbabilities(IEnumerable<long> targets)
		{
			AllDecompositions = targets.SelectMany(x => Decomposer.Decompose(x, (long)OutputFee)).ToArray();

			foreach (var decomp in AllDecompositions)
			{

				for (var i = 0; i < decomp.Count; i++)
				{
					var denom = (char)((decomp.Decomposition >> 8 * i) & 0xff);
					if (!DenominationProbabilities.TryAdd(denom, 1))
					{
						DenominationProbabilities[denom]++;
					}
				}
			}
		}

		public IEnumerable<ulong> Mix(IEnumerable<ulong> myInputs, IEnumerable<ulong> othersInputs)
		{
			var myTarget = myInputs.Select(x => (long)x - (long)InputFee).Sum();
			var myDecompositions = Decomposer.Decompose(myTarget, (long)OutputFee);

			var myDecompositionFit = new Dictionary<ulong, (long Sum, int Count, ulong Decomposition, int Points)>();
			var maxBits = 0;
			foreach (var md in myDecompositions)
			{
				var points = 0;
				for (var i = 0; i < md.Count; i++)
				{
					var denom = (char)((md.Decomposition >> 8 * i) & 0xff);
					points += DenominationProbabilities.TryGetValue(denom, out var denomProb) ? denomProb : 0;
				}
				if (!myDecompositionFit.TryAdd(md.Decomposition, (md.Sum, md.Count, md.Decomposition, points)))
				{
					myDecompositionFit[md.Decomposition] = (md.Sum, md.Count, md.Decomposition, 2 * points);
				}
				/*
				var bits = 0;
				foreach(var od in AllDecompositions)
				{
					bits += CountSetBits(od.Decomposition & md.Decomposition);
				}
				if(!myDecompositionFit.TryAdd(md.Decomposition, (md.Sum, md.Count, md.Decomposition, bits)))
				{
					myDecompositionFit[md.Decomposition] = (md.Sum, md.Count, md.Decomposition, 2 * bits);
				}*/
				maxBits = Math.Max(maxBits, points);
			}

			var candidates = myDecompositionFit.Select(x => x.Value)
				.OrderByDescending(x => (0.1 * (x.Points / (double)maxBits)) - 0.85 * (x.Count / 8.0) - 0.05 * ((myTarget - x.Sum) / 100.0))
				.ToList();
			var dec = candidates.First();
			var val = ToRealValuesArray(dec.Decomposition, dec.Count, Decomposer.DenomsFor(myTarget, (long)OutputFee));
			var totalOutputFee = (dec.Count * (long)OutputFee);
			var expectedValue = myTarget - totalOutputFee;
			var diff = myTarget - dec.Sum;
			if (diff > 100 || expectedValue - (long)val.Sum() != diff)
			{
				throw new Exception("BUG: Decomposition is not valid");
			}
			return val;
		}

		private static int CountSetBits(ulong n)
		{
			int count = 0;
			while (n > 0)
			{
				count += (int)n & 1;
				n >>= 1;
			}
			return count;
		}

		private IEnumerable<ulong> ToRealValuesArray(ulong decomposition, int count, long[] denoms)
		{
			var list = new ulong[count];
			for (var i = 0; i < count; i++)
			{
				var index = (decomposition >> (i * 8)) & 0xff;
				list[count - i - 1] = (ulong)denoms[index] - OutputFee;
			}
			return list;
		}

	}

	public static class Decomposer
	{
		public static readonly long[] StdDenoms = new long[] {
			1, 2, 3, 4, 5, 6, 8, 9, 10, 16, 18, 20, 27, 32, 50, 54, 64, 81, 100, 128, 162, 200,
			243, 256, 486, 500, 512, 729, 1000, 1024, 1458, 2000, 2048, 2187, 4096, 4374, 5000,
			6561, 8192, 10000, 13122, 16384, 19683, 20000, 32768, 39366, 50000, 59049, 65536,
			100000, 118098, 131072, 177147, 200000, 262144, 354294, 500000, 524288, 531441,
			1000000, 1048576, 1062882, 1594323, 2000000, 2097152, 3188646, 4194304, 4782969,
			5000000, 8388608, 9565938, 10000000, 14348907, 16777216, 20000000, 28697814,
			33554432, 43046721, 50000000, 67108864, 86093442, 100000000, 129140163, 134217728,
			200000000, 258280326, 268435456, 387420489, 500000000, 536870912, 774840978,
			1000000000, 1073741824, 1162261467, 2000000000, 2147483648, 2324522934, 3486784401,
			4294967296, 5000000000, 6973568802, 8589934592, 10000000000, 10460353203, 17179869184,
			20000000000, 20920706406, 31381059609, 34359738368, 50000000000, 62762119218,
			68719476736, 94143178827, 100000000000, 137438953472, 188286357654, 200000000000,
			274877906944, 282429536481, 500000000000, 549755813888, 564859072962, 847288609443,
			1000000000000, 1099511627776, 1694577218886, 2000000000000, 2199023255552, 2541865828329
		}.SkipWhile(x => x < 486 + 330).Reverse().ToArray();

		private static readonly Dictionary<long, IEnumerable<(long Sum, int Count, ulong Decomposition)>> _cache = new();

		public static IEnumerable<(long Sum, int Count, ulong Decomposition)> Decompose(long target, long outputFee)
		{
			if (_cache.TryGetValue(target, out var result))
			{
				return result;
			}
			var denoms = DenomsFor(target, outputFee);
			var tolerance = 10;
			var ret = denoms.SelectMany((_, i) => InternalCombinations(target, tolerance, maxLength: 8, denoms)).Take(40).ToList();
			while (ret.Count == 0)
			{
				tolerance += 20;
				ret = denoms.SelectMany((_, i) => InternalCombinations(target, tolerance, maxLength: 8, denoms)).Take(50).ToList();
			}
			_cache.Add(target, ret);
			return ret;
		}

		public static long[] DenomsFor(long target, long outputFee)
		{
			return StdDenoms.Select(x => x + outputFee).SkipWhile(x => x > target).Skip(1).ToArray();
		}

		private static IEnumerable<(long Sum, int Count, ulong Decomposition)> InternalCombinations(long target, long tolerance, int maxLength, long[] denoms)
		{
			IEnumerable<(long Sum, int Count, ulong Decomposition)> Combinations(
				int currentDenominationIdx,
				ulong accumulator,
				long sum,
				int k)
			{
				accumulator = (accumulator << 8) | ((ulong)currentDenominationIdx & 0xff);
				var currentDenomination = denoms[currentDenominationIdx];
				sum += currentDenomination;
				var remaining = target - sum;
				if (k == 0 || remaining < tolerance)
					return new[] { (sum, maxLength - k, accumulator) };

				currentDenominationIdx = Search(remaining, denoms, currentDenominationIdx);

				return Enumerable.Range(0, denoms.Length - currentDenominationIdx)
					.TakeWhile(i => k * denoms[currentDenominationIdx + i] >= remaining - tolerance)
					.SelectMany((_, i) =>
						Combinations(currentDenominationIdx + i, accumulator, sum, k - 1)
						.TakeUntil(x => x.Sum == target));
			}

			return denoms.SelectMany((_, i) => Combinations(i, 0ul, 0, maxLength - 1)).Take(60).ToList();
		}

		private static int Search(long value, long[] denoms, int offset)
		{
			var startingIndex = Array.BinarySearch(denoms, offset, denoms.Length - offset, value, ReverseComparer.Default);
			return startingIndex < 0 ? ~startingIndex : startingIndex;
		}
	}

	public static class LinqEx
	{
		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> list, Func<T, bool> predicate)
		{
			foreach (T el in list)
			{
				yield return el;
				if (predicate(el))
					yield break;
			}
		}
	}

	public class ReverseComparer : IComparer<long>
	{
		public static readonly ReverseComparer Default = new();
		public int Compare(long x, long y)
		{
			// Compare y and x in reverse order.
			return y.CompareTo(x);
		}
	}
}
