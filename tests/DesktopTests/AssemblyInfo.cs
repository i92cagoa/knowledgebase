using Xunit;

// Avalonia headless sessions are not safe to run concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]