// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Performance.PreferContainsKeyOrValueOverPropertyAccessAnalyzer,
    Microsoft.NetCore.Analyzers.Performance.PreferContainsKeyOrValueOverPropertyAccessFixer>;

namespace Microsoft.CodeAnalysis.NetAnalyzers.UnitTests.Microsoft.NetCore.Analyzers.Performance
{
    public sealed class PreferContainsKeyOrValueOverPropertyAccessAnalyzerTest
    {
        public static TheoryData<string> DictionaryTypeData { get; } = new TheoryData<string>
        {
            "Dictionary<int,int>",
            "IDictionary<int,int>",
            "IReadOnlyDictionary<int,int>"
        };

        [Theory]
        [MemberData(nameof(DictionaryTypeData))]
        public async Task 
    }
}
