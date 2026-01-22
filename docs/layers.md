Layering Guide

Purpose
- Keep UI concerns in Presentation.
- Keep IO/technology concerns in Infrastructure.
- Keep business flow in Application.
- Keep business rules in Domain.
- Wire everything in CompositionRoot only.

Rules
- Presentation may depend on Application, Domain, Infrastructure.
- Application may depend on Domain only.
- Domain depends on nothing.
- Infrastructure depends on Application and Domain, but not Presentation.
- CompositionRoot is the only place that references Presentation + Application + Infrastructure together.

Placement
- Presentation: Views, ViewModels, Navigation, Popup, UI services, WPF types.
- Application: Use cases, interfaces, orchestration, business workflows.
- Domain: Entities, value objects, domain rules.
- Infrastructure: DB, API clients, device drivers, filesystem, logging, network.
- CompositionRoot: DI modules, host/bootstrap wiring.
