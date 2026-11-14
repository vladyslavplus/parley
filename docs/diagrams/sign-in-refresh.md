# Sign-in and session refresh through the BFF

```mermaid
sequenceDiagram
    autonumber
    actor Browser
    participant Web as Next.js BFF
    participant Proxy as proxy.ts
    participant API as Auth API
    participant DB as identity schema

    Note over Browser,DB: Sign-in
    Browser->>Web: POST /api/auth/login {email, password}
    Web->>Web: origin/CSRF guard, read body
    Web->>API: POST /auth/login
    API->>DB: load user, check lockout and password
    API->>DB: resolve active membership
    API->>DB: insert refresh token (new family, SHA-256 hash)
    API-->>Web: access token (15 min) + refresh token (30 days)
    Web-->>Browser: 204 plus two httpOnly cookies (SameSite=Lax, Secure in production)

    Note over Browser,DB: A guarded page request with an expired access token
    Browser->>Proxy: GET /en/app
    Proxy->>Proxy: no access cookie, read refresh cookie
    Proxy->>Web: single-flight refresh for this refresh value
    Web->>API: POST /auth/refresh {refreshToken}
    API->>DB: lookup by hash, insert successor, revoke presented row
    API-->>Web: new access and refresh pair
    Web-->>Proxy: tokens
    Proxy->>Proxy: set the new pair on the response and on this request
    Proxy-->>Browser: page renders with the new cookies

    Note over Browser,DB: Reuse of a rotated refresh token
    Browser->>Web: POST /auth/refresh with a token rotated earlier
    Web->>API: POST /auth/refresh
    API->>DB: rotated within the 30 s grace and family not terminated?
    alt inside the grace window
        API->>DB: insert another token in the same family
        API-->>Web: new pair
    else after the grace window or a terminal revocation
        API->>DB: revoke the whole family
        API-->>Web: 401 auth.refresh_token_reused or auth.refresh_token_invalid
        Web-->>Browser: clear cookies, redirect to /en/login
    end
```

Reading guide: the browser never holds a token. The login route handler forwards the credentials to the API and writes both tokens as httpOnly cookies; the API stores only a SHA-256 hash of the refresh value. On any guarded navigation `proxy.ts` checks the access cookie first and only refreshes when it is missing, using a single-flight wrapper so concurrent tabs or prefetches share one rotation call. The API rotates in one transaction: it inserts the successor and conditionally revokes the presented row, so two racing requests cannot both win; the loser takes the grace path. A token rotated less than 30 seconds ago returns a new pair in the same family (two tabs refreshing at once is normal), but any terminal revocation in the family (logout, password reset, family revocation) blocks that path. A rotated token presented after the grace window, or an already revoked token, revokes the entire family and answers 401 with a stable code; the BFF then clears the cookies and redirects to the localized login page. A network failure, but not a rejection, passes the request through so the page can render its own error state instead of destroying the session. Sign-out calls the API to revoke the family and clears both cookies.

Verified against: `frontend/src/proxy.ts`, `frontend/src/lib/session/refresh.ts`, `frontend/src/lib/session/tokens.ts`, `frontend/src/lib/bff/handlers.ts`, `frontend/src/app/api/auth/*/route.ts`, `backend/src/Modules/Identity/Identity/Authentication/Refresh/RefreshTokenService.cs`, `backend/src/Modules/Identity/Identity/Features/Sessions/**`, and the `Auth` section of `backend/src/Parley.Api/appsettings.json`.
