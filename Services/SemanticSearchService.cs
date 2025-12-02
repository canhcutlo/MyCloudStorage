using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CloudStorage.Services
{
    public interface ISemanticSearchService
    {
        string NormalizeVietnamese(string text);
        double CalculateSimilarity(string text1, string text2);
        bool IsSemanticMatch(string searchQuery, string targetText, double threshold = 0.6);
    }

    public class SemanticSearchService : ISemanticSearchService
    {
        private static readonly Dictionary<char, char> VietnameseCharMap = new()
        {
            // Lowercase vowels with accents
            {'á', 'a'}, {'à', 'a'}, {'ả', 'a'}, {'ã', 'a'}, {'ạ', 'a'},
            {'ă', 'a'}, {'ắ', 'a'}, {'ằ', 'a'}, {'ẳ', 'a'}, {'ẵ', 'a'}, {'ặ', 'a'},
            {'â', 'a'}, {'ấ', 'a'}, {'ầ', 'a'}, {'ẩ', 'a'}, {'ẫ', 'a'}, {'ậ', 'a'},
            {'é', 'e'}, {'è', 'e'}, {'ẻ', 'e'}, {'ẽ', 'e'}, {'ẹ', 'e'},
            {'ê', 'e'}, {'ế', 'e'}, {'ề', 'e'}, {'ể', 'e'}, {'ễ', 'e'}, {'ệ', 'e'},
            {'í', 'i'}, {'ì', 'i'}, {'ỉ', 'i'}, {'ĩ', 'i'}, {'ị', 'i'},
            {'ó', 'o'}, {'ò', 'o'}, {'ỏ', 'o'}, {'õ', 'o'}, {'ọ', 'o'},
            {'ô', 'o'}, {'ố', 'o'}, {'ồ', 'o'}, {'ổ', 'o'}, {'ỗ', 'o'}, {'ộ', 'o'},
            {'ơ', 'o'}, {'ớ', 'o'}, {'ờ', 'o'}, {'ở', 'o'}, {'ỡ', 'o'}, {'ợ', 'o'},
            {'ú', 'u'}, {'ù', 'u'}, {'ủ', 'u'}, {'ũ', 'u'}, {'ụ', 'u'},
            {'ư', 'u'}, {'ứ', 'u'}, {'ừ', 'u'}, {'ử', 'u'}, {'ữ', 'u'}, {'ự', 'u'},
            {'ý', 'y'}, {'ỳ', 'y'}, {'ỷ', 'y'}, {'ỹ', 'y'}, {'ỵ', 'y'},
            {'đ', 'd'},
            // Uppercase vowels with accents
            {'Á', 'A'}, {'À', 'A'}, {'Ả', 'A'}, {'Ã', 'A'}, {'Ạ', 'A'},
            {'Ă', 'A'}, {'Ắ', 'A'}, {'Ằ', 'A'}, {'Ẳ', 'A'}, {'Ẵ', 'A'}, {'Ặ', 'A'},
            {'Â', 'A'}, {'Ấ', 'A'}, {'Ầ', 'A'}, {'Ẩ', 'A'}, {'Ẫ', 'A'}, {'Ậ', 'A'},
            {'É', 'E'}, {'È', 'E'}, {'Ẻ', 'E'}, {'Ẽ', 'E'}, {'Ẹ', 'E'},
            {'Ê', 'E'}, {'Ế', 'E'}, {'Ề', 'E'}, {'Ể', 'E'}, {'Ễ', 'E'}, {'Ệ', 'E'},
            {'Í', 'I'}, {'Ì', 'I'}, {'Ỉ', 'I'}, {'Ĩ', 'I'}, {'Ị', 'I'},
            {'Ó', 'O'}, {'Ò', 'O'}, {'Ỏ', 'O'}, {'Õ', 'O'}, {'Ọ', 'O'},
            {'Ô', 'O'}, {'Ố', 'O'}, {'Ồ', 'O'}, {'Ổ', 'O'}, {'Ỗ', 'O'}, {'Ộ', 'O'},
            {'Ơ', 'O'}, {'Ớ', 'O'}, {'Ờ', 'O'}, {'Ở', 'O'}, {'Ỡ', 'O'}, {'Ợ', 'O'},
            {'Ú', 'U'}, {'Ù', 'U'}, {'Ủ', 'U'}, {'Ũ', 'U'}, {'Ụ', 'U'},
            {'Ư', 'U'}, {'Ứ', 'U'}, {'Ừ', 'U'}, {'Ử', 'U'}, {'Ữ', 'U'}, {'Ự', 'U'},
            {'Ý', 'Y'}, {'Ỳ', 'Y'}, {'Ỷ', 'Y'}, {'Ỹ', 'Y'}, {'Ỵ', 'Y'},
            {'Đ', 'D'}
        };

        private static readonly Dictionary<string, string[]> VietnameseAbbreviations = new()
        {
            {"thang", new[] {"T", "t"}},
            {"nam", new[] {"N", "n"}},
            {"quy", new[] {"Q", "q"}},
            {"tuan", new[] {"W", "w"}},
            {"ngay", new[] {"D", "d"}},
            {"bao cao", new[] {"BC", "bc", "baocao"}},
            {"tai chinh", new[] {"TC", "tc", "taichinh"}},
            {"hop dong", new[] {"HD", "hd", "hopdong"}},
            {"van ban", new[] {"VB", "vb", "vanban"}},
            {"de an", new[] {"DA", "da", "dean"}},
            {"du an", new[] {"DA", "da", "duan"}},
            {"ke hoach", new[] {"KH", "kh", "kehoach"}},
            {"bao gia", new[] {"BG", "bg", "baogia"}},
            {"hop tac", new[] {"HT", "ht", "hoptac"}},
            {"kinh doanh", new[] {"KD", "kd", "kinhdoanh"}},
            {"nhan su", new[] {"NS", "ns", "nhansu"}},
            {"hanh chinh", new[] {"HC", "hc", "hanhchinh"}},
            {"cong ty", new[] {"CT", "ct", "congty"}},
            {"giam doc", new[] {"GD", "gd", "giamdoc"}},
            {"tong giam doc", new[] {"TGD", "tgd", "tonggiamdoc"}},
            {"phong", new[] {"P", "p"}},
            {"don vi", new[] {"DV", "dv", "donvi"}},
            {"so", new[] {"S", "s"}},
            {"chi nhanh", new[] {"CN", "cn", "chinhanh"}},
            {"van phong", new[] {"VP", "vp", "vanphong"}}
        };

        /// <summary>
        /// Removes Vietnamese accents and normalizes text for comparison
        /// </summary>
        public string NormalizeVietnamese(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (VietnameseCharMap.TryGetValue(c, out char normalized))
                {
                    sb.Append(normalized);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Tokenizes text into meaningful words
        /// </summary>
        private List<string> Tokenize(string text)
        {
            // Replace common separators with spaces
            text = Regex.Replace(text, @"[_\-\.\,\;\:\(\)\[\]\{\}\/\\]", " ");
            
            // Split on whitespace and filter empty strings
            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(t => t.ToLowerInvariant())
                       .ToList();
        }

        /// <summary>
        /// Expands abbreviations to full Vietnamese words
        /// </summary>
        private List<string> ExpandAbbreviations(List<string> tokens)
        {
            var expanded = new List<string>(tokens);
            
            foreach (var token in tokens)
            {
                foreach (var abbr in VietnameseAbbreviations)
                {
                    if (abbr.Value.Contains(token, StringComparer.OrdinalIgnoreCase))
                    {
                        expanded.Add(abbr.Key);
                        break;
                    }
                }
            }

            return expanded;
        }

        /// <summary>
        /// Calculates similarity between two texts using token-based matching
        /// </summary>
        public double CalculateSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0.0;

            // Normalize both texts
            var normalized1 = NormalizeVietnamese(text1);
            var normalized2 = NormalizeVietnamese(text2);

            // Tokenize
            var tokens1 = Tokenize(normalized1);
            var tokens2 = Tokenize(normalized2);

            // Expand abbreviations
            tokens1 = ExpandAbbreviations(tokens1);
            tokens2 = ExpandAbbreviations(tokens2);

            if (!tokens1.Any() || !tokens2.Any())
                return 0.0;

            // Calculate Jaccard similarity
            var intersection = tokens1.Intersect(tokens2, StringComparer.OrdinalIgnoreCase).Count();
            var union = tokens1.Union(tokens2, StringComparer.OrdinalIgnoreCase).Count();

            double jaccardSimilarity = union > 0 ? (double)intersection / union : 0.0;

            // Bonus for exact substring matches (after normalization)
            var norm1Lower = normalized1.ToLowerInvariant();
            var norm2Lower = normalized2.ToLowerInvariant();
            
            double substringBonus = 0.0;
            if (norm1Lower.Contains(norm2Lower) || norm2Lower.Contains(norm1Lower))
            {
                substringBonus = 0.3;
            }

            // Bonus for number matches (useful for dates, quarters, etc.)
            var numbers1 = Regex.Matches(text1, @"\d+").Select(m => m.Value).ToHashSet();
            var numbers2 = Regex.Matches(text2, @"\d+").Select(m => m.Value).ToHashSet();
            
            double numberBonus = 0.0;
            if (numbers1.Any() && numbers2.Any())
            {
                var numberIntersection = numbers1.Intersect(numbers2).Count();
                var numberUnion = numbers1.Union(numbers2).Count();
                numberBonus = numberUnion > 0 ? (double)numberIntersection / numberUnion * 0.2 : 0.0;
            }

            return Math.Min(1.0, jaccardSimilarity + substringBonus + numberBonus);
        }

        /// <summary>
        /// Determines if a search query semantically matches a target text
        /// </summary>
        public bool IsSemanticMatch(string searchQuery, string targetText, double threshold = 0.6)
        {
            var similarity = CalculateSimilarity(searchQuery, targetText);
            return similarity >= threshold;
        }
    }
}
