using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    internal class RegistryMap
    {
        private readonly Dictionary<string, RegistryModel> _registryModels;

        private RegistryMap(Dictionary<string, RegistryModel> registryModels)
        {
            _registryModels = registryModels;
            var a = (1, 2);
        }

        public static RegistryMap Create(
            (ImmutableArray<RegistryModel> Left, ValueCompareSet<RegistryModel> Right) valueTuple)
        {
            Dictionary<string, RegistryModel> models = [];
            
            foreach (var registryModel in valueTuple.Left)
            {
                models.Add(Combine(registryModel.ContainingNamespace, registryModel.TypeName), registryModel);
            }
            
            foreach (var registryModel in valueTuple.Right)
            {
                models.Add(Combine(registryModel.ContainingNamespace, registryModel.TypeName), registryModel);
            }

            return new RegistryMap(models);
        }

        public bool TryGetValueByMetadataName(string metadataName, out RegistryModel? model)
        {
            const string suffix = "_Metadata";
            
            model = null;
            if (!metadataName.EndsWith(suffix)) return false;
            var accessor = metadataName.Substring(0, metadataName.LastIndexOf(suffix, StringComparison.Ordinal));
            return _registryModels.TryGetValue(accessor, out model);
        }

        public bool TryGetValue(string @namespace, string typeName, out RegistryModel model)
        {
            return TryGetValue(Combine(@namespace, typeName), out model);
        }
        
        public bool TryGetValue(string fullName, out RegistryModel model)
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
                hashCode.Add(value);
            }

            return hashCode.ToHashCode();
        }
    }
}