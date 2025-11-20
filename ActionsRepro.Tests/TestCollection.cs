using Meziantou.Xunit.v3;
using System;
using System.Collections.Generic;
using System.Text;

namespace ActionsRepro.Tests;

[CollectionDefinition(nameof(TestCollection))]
[EnableParallelization]
public class TestCollection : ICollectionFixture<TestFixture>
{
}
