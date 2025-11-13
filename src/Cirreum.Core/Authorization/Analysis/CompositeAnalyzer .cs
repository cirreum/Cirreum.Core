namespace Cirreum.Authorization.Analysis;

public class CompositeAnalyzer(IEnumerable<IAuthorizationAnalyzer> analyzers) {

	public async Task<AnalysisReport> AnalyzeAllAsync() {
		List<AnalysisReport> reports = [];
		if (OperatingSystem.IsBrowser()) {
			foreach (var analyzer in analyzers) {
				await Task.Yield();
				var report = analyzer.Analyze();
				reports.Add(report);
			}
		} else {
			foreach (var analyzer in analyzers) {
				var report = analyzer.Analyze();
				reports.Add(report);
			}
		}
		return AnalysisReport.Combine(reports);
	}

}