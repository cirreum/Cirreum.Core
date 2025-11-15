# Cirreum Core

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.svg?style=flat-square)](https://www.nuget.org/packages/Cirreum/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.svg?style=flat-square)](https://www.nuget.org/packages/Cirreum/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum?style=flat-square)](https://github.com/cirreum/Cirreum/releases)

**Foundational primitives and abstractions for the Cirreum Framework**

## Overview
**Cirreum.Core** is the foundational library of the Cirreum ecosystem. It provides the core abstractions, primitives, and shared patterns used across all Cirreum libraries and runtime components.

This is *not* the application’s domain core. Instead, it acts as the **framework core**—the layer that defines the structural backbone of the entire stack.

All other Cirreum libraries (Messaging, Authorization, Runtime, Components, and more) build directly on this project.

## Purpose
Cirreum.Core exists to deliver a stable, consistent, and expressive foundation that:

- Defines contracts and interfaces shared across the framework  
- Supplies lightweight primitives for results, contexts, identifiers, and pipelines  
- Hosts cross-cutting patterns such as CQRS contracts and authorization resources  
- Provides utilities supporting consistent behavior across the ecosystem  

Its mission is to centralize building blocks that must be **universally accessible and long-lived** across all Cirreum packages.

## Responsibilities

### 1. Cross-Framework Abstractions
Base interfaces and extensibility points for:

- Messaging and dispatch behaviors  
- Authorization and evaluator pipelines  
- Environment and identity access  
- Plugin and integration boundaries  

### 2. Core Primitives
Foundational building blocks including:

- `Result` and `Result<T>`  
- Identifiers, markers, and context structures  
- Base implementations for validators, handlers, and authorizable resources  

### 3. Shared Patterns
Definitions that support Cirreum’s architectural patterns:

- CQRS-style request and response contracts  
- ABAC/RBAC authorization  
- Interceptors, pipelines, and execution flows  
- Metadata propagation and scoped request details  

### 4. Utilities & Helpers
Common functionality implemented using a curated set of stable dependencies:

- **SmartFormat** for formatting and templating  
- **FluentValidation** for rule-based validation  
- **Humanizer** for readable string and value transformations  
- **CsvHelper** for import/export workflows  

## Dependencies
Cirreum.Core is intentionally lightweight, but not dependency-free. It includes a **small, stable set of critical foundational libraries**:

- `Microsoft.Extensions.Telemetry.Abstractions`  
- `Microsoft.Extensions.Configuration.Json`  
- `Microsoft.Extensions.Configuration.Binder`  
- `FluentValidation`  
- `Humanizer.Core`  
- `SmartFormat`  
- `CsvHelper`  

These are *framework-level* dependencies chosen for stability, longevity, and ecosystem alignment.

## Design Principles

- **Lightweight, Not Minimalist**  
  Dependencies are curated—not avoided for their own sake.

- **Stable and Forward-Compatible**  
  The API surface here is foundational and should evolve slowly.

- **Extensible by Design**  
  Every contract is intended to be implemented differently across runtimes or applications.

- **Testability First**  
  All primitives and abstractions are unit-test-friendly.

## Structure

- **Abstractions** – Messaging, authorization, identity, environment, and integration contracts  
- **Primitives** – Results, identifiers, contexts, markers  
- **Patterns** – Validators, interceptors, CQRS contracts, authorizable resource models  
- **Utilities** – Formatting, CSV helpers, common services  

## Usage
Every other Cirreum library depends on **Cirreum.Core**.

This library:

- Defines the shared vocabulary of the framework  
- Establishes conventions for request handling, authorization, and behaviors  
- Acts as the foundational contract layer between domain-agnostic logic and implementation-specific libraries  

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*
