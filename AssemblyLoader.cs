using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace FieldViewer
{
    public class FieldInfo
    {
        public string TypeName { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string FieldType { get; set; } = "";
        public string Attributes { get; set; } = "";
        public string FullDescription => $"{Attributes} {FieldType} {FieldName}";
    }

    public class AssemblyLoader : IDisposable
    {
        private ModuleDefMD? _module;
        private bool _disposed;

  
        public void Load(string filePath)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(AssemblyLoader));
            _module?.Dispose();
            _module = ModuleDefMD.Load(filePath, new ModuleCreationOptions()
            {
                TryToLoadPdbFromDisk = false,
                IgnoreAccessError = true
            });
        }

        
        public async Task LoadAsync(string filePath)
        {
            await Task.Run(() => Load(filePath)).ConfigureAwait(false);
        }

        
        public List<FieldInfo> GetAllFields(int maxTypes = int.MaxValue)
        {
            if (_module == null)
                throw new InvalidOperationException("Сборка не загружена");

            var result = new List<FieldInfo>();
            int typeCount = 0;

            foreach (var type in _module.GetTypes())
            {
                if (typeCount >= maxTypes)
                    break;
                typeCount++;

                if (type.HasFields)
                {
                    foreach (var field in type.Fields)
                    {
                        result.Add(new FieldInfo
                        {
                            TypeName = type.FullName,
                            FieldName = field.Name,
                            FieldType = field.FieldType?.FullName ?? "unknown",
                            Attributes = field.Attributes.ToString()
                        });
                    }
                }
            }

            return result;
        }

        
        public List<FieldInfo> GetFieldsOfType(string typeFullName)
        {
            if (_module == null)
                throw new InvalidOperationException("Сборка не загружена");

            var type = _module.Find(typeFullName, false);
            if (type == null || !type.HasFields)
                return new List<FieldInfo>();

            return type.Fields.Select(f => new FieldInfo
            {
                TypeName = type.FullName,
                FieldName = f.Name,
                FieldType = f.FieldType?.FullName ?? "unknown",
                Attributes = f.Attributes.ToString()
            }).ToList();
        }

  
        public void Dispose()
        {
            if (_disposed) return;
            _module?.Dispose();
            _module = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
