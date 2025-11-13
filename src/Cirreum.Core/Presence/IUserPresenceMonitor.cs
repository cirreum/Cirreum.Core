namespace Cirreum.Presence;

public interface IUserPresenceMonitor {

	/// <summary>
	/// Start monitoring the users presence.
	/// </summary>
	Task StartMonitoringPresence();

	/// <summary>
	/// Stop monitoring the users presence.
	/// </summary>
	Task StopMonitoringPresence();

	/// <summary>
	/// Is the monitor currently running?
	/// </summary>
	bool IsMonitoring { get; }

}