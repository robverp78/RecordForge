using Subro.RecordForge;
using Subro.Generators;

using Xunit;
using static Subro.Generators.RecordForger;

namespace Subro.Generators.Tests
{
    public class TestAutoImplementedRecords
    {
        TestCompiler GetTestCompilation()
        {
            var res = new TestCompiler();
            res.IncrementalGenerators.Add(new RecordForger());
            res.AdditionalReferences.Add(
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(
                    typeof(GenerateRecordAttribute).Assembly.Location));
            return res;
        }


        const string testInterface =
        "interface IBar { int AnInt { get; } string AString{ get; set; } DateTime? SomeDate{get;init;} }";

        static string interfaceInNamespace(string Interface)
            => $"using Subro.RecordForge;using System; namespace Foo{{ {Interface} }}";
        
        [Fact]
        public void TestRecordsAreCreatedForAllRecordKinds()
        {
            foreach (var kind in Enum.GetValues<RecordKind>())
            {
                TestRecordKind(kind);
            }
        }

        internal void TestRecordKind(RecordKind kind)
        {
          var comp = GetTestCompilation().Create(interfaceInNamespace($"[GenerateRecord({nameof(RecordKind)}.{kind})]{testInterface}" )); 
                Assert.NotNull(comp.GetSymbol("Foo.Bar"));
        }

        [Fact]
        public void TestCreatedRecordIsUsable()
        {
            var comp = GetTestCompilation().Create(interfaceInNamespace($"[GenerateRecord]{testInterface} class AClass{{public AClass(){{var rec = new Bar(1);}}}}"));
        }

        [Fact]
        public void TestUseOtherNamespaceAndName()
        {
            var comp = GetTestCompilation().Create(interfaceInNamespace($"[GenerateRecord(NameSpace=\"Test\", RecordName=\"Bar2\")]{testInterface}"));
            Assert.NotNull(comp.GetSymbol("Test.Bar2"));
        }
 
        [Fact]
        public void TestAssemblyLevelDeclaration()
        {
            var code = @$"
using Subro.RecordForge;
using System; 

[assembly:GenerateRecordFromInterface(typeof(Foo.IBar))]
[assembly:GenerateRecordFromInterface(typeof(Foo.IBar),RecordName = ""Bar2"")]
namespace Foo{{ {testInterface} }}";
            var comp = GetTestCompilation().Create(code );
            Assert.NotNull(comp.GetSymbol("Foo.Bar"));
            Assert.NotNull(comp.GetSymbol("Foo.Bar2"));
        }

        [Fact]
        public void TestAssemblyLevelDeclarationCrossAssembly()
        {
            // Compile the interface in a separate assembly
            var interfaceAssembly = new TestCompiler { AssemblyName = "InterfaceLib" }
                .Create($"using System; namespace Foo{{ public {testInterface} }}");

            // Compile the assembly-level attribute in a different assembly that references the interface assembly
            var compiler = GetTestCompilation();
            compiler.AdditionalReferences.Add(interfaceAssembly.ToMetadataReference());
            var comp = compiler.Create(@"
using Subro.RecordForge;
[assembly:GenerateRecordFromInterface(typeof(Foo.IBar))]
");
            Assert.NotNull(comp.GetSymbol("Foo.Bar"));
        }

    }

}



