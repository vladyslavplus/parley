# Campaign and send record states

```mermaid
stateDiagram-v2
    [*] --> draft: create
    draft --> preparing: prepare, as-of pinned
    preparing --> ready: audience frozen at the commit point
    preparing --> failed: freeze error
    failed --> preparing: prepare again
    ready --> sending: start, conditional ready to sending
    sending --> completed: no targeted treatment recipient outstanding
    sending --> sending: worker crash, recovery sweep requeues
    draft --> cancelled: defined state, no transition in the API
    preparing --> cancelled: defined state, no transition in the API
    ready --> cancelled: defined state, no transition in the API
    completed --> [*]
    cancelled --> [*]
    failed --> [*]: abandoned

    note right of preparing
        A second freeze run takes the advisory
        lock or exits. Rows of an unfinished run
        are deleted before they are written again.
    end note
    note right of completed
        Completion also schedules the final
        attribution run at the end of the window.
    end note
```

```mermaid
stateDiagram-v2
    [*] --> claimed: claim inserts the row, unique per campaign and contact
    claimed --> skipped_suppressed: address or contact suppressed at claim
    claimed --> sent: mail server accepted
    claimed --> retryable_failed: connection failure before acceptance
    claimed --> unknown: outcome unknown or lease expired after submission
    retryable_failed --> claimed: next claim, attempt plus one
    claimed --> retryable_failed: lease expired before submission, attempts remain
    claimed --> failed: lease expired before submission, last attempt
    sent --> [*]
    skipped_suppressed --> [*]
    failed --> [*]
    unknown --> [*]

    note right of claimed
        submitted_at is stamped right before the
        mail is handed to the server. A claim that
        expires without it never reached the server.
        A stamped one becomes unknown and is never
        sent again.
    end note
    note right of skipped_suppressed
        A suppression is checked after the freeze,
        so an unsubscribe between freeze and send
        is still honoured.
    end note
```

Reading guide: the campaign states describe the authoring and delivery lifecycle, and the guards are the status column itself: `prepare` accepts `draft`, `preparing` and `failed`; `start` only accepts `ready` and performs a conditional `ready -> sending` update; completion only fires from `sending`. `preparing` is resumable: the freeze deletes the rows of an unfinished run and rewrites them, with `audience_frozen_at` and `ready` set in the same statement. `failed` is a retryable freeze failure, and a campaign that is never prepared again stays failed. The `cancelled` value exists in the stored vocabulary and in the UI styling, but no handler in the current API sets it, so it is drawn as a defined terminal state without a transition rather than pretending an endpoint exists. Send records are a separate machine per recipient: the claim is inserted before the mail leaves, the lease is renewed and `submitted_at` stamped immediately before submission, and only `retryable_failed` can be claimed again. The distinction between a claim that expired before submission (safe to retry) and one that expired after submission (unknown, never resent) is what makes a crash mid-batch safe. `sent`, `skipped_suppressed`, `failed` and `unknown` are terminal. Two arrows leave `claimed` for `failed` because two different rules can produce it: the claim-time failures (no address, render failure) and the last-attempt lease expiry.

Verified against: `backend/src/Modules/Campaigns/Campaigns/Domain/Campaigns/CampaignStatuses.cs`, `Features/Campaigns/Prepare/PrepareCampaignCommandHandler.cs`, `Features/Campaigns/Prepare/CampaignAudienceFreezer.cs`, `Features/Campaigns/Start/StartCampaignCommandHandler.cs`, `Features/Campaigns/Dispatch/Outcomes/CampaignSendCompletion.cs`, `Domain/Sends/SendStatuses.cs`, `Domain/Sends/SendPolicy.cs` and `Features/Campaigns/Dispatch/Claims/CampaignSendClaimer.cs`.
