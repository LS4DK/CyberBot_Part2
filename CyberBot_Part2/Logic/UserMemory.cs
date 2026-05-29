using System;
using System.Collections.Generic;

namespace CyberBot_Part2.Logic
{
    public class UserMemory
    {
        public string Name { get; set; } = "";
        public string FavouriteTopic { get; set; } = "";

        private readonly Dictionary<string, string> _facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void SetFact(string key, string value) => _facts[key] = value;

        public string GetFact(string key) => _facts.TryGetValue(key, out string val) ? val : "";

        public bool HasFacts => _facts.Count > 0;

        public string PersonalisedHint(string topic)
        {
            if (!string.IsNullOrEmpty(FavouriteTopic) &&
                FavouriteTopic.Equals(topic, StringComparison.OrdinalIgnoreCase))
                return $"Since {topic} is your favourite topic, here's something extra useful: ";
            return "";
        }
    }
}
