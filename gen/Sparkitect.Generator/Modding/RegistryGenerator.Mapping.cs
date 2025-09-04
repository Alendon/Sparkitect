using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    internal class RegistryMap
    {
        private readonly Dictionary<string, RegistryModel> _registryModels;

        internal RegistryMap(Dictionary<string, RegistryModel> registryModels)
        {
            _registryModels = registryModels;
        }

        public static RegistryMap Create(
            (ImmutableArray<RegistryModel> Left, ImmutableValueArray<RegistryModel> Right) valueTuple)
        {
            Dictionary<string, RegistryModel> models = [];

            // Left: current compilation
            foreach (var registryModel in valueTuple.Left)
            {
                var key = Combine(registryModel.ContainingNamespace, registryModel.TypeName);
                models[key] = registryModel;
            }

            // Right: from referenced assemblies
            foreach (var registryModel in valueTuple.Right)
            {
                var key = Combine(registryModel.ContainingNamespace, registryModel.TypeName);
                models[key] = registryModel;
            }

            return new RegistryMap(models);
        }

        public bool TryGetByFullName(string typeName, string? @namespace, out RegistryModel? model)
        {
            return TryGetByFullName($"{(@namespace is null ? "" : $"{@namespace}.")}{typeName}", out model);
        }
        
        public bool TryGetByFullName(string fullName, out RegistryModel? model)
        {
            return _registryModels.TryGetValue(fullName, out model);
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
                if (!other._registryModels.TryGetValue(kvp.Key, out var value) || !kvp.Value.Equals(value))
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
                hashCode.Add(value.Value);
            }

            return hashCode.ToHashCode();
        }
    }
}
