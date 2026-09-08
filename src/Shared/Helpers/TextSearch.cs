using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Whitestone.SegnoSharp.Database.Models;

namespace Whitestone.SegnoSharp.Shared.Helpers
{
    /// <summary>
    /// Stage-two (in-memory) fuzzy matching plus the portable stage-one LIKE predicates.
    /// Stage one runs in the database to gather a candidate set; stage two ranks the
    /// survivors here in C#, so none of the fuzzy logic depends on the SQL provider.
    ///
    /// Stage one comes in three tiers of decreasing precision — <see cref="TitlePhraseLike"/>,
    /// <see cref="TitleAllTokensLike"/>, <see cref="TitleLike"/>. The caller tries them in
    /// order and stops at the first that returns anything. This matters because the caller
    /// caps the candidate set before ranking: with only the broad OR tier, a query of common
    /// words ("now we are free") fills the cap with rows matched on "%we%" and the real track
    /// never reaches the scorer. The loose tier is still kept last, because a misspelling
    /// ("Gladeator") produces nothing in the stricter tiers and only fuzzy scoring can recover it.
    /// </summary>
    public static class TextSearch
    {
        // Common words that match half the library and only bloat the candidate set.
        // Dropped from query tokens, but titles keep them so the order check still works.
        private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
        {
            "the", "a", "an", "of", "and", "in", "on", "to", "for", "from"
        };

        /// <summary>Lowercase, strip diacritics, turn punctuation into spaces.</summary>
        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            string decomposed = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (char ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                {
                    continue; // drop accents
                }

                sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ');
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Normalized text collapsed to single spaces, for the exact-phrase candidate tier.
        /// Stopwords are deliberately kept here: the phrase tier is matched as one contiguous
        /// substring, so dropping "the" would stop "Fellowship of the Ring" matching itself.
        /// </summary>
        public static string NormalizePhrase(string s)
        {
            string[] parts = Normalize(s).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', parts);
        }

        public static List<string> Tokenize(string s, bool dropStopWords = true)
        {
            string[] parts = Normalize(s).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (!dropStopWords)
            {
                return parts.ToList();
            }

            var kept = parts.Where(p => !StopWords.Contains(p)).ToList();
            // If the query was nothing but stopwords, keep them rather than searching for nothing.
            return kept.Count > 0 ? kept : parts.ToList();
        }

        /// <summary>
        /// Score a title against the query on a 0..1 scale, combining two views:
        ///   recall    — how well every query token is covered by the title (typo-tolerant);
        ///   precision — how much of the title is actually accounted for by the query.
        /// Recall answers "did we find the user's words?"; precision is what separates an
        /// exact "Gladiator" from "Gladiator II" or "Gladiator (The Complete Rejected Score)",
        /// which all have full recall but carry extra, unmatched words. Recall is weighted
        /// higher so a correct-but-verbose title still scores well; precision breaks the tie.
        /// The result is in [0,1] by construction (no bonus that overshoots and gets clamped).
        /// </summary>
        public static (double Score, string MatchedOn) ScoreTitle(string query, string title)
        {
            List<string> q = Tokenize(query, dropStopWords: true);
            List<string> t = Tokenize(title, dropStopWords: true);

            if (q.Count == 0 || t.Count == 0)
            {
                return (0, string.Empty);
            }

            // Recall: average best match of each query token against the title.
            double recallSum = 0;
            var matched = new List<string>();
            foreach (string qt in q)
            {
                double best = 0;
                string bestTok = null;
                foreach (string tt in t)
                {
                    double sim = TokenMatch(qt, tt);
                    if (sim > best)
                    {
                        best = sim;
                        bestTok = tt;
                    }
                }

                recallSum += best;
                if (best >= 0.99 && bestTok != null)
                {
                    matched.Add(bestTok);
                }
            }
            double recall = recallSum / q.Count;

            // Precision: average best match of each title token against the query.
            // Extra title words (II, "complete rejected score", ...) match nothing here
            // and drag the average down, which is exactly the discrimination we want.
            double precisionSum = 0;
            foreach (string tt in t)
            {
                double best = 0;
                foreach (string qt in q)
                {
                    double sim = TokenMatch(qt, tt);
                    if (sim > best)
                    {
                        best = sim;
                    }
                }

                precisionSum += best;
            }
            double precision = precisionSum / t.Count;

            double score = (0.7 * recall) + (0.3 * precision);

            // Small nudge down if the shared tokens appear out of order in the title.
            if (!ContainsInOrder(t, q))
            {
                score *= 0.95;
            }

            score = Math.Clamp(score, 0, 1);

            string matchedOn = matched.Count > 0
                ? "matched tokens: " + string.Join(", ", matched.Distinct())
                : "fuzzy match";

            return (score, matchedOn);
        }

        /// <summary>Token-to-token match: 1.0 on a prefix relationship, else normalized Levenshtein.</summary>
        private static double TokenMatch(string a, string b) =>
            a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal)
                ? 1.0
                : Similarity(a, b);

