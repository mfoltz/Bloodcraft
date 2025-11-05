using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bloodcraft;

#nullable enable

/// <summary>
/// Provides an interactive helper that scaffolds system work implementations.
/// </summary>
internal static class SystemWorkGenerator
{
    public static void RunInteractive()
    {
        Console.WriteLine("Bloodcraft System Work Generator");
        Console.WriteLine("This helper scaffolds code that targets the builder/pipeline APIs.");
        Console.WriteLine("For persistent state across updates, prefer SystemWorkBuilder.WithNativeContainer over managed collections.");
        Console.WriteLine();

        string systemName = PromptSystemName();
        OutputStyle style = PromptOutputStyle();

        List<ComponentRequest> primaryComponents = PromptComponents();
        List<string> anyComponents = PromptOptionalList("Components that can satisfy AddAny (comma separated, optional): ");
        List<string> noneComponents = PromptOptionalList("Components that should be excluded with AddNone (comma separated, optional): ");
        bool includeDisabled = PromptBoolean("Include disabled entities in the query? (y/N): ", defaultValue: false);
        bool includeSpawnTag = PromptBoolean("Include spawn-tagged entities in the query? (y/N): ", defaultValue: false);
        bool includeSystems = PromptBoolean("Include system entities in the query? (y/N): ", defaultValue: false);
        bool requireForUpdate = PromptBoolean("Require the query for update? (Y/n): ", defaultValue: true);

        Console.WriteLine();

        bool needsPersistentState = PromptBoolean("Should the scaffold include persistent native container state? (y/N): ", defaultValue: false);
        List<NativeContainerRequest> nativeContainers = needsPersistentState
            ? PromptNativeContainers()
            : new List<NativeContainerRequest>();

        Console.WriteLine();

        string snippet = style == OutputStyle.BuilderInvocation
            ? BuildBuilderSnippet(systemName, primaryComponents, anyComponents, noneComponents, includeDisabled, includeSpawnTag, includeSystems, requireForUpdate, nativeContainers)
            : BuildWorkClassSnippet(systemName, primaryComponents, anyComponents, noneComponents, includeDisabled, includeSpawnTag, includeSystems, requireForUpdate, nativeContainers);

        Console.WriteLine("Generated snippet:\n");
        Console.WriteLine(snippet);
        Console.WriteLine("\nCopy the snippet into your system and adjust as required.");
        if (nativeContainers.Count > 0)
        {
            Console.WriteLine("The native container allocation blocks include placeholders—swap in the correct capacities and element types.");
        }
    }

    static string PromptSystemName()
    {
        while (true)
        {
            Console.Write("System name (without namespace): ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("A system name is required.");
                continue;
            }

            string sanitised = RemoveWhitespace(input);
            if (!sanitised.EndsWith("System", StringComparison.Ordinal))
            {
                sanitised += "System";
            }

            return sanitised;
        }
    }

    static OutputStyle PromptOutputStyle()
    {
        while (true)
        {
            Console.Write("Output style - builder invocation or work class? ([B]/W): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                return OutputStyle.WorkClass;
            }

            input = input.Trim();
            if (input.Equals("b", StringComparison.OrdinalIgnoreCase) || input.Equals("builder", StringComparison.OrdinalIgnoreCase))
                return OutputStyle.BuilderInvocation;
            if (input.Equals("w", StringComparison.OrdinalIgnoreCase) || input.Equals("work", StringComparison.OrdinalIgnoreCase))
                return OutputStyle.WorkClass;

            Console.WriteLine("Please enter 'b' for builder or 'w' for work class.");
        }
    }

