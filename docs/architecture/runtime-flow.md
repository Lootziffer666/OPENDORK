# Runtime Flow

1. `opendork-cli run` oder `opendork-cli chat` startet einen Request.
2. `LiteLlmStyleGateway` löst Modell + Provider auf, prüft Cache und Budget.
3. Bei Misses ruft `ProviderRouter` Provider mit Retry/Failover/Cooldown an.
4. Usage + Spend werden in SQLite (`spend_logs`) gespeichert.
5. `RunOrchestrator` persistiert Kandidaten und führt `ValidationPipeline` aus.
6. Ergebnis wird nach `results/raw|validated|rejected|gold` exportiert.
7. Status/Report verwenden SQLite + Artifacts.
