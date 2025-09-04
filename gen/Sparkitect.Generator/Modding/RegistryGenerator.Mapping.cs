using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    internal class RegistryMap
    {
        private readonly Dictionary<string, (RegistryModel Model, bool IsCurrentCompilation)> _registryModels;

        private RegistryMap(Dictionary<string, (RegistryModel Model, bool IsCurrentCompilation)> registryModels)
        {
            _registryModels = registryModels;
        }

        public static RegistryMap Create(
            (ImmutableArray<RegistryModel> Left, ImmutableValueArray<RegistryModel> Right) valueTuple)
        {
            Dictionary<string, (RegistryModel, bool)> models = [];

            // Left: current compilation
            foreach (var registryModel in valueTuple.Left)
            {
                var key = Combine(registryModel.ContainingNamespace, registryModel.TypeName);
                models[key] = (registryModel, true);
            }

            // Right: from referenced assemblies
            foreach (var registryModel in valueTuple.Right)
            {
                var key = Combine(registryModel.ContainingNamespace, registryModel.TypeName);
                if (models.TryGetValue(key, out var existing))
                {
                    // Prefer current compilation when clashing
                    if (existing.Item2)
                        continue;
                }
                models[key] = (registryModel, false);
            }

            return new RegistryMap(models);
        }

        public bool TryGetValueByMetadataName(string metadataName, out RegistryModel? model)
        {
            const string suffix = "_Metadata";
            
            model = null;
            if (!metadataName.EndsWith(suffix)) return false;
            var accessor = metadataName.Substring(0, metadataName.LastIndexOf(suffix, StringComparison.Ordinal));
            if (_registryModels.TryGetValue(accessor, out var tuple))
            {
                model = tuple.Model;
                return true;
            }
            return false;
        }

        public bool TryGetValue(string @namespace, string typeName, out RegistryModel model)
        {
            return TryGetValue(Combine(@namespace, typeName), out model);
        }
        
        public bool TryGetValue(string fullName, out RegistryModel model)
        {
            if (_registryModels.TryGetValue(fullName, out var tuple))
            {
                model = tuple.Model;
                return true;
            }
            model = null!;
            return false;
        }

        public bool TryGetByTypeName(string typeName, out RegistryModel? model)
        {
            model = null;
            RegistryModel? candidateFromCurrent = null;
            RegistryModel? candidateFromRefs = null;

            foreach (var kvp in _registryModels)
            {
                var lastDot = kvp.Key.LastIndexOf('.') + 1;
                var simple = lastDot > 0 && lastDot < kvp.Key.Length ? kvp.Key.Substring(lastDot) : kvp.Key;
                if (string.Equals(simple, typeName, StringComparison.Ordinal))
                {
                    if (kvp.Value.IsCurrentCompilation)
                        candidateFromCurrent = kvp.Value.Model;
                    else if (candidateFromRefs is null)
                        candidateFromRefs = kvp.Value.Model;
                }
            }

            model = candidateFromCurrent ?? candidateFromRefs;
            return model is not null;
        }
        
        



        public override bool Equals(object? obj)
        {
            return obj is RegistryMap other && Equals(other);
        }

        private static string Combine(string @namespace, string type) => $"{@namespace}.{type}";

        public bool Equals(RegistryMap other)
        {
            var notVisited = new HashSet<string>(other._registryModels.Keys);

            foreach (var kvp in _registryModels)
            {
                if (!other._registryModels.TryGetValue(kvp.Key, out var value) || !kvp.Value.Model.Equals(value.Model))
                {
                    return false;
                }

                notVisited.Remove(kvp.Key);
            }

            return notVisited.Count == 0;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            foreach (var value in _registryModels)
            {
                hashCode.Add(value.Key);
                hashCode.Add(value.Value.Model);
            }

            return hashCode.ToHashCode();
        }
    }
}
