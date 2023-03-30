namespace VisualMonkeySort;

public class MergeSort
{
	public static List<T> Sort<T>(List<T> unsorted, IComparer<T> comparer)
	{
		if (unsorted.Count <= 1)
			return unsorted;

		var left = new List<T>();
		var right = new List<T>();

		var median = unsorted.Count / 2;
		for (var i = 0; i < median; i++)
			left.Add(unsorted[i]);
		for (var i = median; i < unsorted.Count; i++) 
			right.Add(unsorted[i]);

		left = Sort(left, comparer);
		right = Sort(right, comparer);
		return Merge(left, right, comparer);
	}

	private static List<T> Merge<T>(List<T> left, List<T> right, IComparer<T> comparer)
	{
		var result = new List<T>();

		while (left.Count > 0 || right.Count > 0)
		{
			if (left.Count > 0 && right.Count > 0)
			{
				if (comparer.Compare(left[0], right[0]) <= 0)
				{
					result.Add(left.First());
					left.Remove(left.First());      
				}
				else
				{
					result.Add(right.First());
					right.Remove(right.First());
				}
			}
			else if (left.Count > 0)
			{
				result.Add(left.First());
				left.Remove(left.First());
			}
			else if (right.Count > 0)
			{
				result.Add(right.First());
				right.Remove(right.First());
			}
		}
		
		return result;
	}
}