using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Basis.Common.Localization
{
    public class JsonFileStringLocalizerFactory : IStringLocalizerFactory
    {
        public IStringLocalizer Create(Type resourceSource)
        {
            throw new NotImplementedException();
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        private class AttributeKey : IEquatable<AttributeKey>
        {
            public string Name { get; set; }
            public int ArgumentName { get; set; }

            public override bool Equals(object obj)
            {
                return Equals(obj as AttributeKey);
            }

            public bool Equals(AttributeKey other)
            {
                return other != null &&
                       Name == other.Name &&
                       ArgumentName == other.ArgumentName;
            }

            public static bool operator ==(AttributeKey left, AttributeKey right)
            {
                return EqualityComparer<AttributeKey>.Default.Equals(left, right);
            }

            public static bool operator !=(AttributeKey left, AttributeKey right)
            {
                return !(left == right);
            }
        }
    }
}
