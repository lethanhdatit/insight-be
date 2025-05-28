using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

public static partial class StringExtensions
{
    private static readonly Random random = new Random();
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:'\",.<>?/`~";
    private const string Digits = "0123456789";
    private const string UppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string LowercaseLetters = "abcdefghijklmnopqrstuvwxyz";
    private static readonly Regex UserNameAcceptedPattern = new(@"^(?!.*[_\-\.]{2})(?![_\-\.])[a-zA-Z0-9\uAC00-\uD7AF\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF\u1780-\u17FF_\-\.]*(?<![_\-\.])$", RegexOptions.Compiled);
    private static readonly Regex InvalidCharsPattern = new(@"[^a-zA-Z0-9\uAC00-\uD7AF\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FFF\u1780-\u17FF_\-\. ]", RegexOptions.Compiled); // Spaces allowed
    private static readonly Regex ConsecutiveInvalidCharsPattern = new(@"[_\-\.]{2,}", RegexOptions.Compiled);
    private static readonly Regex StartEndInvalidCharsPattern = new(@"^[_\-\.]+|[_\-\.]+$", RegexOptions.Compiled);

    public static bool IsMissing(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static bool IsPresent(this string value)
    {
        return !value.IsMissing();
    }

    public static bool HasQuery(this string url)
    {
        string decodedUrl = url.TryUrlDecode();

        if (Uri.TryCreate(decodedUrl, UriKind.Absolute, out var uri))
        {
            return uri.Query.IsPresent();
        }

        return false;
    }

    public static string TryUrlDecode(this string url)
    {
        return IsEncoded(url) ? Uri.UnescapeDataString(url) : url;
    }

    private static bool IsEncoded(string url)
    {
        return url.Contains('%') || url.Contains('+');
    }

    public static (bool, string) CompareWords(this string source, string target, string replaceBy = null)
    {
        if (source.IsMissing() || target.IsMissing())
            return (source == target, source);

        var words1 = source.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var words2 = target.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var lowerWords1 = words1.Select(w => w.ToLower()).ToArray();
        var lowerWords2 = words2.Select(w => w.ToLower()).ToArray();

        for (int i = 0; i <= lowerWords1.Length - lowerWords2.Length; i++)
        {
            bool match = true;

            for (int j = 0; j < lowerWords2.Length; j++)
            {
                if (lowerWords1[i + j] != lowerWords2[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                if (replaceBy.IsPresent())
                {
                    for (int j = 0; j < lowerWords2.Length; j++)
                    {
                        int originalWordLength = words1[i + j].Length;
                        if (replaceBy.Length < originalWordLength)
                        {
                            replaceBy = string.Concat(Enumerable.Repeat(replaceBy, (originalWordLength / replaceBy.Length) + 1))
                                          .Substring(0, originalWordLength);
                        }

                        words1[i + j] = replaceBy;
                    }
                }

                return (true, string.Join(" ", words1));
            }
        }

        return (false, source);
    }

    public static string JoinString(this string value, string separator = ",", params string[] paths)
    {
        List<string> temp = [value, .. paths];
        temp = temp.Select(s => s.Trim()).Where(w => w.IsPresent()).ToList();

        return string.Join(separator, temp);
    }

    public static bool HasVisibleInnerText(this string input)
    {
        if (input.IsMissing())
            return false;

        string textOnly = Regex.Replace(input, "<.*?>", string.Empty);

        textOnly = textOnly.Trim('"');

        return textOnly.IsPresent();
    }

    public static string WithPath(this string source, params string[] paths)
    {
        List<string> lst = [];
        source = source?.SlashTrim();

        if (source.IsPresent())
            lst.Add(source);

        if(paths != null)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i]?.SlashTrim();
                if (path.IsPresent())
                    lst.Add(path);
            }
        }

        return string.Join("/", lst.ToArray());
    }

    public static string WithQuery(this string source, string query)
    {
        query = query.SlashTrim();
        source = source.SlashTrim();

        if(!query.StartsWith("?"))
            query = $"?{query}";

        return source + query;
    }

