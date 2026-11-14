# Campaign freeze, holdout, send claim and attribution

```mermaid
sequenceDiagram
    autonumber
    actor Marketer
    participant API as Campaigns API
    participant Job as Hangfire job
    participant CRM as Crm.Contracts
    participant Insights as Insights.Contracts
    participant DB as campaigns schema
    participant SMTP as SMTP / Mailpit

    Note over Marketer,DB: Prepare and freeze the audience
    Marketer->>API: POST /campaigns/{id}/prepare
    API->>API: guard status (draft, preparing, failed), targeting permission, template, segment, scores
    API->>DB: status = preparing, prepare_as_of = now
    API->>Job: enqueue PrepareCampaignJob after commit
    Job->>DB: take the per-campaign advisory lock
    Job->>DB: delete recipients of any earlier unfinished run
    Job->>CRM: read eligible contacts in batches, minus suppressed
    Job->>Insights: read the latest complete RFM day and, for score targeting, one completed scoring run
    Job->>Job: bucket = SHA-256(holdout_seed:contact) mod 10000, holdout if bucket < holdout_share_bps
    Job->>Job: rank both arms, mark is_targeted, snapshot the address and locale
    Job->>DB: insert recipients, then set audience_frozen_at and status = ready (the commit point)

    Note over Marketer,SMTP: Start and send under a claim
    Marketer->>API: POST /campaigns/{id}/start
    API->>DB: status ready -> sending (conditional update)
    API->>Job: schedule CampaignSendJob
    loop until nothing is claimable
        Job->>DB: claim a batch with FOR UPDATE SKIP LOCKED, then insert send_records with a unique key on campaign_id and contact_id
        Job->>DB: suppressed address or contact -> skipped_suppressed
        Job->>CRM: read recipient profile (name, company)
        Job->>Job: render Fluid subject and body, build the unsubscribe links
        Job->>DB: renew the lease and stamp submitted_at
        Job->>SMTP: submit the message
        Job->>DB: record sent, retryable_failed, failed or unknown in its own transaction
    end
    Job->>DB: no unclaimed targeted treatment recipient and no claim in flight
    Job->>DB: status sending -> completed, outbox CampaignCompleted

    Note over Job,DB: Attribution
    Job->>CRM: read orders in each targeted recipient's window
    Job->>DB: write recipient_outcomes per recipient
    Job->>DB: compute rates, lift, interval, p-value, SRM, MDE, incremental revenue and store campaign_results
```

Reading guide: the freeze is the only step that writes the audience, and it is idempotent: a resumed run deletes its own unfinished rows and writes them again, and the `audience_frozen_at` update together with `status = ready` is the single commit point that makes the audience visible to sending and review. The holdout arm is a deterministic function of the campaign seed (the campaign id) and the contact id, so the same campaign always assigns the same people; the same vectors are asserted in C# and Python. Targeting ranks contacts across both arms and only targeted treatment recipients are sent to, which keeps the comparison between the two arms on the same selected population. The send loop claims a row in `send_records` before any mail leaves, under a lease, and stamps `submitted_at` immediately before submission. A claim whose lease expires without that stamp was never submitted and returns to the pool; a claim stamped as submitted but not recorded becomes `unknown` and is never sent again, so a crash cannot produce a second message. Each outcome is a short transaction and the accepted case writes `CampaignEmailSent` to the outbox in the same commit, which is how the CRM timeline and the suppression state learn about the send. Completion is conditional on `sending`, so two workers finishing together complete the campaign once. Attribution then reads orders from the CRM outcome reader and stores one result row per campaign; the sample ratio check uses assigned counts, the lift comparison uses targeted counts, and a result that includes a simulated order is labelled simulated everywhere.

Verified against: `backend/src/Modules/Campaigns/Campaigns/Features/Campaigns/Prepare/**`, `Domain/Holdout/HoldoutAssigner.cs`, `Features/Campaigns/Start/**`, `Features/Campaigns/Dispatch/**`, `Features/Campaigns/Attribution/**`, and the seeded historic campaigns in `Features/Campaigns/DemoSeed/**`.