    static List<ComponentRequest> PromptComponents()
    {
        List<ComponentRequest> components = new();

        while (components.Count == 0)
        {
            Console.Write("Primary components (comma separated): ");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("At least one component is required.");
                continue;
            }

            string[] parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (string part in parts)
            {
                string component = part.Trim();
                if (component.Length == 0)
                    continue;

                bool isBuffer = PromptBoolean($" - Is {component} a dynamic buffer element? (y/N): ", defaultValue: false);
                bool isReadOnly = PromptBoolean($" - Should {component} be read-only in the query? (Y/n): ", defaultValue: true);
                bool needsLookup = PromptBoolean($" - Create {(isBuffer ? "buffer lookup" : "component lookup")} for {component}? (y/N): ", defaultValue: false);
                bool needsTypeHandle = PromptBoolean($" - Create {(isBuffer ? "buffer type handle" : "component type handle")} for {component}? (y/N): ", defaultValue: false);

                components.Add(new ComponentRequest(component, isBuffer, isReadOnly, needsLookup, needsTypeHandle));
            }

            if (components.Count == 0)
            {
                Console.WriteLine("Unable to parse any components. Please try again.");
            }
        }

        return components;
    }

    static List<string> PromptOptionalList(string prompt)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(RemoveWhitespace)
            .Where(value => !string.IsNullOrEmpty(value))
            .ToList();
    }

    static bool PromptBoolean(string prompt, bool defaultValue)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            input = input.Trim();
            if (input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                return true;
            if (input.Equals("n", StringComparison.OrdinalIgnoreCase) || input.Equals("no", StringComparison.OrdinalIgnoreCase))
                return false;

            Console.WriteLine("Please enter 'y' or 'n'.");
        }
    }

    static List<NativeContainerRequest> PromptNativeContainers()
    {
        List<NativeContainerRequest> containers = new();

        Console.WriteLine("Persistent native containers avoid GC pressure and are disposed automatically by the builder.");
        Console.WriteLine("Specify each container you need. Leave the type blank once you're done.");

        while (true)
        {
            Console.Write("Native container type (blank to finish): ");
            string? typeInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(typeInput))
            {
                if (containers.Count == 0)
                {
                    Console.WriteLine("At least one native container is required when persistent state is enabled.");
                    continue;
                }

                break;
            }

            string typeName = typeInput.Trim();

            while (true)
            {
                Console.Write($"Allocation expression for {typeName} (executed during OnCreate): ");
                string? allocationInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(allocationInput))
                {
                    Console.WriteLine("An allocation expression is required to demonstrate the helper usage.");
                    continue;
                }

                containers.Add(new NativeContainerRequest(typeName, allocationInput.Trim()));
                Console.WriteLine("Native container added. Configure another or press enter to finish.");
                break;
            }
        }

        return containers;
    }

    static string BuildBuilderSnippet(
        string systemName,
        IReadOnlyList<ComponentRequest> components,
        IReadOnlyCollection<string> anyComponents,
        IReadOnlyCollection<string> noneComponents,
        bool includeDisabled,
        bool includeSpawnTag,
        bool includeSystems,
        bool requireForUpdate,
        IReadOnlyList<NativeContainerRequest> nativeContainers)
    {
        StringBuilder sb = new();
        sb.AppendLine("using Bloodcraft.Factory;");
        sb.AppendLine("using Unity.Entities;");
        if (nativeContainers.Count > 0)
            sb.AppendLine("using Unity.Collections;");
        sb.AppendLine();

        foreach (ComponentRequest component in components)
        {
            if (component.NeedsLookup)
            {
                sb.AppendLine($"SystemWorkBuilder.{(component.IsBuffer ? "BufferLookupHandle" : "ComponentLookupHandle")}<{component.TypeName}> {GetLookupIdentifier(component)};");
            }

            if (component.NeedsTypeHandle)
            {
                sb.AppendLine($"SystemWorkBuilder.{(component.IsBuffer ? "BufferTypeHandleHandle" : "ComponentTypeHandleHandle")}<{component.TypeName}> {GetHandleIdentifier(component)};");
            }
        }

        if (nativeContainers.Count > 0)
        {
            sb.AppendLine("// Persistent native containers managed by the builder. These are disposed automatically.");
            foreach (NativeContainerRequest container in nativeContainers)
            {
                sb.AppendLine($"SystemWorkBuilder.NativeContainerHolder<{container.TypeName}> {GetNativeContainerIdentifier(container)};");
            }
            sb.AppendLine();
        }

        sb.AppendLine("SystemWorkBuilder.QueryHandleHolder query;");
        sb.AppendLine();

        List<string> descriptorLines = BuildDescriptorLines(
            components,
            anyComponents,
            noneComponents,
            includeDisabled,
            includeSpawnTag,
            includeSystems,
            requireForUpdateOverride: requireForUpdate ? null : false);

        AppendDescriptorDeclaration(sb, "var descriptor", descriptorLines, string.Empty);
        sb.AppendLine();

        sb.AppendLine("var builder = new SystemWorkBuilder()");
        sb.AppendLine("    .WithQuery(descriptor);");
        sb.AppendLine();

        sb.AppendLine($"query = builder.WithPrimaryQuery(requireForUpdate: {requireForUpdate.ToString().ToLowerInvariant()});");

        if (nativeContainers.Count > 0)
        {
            sb.AppendLine();
            foreach (NativeContainerRequest container in nativeContainers)
            {
                sb.AppendLine($"{GetNativeContainerIdentifier(container)} = builder.WithNativeContainer(_ =>");
                sb.AppendLine("{");
                sb.AppendLine("    // Allocate the persistent native container once during OnCreate.");
                sb.AppendLine($"    return {container.AllocationExpression};");
                sb.AppendLine("});");
                sb.AppendLine();
            }
        }

        foreach (ComponentRequest component in components)
        {
            if (component.NeedsLookup)
            {
                string method = component.IsBuffer ? "WithBuffer" : "WithLookup";
                sb.AppendLine($"{GetLookupIdentifier(component)} = builder.{method}<{component.TypeName}>(isReadOnly: {component.IsReadOnly.ToString().ToLowerInvariant()});");
            }

            if (component.NeedsTypeHandle)
            {
                string method = component.IsBuffer ? "WithBufferTypeHandle" : "WithComponentTypeHandle";
                sb.AppendLine($"{GetHandleIdentifier(component)} = builder.{method}<{component.TypeName}>(isReadOnly: {component.IsReadOnly.ToString().ToLowerInvariant()});");
            }
        }

        sb.AppendLine();
        sb.AppendLine("builder.OnUpdate(context =>");
        sb.AppendLine("{");
        sb.AppendLine("    var queryHandle = query.Handle;");
        sb.AppendLine("    if (queryHandle == null || queryHandle.IsDisposed)");
        sb.AppendLine("    {");
        sb.AppendLine("        return;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (nativeContainers.Count > 0)
        {
            sb.AppendLine("    // Native containers are allocated during OnCreate and exposed via the holders above.");
            foreach (NativeContainerRequest container in nativeContainers)
            {
                sb.AppendLine($"    ref var {GetNativeContainerLocalIdentifier(container)} = ref {GetNativeContainerIdentifier(container)}.Container;");
            }
            sb.AppendLine("    // Refresh or clear persistent caches here before iterating.");
            sb.AppendLine();
        }

        if (components.Any(component => component.NeedsTypeHandle))
        {
            List<ComponentRequest> handleRequests = components.Where(c => c.NeedsTypeHandle).ToList();
            sb.AppendLine("    SystemWorkBuilder.ForEachChunk(context, queryHandle)");
            foreach (ComponentRequest component in handleRequests)
            {
                string methodName = component.IsBuffer
                    ? "WithBuffer"
                    : component.IsReadOnly ? "WithReadOnlyComponent" : "WithComponent";
                sb.AppendLine($"        .{methodName}({GetHandleIdentifier(component)})");
            }

            string parameterList = string.Join(", ",
                new[] { "chunkContext" }.Concat(handleRequests.Select(GetChunkAccessorIdentifier)));
            sb.AppendLine($"        .ForEach(({parameterList}) =>");
            sb.AppendLine("        {");
            sb.AppendLine("            var entities = chunkContext.Entities;");

            foreach (ComponentRequest component in components.Where(c => c.NeedsLookup))
            {
                string accessorName = GetLookupAccessorIdentifier(component);
                sb.AppendLine($"            var {accessorName} = chunkContext.GetLookup({GetLookupIdentifier(component)});");
            }

            sb.AppendLine("            // TODO: Implement behaviour.");
            sb.AppendLine("        });");
        }
        else
        {
            sb.AppendLine("    SystemWorkBuilder.ForEachEntity(context, queryHandle, iterator =>");
            sb.AppendLine("    {");
            sb.AppendLine("        var entity = iterator.Entity;");

            foreach (ComponentRequest component in components.Where(c => c.NeedsLookup))
            {
                string accessorName = GetLookupAccessorIdentifier(component);
                sb.AppendLine($"        var {accessorName} = iterator.GetLookup({GetLookupIdentifier(component)});");
            }

            sb.AppendLine("        // TODO: Implement behaviour.");
            sb.AppendLine("    });");
        }

        sb.AppendLine("});");
        sb.AppendLine();
        if (nativeContainers.Count > 0)
            sb.AppendLine("// Native containers registered with the builder are disposed automatically during teardown.");
        sb.AppendLine("return builder.Build();");

        return sb.ToString();
    }

    static string BuildWorkClassSnippet(
        string systemName,
        IReadOnlyList<ComponentRequest> components,
        IReadOnlyCollection<string> anyComponents,
        IReadOnlyCollection<string> noneComponents,
        bool includeDisabled,
        bool includeSpawnTag,
        bool includeSystems,
        bool requireForUpdate,
        IReadOnlyList<NativeContainerRequest> nativeContainers)
    {
        StringBuilder sb = new();
        sb.AppendLine("using Bloodcraft.Factory;");
        sb.AppendLine("using Unity.Entities;");
        if (nativeContainers.Count > 0)
            sb.AppendLine("using Unity.Collections;");
        sb.AppendLine();
        sb.AppendLine("namespace Bloodcraft.Systems;");
        sb.AppendLine();
        sb.AppendLine($"public sealed partial class {systemName} : VSystemBase<{systemName}.Work>");
        sb.AppendLine("{");
        sb.AppendLine("    public sealed class Work : ISystemWork");
        sb.AppendLine("    {");

        sb.AppendLine("        readonly ISystemWork _implementation;");
        sb.AppendLine("        readonly SystemWorkBuilder.QueryHandleHolder _query;");

        if (nativeContainers.Count > 0)
        {
            sb.AppendLine("        // Persistent native containers managed by the builder. These are disposed automatically.");
            foreach (NativeContainerRequest container in nativeContainers)
            {
                sb.AppendLine($"        readonly SystemWorkBuilder.NativeContainerHolder<{container.TypeName}> {GetNativeContainerFieldName(container)};");
            }
            sb.AppendLine();
        }

        foreach (ComponentRequest component in components)
        {
            if (component.NeedsLookup)
            {
                string lookupType = component.IsBuffer ? "BufferLookupHandle" : "ComponentLookupHandle";
                sb.AppendLine($"        readonly SystemWorkBuilder.{lookupType}<{component.TypeName}> {GetLookupFieldName(component)};");
            }

            if (component.NeedsTypeHandle)
            {
                string handleType = component.IsBuffer ? "BufferTypeHandleHandle" : "ComponentTypeHandleHandle";
                sb.AppendLine($"        readonly SystemWorkBuilder.{handleType}<{component.TypeName}> {GetHandleFieldName(component)};");
            }
        }

        sb.AppendLine();

        List<string> descriptorLines = BuildDescriptorLines(
            components,
            anyComponents,
            noneComponents,
            includeDisabled,
            includeSpawnTag,
            includeSystems,
            requireForUpdateOverride: requireForUpdate ? null : false);

        AppendDescriptorDeclaration(sb, "static readonly QueryDescriptor PrimaryQuery", descriptorLines, "        ");

        sb.AppendLine();

        sb.AppendLine("        public Work()");
        sb.AppendLine("        {");
        sb.AppendLine("            var descriptor = PrimaryQuery;");
        sb.AppendLine();
        sb.AppendLine("            var builder = new SystemWorkBuilder()");
        sb.AppendLine("                .WithQuery(descriptor);");
        sb.AppendLine();
        sb.AppendLine($"            _query = builder.WithPrimaryQuery(requireForUpdate: {requireForUpdate.ToString().ToLowerInvariant()});");
        sb.AppendLine();

        if (nativeContainers.Count > 0)
        {
            foreach (NativeContainerRequest container in nativeContainers)
            {
                sb.AppendLine($"            {GetNativeContainerFieldName(container)} = builder.WithNativeContainer(_ =>");
                sb.AppendLine("            {");
                sb.AppendLine("                // Allocate the persistent native container once during OnCreate.");
                sb.AppendLine($"                return {container.AllocationExpression};");
                sb.AppendLine("            });");
                sb.AppendLine();
            }
        }

        foreach (ComponentRequest component in components)
        {
            if (component.NeedsLookup)
            {
                string method = component.IsBuffer ? "WithBuffer" : "WithLookup";
                sb.AppendLine($"            {GetLookupFieldName(component)} = builder.{method}<{component.TypeName}>(isReadOnly: {component.IsReadOnly.ToString().ToLowerInvariant()});");
            }

            if (component.NeedsTypeHandle)
            {
                string method = component.IsBuffer ? "WithBufferTypeHandle" : "WithComponentTypeHandle";
                sb.AppendLine($"            {GetHandleFieldName(component)} = builder.{method}<{component.TypeName}>(isReadOnly: {component.IsReadOnly.ToString().ToLowerInvariant()});");
            }
        }

        sb.AppendLine();
        sb.AppendLine("            builder.OnUpdate(context =>");
        sb.AppendLine("            {");
        sb.AppendLine("                var queryHandle = _query.Handle;");
        sb.AppendLine("                if (queryHandle == null || queryHandle.IsDisposed)");
        sb.AppendLine("                {");
        sb.AppendLine("                    return;");
        sb.AppendLine("                }");
        sb.AppendLine();

        if (nativeContainers.Count > 0)
        {
            sb.AppendLine("                // Native containers are allocated during OnCreate and exposed via the holders above.");
            foreach (NativeContainerRequest container in nativeContainers)
            {
                sb.AppendLine($"                ref var {GetNativeContainerLocalIdentifier(container)} = ref {GetNativeContainerFieldName(container)}.Container;");
            }
            sb.AppendLine("                // Refresh or clear persistent caches here before iterating.");
            sb.AppendLine();
        }

        if (components.Any(component => component.NeedsTypeHandle))
        {
            List<ComponentRequest> handleRequests = components.Where(c => c.NeedsTypeHandle).ToList();
            sb.AppendLine("                SystemWorkBuilder.ForEachChunk(context, queryHandle)");
            foreach (ComponentRequest component in handleRequests)
            {
                string methodName = component.IsBuffer
                    ? "WithBuffer"
                    : component.IsReadOnly ? "WithReadOnlyComponent" : "WithComponent";
                sb.AppendLine($"                    .{methodName}({GetHandleFieldName(component)})");
            }

            string parameterList = string.Join(", ",
                new[] { "chunkContext" }.Concat(handleRequests.Select(GetChunkAccessorIdentifier)));
            sb.AppendLine($"                    .ForEach(({parameterList}) =>");
            sb.AppendLine("                {");
            sb.AppendLine("                    var entities = chunkContext.Entities;");

            foreach (ComponentRequest component in components.Where(c => c.NeedsLookup))
            {
                string accessorName = GetLookupAccessorIdentifier(component);
                sb.AppendLine($"                    var {accessorName} = chunkContext.GetLookup({GetLookupFieldName(component)});");
            }

            sb.AppendLine("                    // TODO: Implement behaviour.");
            sb.AppendLine("                });");
        }
        else
        {
            sb.AppendLine("                SystemWorkBuilder.ForEachEntity(context, queryHandle, iterator =>");
            sb.AppendLine("                {");
            sb.AppendLine("                    var entity = iterator.Entity;");

            foreach (ComponentRequest component in components.Where(c => c.NeedsLookup))
            {
                string accessorName = GetLookupAccessorIdentifier(component);
                sb.AppendLine($"                    var {accessorName} = iterator.GetLookup({GetLookupFieldName(component)});");
            }

            sb.AppendLine("                    // TODO: Implement behaviour.");
            sb.AppendLine("                });");
        }

        sb.AppendLine();
        sb.AppendLine("            });");
        sb.AppendLine();
        sb.AppendLine("            _implementation = builder.Build();");
        if (nativeContainers.Count > 0)
            sb.AppendLine("            // Native containers registered with the builder are disposed automatically during teardown.");
        sb.AppendLine("        }");

        if (!requireForUpdate)
        {
            sb.AppendLine();
            sb.AppendLine("        public bool RequireForUpdate => false;");
        }

        sb.AppendLine();
        sb.AppendLine("        public void Build(ref EntityQueryBuilder builder) =>");
        sb.AppendLine("            _implementation.Build(ref builder);");
        sb.AppendLine();
        sb.AppendLine("        public void OnCreate(SystemContext context) =>");
        sb.AppendLine("            _implementation.OnCreate(context);");
        sb.AppendLine();
        sb.AppendLine("        public void OnStartRunning(SystemContext context) =>");
        sb.AppendLine("            _implementation.OnStartRunning(context);");
        sb.AppendLine();
        sb.AppendLine("        public void OnUpdate(SystemContext context) =>");
        sb.AppendLine("            _implementation.OnUpdate(context);");
        sb.AppendLine();
        sb.AppendLine("        public void OnStopRunning(SystemContext context) =>");
        sb.AppendLine("            _implementation.OnStopRunning(context);");
        sb.AppendLine();
        sb.AppendLine("        public void OnDestroy(SystemContext context) =>");
        sb.AppendLine("            _implementation.OnDestroy(context);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }


    static List<string> BuildDescriptorLines(
        IReadOnlyList<ComponentRequest> components,
        IReadOnlyCollection<string> anyComponents,
        IReadOnlyCollection<string> noneComponents,
        bool includeDisabled,
        bool includeSpawnTag,
        bool includeSystems,
        bool? requireForUpdateOverride)
    {
        List<string> lines = new();

        foreach (ComponentRequest component in components)
        {
            lines.Add(GetWithAllInvocation(component));
        }

        foreach (string anyComponent in anyComponents)
        {
            lines.Add($".WithAny<{anyComponent}>()");
        }

        foreach (string noneComponent in noneComponents)
        {
            lines.Add($".WithNone<{noneComponent}>()");
        }

        if (includeDisabled)
        {
            lines.Add(".IncludeDisabled()");
        }

        if (includeSpawnTag)
        {
            lines.Add(".IncludeSpawnTag()");
        }

        if (includeSystems)
        {
            lines.Add(".IncludeSystems()");
        }

        if (requireForUpdateOverride.HasValue)
        {
            lines.Add($".RequireForUpdate({requireForUpdateOverride.Value.ToString().ToLowerInvariant()})");
        }

        return lines;
    }

    static void AppendDescriptorDeclaration(StringBuilder sb, string declaration, IReadOnlyList<string> lines, string indent)
    {
        if (lines.Count > 0)
        {
            sb.AppendLine($"{indent}{declaration} = QueryDescriptor.Create()");

            for (int i = 0; i < lines.Count; ++i)
            {
                bool isLast = i == lines.Count - 1;
                sb.AppendLine($"{indent}    {lines[i]}{(isLast ? ";" : string.Empty)}");
            }

            return;
        }

        sb.AppendLine($"{indent}{declaration} = QueryDescriptor.Create();");
    }

    static string GetWithAllInvocation(ComponentRequest component)
    {
        if (component.IsReadOnly)
            return $".WithAll<{component.TypeName}>()";

        return $".WithAll<{component.TypeName}>(QueryDescriptor.AccessMode.ReadWrite)";
    }

    static string ToIdentifier(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return "component";

        StringBuilder sb = new();
        bool capitaliseNext = false;

        foreach (char c in typeName)
        {
            if (char.IsLetterOrDigit(c))
            {
                char next = capitaliseNext ? char.ToUpperInvariant(c) : c;
                capitaliseNext = false;
                sb.Append(sb.Length == 0 ? char.ToLowerInvariant(next) : next);
            }
            else
            {
                capitaliseNext = true;
            }
        }

        return sb.ToString();
    }

    static string GetLookupIdentifier(ComponentRequest component)
    {
        string baseName = ToIdentifier(component.TypeName);
        return string.IsNullOrEmpty(baseName)
            ? (component.IsBuffer ? "bufferLookup" : "lookup")
            : baseName + (component.IsBuffer ? "BufferLookup" : "Lookup");
    }

    static string GetHandleIdentifier(ComponentRequest component)
    {
        string baseName = ToIdentifier(component.TypeName);
        return string.IsNullOrEmpty(baseName)
            ? (component.IsBuffer ? "bufferHandle" : "handle")
            : baseName + (component.IsBuffer ? "BufferHandle" : "Handle");
    }

    static string GetLookupFieldName(ComponentRequest component) => "_" + GetLookupIdentifier(component);

    static string GetHandleFieldName(ComponentRequest component) => "_" + GetHandleIdentifier(component);

    static string GetChunkAccessorIdentifier(ComponentRequest component)
    {
        string baseName = ToIdentifier(component.TypeName);
        if (string.IsNullOrEmpty(baseName))
            baseName = "component";
        return component.IsBuffer ? baseName + "BufferAccessor" : baseName + "Array";
    }

    static string GetLookupAccessorIdentifier(ComponentRequest component)
    {
        string baseName = ToIdentifier(component.TypeName);
        if (string.IsNullOrEmpty(baseName))
            baseName = "component";
        return baseName + (component.IsBuffer ? "BufferLookupAccessor" : "LookupAccessor");
    }

    static string GetNativeContainerIdentifier(NativeContainerRequest container)
    {
        string baseName = ToIdentifier(container.TypeName);
        if (string.IsNullOrEmpty(baseName))
            baseName = "nativeContainer";
        return baseName + "Holder";
    }

    static string GetNativeContainerFieldName(NativeContainerRequest container) =>
        "_" + GetNativeContainerIdentifier(container);

    static string GetNativeContainerLocalIdentifier(NativeContainerRequest container)
    {
        string baseName = ToIdentifier(container.TypeName);
        if (string.IsNullOrEmpty(baseName))
            baseName = "nativeContainer";
        return baseName + "Container";
    }

    static string RemoveWhitespace(string value)
    {
        StringBuilder sb = new(value.Length);
        foreach (char c in value)
        {
            if (!char.IsWhiteSpace(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    readonly struct ComponentRequest(
        string typeName,
        bool isBuffer,
        bool isReadOnly,
        bool needsLookup,
        bool needsTypeHandle)
    {
        public string TypeName { get; } = typeName;
        public bool IsBuffer { get; } = isBuffer;
        public bool IsReadOnly { get; } = isReadOnly;
        public bool NeedsLookup { get; } = needsLookup;
        public bool NeedsTypeHandle { get; } = needsTypeHandle;
    }

    readonly struct NativeContainerRequest(
        string typeName,
        string allocationExpression)
    {
        public string TypeName { get; } = typeName;
        public string AllocationExpression { get; } = allocationExpression;
    }

    enum OutputStyle
    {
        BuilderInvocation,
        WorkClass,
    }
}
