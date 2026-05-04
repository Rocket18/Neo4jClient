# 01-update-package-reference: update-package-reference

Remove the `Newtonsoft.Json` package reference from `Neo4jClient.csproj` and add `System.Text.Json`. Since `netstandard2.0` is targeted, an explicit NuGet reference is needed. For `net6.0`, STJ is inbox but an explicit reference ensures version consistency.

**Done when**: `Newtonsoft.Json` package reference is removed from the project file; `System.Text.Json` package reference is present; project restores without errors.
