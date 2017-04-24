using System;
using System.Runtime.Serialization;

namespace ConfigService.Interfaces
{
    public interface IConfig
    {
        string Name { get; set; }
        string Version { get; set; }
    }

    [DataContract]
    [Serializable]
    [KnownType(typeof(ConfigKey))]
    public class ConfigKey : IConfig, IComparable<ConfigKey>, IEquatable<ConfigKey>
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Version { get; set; }
        public ConfigKey(IConfig seed) 
        {
            this.Name = seed.Name;
            this.Version = seed.Version;
        }
        public ConfigKey(string name, string version) 
        {
            this.Name = name;
            this.Version = version;
        }

        /// <summary>
        /// The override of the equals operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator ==(ConfigKey left, ConfigKey right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// The override of the equals. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool Equals(ConfigKey x, ConfigKey y)
        {
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            return x.Equals(y);
        }

        /// <summary>
        /// The override of the bang equals operator. 
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is equal to right <c>true</c>, else <c>false</c>.</returns>
        public static bool operator !=(ConfigKey left, ConfigKey right)
        {
            return !Equals(left, right);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode() + new SemVer(Version).GetHashCode();
        }

        public int CompareTo(ConfigKey other)
        {
            if(this.Name != other.Name) 
            {
                return -1;
            }
            try 
            {
                var semverThis = new SemVer(Version);
                var semverThat = new SemVer(other.Version);
                return semverThis.CompareTo(semverThat);
            }
            catch 
            {
                return -1;
            }
        }

        public override bool Equals(object o) 
        {
            if (o is ConfigKey) return this.Equals(o);
            else return false;
        }

        public bool Equals(ConfigKey other)
        {
            if(this.Name != other.Name) 
            {
                return false;
            }
            try 
            {
                var semverThis = new SemVer(Version);
                var semverThat = new SemVer(other.Version);
                return semverThis.Equals(semverThat);
            }
            catch 
            {
                return false;
            }
        }
    }
}
