// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.NetCore.Analyzers.Performance;
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

        private static DiagnosticResult CSharpKeyResult(int markupKey, params string[] arguments)
           => VerifyCS.Diagnostic(PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsKeyRule)
               .WithLocation(markupKey)
               .WithArguments(arguments);

        private static DiagnosticResult CSharpValueResult(int markupKey, params string[] arguments)
          => VerifyCS.Diagnostic(PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsValueRule)
              .WithLocation(markupKey)
              .WithArguments(arguments);

        [Theory]
        [MemberData(nameof(DictionaryTypeData))]
        public async Task CA1839_ContainsKey(string data)
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public " + data + @" dictionary {get;set;} = new Dictionary<int,int>();
    public bool Test() => {|#0:dictionary.Keys.Contains(1)|};
}", CSharpKeyResult(0), @"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public " + data + @" dictionary {get;set;} = new Dictionary<int,int>();
    public bool Test() => dictionary.ContainsKey(1);
}"
);
        }

        [Fact]
        public async Task CA1840_ContainsValue()
        {
            await VerifyCS.VerifyCodeFixAsync(@"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public Dictionary<int,int> dictionary {get;set;} = new Dictionary<int,int>();
    public bool Test() => {|#0:dictionary.Values.Contains(1)|};
}", CSharpValueResult(0), @"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public Dictionary<int,int> dictionary {get;set;} = new Dictionary<int,int>();
    public bool Test() => dictionary.ContainsValue(1);
}");
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public IDictionary<int,int> dictionary {get;set;} = new Dictionary<int,int>();
    public bool Test() => {|#0:dictionary.Values.Contains(1)|};
}");
        }

        [Fact]
        public async Task CA1840_ContainsOnListNoResult()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public List<int> list {get;set;} = new List<int>();
    public bool Test() => list.Contains(1);
}");
            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public List<int> list {get;set;} = new List<int>();
    public bool Test() => new C().list.Contains(1);
}");

            await VerifyCS.VerifyAnalyzerAsync(@"
using System.Collections.Generic;
using System.Linq;

public class C
{
    public bool Test() => new List<int>().Contains(1);
}");
        }
    }
}
