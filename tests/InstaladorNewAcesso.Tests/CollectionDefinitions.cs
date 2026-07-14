/// <summary>
/// Collection definition para testes de integração que usam
/// estado estático (SummaryStore, AuditLogger).
/// Impede execução paralela com outros testes nesta collection.
/// </summary>
[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestsCollectionDefinition { }
