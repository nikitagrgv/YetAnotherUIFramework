namespace SFMLUI;

internal static partial class Utils
{
	public static T GetLast<T>(this IEnumerator<T> enumerator)
	{
		bool hasAny = false;
		T last = default!;

		while (enumerator.MoveNext())
		{
			hasAny = true;
			last = enumerator.Current;
		}

		if (!hasAny)
		{
			throw new InvalidOperationException("Sequence contains no elements.");
		}

		return last;
	}
}