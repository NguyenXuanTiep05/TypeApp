using UglyToad.PdfPig;
using System.Text.RegularExpressions;
using System.Text;






internal class Program{
    

    private static readonly string[] Books = Directory.GetFiles("Books");
    private readonly static Random rnd = new();

    //Main function
    //command for word wrap dotnet run | fold -s -w $(tput cols)
    private static async Task Main(string[] args){
        bool programRunning = true;
        Console.CursorVisible = false;
        List<string> list = GetBookText().OrderBy(x => x.Length).ToList();
        WriteText(list);

        while (programRunning){
            Console.WriteLine("");
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Q && key.Modifiers == ConsoleModifiers.Control){
                programRunning = false;
            }
            else if (key.Key == ConsoleKey.R && key.Modifiers == ConsoleModifiers.Control){
                WriteText(list);
            }
        }



        Console.Clear();


    }

    private static void WriteText(List<string> list){
        Console.Clear();
        List<int> selectedLines = new();
        int length = 0;
        StringBuilder builder = new();
        for (int i = 0; i < 5; i++){
            if (length >= 1000){break;}
            int index = rnd.Next(0, list.Count);
            string sentence = list[index];
            if (sentence.Length > 500 && sentence.Length >= length){ i--;continue;}
            if (selectedLines.Contains(index)){i--; continue;}
            builder.Append($"{" " + sentence}");
            selectedLines.Add(index);
            length += sentence.Length;
        }

        Console.WriteLine(CleanText(builder.ToString()));
    }



    private static string GetBookName(int index){
        return Path.GetFileName(Books[index]);

    }

    private static int GetStartPage(ref string bookName){
        if (int.TryParse(Regex.Match(bookName, @"\{(\d+)\}").Groups[1].Value, out int number)){
            bookName = Regex.Replace(bookName,@"\{(\d+)\}", "");
            return number - 1;
        }
        return 99999;
    }

    private static List<string> GetBookText(){
        string bookName = GetBookName(rnd.Next(0, Books.Length));
        
        using var document = PdfDocument.Open($"Books/{bookName}");
        List<string> pages = document.GetPages().Skip(GetStartPage(ref bookName))
                        .Select(x => x.Text)
                        .Where(x => x.Length >= 50)
                        .ToList();


        pages = pages.Select(x => CleanText(x) + " ").ToList();
        return Regex.Split(BuildString(pages), @"(?<=[.?])").Where(x => x.Length > 3)
            .ToList();
    }

    private static string BuildString(List<string> list){
        return string.Join(" ",list).Replace("‘", "");
    }

    private static string CleanText(string text){

        for (int i = text.Length - 1; i >= text.Length; i--){
            if (char.IsDigit(text[i])){
                text = text.Remove(i,1);
            }

        }
        if (text.Contains("PART") || text.Contains("Chapter"))
        {
            int delStringLen = 0;
            for (int i = 0; i < 30 || i <  text.Length; i++){
                if (char.IsDigit(text[i])){
                    delStringLen = i + 1;
                    break;
                }
            }

            text = text.Remove(0,delStringLen);
        }

        text = Regex.Replace(text , @"[""‟‟""„’”]", "");
        text = Regex.Replace(text, @"(?<![a-zA-Z]),(?![a-zA-Z])", ", ");
        text = text.Replace("—", "-");
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Substring(1);
        text = StringFixer.FixConcatenatedWords(text);
        return text;
    }



}


static class StringFixer
{
    private static readonly HashSet<string> WordList =
        File.ReadAllLines("words.txt")
            .Select(w => w.Trim().ToLower())
            .Where(w => w.Length > 0)
            .ToHashSet();
 
    // The only 1-2 letter strings allowed as split halves. The word list's
    // short entries are real words, but split halves need a stricter standard:
    // "aw" is a word, yet "aw|oman" is never the right reading of "awoman".
    private static readonly HashSet<string> CommonShortWords = new()
    {
        "a", "i", "an", "am", "as", "at", "be", "by", "do", "go", "he",
        "if", "in", "is", "it", "me", "my", "no", "of", "oh", "on",
        "or", "so", "to", "up", "us", "we"
    };
 
    // Missing-apostrophe repairs. Case-sensitive entries avoid clobbering
    // real words ("ill", "id"). Ambiguous forms are deliberately absent:
    // its, well, wont, hell, shed, lets, were - all legitimate words.
    private static readonly Dictionary<string, string> ContractionsExact = new()
    {
        ["Ill"] = "I'll", ["Im"] = "I'm", ["Id"] = "I'd", ["Ive"] = "I've",["Itll"] = "It'll"
    };
 
