# 08-update-tests: update-tests

Update test files in `Neo4jClient.Tests` that reference Newtonsoft types directly. Most tests use Newtonsoft only because they configure `JsonConverters` on the client — these will follow the public API changes from task 06.

Files include all 10 test files identified in assessment with direct Newtonsoft references.

**Done when**: All tests compile; test suite passes (or known pre-existing failures are documented).
