// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using Microsoft.NetCore.Analyzers.Usage;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Usage.JsonConstructorParametersShouldMatchPropertyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Microsoft.NetCore.Analyzers.Usage.JsonConstructorParametersShouldMatchPropertyAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.NetAnalyzers.UnitTests.Microsoft.NetCore.Analyzers.Usage
{
    public class JsonConstructorParametersShouldMatchPropertyAnalyzerTest
    {
        private static async Task VerifyCSharpNewtonsoftAsync(string source, params Testing.DiagnosticResult[] expected)
        {
            var csharpTest = new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences
                    .DefaultWithNewtonsoftJson12,
                TestState =
                {
                Sources = { source },
                }
            };

            csharpTest.ExpectedDiagnostics.AddRange(expected);

            await csharpTest.RunAsync();
        }

        [Fact]
        public async Task CA1072_ConstructorsMarkedWithAttributeAndNoMatchingParameterShouldWarn()
        {
            await VerifyCSharpNewtonsoftAsync(@"
using System;
using Newtonsoft.Json;

public class C
{
    public string Test {get;set;}
    [JsonConstructor]
    public C(string {|#0:notCorrect|})
    {
    }
}
public class D
{
    public string Test {get;set;}
    public D(string test)
    {
    }
}
public class E
{
    [JsonConstructor]
    public E()
    {
    }
}
public class F
{
    [JsonProperty(""NotTest"")]
    public string Test {get;set;}
    [JsonConstructor]
    public F(string notTest)
    {
    }
}",
                    VerifyCS.Diagnostic(JsonConstructorParametersShouldMatchPropertyAnalyzer.DefaultRule).WithLocation(0).WithArguments("notCorrect"));
        }

        [Fact]
        public async Task CA1072_NewtonSoftMissingDisableAnalyzer()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
public class JsonConstructorAttribute : Attribute {}
public class C
{
    public string Test {get;set;}
    [JsonConstructor]
    public C(string test)
    {
    }
}
");
        }
    }
}