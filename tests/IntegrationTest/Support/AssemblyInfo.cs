using Xunit;

// Las pruebas de logging redirigen Console.Out (global al proceso); sin paralelismo no se mezclan salidas.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
