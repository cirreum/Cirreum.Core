namespace Cirreum.Messaging.Options;
using Microsoft.Extensions.Options;

/// <summary>
/// Validates the <see cref="TimeBatchingProfile.Rules"/>
/// </summary>
public class TimeBatchingValidation : IValidateOptions<DistributionOptions> {

	/// <inheritdoc/>
	public ValidateOptionsResult Validate(string? name, DistributionOptions options) {

		var failures = new List<string>();

		if (options.Sender.BackgroundDelivery?.TimeBatchingProfiles != null) {
			foreach (var profileEntry in options.Sender.BackgroundDelivery.TimeBatchingProfiles) {
				var profileName = profileEntry.Key;
				var profile = profileEntry.Value;

				foreach (var rule in profile.Rules) {

					// Validate StartHour
					if (rule.StartHour < 0 || rule.StartHour > 23) {
						failures.Add($"In profile '{profileName}', rule '{rule.Description}': StartHour must be between 0-23. Invalid value {rule.StartHour}");
					}

					// Validate EndHour
					if (rule.EndHour < 0 || rule.EndHour > 24) {
						failures.Add($"In profile '{profileName}', rule '{rule.Description}': EndHour must be between 0-24. Invalid value {rule.EndHour}");
					}

				}
			}
		}

		if (failures.Count > 0) {
			return ValidateOptionsResult.Fail(failures);
		}

		return ValidateOptionsResult.Success;

	}

}