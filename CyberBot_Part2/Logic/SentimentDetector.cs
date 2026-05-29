using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CyberBot_Part2.Logic
{
    public enum Sentiment { Neutral, Worried, Frustrated, Curious, Happy }

    public static class SentimentDetector
    {
        private static readonly string[] WorriedWords =
            { "worried", "scared", "afraid", "nervous", "anxious", "fear", "terrified", "unsafe", "threatened", "help" };

        private static readonly string[] FrustratedWords =
            { "frustrated", "annoyed", "angry", "upset", "confused", "lost", "stuck", "hate", "useless", "broken" };

        private static readonly string[] CuriousWords =
            { "curious", "interested", "wondering", "how", "why", "what", "tell me", "explain", "learn", "understand" };

        private static readonly string[] HappyWords =
            { "great", "awesome", "thanks", "thank you", "cool", "good", "nice", "love", "happy", "excellent" };

        public static Sentiment Detect(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Sentiment.Neutral;

            string lower = input.ToLowerInvariant();
            string[] tokens = Regex.Split(lower, @"\W+")
                                   .Where(t => !string.IsNullOrEmpty(t))
                                   .ToArray();

            int worried = Score(tokens, lower, WorriedWords);
            int frustrated = Score(tokens, lower, FrustratedWords);
            int curious = Score(tokens, lower, CuriousWords);
            int happy = Score(tokens, lower, HappyWords);

            int max = Math.Max(Math.Max(worried, frustrated), Math.Max(curious, happy));
            if (max == 0) return Sentiment.Neutral;

            if (max == worried) return Sentiment.Worried;
            if (max == frustrated) return Sentiment.Frustrated;
            if (max == happy) return Sentiment.Happy;
            return Sentiment.Curious;
        }

        public static string EmpathyPrefix(Sentiment s)
        {
            switch (s)
            {
                case Sentiment.Worried: return "It's completely understandable to feel worried — you're not alone. ";
                case Sentiment.Frustrated: return "I hear you, cybersecurity can feel overwhelming. Let me help. ";
                case Sentiment.Curious: return "Great curiosity! Learning about this will keep you safer. ";
                case Sentiment.Happy: return "Glad you're feeling positive! Here's what I know: ";
                default: return "";
            }
        }

        private static int Score(string[] tokens, string raw, string[] keywords)
        {
            int score = 0;
            foreach (string kw in keywords)
            {
                if (kw.Contains(" ")) { if (raw.Contains(kw)) score++; }
                else if (tokens.Contains(kw)) score++;
            }
            return score;
        }
    }
}