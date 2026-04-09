using Xunit;

// Disable parallel test execution — GameState is static and shared across all tests
[assembly: CollectionBehavior(DisableTestParallelization = true)]
