``` mermaid
sequenceDiagram
	participant C as Client
	participant E as API Endpoint
	participant M as MediatR Pipeline
	participant A as Authorization Behavior
	participant R as Permission Registry

	Note over C,R: 1. System Initialization
	activate R
	R->>R: Register Default Permissions
	R->>R: Register Default Roles
	R->>R: Set up Role Inheritance
	deactivate R
	
	Note over C,R: 2. Domain Registration
	activate R
	R->>R: Register Domain Permissions
	R->>R: Register Domain Roles
	R->>R: Set up Domain Role Inheritance
	deactivate R

	Note over C,R: 3. Runtime Request Flow
	C->>E: HTTP Request
	activate E
	E->>E: Check Role + Scope (ASP.NET Auth)
	E->>M: Delegate to MediatR
	deactivate E
	
	activate M
	M->>A: Request Behavior
	deactivate M
	
	activate A
	Note over A: 4. Ensure User Authenticated
	A-->>A: Not Authenticated
	alt Not Authenticated
		A->>E: UnauthorizedRequestException
		E->>C: 401 Unauthorized
	end
	
	Note over A: 5. Fine-Grained Permission Check
	A->>A: Get User Roles
	A->>R: Check User Permissions
	activate R
	R->>R: Check Direct Permissions
	R->>R: Check Inherited Permissions
	R->>A: Permission Result
	A->>A: Evaluate Allow/Deny Access
	deactivate R
	
	Note over A: 6. Outcome
	alt Authorized
		A-->>M: Continue Pipeline
		M-->>E: Result
		E-->>C: Response
	else Not Authorized
		A->>E: ForbiddenRequestException
		E->>C: 403 Forbidden
	end
	deactivate A
```