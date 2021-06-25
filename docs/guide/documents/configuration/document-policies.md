# Document Policies

Document Policies enable convention-based customizations to be applied across the Document Store. While Marten has some existing policies that can be enabled, any custom policy can be introduced  through implementing the `IDocumentPolicy` interface and applying it on `StoreOptions.Policies` or through using the `Policies.ForAllDocuments(Action<DocumentMapping> configure)` shorthand.

The following sample demonstrates a policy that sets types implementing `IRequireMultiTenancy` marker-interface to be multi-tenanted (see [tenancy](/guide/documents/tenancy/)).

<!-- snippet: sample_sample-policy-configure -->
<!-- endSnippet -->

<!-- snippet: sample_sample-policy-implementation -->
<!-- endSnippet -->

To set all types to be multi-tenanted, the pre-baked `Policies.AllDocumentsAreMultiTenanted` could also have been used.

Remarks: Given the sample, you might not want to let tenancy concerns propagate to your types in a real data model.