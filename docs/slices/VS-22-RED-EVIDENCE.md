# VS-22 RED checkpoint

The first TDD test commit adds calls to `PublicBusinessUrlPolicy.TryCanonicalize` and `PublicBusinessUrlPolicy.CanonicalizeMany`, which are intentionally not implemented yet. Exact-head CI must fail at compile/test for those missing production interfaces before GREEN implementation is added.
