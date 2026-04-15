CREATE TABLE IF NOT EXISTS runs (
  id TEXT PRIMARY KEY,
  job_id TEXT NOT NULL,
  prompt TEXT NOT NULL,
  runtime_profile TEXT NOT NULL,
  started_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS candidates (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL,
  content TEXT NOT NULL,
  state TEXT NOT NULL,
  score INTEGER NOT NULL,
  created_at_utc TEXT NOT NULL,
  FOREIGN KEY(run_id) REFERENCES runs(id)
);

CREATE TABLE IF NOT EXISTS provider_attempts (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL,
  provider_name TEXT NOT NULL,
  success INTEGER NOT NULL,
  message TEXT NOT NULL,
  attempted_at_utc TEXT NOT NULL,
  FOREIGN KEY(run_id) REFERENCES runs(id)
);

CREATE TABLE IF NOT EXISTS validation_results (
  id TEXT PRIMARY KEY,
  candidate_id TEXT NOT NULL,
  validator_name TEXT NOT NULL,
  passed INTEGER NOT NULL,
  evidence TEXT NOT NULL,
  validated_at_utc TEXT NOT NULL,
  FOREIGN KEY(candidate_id) REFERENCES candidates(id)
);

CREATE TABLE IF NOT EXISTS events (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL,
  type TEXT NOT NULL,
  message TEXT NOT NULL,
  created_at_utc TEXT NOT NULL,
  FOREIGN KEY(run_id) REFERENCES runs(id)
);

CREATE TABLE IF NOT EXISTS spend_logs (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL,
  model_name TEXT NOT NULL,
  provider_name TEXT NOT NULL,
  cost_usd REAL NOT NULL,
  prompt_tokens INTEGER NOT NULL,
  completion_tokens INTEGER NOT NULL,
  cache_hit INTEGER NOT NULL,
  created_at_utc TEXT NOT NULL,
  FOREIGN KEY(run_id) REFERENCES runs(id)
);
