namespace Project.Modules.Users.IntegrationTests.Infrastructure;

/// <summary>
/// Shares one set of containers (and one booted host) across every integration test
/// class, so the expensive Postgres/Redis start-up happens once per run.
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory>;
