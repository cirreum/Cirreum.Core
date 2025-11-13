# Cirreum Core

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.svg?style=flat-square)](https://www.nuget.org/packages/Cirreum/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.svg?style=flat-square)](https://www.nuget.org/packages/Cirreum/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum?style=flat-square)](https://github.com/cirreum/Cirreum/releases)

## Overview
The CORE layer serves as the foundation of the application, encapsulating the core business logic and application rules. It is designed to be independent of external frameworks, ensuring that the core logic remains reusable and testable.

## Responsibilities
- **Business Logic**: Implements the core business rules and processes.
- **Interfaces**: Defines contracts and abstractions that are implemented by other layers (e.g., Infrastructure, Presentation).
- **Services**: Provides reusable services and utilities for the application.
- **Validation**: Includes validation logic for entities and business rules.

## Structure
The CORE layer is structured to promote separation of concerns and maintainability. Key components include:
- **Interfaces**: Define contracts for services, repositories, and other dependencies.
- **Services**: Contain reusable logic that supports the application.
- **Utilities**: Provide helper methods and tools for common operations.

## Key Principles
- **Independence**: The CORE layer does not depend on any external frameworks or libraries.
- **Reusability**: Logic in this layer can be reused across different parts of the application.
- **Testability**: The CORE layer is designed to be easily testable with minimal dependencies.

## Usage
This layer is consumed by other layers, such as Infrastructure and Presentation, to implement the application's functionality. It acts as the central point for defining the application's behavior and rules.

## Contribution
When contributing to this layer:
1. Ensure that all logic adheres to the principles of separation of concerns.
2. Avoid introducing dependencies on external frameworks or libraries.
3. Write unit tests for all new functionality to ensure reliability and maintainability.

---

**Cirreum Foundation Framework** - Layered simplicity for modern .NET