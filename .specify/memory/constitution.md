# ContosoDashboard Constitution

<!--
Sync Impact Report
- Version change: 0.0.0 -> 1.0.0
- Modified principles: n/a -> established five governing principles
- Added sections: Additional Constraints, Development Workflow
- Removed sections: none
- Follow-up TODOs: none
-->

## Core Principles

### I. Training-First Simplicity
ContosoDashboard exists to teach secure, maintainable application patterns in a constrained lab environment. Every feature MUST prioritize clarity, explicit service boundaries, and offline-first behavior over production-only complexity. The project MUST remain understandable to learners and MUST avoid unnecessary external dependencies or cloud-only assumptions.

### II. Security by Default
All authenticated and authorized user flows MUST enforce least privilege. The application MUST protect user data through server-side authorization checks, input validation, and safe storage practices. Default behavior MUST reject unauthorized access, unsafe file handling, and direct object access before any data is returned.

### III. Spec-Driven Delivery
Any feature work MUST be grounded in a reviewed specification, plan, and task breakdown before implementation. Changes to behavior MUST be traceable to the relevant requirement and MUST not bypass the repository’s Spec Kit workflow without explicit governance approval.

### IV. Testable, Reviewable Changes
Code changes MUST be small, readable, and verifiable. New behavior MUST be easy to validate through build checks, local execution, and review of the affected service, page, or data model. If a change cannot be explained by a requirement or test, it MUST be treated as a governance exception.

### V. Offline-Compatible Architecture
The application MUST remain usable without external cloud services, and infrastructure dependencies MUST be abstracted behind clear contracts. Local filesystem storage, SQLite, and mock authentication are acceptable for training, provided the architecture keeps infrastructure details isolated from business logic and remains easy to replace later.

## Additional Constraints
- The project MUST remain a .NET training application and MUST not introduce production-grade platform assumptions without explicit governance review.
- Data access MUST use EF Core models with clear ownership and relationship constraints.
- File storage MUST keep user-provided files outside the public web root and MUST generate safe unique file names before persisting metadata.
- Authentication and authorization MUST remain explicit and service-level checks MUST enforce access rules rather than trusting UI state alone.
- Database changes MUST be compatible with the current local/offline development model; SQLite is the default local datastore for this repository.

## Development Workflow
- Every feature MUST begin with a requirement or spec update unless the change is a direct governance clarification.
- Implementation MUST follow the repository’s service-oriented layering: models, data access, services, and UI pages remain distinct.
- Build validation MUST occur before completion claims, and warnings or errors that block execution MUST be resolved or explicitly documented.
- Documentation updates MUST be kept in sync with implementation changes when behavior, security, or data storage model changes.

## Governance
This Constitution governs the repository’s engineering and documentation practices. Amendments require a documented rationale, version update, and an explicit review of the affected principles and workflow. No change may bypass these requirements.

All contributions MUST verify compliance with this Constitution before merge or release. If a feature conflicts with a principle, the team MUST either redesign the implementation to comply or update the Constitution through a formal amendment with a version bump and explanation.

**Version**: 1.0.0 | **Ratified**: 2026-09-10 | **Last Amended**: 2026-09-10
