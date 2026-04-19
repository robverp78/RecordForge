using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Subro.RecordForge;
using System;
using System.Collections.Generic;
using System.Text;

namespace Subro.Generators
{

    partial class RecordForger
    {


        /// <summary>
        /// All available options of <see cref="RecordKind"/>, expressed through <see cref="GenerateTypeInfo"/>. 
        /// This is used to convert from the enum value to the needed information for generation
        /// </summary>
        internal static GenerateTypeInfo[] generateTypeInfos =
           [
                new( (int)RecordKind.Record, "record", true),
                new( (int)RecordKind.RecordStruct, "record struct", true),
                new((int)RecordKind.ReadOnlyRecordStruct, "record struct", true, true ),
                new( (int)RecordKind.Struct, "struct", false),
                new((int)RecordKind.ReadonlyStruct, "struct", false,true),
                new((int)RecordKind.Class, "class", false),
           ];


        internal static GenerateTypeInfo? GetGenerateTypeInfo(AttributeData attributeData, int Index)
        {
            var kind  = attributeData.ConstructorArguments.Length > Index
                    && attributeData.ConstructorArguments[Index].Value is int kindValue
                        ? kindValue
                        : (int)GenerateRecordAttributeBase.DefaultKind;
            return GetGenerateTypeInfo(kind);
        }

        internal static GenerateTypeInfo? GetGenerateTypeInfo(int kind)
            => generateTypeInfos.FirstOrDefault(info => info.KindValue == kind);
        internal static DiagnosticInfoProvider CreateInvalidKindDiagnostic(AttributeData attributeData)
            => Diagnostics.Error("AIRINVKIND", "Invalid value for " + nameof(RecordKind))
                    .CreateDiagnosticInfo(LocationInfo.From(attributeData));
    }

    /// <summary>
    /// Information on what a <see cref="RecordKind"/> entails, and how to convert it
    /// </summary>
    internal class GenerateTypeInfo(int KindValue, string Keywords, bool OnlyNeedsConstructor, bool IsReadOnly = false)
    {
        public readonly int KindValue = KindValue;
        public readonly string Keywords = Keywords;
        public readonly bool OnlyNeedsConstructor = OnlyNeedsConstructor;
        public readonly bool IsReadOnly = IsReadOnly;
    }

    record CreationSettings
    {
        public bool AsPartial { get; set; } = GenerateRecordAttribute.DefaultAsPartial;

        public bool AsAbstract { get; set; } = false;
        public bool AlwaysCreateSetters { get; set; }

        public CtorUsage ConstructorUsage { get; set; }
    }

    [Flags]
    enum CtorUsage
    {
        Automatic = 0,
        Empty = ConstructorUsage.Empty,
        ReadonlyProperties = ConstructorUsage.ReadonlyProperties,
        ReadonlyAndInitProperties = ConstructorUsage.ReadonlyAndInitProperties,
        AllProperties = ConstructorUsage.AllProperties,
    }

    record CreationInfo(
        GenerateTypeInfo TypeInfo,
        string InterfaceName,
        string InterfaceNameSpace,
        string RecordName,
        string RecordNameSpace,
        EquatableArray<PropertyCreationInfo> Properties,
        CreationSettings Settings)
    {
        /*
         * In loving memory of Smokey. 
         * Smokes, you were such an awesome kitty cat. You were my buddy.
         * I miss your presence and I miss you jumping on my desk while I was coding. 
         * I miss it even more how you crawled on my chest to get your cuddles.
         * When I was working on the last bits of this generator, you were too sick to still do that,
         * but we never imagined those would be the last days we would have you in our lives.
         */


        public IEnumerable<CtorUsage> GetConstructorTypes()
        {
            var ctors = Settings.ConstructorUsage;
            if (ctors == CtorUsage.Automatic)
            {
                //Default value is automatic, behavior should correspond with  ConstructorUsage.Automatic description
                if (TypeInfo.IsReadOnly)
                    yield return CtorUsage.AllProperties;
                else if (TypeInfo.OnlyNeedsConstructor)
                    yield return CtorUsage.ReadonlyProperties;
                else
                    yield return CtorUsage.Empty;
            }
            else if ((ctors & (ctors - 1)) == 0) // single flag, common case
                yield return ctors;
            else
            {
                //multiple flags — check each one
                if (ctors.HasFlag(CtorUsage.Empty)) yield return CtorUsage.Empty;
                if (ctors.HasFlag(CtorUsage.ReadonlyProperties)) yield return CtorUsage.ReadonlyProperties;
                if (ctors.HasFlag(CtorUsage.ReadonlyAndInitProperties)) yield return CtorUsage.ReadonlyAndInitProperties; 
                if (ctors.HasFlag(CtorUsage.AllProperties)) yield return CtorUsage.AllProperties;
            }
        }

        public IEnumerable<IReadOnlyList<PropertyCreationInfo>> GetConstructors()
        {
            foreach (var ctor in GetConstructorTypes())
                yield return ctor switch
                {
                    CtorUsage.Empty => [],
                    CtorUsage.ReadonlyProperties => [.. Properties.Where(static p => !p.HasSetter)],
                    CtorUsage.ReadonlyAndInitProperties => [.. Properties.Where(static p => p.HasInit || !p.HasSetter)],
                    CtorUsage.AllProperties => Properties,
                    _ => throw new InvalidOperationException("Invalid constructor usage")
                };
        }

        public bool IsReadOnly => TypeInfo.IsReadOnly;
        public bool IsPartial => Settings.AsPartial;
    }

    internal class PropertyReference
    {
        public required PropertyCreationInfo Property{ get; init; }
        public bool IsInPrimaryCtor { get; set; }

        public string Name => Property.Name;
    }

    internal record PropertyCreationInfo : PropertyInfo
    {
        public PropertyCreationInfo(IPropertySymbol prop) : base(prop)
        {
            HasInit = prop.SetMethod?.IsInitOnly == true;
        }

        /// <summary>
        /// Syntax-only constructor: extracts property info from syntax without needing a semantic model.
        /// Type names are taken as-written (not fully qualified).
        /// </summary>
        public PropertyCreationInfo(PropertyDeclarationSyntax syntax)
            : base(
                syntax.Identifier.Text,
                new TypeReference(syntax.Type.ToString(), syntax.Type.ToString()),
                syntax.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true
                    || syntax.ExpressionBody != null,
                syntax.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) == true)
        {
            HasInit = syntax.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)) == true;
        }

        public readonly bool HasInit;

    }
}
