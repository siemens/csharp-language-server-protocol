using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization.Converters;

namespace OmniSharp.Extensions.LanguageServer.Protocol.Models
{
    public class Location : IEquatable<Location>
    {
        /// <summary>
        /// The uri of the document
        /// </summary>
        [JsonConverter(typeof(AbsoluteUriConverter))]
        public Uri Uri { get; set; }

        /// <summary>
        /// The range in side the document given by the uri
        /// </summary>
        public Range Range { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Location);
        }

        public bool Equals(Location other)
        {
            return other != null &&
                   EqualityComparer<Uri>.Default.Equals(Uri, other.Uri) &&
                   EqualityComparer<Range>.Default.Equals(Range, other.Range);
        }

        public override int GetHashCode()
        {
            var hashCode = 1486144663;
            hashCode = hashCode * -1521134295 + EqualityComparer<Uri>.Default.GetHashCode(Uri);
            hashCode = hashCode * -1521134295 + EqualityComparer<Range>.Default.GetHashCode(Range);
            return hashCode;
        }

        public static bool operator ==(Location location1, Location location2)
        {
            return EqualityComparer<Location>.Default.Equals(location1, location2);
        }

        public static bool operator !=(Location location1, Location location2)
        {
            return !(location1 == location2);
        }
    }
}
