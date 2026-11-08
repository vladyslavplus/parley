namespace Parley.IntegrationTests.Infrastructure.Fixtures;

/// <summary>
/// Container image for test databases; keep in sync with docker-compose.yml.
/// </summary>
public static class TestDatabaseImages
{
    public const string Postgres = "pgvector/pgvector:0.8.7-pg17@sha256:7d0a41464797aeb9862dfdc0bbf1526b82cfd324cdd8036df19e3545f4a9ccb8";
}
