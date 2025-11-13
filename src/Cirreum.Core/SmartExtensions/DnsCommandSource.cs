namespace Cirreum.SmartExtensions;

using System.Net;

#region DnsCommandSource
/// <summary>
/// A custom <see cref="CommandSource{T}"/>, that resolve various IPAddress values.
/// </summary>
/// <remarks>
/// <para>
/// The commands supported are (non-case sensitive)
/// <list type="table">
/// <listheader>
/// <term>Command</term>
/// <description>Details</description>
/// </listheader>
/// <item>
/// <term>IPAddress (or ipaddress)</term>
/// <description>Resolves the first or null, IPAddress returned from Dns.GetHostAddresses(Dns.GetHostName())</description>
/// </item>
/// <item>
/// <term>IP4Address (or ip4address)</term>
/// <description>Resolves the first or null, IP v4 Address returned from Dns.GetHostAddresses(Dns.GetHostName())</description>
/// </item>
/// <item>
/// <term>IP6Address (or ip6address)</term>
/// <description>Resolves the first or null, IP v6 Address returned from Dns.GetHostAddresses(Dns.GetHostName())</description>
/// </item>
/// <item>
/// <term>HostName (or hostname)</term>
/// <description>Resolves host name of the local computer from Dns.GetHostName()</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public class DnsCommandSource() : CommandSource<string>("dns") {

	/// <summary>
	/// The command prefix Key used to identify this command source
	/// in a format string.
	/// </summary>
	private const string dnsKey = "dns";

	/// <summary>
	/// KeyValue Pairs containing the supported command names and their associated resolver functions.
	/// </summary>
	private readonly static Dictionary<string, Func<string>> Commands =
		new Dictionary<string, Func<string>>(StringComparer.OrdinalIgnoreCase) {
			{ "IPAddress", () => GetIpAddress(null) },
			{ "IP4Address", () => GetIpAddress(false) },
			{ "IP6Address", () => GetIpAddress(true) },
			{ "HostName", () => Dns.GetHostName() }
	};

	/// <summary>
	/// Helper method to get the first or default IP Address
	/// </summary>
	/// <param name="isIP6">
	/// <c>null</c> for the first IPAddress; <see langword="true"/> for the first IP v6
	/// Address, or <see langword="false"/> for the first IP v4 Address.</param>
	/// <returns>The value resolved or null.</returns>
	private static string GetIpAddress(bool? isIP6 = null) {

		if (isIP6 == false) {
			var ip4 = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
			if (ip4 != null) {
				return ip4.ToString();
			}
		}

		if (isIP6 == true) {
			var ip6 = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
			if (ip6 != null) {
				return ip6.ToString();
			}
		}

		var ip = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault();
		if (ip != null) {
			return ip.ToString();
		}

		return "[No IP Address Found]";

	}

	/// <summary>
	/// Resolve a command for the first IPAddress, the first IP4Address, the first IP6Address or HostName
	/// using the <see cref="Dns"/> object.
	/// </summary>
	/// <param name="command"></param>
	/// <returns>The resolve value; or null.</returns>
	protected override string? ResolveValue(string command) {

		if (Commands.TryGetValue(command, out var cmd)) {
			return cmd();
		}
		;

		return null;

	}

}
#endregion