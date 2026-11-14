# Scoring run, failure path and fallback

```mermaid
sequenceDiagram
    autonumber
    actor Admin
    participant API as Insights API
    participant Job as Hangfire job
    participant DB as insights schema
    participant CRM as Crm.Contracts
    participant ML as Scoring service (FastAPI)

    Note over Admin,ML: Model sync (separate, on demand)
    Admin->>API: POST /insights/models/sync
    API->>ML: GET /models with X-Parley-Ml-Key
    ML-->>API: versions, provenance, schema version, metrics, availability
    API->>DB: upsert model_versions

    Note over Admin,ML: A scoring run
    Admin->>API: POST /insights/scoring-runs {kind}
    API->>DB: insert scoring_runs (queued), max one active run per kind
    API->>Job: enqueue RunScoringJob after commit
    Job->>DB: run still queued or running, ML enabled
    Job->>DB: pin the active model version from model_versions
    Job->>DB: status = running, remove rows of an earlier attempt
    loop batches of 500 contacts
        Job->>CRM: read customer metrics and features as of one instant
        Job->>ML: POST /score/uplift or /score/clv (at most 1000 items)
        ML-->>Job: scores plus per-item rejections and the model version
        Job->>Job: reject a response that does not echo the pinned version
        Job->>DB: write scores of this run
    end
    alt rejected share above the limit or a call failed
        Job->>DB: status = failed, stable error code, delete this run's scores
        Note over DB: latest_scores is untouched, so the product keeps the last successful scores
    else run complete
        Job->>DB: one transaction: status = completed, replace latest_scores of this kind with the run's rows, outbox ScoringRunCompleted
    end

    Note over Admin,ML: Fallback when ML is off or the service is down
    Admin->>API: PUT /insights/ml-settings {mlEnabled: false}
    API->>DB: ml_settings, outbox MlSettingsChanged
    Job->>DB: rebuild the priority list in RFM mode
    Note over API,ML: With ML enabled but the service down, the run fails with insights.scoring.service_unavailable. The priority list, campaigns and every CRM screen keep working
```

Reading guide: a scoring run is queued from the API and executed by a background job; no user request waits on the scoring service. The job pins one model version from the synced registry and refuses a response that reports another version, so a run cannot mix models. It reads CRM facts in batches of 500 as of one instant, sends at most 1000 items per call, and writes each batch of scores under the run id. The commit point is a single transaction that completes the run, replaces `latest_scores` for that kind with the run's rows, and writes `ScoringRunCompleted` to the outbox; if anything fails before that point, the run ends failed with a stable code and its own rows are deleted, while `latest_scores`, and therefore every screen, stays on the last successful scores. Rejections are counted and a run whose rejected share exceeds 5 % is failed rather than published. Turning ML off is a separate switch: the settings change is committed with an outbox event, the priority list is rebuilt in RFM mode, and no scoring call is made. With ML on but the scoring service unreachable, the run fails with `insights.scoring.service_unavailable` and the product behaves exactly like the last successful run, which is the state captured in the ML-down evidence. The model registry page shows each version's provenance; the demo `crm_uplift` model is labelled semi-synthetic.

Verified against: `backend/src/Modules/Insights/Insights/Features/Scoring/Run/ScoringRunRunner.cs`, `Features/Scoring/Client/ScoringServiceClient.cs`, `Features/Scoring/Models/ScoringModelsSync.cs`, `Options/InsightsScoringOptions.cs`, `ml/parley_ml/serving/service.py`, `ml/parley_ml/serving/scoring.py`, and `backend/src/Modules/Insights/Insights/Features/Priority/Build/PriorityBuildRunner.cs`.


