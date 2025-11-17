namespace Cirreum;

class DomainContextInitializer(
	IDomainEnvironment domainEnvironment
) : IDomainContextInitializer {
	public void Initialize() {
		DomainContext.Initialize(domainEnvironment);
	}
}