    public static string StandardizePhoneNumber(this string phoneNumber)
    {
        if (phoneNumber.IsMissing())
            return phoneNumber;

        phoneNumber = phoneNumber.Replace(" ", string.Empty)
                                 .Replace("+", string.Empty);

        if (phoneNumber.StartsWith("840"))
            phoneNumber = $"{phoneNumber[2..]}";
        else if (phoneNumber.StartsWith("84"))
            phoneNumber = $"0{phoneNumber[2..]}";
        else if (phoneNumber.StartsWith("600"))
            phoneNumber = $"{phoneNumber[2..]}";
        else if (phoneNumber.StartsWith("60"))
            phoneNumber = $"0{phoneNumber[2..]}";

        return phoneNumber;
    }

    public static string WithQuery<T>(this string source, T queryObject)
    {
        if (queryObject == null)
        {
            throw new ArgumentNullException(nameof(queryObject));
        }

        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                  .Where(p => p.GetValue(queryObject) != null) // Exclude null values
                                  .Select(p => $"{p.Name.UrlEncode()}={p.GetValue(queryObject)?.ToString().UrlEncode()}");

        var query = string.Join("&", properties);

        return source.WithQuery(query);
    }

    public static string PathAt(this string path, byte levels, char separator = '.')
    {
        if (levels <= 0)
        {
            throw new ArgumentException("Levels must be greater than 0.");
        }

        // Trim any leading or trailing dots
        path = path.Trim(separator);

        // Split the path into parts based on the dot separator
        var parts = path.Split(separator);

        // Ensure not to exceed the number of actual parts
        int maxLevels = Math.Min(parts.Length, levels);

        // If there are enough parts, return the requested number of levels
        if (maxLevels > 0)
        {
            return string.Join(separator, parts, 0, maxLevels);
        }

        // If there are no parts or parts are less than requested, return the entire path
        return path;
    }

    public static string HtmlDecode(this string input)
    {
        if (input.IsPresent())
            return WebUtility.HtmlDecode(input);

        return string.Empty;
    }

    public static string HtmlEncode(this string input)
    {
        if (input.IsPresent())
            return WebUtility.HtmlEncode(input);

        return string.Empty;
    }

    public static string UrlEncode(this string input)
    {
        if (input.IsPresent())
            return WebUtility.UrlEncode(input);

        return string.Empty;
    }

    public static string CodeAt(this string path, byte level, char separator = '.')
    {
        if (path.IsMissing())
            return string.Empty;

        if (level <= 0)
        {
            throw new ArgumentException("Level must be greater than 0.");
        }

        // Trim any leading or trailing dots
        path = path.Trim(separator);

        // Split the path into parts based on the dot separator
        var parts = path.Split(separator);

        // Check if the desired level exists in the parts array (0-based index)
        if (level <= parts.Length)
        {
            return parts[level - 1].ToUpper();
        }

        // Return an empty string if the requested level is not present
        return string.Empty;
    }

    public static string NearestParentCode(this string path, char separator = '.')
    {
        // Trim any leading or trailing dots
        path = path.Trim(separator);

        // Split the path into parts based on the dot separator
        var parts = path.Split(separator);

        if(parts.Length > 1)
        {
            return parts[^2];
        }

        // Return an empty string if the requested level is not present
        return string.Empty;
    }

    public static DateTimeOffset ToDateTimeOffset(this string timeString, DateTime? date = null)
    {
        // If no date is provided, use the current date
        DateTime baseDate = date ?? DateTime.UtcNow.Date;

        // Handling "24:00" as "00:00" of the next day
        if (timeString == "24:00")
        {
            return new DateTimeOffset(baseDate.AddDays(1).Date, TimeSpan.Zero);
        }
        else
        {
            TimeSpan time = TimeSpan.Parse(timeString);
            return new DateTimeOffset(baseDate.Date + time, TimeSpan.Zero);
        }
    }

    public static bool IsUrlEncoded(this string input)
    {
        return RegexIsUrlEncoded().IsMatch(input);
    }

    public static string Standardize(this string input)
    {
        if (input.IsMissing())
        {
            return input;
        }

        // Convert to lowercase
        string result = input.ToLowerInvariant();

        // Remove special characters
        result = Regex.Replace(result, @"[^\w\s]", "");

        // Normalize whitespace
        result = Regex.Replace(result, @"\s+", " ").Trim();

        return result;
    }

