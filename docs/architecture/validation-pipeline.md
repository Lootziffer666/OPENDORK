# Validation Pipeline

Validation is plugin-oriented:

- `IValidator` defines the extension seam.
- `ValidationPipeline` composes validators and collects `ValidationResult` records.
- `ValidationProfileResolver` maps runtime profiles to validator lists.

Current validators are `LengthValidator` and `StatusTagValidator`; additional validators can be added without changing orchestrator logic.
