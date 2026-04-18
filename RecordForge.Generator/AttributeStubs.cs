// Internal stubs mirroring the public attribute types in Subro.RecordForge.
// These exist only so the generator can compile its `nameof(...)` references and
// access const defaults without taking a compile-time dependency on the runtime
// library project (which would introduce a pack-time circular dependency).
//
// Kept deliberately in a *different* namespace (Subro.RecordForge.Generator.Internal)
// so even projects that have been granted InternalsVisibleTo to the generator DLL
// (see the [assembly: InternalsVisibleTo(...)] declarations in RecordForger.cs)
// cannot see them as duplicates of the real public types.
//
// Keep these names / property names / enum values in sync with the real public
// types in ..\RecordForge\GenerateRecordAttribute.cs.

namespace Subro.RecordForge.Generator.Internal;

using System;

internal enum RecordKind
{
    Record = 0,
    RecordStruct = 1,
    ReadOnlyRecordStruct = 2,
    Struct = 3,
    ReadonlyStruct = 4,
    Class = 5,
}

[Flags]
internal enum ConstructorUsage
{
    Automatic = 0,
    Empty = 1,
    ReadonlyProperties = 2,
    ReadonlyAndInitProperties = 4,
    AllProperties = 8,
}

internal abstract class GenerateRecordAttributeBase : Attribute
{
    public const RecordKind DefaultKind = RecordKind.Record;
    public const bool DefaultAsPartial = true;

    public string? RecordName { get; init; }
    public string? NameSpace { get; init; }
    public bool AsPartial { get; init; } = DefaultAsPartial;
    public bool AsAbstract { get; init; } = false;
    public bool AlwaysCreateSetters { get; init; } = false;
    public ConstructorUsage ConstructorUsage { get; set; }
}

internal sealed class GenerateRecordAttribute : GenerateRecordAttributeBase { }

internal sealed class GenerateRecordFromInterfaceAttribute : GenerateRecordAttributeBase { }