    public static List<string> ExtractHashtags(this string input, bool isDistinct = false)
    {
        if (input.IsMissing())
        {
            return [];
        }

        input = input.Trim();

        var regex = new Regex(@"#\w+");
        var matches = regex.Matches(input);

        return isDistinct ? matches.Select(m => m.Value.ToLower()).Distinct().ToList() : matches.Select(m => m.Value).ToList();
    }

    /// <summary>
    /// Converts a search string to a PostgreSQL TSQUERY format with prefix matching.
    /// </summary>
    /// <param name="searchTerm">The user's search term.</param>
    /// <returns>A formatted string suitable for to_tsquery with prefix matching.</returns>
    public static string ToTsQuery(this string searchTerm, bool anyKeyword = false)
    {
        if (searchTerm.IsMissing())
            return null;

        searchTerm = searchTerm.Standardize();

        var terms = searchTerm.Trim().Split(new[] { ' ', ',', '.', ';', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        var formattedTerms = terms.Select(term => $"{term}:*").ToArray();

        var condition = anyKeyword ? "|" : "&";

        return string.Join($" {condition} ", formattedTerms).ToLower();
    }

    public static string RemoveDiacritics(this string text, bool removeSpace = false, bool removeSpecialCharacters = true)
    {
        var stringBuilder = new StringBuilder();
        foreach (var c in text)
        {
            if (SpecialReplacements.TryGetValue(c, out var replacement))
            {
                stringBuilder.Append(replacement);
            }
            else
            {
                stringBuilder.Append(c);
            }
        }

        var normalizedString = stringBuilder.ToString().Normalize(NormalizationForm.FormD);
        stringBuilder.Clear();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

        if (removeSpecialCharacters)
        {
            result = result.StandardizeString(removeSpace);
        }

        return removeSpace ? result.Replace(" ", "_") : result;
    }

    public static string StandardizeString(this string input, bool removeSpace = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove invalid characters (apostrophe and other disallowed chars)
        input = InvalidCharsPattern.Replace(input, "");

        // Remove spaces if the option is enabled
        if (removeSpace)
        {
            input = input.Replace(" ", "_");
        }

        // Remove consecutive invalid characters
        input = ConsecutiveInvalidCharsPattern.Replace(input, "_");

        // Remove invalid characters at start or end
        input = StartEndInvalidCharsPattern.Replace(input, "");

        // Ensure final string matches the accepted pattern
        return UserNameAcceptedPattern.IsMatch(input) ? input : string.Empty;
    }

    public static string RemoveFromSuffix(this string input, string suffix)
    {
        int index = input.IndexOf(suffix, StringComparison.Ordinal);
        if (index >= 0)
        {
            return input[..index];
        }

        return input;
    }


    public static (string FirstName, string MiddleName, string LastName) FromFullName(this string username)
    {
        if (username.IsMissing())
            return (username, "", "");

        List<string> words = [];

        if (username.Contains(" "))
        {
            words = [.. username.Split(" ", StringSplitOptions.RemoveEmptyEntries)];
            username = username.Replace(" ", string.Empty);
        }
        else
        {
            // Kiểm tra xem username có ký tự viết hoa hay không
            if (!username.Any(char.IsUpper) || username.All(char.IsUpper))
                return (username, "", "");

            words = Regex.Matches(username, @"([A-Z][a-z]*)").Cast<Match>().Select(m => m.Value).ToList();
        }

        // Tách từ        
        int totalLength = words.Sum(w => w.Length);

        // Kiểm tra chiều dài tổng của từ có bằng với chiều dài của username không
        if (totalLength != username.Length)
            return (username, "", "");

        string firstName = "";
        string lastName = "";
        string middleName = "";

        int wordCount = words.Count;

        if (wordCount == 1)
        {
            firstName = words[0];
        }
        else if (wordCount == 2)
        {
            firstName = words[0];
            lastName = words[1];
        }
        else if (wordCount >= 3)
        {
            firstName = words[0];
            lastName = words[wordCount - 1];
            middleName = string.Join("", words.Skip(1).Take(wordCount - 2));
        }

        return (firstName, middleName, lastName);
    }

    public static string ToUserName(string firstName, string middleName, string lastName)
    {
        return ToUserName(firstName, middleName, lastName, false, true);
    }

    public static string ToUserName(string firstName, string middleName, string lastName, bool isVNformat = false, bool isNoSpace = true)
    {
        List<string> _ = [];

        if (firstName.IsPresent()) _.Add(firstName.Trim());
        if (middleName.IsPresent()) _.Add(middleName.Trim());
        if (lastName.IsPresent()) _.Add(lastName.Trim());

        if (isVNformat)
            _.Reverse();

        return string.Join(isNoSpace ? string.Empty : " ", _);
    }

    public static string GenerateRandomPassword(this int length)
    {
        if (length < 6)
            throw new ArgumentException("Password length must be at least 6 characters to meet the criteria.");

        // Ensure minimum requirements
        var password = new StringBuilder();

        // Add at least 2 special characters
        password.Append(SpecialChars[random.Next(SpecialChars.Length)]);
        password.Append(SpecialChars[random.Next(SpecialChars.Length)]);

        // Add at least 2 digits
        password.Append(Digits[random.Next(Digits.Length)]);
        password.Append(Digits[random.Next(Digits.Length)]);

        // Add at least 1 uppercase letter
        password.Append(UppercaseLetters[random.Next(UppercaseLetters.Length)]);

        // Add at least 1 lowercase letter
        password.Append(LowercaseLetters[random.Next(LowercaseLetters.Length)]);

        // Fill the rest with random characters from all possible sets
        string allChars = SpecialChars + Digits + UppercaseLetters + LowercaseLetters;
        for (int i = password.Length; i < length; i++)
        {
            password.Append(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the characters to make the password unpredictable
        return new string(password.ToString().OrderBy(_ => random.Next()).ToArray());
    }

    public static string GetStringAfterKey(this string input, string key)
    {
        if (input.IsMissing() || key.IsMissing())
            return string.Empty;

        int keyIndex = input.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (keyIndex == -1)
            return string.Empty;

        int valueStartIndex = keyIndex + key.Length;

        return input[valueStartIndex..];
    }

    public static int GetTimeZoneOffsetInMinutes(this string timeZoneId)
    {
        try
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            TimeSpan offset = timeZone.BaseUtcOffset;

            return -(int)offset.TotalMinutes;
        }
        catch (TimeZoneNotFoundException)
        {
            return 0;
        }
        catch (InvalidTimeZoneException)
        {
            return 0;
        }
    }

    #region Private

    private static string SlashTrim(this string source)
    {
        if (source.IsMissing())
            return string.Empty;

        source = source.EndsWith("/") ? source[..^1] : source;
        source = source.StartsWith("/") ? source[1..] : source;

        return source;
    }

    #region Special characters mapping
    private static readonly Dictionary<char, char> SpecialReplacements = new Dictionary<char, char>
    {
        // Vietnamese, Serbian, Croatian, etc.
        { 'Đ', 'D' },
        { 'đ', 'd' },

        // Icelandic, Old English, and others
        { 'Þ', 'T' }, // Thorn -> Th is common but using T for simplicity
        { 'þ', 't' },
        { 'Ð', 'D' }, // Eth -> D is common but it's phonetically closer to Th
        { 'ð', 'd' },

        // Polish, Sorbian, and others
        { 'Ł', 'L' },
        { 'ł', 'l' },

        // Danish, Norwegian, and others
        { 'Ø', 'O' }, // Often Oe but simplified to O
        { 'ø', 'o' },

        // Danish, Norwegian, Icelandic, and others
        { 'Æ', 'A' }, // Often Ae
        { 'æ', 'a' },

        // French and some other languages
        { 'Œ', 'O' }, // Often Oe
        { 'œ', 'o' },

        // German
        { 'ẞ', 'S' }, // Sharp S -> Ss
        { 'ß', 's' },

        // French, Portuguese, Catalan, Turkish, and others
        { 'Ç', 'C' },
        { 'ç', 'c' },

        // Additional examples
        // Spanish, Portuguese
        { 'Ñ', 'N' },
        { 'ñ', 'n' },

        // Turkish
        { 'Ş', 'S' },
        { 'ş', 's' },
        { 'Ğ', 'G' },
        { 'ğ', 'g' },

        // Azerbaijani, Turkish
        { 'Ə', 'E' },
        { 'ə', 'e' },
    };


    // Check for the presence of % followed by two hexadecimal digits
    [GeneratedRegex(@"%[0-9A-Fa-f]{2}")]
    private static partial Regex RegexIsUrlEncoded();
    #endregion

    #endregion
}

public static class Base64Extensions
{
    private const string Prefix = ";base64,";

    public static bool IsBase64FileString(this string input)
    {
        if (input.IsMissing())
            return false;

        if (!input.Contains(Prefix))
            return false;

        var content = GetBase64Content(input);

        string pattern = @"^(?:[A-Za-z0-9+\/]{4})*?(?:[A-Za-z0-9+\/]{2}(?:==)?|[A-Za-z0-9+\/]{3}=?)?$";

        if (content.IsMissing() || !new Regex(pattern).IsMatch(content))
            return false;

        return true;
    }


    public static Stream GetFileStream(this string base64String)
    {
        if (base64String.Contains(Prefix))
        {
            byte[] bytes = Convert.FromBase64String(GetBase64Content(base64String));
            return new MemoryStream(bytes);
        }
        return null;
    }

    public static string GetBase64Content(this string base64FileString)
    {
        if (base64FileString.Contains(Prefix))
        {
            return base64FileString[(base64FileString.IndexOf(Prefix) + Prefix.Length)..];
        }

        return null;
    }

    public static bool IsValidImageFromBase64(this string base64FileString, HashSet<string> extendAllowedMimeTypes = null)
    {
        if (!base64FileString.IsBase64FileString())
            return false;

        string mimeType = base64FileString.GetMimeTypeByBase64();

        var allowedImageMimeTypes = new HashSet<string>
        {
            "image/jpeg",
            "image/png"
        };

        if (extendAllowedMimeTypes != null)
        {
            foreach (var extendType in extendAllowedMimeTypes)
            {
                allowedImageMimeTypes.Add(extendType);
            }
        }

        return allowedImageMimeTypes.Contains(mimeType);
    }

    public static bool IsValidEmail(this string email)
    {
        if (email.IsMissing())
            return false;

        string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern);
    }

    public static bool IsValidVietnamesePhoneNumber(this string phoneNumber)
    {
        if (phoneNumber.IsMissing())
            return false;

        // Regular expression for Vietnamese phone numbers (starting with 03, 05, 07, 08, 09, and 10 digits in total)
        string phonePattern = @"^(03|05|07|08|09)\d{8}$";
        return Regex.IsMatch(phoneNumber, phonePattern);
    }

    public static string GetMimeTypeByBase64(this string base64FileString)
    {
        if (!base64FileString.IsBase64FileString())
            return null;

        // Find the start of the Base64 content, which starts after the first comma
        int startIndex = base64FileString.IndexOf(',');
        if (startIndex < 0 || !base64FileString.StartsWith("data:"))
            return null;

        // Extract the MIME type part, which is between 'data:' and the first semicolon
        return base64FileString[5..base64FileString.IndexOf(';', 5)];
    }

    public static string ToPadBase64(this string base64)
    {
        int paddingNeeded = base64.Length % 4;
        if (paddingNeeded > 0)
        {
            paddingNeeded = 4 - paddingNeeded;
        }
        return base64 + new string('=', paddingNeeded);
    }
}

public static partial class UrlExtensions
{
    private static readonly Regex UrlRegex = RegexDetectUrl();

    public static string UrlsToHtml(this string input)
    {
        if (input.IsMissing())
        {
            return input;
        }

        return UrlRegex.Replace(input, match => $"<a href='{match.Value}' target='_blank'>{match.Value}</a>");
    }

    [GeneratedRegex(@"(http|https|ftp|ftps)://[a-zA-Z0-9\-.]+(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*", RegexOptions.Compiled)]
    private static partial Regex RegexDetectUrl();
}