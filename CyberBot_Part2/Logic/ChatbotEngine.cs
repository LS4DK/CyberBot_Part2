using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CyberBot_Part2.Logic;

namespace CyberBot_Part2.Logic
{
    public class ChatbotEngine
    {
        private string _lastTopic = "";
        private readonly Random _rng = new Random();
        public UserMemory Memory { get; } = new UserMemory();

        private static readonly Dictionary<string, List<string>> RandomPools =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["phishing"] = new List<string>
            {
                "Be cautious of emails asking for personal information — scammers often disguise themselves as trusted organisations.",
                "Always hover over links before clicking to see where they really lead.",
                "Legitimate companies will never ask for your password via email.",
                "Look for spelling mistakes and mismatched sender addresses in emails.",
                "When in doubt, go directly to the company website instead of clicking links."
            },
                ["password"] = new List<string>
            {
                "Use at least 12 characters with a mix of letters, numbers and symbols.",
                "Consider using a passphrase — four random words strung together are very strong.",
                "Never reuse the same password across multiple websites.",
                "A password manager can generate and store strong, unique passwords for you.",
                "Avoid using personal details like your name or birthday in passwords."
            },
                ["scam"] = new List<string>
            {
                "If an offer sounds too good to be true, it almost certainly is.",
                "Never transfer money to someone you have only met online.",
                "Government agencies will never demand immediate payment by gift card.",
                "Verify unexpected prize or lottery wins before taking any action.",
                "Report scams to your national cybercrime authority to protect others."
            },
                ["malware"] = new List<string>
            {
                "Keep your operating system and all software up to date to patch known vulnerabilities.",
                "Only download software from official or well-known sources.",
                "Run a reputable antivirus/antimalware tool and keep its definitions current.",
                "Be wary of USB drives from unknown sources — they can carry malware.",
                "Ransomware spreads via email attachments — never open files you weren't expecting."
            }
            };

