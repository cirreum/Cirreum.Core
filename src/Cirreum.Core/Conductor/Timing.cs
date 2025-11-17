namespace Cirreum.Conductor;

using System.Diagnostics;
using System.Runtime.CompilerServices;

internal static class Timing {

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static long Start() => Stopwatch.GetTimestamp();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static double GetElapsedMilliseconds(long startTimestamp)
		=> Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

}