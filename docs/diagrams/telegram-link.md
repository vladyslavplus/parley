# Telegram link and notification flow

```mermaid
sequenceDiagram
    autonumber
    actor Member
    participant Web as Web API
    participant TgUser as Member in Telegram
    participant Tg as Telegram Bot API
    participant Dispatcher as Update dispatcher
    participant DB as notifications schema

    Note over Member,DB: Create a one-time link code
    Member->>Web: POST /telegram/link-code
    Web->>DB: token configured? active link for this user? then conflict
    Web->>Web: rate limit 5 codes per 10 minutes
    Web->>DB: insert SHA-256 hash of a 32-character code, expiry 10 minutes
    Web-->>Member: code and a t.me deep link with the code as the start payload

    Note over Member,DB: Redeem the code in the chat
    Member->>TgUser: open the deep link
    TgUser->>Tg: /start with the code argument
    Tg->>Dispatcher: update by long polling, or webhook with the secret header
    Dispatcher->>DB: insert update_id (a replay is a no-op), then handle in the same transaction
    Dispatcher->>DB: hash the code, look it up across tenants, check unused and unexpired
    Dispatcher->>DB: membership active, chat not linked, user not linked
    Dispatcher->>DB: spend the code with a conditional update, insert the active link, outbox TelegramLinked
    Dispatcher->>Tg: reply after the commit, through the per-chat pacer

    Note over TgUser,DB: Commands after linking
    TgUser->>Tg: /today, or /today with an organization number
    Dispatcher->>DB: links for the chat, membership, insights.view, contacts.read scope
    Dispatcher->>DB: read the top priority rows for that scope, drop do-not-disturb rows
    Dispatcher->>Tg: action text plus a link to each contact record, no contact names
    TgUser->>Tg: /unlink
    Dispatcher->>DB: end every active link of the chat, cancel its pending deliveries
```

```mermaid
sequenceDiagram
    autonumber
    participant Event as Campaigns, Crm or Insights outbox
    participant Consumer as Notifications consumer
    participant Writer as InAppNotificationWriter
    participant DB as notifications schema
    participant Worker as Telegram delivery worker
    participant Tg as Telegram Bot API

    Note over Event,DB: A notification is written with its delivery rows in one transaction
    Event->>Consumer: deliver the integration event
    Consumer->>Writer: add notification for the recipient
    Writer->>DB: read the user's channel switches for the type
    Writer->>DB: insert the notification when in-app or Telegram is on
    Writer->>DB: for Telegram, stage one pending delivery per active chat
    Note over Consumer,DB: The inbox row, the notification and the deliveries commit together

    Note over Worker,Tg: The worker sends outside any transaction
    loop every few seconds, batches of at most 50
        Worker->>DB: claim due pending rows with a 2-minute lease and SKIP LOCKED
        Worker->>DB: link, notification and active membership still valid? otherwise cancel
        Worker->>DB: attempts = attempts + 1 before the send
        Worker->>Tg: send the localized text, paced per chat
        Worker->>DB: record sent, pending with backoff or retry-after, failed, or blocked
    end
    Note over Worker,DB: A blocked chat loses every link. A failed delivery retries up to five attempts. Delivery is at-least-once, so a crash after Telegram accepted the message but before the record can resend it once.
```

Reading guide: the channel is off until a bot token is configured; without one the API answers `telegram.not_configured` and the rest of the product runs normally. Linking is a one-time code: the API stores only its SHA-256 hash, the code expires after ten minutes and is limited to five per ten minutes per user, and a user who already has an active link or a chat that is already linked is refused. The same update entry serves polling and webhook mode; the webhook checks the secret header in constant time before reading the body, and polling moves its offset only after an update is handled. The update id is inserted in the same transaction as the command, so a replayed Telegram update is a no-op. `/today` is gated by `insights.view` and follows the `contacts.read` scope of the member, exactly like the web list; it lists next actions and links, never contact names, and never suggests a contact with a do-not-disturb caution. `/unlink` and a chat that blocked the bot both end every link of the chat and cancel its pending deliveries. Notifications are fanned out at write time: a consumer writes the in-app row and, when the Telegram switch for that type is on, one pending delivery per active chat, all inside the inbox transaction. The delivery worker claims due rows with a lease and `SKIP LOCKED`, re-checks the link and the membership, counts the attempt before sending, and records the outcome; a retryable failure is rescheduled with exponential backoff or Telegram's `retry_after`, while a blocked chat ends the link. Delivery is deliberately at-least-once: a crash after Telegram accepted a message but before the outcome was saved can resend it, because the unique row prevents duplicate deliveries, not duplicate sends.

Verified against: `backend/src/Modules/Notifications/Notifications/Telegram/**` (`LinkRules`, `LinkRedeemer`, `LinkEnder`, `ChatUnlinker`, `TodayComposer`, `DeliveryPlanner`, `DeliveryClaimer`, `DeliveryAttempt`, `DeliveryRules`, `DeliveryWorker`, `PollingWorker`, `UpdateDispatcher`, `WebhookSecret`), `Features/Telegram/LinkCode/**`, `Features/Telegram/Updates/**`, `Inbox/InAppNotificationWriter.cs`, `Notifications/Module.cs` (options and registrations), `backend/src/Parley.Api/Controllers/Telegram/**`, and the `Telegram` section of `backend/src/Parley.Api/appsettings.json`.