        private static readonly HashSet<string> ReferenceTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "that", "this", "it", "more", "again", "same", "another",
            "details", "continue", "repeat", "else", "further", "explain",
            "elaborate", "tell"
        };

        private static readonly Dictionary<Sentiment, string> SentimentTips =
            new Dictionary<Sentiment, string>
            {
                [Sentiment.Worried] = "\n💡 Tip: Start with one small step — even changing one password today makes you safer.",
                [Sentiment.Frustrated] = "\n💡 Tip: Cybersecurity doesn't have to be perfect. Small improvements add up.",
                [Sentiment.Curious] = "\n💡 Tip: Curiosity is your best defence. Keep asking questions!",
                [Sentiment.Happy] = "",
                [Sentiment.Neutral] = ""
            };

        public string GetResponse(string input, string personality)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Please type something so I can help you.";

            Sentiment sentiment = SentimentDetector.Detect(input);
            string empathy = SentimentDetector.EmpathyPrefix(sentiment);
            string sentimentTip = SentimentTips.TryGetValue(sentiment, out string st) ? st : "";

            string normalized = input.ToLowerInvariant();
            string[] tokens = Regex.Split(normalized, @"\W+")
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToArray();

            string memoryReply = TryExtractMemory(normalized, tokens);
            if (memoryReply != null) return empathy + memoryReply;

            string special = HandleSpecial(normalized, tokens, personality);
            if (special != null) return empathy + special;

            string detectedTopic = DetectTopic(normalized);
            bool referencesPrev = tokens.Intersect(ReferenceTokens, StringComparer.OrdinalIgnoreCase).Any();

            string topic;
            if (!string.IsNullOrEmpty(detectedTopic))
            {
                topic = detectedTopic;
                _lastTopic = topic;
            }
            else if (referencesPrev && !string.IsNullOrEmpty(_lastTopic))
            {
                topic = _lastTopic;
            }
            else
            {
                return empathy + "I'm not sure I understand. Can you try rephrasing? "
                     + "You can ask about: passwords, phishing, malware, VPN, 2FA, firewalls, encryption, scams, or privacy."
                     + sentimentTip;
            }

            bool isHow = tokens.Contains("how");
            bool isWhy = tokens.Contains("why");
            bool isWhat = tokens.Contains("what");
            bool isWhen = tokens.Contains("when");
            bool wantsTip = normalized.Contains("tip") || normalized.Contains("advice")
                         || normalized.Contains("random") || normalized.Contains("give me");

            string hint = Memory.PersonalisedHint(topic);
            string body = BuildTopicResponse(topic, isHow, isWhy, isWhat, isWhen, wantsTip, personality);

            return empathy + hint + body + sentimentTip;
        }

        private string TryExtractMemory(string normalized, string[] tokens)
        {
            string[] topics = { "password", "phishing", "malware", "vpn", "2fa", "firewall", "encryption", "privacy", "scam" };

            if (normalized.Contains("interested in") || normalized.Contains("my favourite") || normalized.Contains("i like"))
            {
                foreach (string t in topics)
                {
                    if (normalized.Contains(t))
                    {
                        Memory.FavouriteTopic = t;
                        return $"Got it! I'll remember that you're interested in {t}. It's a crucial part of staying safe online. "
                             + $"As someone interested in {t}, you might want to ask me for some tips on it!";
                    }
                }
            }

            if (normalized.Contains("my name is"))
            {
                int idx = normalized.IndexOf("my name is") + "my name is".Length;
                string extracted = normalized.Substring(idx).Trim().Split(' ')[0];
                if (!string.IsNullOrEmpty(extracted))
                {
                    Memory.Name = Capitalise(extracted);
                    return $"Nice to meet you, {Memory.Name}! I'll remember your name.";
                }
            }

            return null;
        }

        private string HandleSpecial(string normalized, string[] tokens, string personality)
        {
            string n = string.IsNullOrEmpty(Memory.Name) ? "there" : Memory.Name;

            if (normalized.Contains("how are you"))
                return Speak("I'm doing great, ready to help keep you safe online!", personality);
            if (normalized.Contains("what is your purpose") || normalized.Contains("what do you do"))
                return Speak("I'm your Cybersecurity Awareness Bot — here to educate and protect you online.", personality);
            if (normalized.Contains("who are you") || normalized.Contains("your name"))
                return Speak("I'm CyberBot, your personal cybersecurity guide!", personality);
            if (tokens.Contains("hello") || tokens.Contains("hi") || tokens.Contains("hey"))
                return Speak($"Hey {n}! What cybersecurity topic can I help you with today?", personality);
            if (normalized.Contains("thank"))
                return Speak($"You're welcome, {n}! Stay safe out there.", personality);
            if (normalized.Contains("remember") && normalized.Contains("about me"))
            {
                string recallName = string.IsNullOrEmpty(Memory.Name) ? "not set" : Memory.Name;
                string recallTopic = string.IsNullOrEmpty(Memory.FavouriteTopic) ? "not set" : Memory.FavouriteTopic;
                return $"Here's what I remember about you:\n• Name: {recallName}\n• Favourite topic: {recallTopic}";
            }

            return null;
        }

        private static string DetectTopic(string normalized)
        {
            if (normalized.Contains("password")) return "password";
            if (normalized.Contains("phishing")) return "phishing";
            if (normalized.Contains("malware") || normalized.Contains("virus") || normalized.Contains("ransomware")) return "malware";
            if (normalized.Contains("vpn")) return "vpn";
            if (normalized.Contains("2fa") || normalized.Contains("two factor") || normalized.Contains("two-factor")) return "2fa";
            if (normalized.Contains("firewall")) return "firewall";
            if (normalized.Contains("encryption") || normalized.Contains("encrypt")) return "encryption";
            if (normalized.Contains("privacy")) return "privacy";
            if (normalized.Contains("scam") || normalized.Contains("fraud")) return "scam";
            return "";
        }

        private string BuildTopicResponse(string topic, bool isHow, bool isWhy, bool isWhat,
                                          bool isWhen, bool wantsTip, string personality)
        {
            if (wantsTip && RandomPools.ContainsKey(topic))
                return PickRandom(topic) + "\n\n💬 Say 'give me another tip' for a different one!";

            switch (topic)
            {
                case "password":
                    if (isHow) return Speak("Use a mix of uppercase, lowercase, numbers and symbols. Aim for 12+ characters or use a passphrase.", personality);
                    if (isWhy) return Speak("Strong passwords prevent brute-force attacks and stop hackers getting into your accounts.", personality);
                    if (isWhat) return Speak("A password is a secret credential used to verify your identity when logging into a service.", personality);
                    if (isWhen) return Speak("Change your passwords immediately after a suspected breach and every 3–6 months for important accounts.", personality);
                    return Speak("Passwords are your first line of defence. " + PickRandom("password"), personality)
                         + "\n💬 Try asking: how should I create a password? | why are passwords important?";

                case "phishing":
                    if (isHow) return Speak("Verify sender addresses, hover over links before clicking, and never open unexpected attachments.", personality);
                    if (isWhy) return Speak("Phishing tricks you into handing over credentials or installing malware, leading to identity theft.", personality);
                    if (isWhat) return Speak("Phishing is a fraudulent attack that impersonates trusted parties to steal sensitive information.", personality);
                    return Speak(PickRandom("phishing"), personality)
                         + "\n💬 Try asking: what is phishing? | how do I avoid phishing?";

                case "malware":
                    if (isHow) return Speak("Install reputable antivirus software, keep everything updated, and avoid unknown downloads.", personality);
                    if (isWhy) return Speak("Malware can steal data, encrypt files for ransom, or give attackers full control of your device.", personality);
                    if (isWhat) return Speak("Malware is malicious software designed to harm your system — including viruses, trojans and ransomware.", personality);
                    if (isWhen) return Speak("Malware most often enters through email attachments, fake downloads, and malicious websites.", personality);
                    return Speak(PickRandom("malware"), personality)
                         + "\n💬 Try asking: what is malware? | how do I protect against malware?";

                case "vpn":
                    if (isHow) return Speak("Download a reputable VPN app, connect before using public Wi-Fi, and keep it on when browsing.", personality);
                    if (isWhy) return Speak("A VPN hides your activity from your ISP and anyone else on the same network.", personality);
                    if (isWhat) return Speak("A VPN (Virtual Private Network) creates an encrypted tunnel for your internet traffic.", personality);
                    return Speak("A VPN encrypts your network traffic and is especially important on public Wi-Fi.\n💬 Try: what is a VPN? | why should I use a VPN?", personality);

                case "2fa":
                    if (isHow) return Speak("Go to your account's security settings and enable 2FA. Use an authenticator app like Google Authenticator.", personality);
                    if (isWhy) return Speak("2FA means an attacker who steals your password still can't log in without the second factor.", personality);
                    if (isWhat) return Speak("Two-factor authentication requires a second proof of identity — usually a code — in addition to your password.", personality);
                    return Speak("2FA is one of the most effective security measures available. Enable it on every account that supports it.\n💬 Try: what is 2FA? | how do I set up 2FA?", personality);

                case "firewall":
                    if (isHow) return Speak("Keep your firewall enabled and configure rules to block unwanted traffic. Don't disable it.", personality);
                    if (isWhy) return Speak("A firewall blocks unauthorised connections and prevents malicious traffic from reaching your device.", personality);
                    if (isWhat) return Speak("A firewall monitors and controls incoming and outgoing network traffic based on security rules.", personality);
                    return Speak("Firewalls are your network's gatekeeper. Keep yours on at all times.\n💬 Try: what is a firewall? | why should I use a firewall?", personality);

                case "encryption":
                    if (isHow) return Speak("Use services that offer end-to-end encryption (e.g. Signal) and enable full-disk encryption on your device.", personality);
                    if (isWhy) return Speak("Encryption ensures that even if data is intercepted, it cannot be read without the decryption key.", personality);
                    if (isWhat) return Speak("Encryption converts readable data into a scrambled format that can only be decoded with the correct key.", personality);
                    return Speak("Encryption is the backbone of online security — it protects your data in transit and at rest.\n💬 Try: what is encryption? | why is encryption important?", personality);

                case "privacy":
                    if (isHow) return Speak("Limit what you share online, review app permissions regularly, and use privacy-focused browsers.", personality);
                    if (isWhy) return Speak("Protecting your privacy reduces the risk of identity theft, targeted scams and unwanted data profiling.", personality);
                    return Speak("Your personal data is valuable. Review privacy settings on all your accounts and only share what's necessary.\n💬 Try: how do I protect my privacy? | why does privacy matter?", personality);

                case "scam":
                    if (isHow) return Speak("Verify requests independently, never share credentials, and be sceptical of urgent or too-good-to-be-true offers.", personality);
                    if (isWhy) return Speak("Scammers exploit trust and urgency to steal money or personal data from victims.", personality);
                    return Speak(PickRandom("scam"), personality)
                         + "\n💬 Try: how do I avoid scams? | why are scams dangerous?";

                default:
                    return "I'm not sure I understand. Can you try rephrasing? You can ask about: passwords, phishing, malware, VPN, 2FA, firewalls, encryption, scams, or privacy.";
            }
        }

        private string PickRandom(string topic)
        {
            if (!RandomPools.TryGetValue(topic, out List<string> pool) || pool.Count == 0) return "";
            return pool[_rng.Next(pool.Count)];
        }

        private static string Speak(string body, string personality)
        {
            switch (personality)
            {
                case "professional": return body;
                case "ai": return "[CYBERBOT-AI] " + body;
                case "casual": return "Hey! " + body;
                default: return "😊 " + body;
            }
        }

        private static string Capitalise(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }
}