    private static readonly Dictionary<string, string> ContractionsAnyCase =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["thats"] = "that's", ["whats"] = "what's", ["theres"] = "there's",
        ["heres"] = "here's", ["dont"] = "don't", ["cant"] = "can't",
        ["didnt"] = "didn't", ["doesnt"] = "doesn't", ["isnt"] = "isn't",
        ["wasnt"] = "wasn't", ["arent"] = "aren't", ["werent"] = "weren't",
        ["havent"] = "haven't", ["hasnt"] = "hasn't", ["hadnt"] = "hadn't",
        ["wouldnt"] = "wouldn't", ["couldnt"] = "couldn't",
        ["shouldnt"] = "shouldn't", ["mustnt"] = "mustn't",
        ["youre"] = "you're", ["youve"] = "you've", ["youll"] = "you'll",
        ["theyre"] = "they're", ["theyve"] = "they've", ["weve"] = "we've",
        ["oclock"] = "o'clock",
    };
 
    private const string SentencePunctuation = ".,;:!?";
 
    public static string FixConcatenatedWords(string text)
    {
        string[] words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
            words[i] = TrySplitWord(words[i]);
        return string.Join(" ", words);
    }
 
    public static string TrySplitWord(string word)
    {
        // Strip leading/trailing punctuation, remember it.
        int start = 0;
        int end = word.Length;
        while (start < end && !char.IsLetter(word[start])) start++;
        while (end > start && !char.IsLetter(word[end - 1])) end--;
 
        string prefix = word.Substring(0, start);
        string core = word.Substring(start, end - start);
        string suffix = word.Substring(end);
 
        if (core.Length == 0)
            return word;
 
        // 1) Missing apostrophes: "Ill" -> "I'll", "thats" -> "that's".
        //    Must run BEFORE the valid-word check: "ill" is a real word,
        //    so the early return would otherwise hide it forever.
        if (ContractionsExact.TryGetValue(core, out string? fixedExact))
            return prefix + fixedExact + suffix;
        if (ContractionsAnyCase.TryGetValue(core, out string? fixedAny))
            return prefix + MatchFirstLetterCase(fixedAny, core) + suffix;
 
        // 2) Punctuation INSIDE the token ("however.The", "what-ever") never
        //    survives the concatenation loop, so it gets its own repair path.
        if (!core.All(char.IsLetter))
            return prefix + TryRepairInternalPunctuation(core) + suffix;
 
        // 3) Already a valid word, or too short to be a glued pair.
        string lower = core.ToLower();
        if (core.Length < 4 || WordList.Contains(lower))
            return word;
 
        // 4) Glued pair: collect EVERY valid split and keep the best-scoring
        //    one, instead of trusting whichever match the scan finds first.
        bool capitalized = char.IsUpper(core[0]);
        int bestScore = 0;
        int bestSplit = -1;
 
        for (int i = 1; i < lower.Length; i++)
        {
            string left = lower.Substring(0, i);
            string right = lower.Substring(i);
 
            if (!IsAcceptableHalf(left) || !IsAcceptableHalf(right))
                continue;
 
            // A capitalized unknown word is most likely a proper noun
            // ("Goldstein" -> gold+stein would be a false split). Only split
            // it when the result looks like a glued function word ("Awoman").
            if (capitalized &&
                !CommonShortWords.Contains(left) && !CommonShortWords.Contains(right))
                continue;
 
            int score = HalfScore(left) + HalfScore(right);
            if (score > bestScore)
            {
                bestScore = score;
                bestSplit = i;
            }
        }
 
        if (bestSplit > 0)
            return prefix + core.Substring(0, bestSplit) + " "
                          + core.Substring(bestSplit) + suffix;
 
        return word;
    }
 
    // Halves of 1-2 letters must come from the curated set; longer halves
    // just need to be known words.
    private static bool IsAcceptableHalf(string half) =>
        half.Length <= 2 ? CommonShortWords.Contains(half)
                         : WordList.Contains(half);
 
    // Longer halves are more trustworthy, and common short words get a boost
    // so "a|woman" outranks any rival reading. Tune freely.
    private static int HalfScore(string half) =>
        half.Length + (CommonShortWords.Contains(half) ? 6 : 0);
 
    private static string TryRepairInternalPunctuation(string core)
    {
        // Apostrophes or digits inside -> probably already correct ("won't").
        if (core.Any(c => !char.IsLetter(c) && c != '-'
                          && !SentencePunctuation.Contains(c)))
            return core;
 
        // Line-break hyphenation: "what-ever" -> "whatever", but only when
        // the joined form is a real word, so "high-heeled" survives intact.
        // Known trade-off: a DELIBERATE hyphen whose joined form is also a
        // word gets joined too ("super-states" -> "superstates"). The two
        // cases are indistinguishable at token level; delete this block if
        // protecting deliberate hyphens matters more than fixing line breaks.
        if (core.Contains('-'))
        {
            string joined = core.Replace("-", "");
            return WordList.Contains(joined.ToLower()) ? joined : core;
        }
 
        // Missing space after sentence punctuation: "however.The" ->
        // "however. The", but only when both sides are real words.
        int p = core.IndexOfAny(SentencePunctuation.ToCharArray());
        if (p > 0 && p < core.Length - 1)
        {
            string left = core.Substring(0, p);
            string right = core.Substring(p + 1);
            if (left.All(char.IsLetter) && right.All(char.IsLetter)
                && WordList.Contains(left.ToLower())
                && WordList.Contains(right.ToLower()))
                return left + core[p] + " " + right;
        }
 
        return core;
    }
 
    private static string MatchFirstLetterCase(string replacement, string original) =>
        char.IsUpper(original[0])
            ? char.ToUpper(replacement[0]) + replacement.Substring(1)
            : replacement;
}