        /// <summary>Do the query tokens appear, in order, as a (prefix-matched) subsequence of the title tokens?</summary>
        private static bool ContainsInOrder(List<string> titleTokens, List<string> queryTokens)
        {
            int qi = 0;
            foreach (string tt in titleTokens)
            {
                if (qi >= queryTokens.Count)
                {
                    break;
                }

                if (tt.StartsWith(queryTokens[qi], StringComparison.Ordinal) ||
                    queryTokens[qi].StartsWith(tt, StringComparison.Ordinal))
                {
                    qi++;
                }
            }

            return qi == queryTokens.Count;
        }

        /// <summary>Normalized Levenshtein similarity, 0..1. Dependency-free.</summary>
        public static double Similarity(string a, string b)
        {
            if (a == b)
            {
                return 1;
            }

            if (a.Length == 0 || b.Length == 0)
            {
                return 0;
            }

            int distance = Levenshtein(a, b);
            return 1.0 - (double)distance / Math.Max(a.Length, b.Length);
        }

        private static int Levenshtein(string a, string b)
        {
            int[] prev = new int[b.Length + 1];
            int[] curr = new int[b.Length + 1];

            for (int j = 0; j <= b.Length; j++)
            {
                prev[j] = j;
            }

            for (int i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }

                (prev, curr) = (curr, prev);
            }

            return prev[b.Length];
        }

        /// <summary>
        /// Stage-one tier 1 (tightest): the whole normalized query as one contiguous substring.
        /// Matches "Now We Are Free" and "Now We Are Free (Reprise)" but nothing built from the
        /// individual words, so the candidate cap fills with rows that are actually plausible.
        /// Only the query side is normalized — the database side is a plain LOWER() — so a title
        /// whose punctuation sits inside the phrase ("Rock'n'Roll" vs "rock n roll") falls
        /// through to a looser tier rather than matching here.
        /// </summary>
        public static Expression<Func<Track, bool>> TitlePhraseLike(string phrase)
        {
            string pattern = "%" + phrase + "%";
            return t => EF.Functions.Like(t.Title.ToLower(), pattern);
        }

        /// <summary>
        /// Stage-one tier 2: AND of one LIKE per token. Every query word must appear somewhere
        /// in the title, in any order, which catches "Free Now We Are" and subtitle reorderings
        /// that the phrase tier misses, while still excluding titles that share only one word.
        /// </summary>
        public static Expression<Func<Track, bool>> TitleAllTokensLike(IReadOnlyList<string> tokens)
        {
            Expression<Func<Track, bool>> predicate = PredicateBuilder.True<Track>();

            foreach (string token in tokens)
            {
                string pattern = "%" + token + "%";
                predicate = predicate.And(t => EF.Functions.Like(t.Title.ToLower(), pattern));
            }

            return predicate;
        }

        /// <summary>
        /// Stage-one tier 3 (loosest): OR of one LIKE per token — deliberately over-broad, and
        /// the only tier that can survive a misspelled word, since the in-memory scorer is
        /// typo-tolerant but the database LIKE is not. Both sides are lowercased so it stays
        /// case-insensitive across every provider (Postgres LIKE is case-sensitive otherwise).
        /// This is the one provider-touching spot; on SQLite, LOWER() is ASCII-only, so
        /// non-ASCII case variants may be missed at this stage (the in-memory scorer still
        /// normalizes them). Postgres users who care about that can swap in ILIKE / citext.
        /// </summary>
        public static Expression<Func<Track, bool>> TitleLike(IReadOnlyList<string> tokens)
        {
            Expression<Func<Track, bool>> predicate = PredicateBuilder.False<Track>();

            foreach (string token in tokens)
            {
                string pattern = "%" + token + "%";
                predicate = predicate.Or(t => EF.Functions.Like(t.Title.ToLower(), pattern));
            }

            return predicate;
        }
    }

    /// <summary>Minimal AND/OR-combining predicate builder so we don't need LinqKit.</summary>
    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> False<T>() => _ => false;

        public static Expression<Func<T, bool>> True<T>() => _ => true;

        public static Expression<Func<T, bool>> Or<T>(
            this Expression<Func<T, bool>> a,
            Expression<Func<T, bool>> b)
        {
            return Combine(a, b, Expression.OrElse);
        }

        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> a,
            Expression<Func<T, bool>> b)
        {
            return Combine(a, b, Expression.AndAlso);
        }

        private static Expression<Func<T, bool>> Combine<T>(
            Expression<Func<T, bool>> a,
            Expression<Func<T, bool>> b,
            Func<Expression, Expression, BinaryExpression> join)
        {
            var parameter = Expression.Parameter(typeof(T));

            Expression left = new ReplaceParameterVisitor(a.Parameters[0], parameter).Visit(a.Body)!;
            Expression right = new ReplaceParameterVisitor(b.Parameters[0], parameter).Visit(b.Body)!;

            return Expression.Lambda<Func<T, bool>>(join(left, right), parameter);
        }

        private sealed class ReplaceParameterVisitor : ExpressionVisitor
        {
            private readonly Expression _from;
            private readonly Expression _to;

            public ReplaceParameterVisitor(Expression from, Expression to)
            {
                _from = from;
                _to = to;
            }

            public override Expression Visit(Expression node) =>
                node == _from ? _to : base.Visit(node);
        }
    }
}