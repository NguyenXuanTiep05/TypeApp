using UglyToad.PdfPig;
using System.Text.RegularExpressions;
using System.Text;
using UglyToad.PdfPig.PdfFonts;
using System.Threading.Tasks.Dataflow;
using System.Data;
using VersOne.Epub;





internal class Program{
    

    private static readonly string[] Books = Directory.GetFiles("Books","*.pdf");
    private static readonly string[] BooksEpub = Directory.GetFiles("Books/Epub", "*.epub");
    private readonly static Random rnd = new();
    private static bool hasMistakes = true;
    private static int textLength = 70;
    private static string selectedBook = "";

    //Main function
    //command for word wrap dotnet run | fold -s -w $(tput cols)
    private static async Task Main(string[] args){
        bool programRunning = true;
        Console.CursorVisible = false;
        List<string> list = GetBookTextEpub();
        string text = FormatTypingText(list, false);
        //text = char.ToUpper(text[0]) + text.Substring(1);
        StringBuilder typedString = new();
        WriteHeader();
        WriteText(text);

        while (programRunning){
            Console.WriteLine("");
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Q && key.Modifiers == ConsoleModifiers.Control){
                programRunning = false;
            }
            else if (key.Key == ConsoleKey.R && key.Modifiers == ConsoleModifiers.Control){
                typedString.Clear();
                text = FormatTypingText(list, false);
                WriteText(text);
                continue;
            }
            else if (key.Key == ConsoleKey.Backspace && typedString.Length > 0)
            {
                if (key.Modifiers == ConsoleModifiers.Control){WordDelete(ref typedString, text);}
                else {CharDelete(ref typedString, text);}

            }

            else
            {
                if(!Char.IsControl(key.KeyChar) && typedString.Length != text.Length && key.Key != ConsoleKey.Backspace 
                            && (key.Modifiers == ConsoleModifiers.None || key.Modifiers == ConsoleModifiers.Shift))
                {typedString.Append(key.KeyChar);}
            }
            UpdateText(text, typedString.ToString());

            if (hasMistakes == false && text.Length == typedString.Length){
                WriteFinish();
                // Console.ReadKey();
                // typedString.Clear();
                // text = FormatTypingText(list);
            }
        }



        Console.Clear();


    }

    private static void WriteText(string text){
        Console.SetCursorPosition(0,2);
        Console.WriteLine(text +  new string(' ', textLength));
        Console.SetCursorPosition(0,3);
    }

    private static void UpdateText(string text, string typedText)
    {
        Console.SetCursorPosition(0,2);
        Console.SetCursorPosition(Math.Max(typedText.Length - 1, 0), 2);
        for (int i = 0; i < typedText.Length; i++)
        {
            hasMistakes = false;
            if (text[i] != typedText[i]){ hasMistakes = true;}
        }
        Console.BackgroundColor = typedText.Length > 0 ? 
                    (text[typedText.Length - 1] == typedText[typedText.Length - 1] ? ConsoleColor.Green : ConsoleColor.Red) : ConsoleColor.Black;
        Console.WriteLine(typedText.Length == 0 ?"" : text[typedText.Length - 1]);
        Console.SetCursorPosition(0,3);
        Console.ResetColor();
    }

    

    private static void WriteFinish()
    {
        string finnishedString = @"Finnished";
        Console.SetCursorPosition(0,2);
        Console.WriteLine(finnishedString + new string(' ', Console.WindowWidth - finnishedString.Length));
        Console.SetCursorPosition(0,3);
    }

    private static void WriteHeader()
    {
        Console.Clear();
        Console.WriteLine("Typing app \t\t To quit: CTRL + Q/C\t New text: CTRL + R\t");
        Console.WriteLine($"Selected book: {selectedBook}");
    }
    private static string FormatTypingText(List<string> list, bool cleanText = true)
    {
        if (list == null || list.Count == 0)
        {
            return "Error: No sentences available";  
        }
        List<int> selectedLines = new();
        int length = 0;
        StringBuilder builder = new();
        for (int i = 0; i < 5; i++){
            if (length >= textLength){break;}
            int index = rnd.Next(0, list.Count);
            string sentence = list[index];
            if ((sentence.Length + sentence.Length) > textLength && sentence.Length >= length){ i--;continue;}
            if (selectedLines.Contains(index)){i--; continue;}
            builder.Append($"{sentence} ");
            selectedLines.Add(index);
            length += sentence.Length;
        }
        if (!cleanText){return builder.ToString().Trim();}
        return CleanText(builder.ToString());
    }

    private static void WordDelete(ref StringBuilder typedText, string text)
    {
        if (text.Length > 0){
            Console.SetCursorPosition(typedText.Length - 1,2);
            Console.WriteLine(text[typedText.Length - 1]);
            Console.SetCursorPosition(0,3);
            typedText.Remove(typedText.Length - 1, 1);            
        }
        while (typedText.Length > 0 && text[typedText.Length - 1] != ' ' && text[typedText.Length - 1] != '_')
        {
            Console.SetCursorPosition(typedText.Length - 1,2);
            Console.WriteLine(text[typedText.Length - 1]);
            Console.SetCursorPosition(0,3);
            typedText.Remove(typedText.Length - 1, 1);

        }
    }

    private static void CharDelete(ref StringBuilder typedText, string text)
    {
            Console.SetCursorPosition(typedText.Length - 1,2);
            Console.WriteLine(text[typedText.Length - 1]);
            Console.SetCursorPosition(0,3);
            typedText.Remove(typedText.Length - 1, 1);
    }

    private static string GetBookName(int index, bool pdf = true){
        if (!pdf){return Path.GetFileName(BooksEpub[index]);}
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
        selectedBook = bookName;


        pages = pages.Select(x => CleanText(x) + " ").ToList();
        return Regex.Split(BuildString(pages), @"(?<=[.?!])").Where(x => x.Length > 30)
            .ToList();
    }


    private static List<string> GetBookTextEpub()
    {
        string bookName = GetBookName(rnd.Next(0, BooksEpub.Length), false);
        
        var book = EpubReader.ReadBook($"Books/Epub/{bookName}");
        var pages = book.ReadingOrder.Skip(GetStartPage(ref bookName))
                                .SelectMany(x => Regex.Matches(x.Content, $"<p[^>]*>(.*?)</p>", RegexOptions.Singleline)
                                .Select(x => x.Groups[1].Value)
                                .Select(x => Regex.Replace(x, $"<[^>]*>", ""))).ToList();
        selectedBook = bookName;
        string fullText = string.Join(" ",pages);
        fullText = fullText.Trim();
        fullText = Regex.Replace(fullText, @"'[^']*'", m => m.Value.Replace(".", "◆"));
        return Regex.Split(fullText, @"(?<=[.?!])\s+")
                                    .Select(x => x.Replace("\n", " ")) 
                                    .Select(x => Regex.Replace(x, @"\s+", " "))  
                                    .Select(x => x.Replace("◆", ".")) 
                                    .ToList();


    }

    private static string BuildString(List<string> list){
        return string.Join(" ",list).Replace("‘", "");
    }

    private static string CleanText(string text){

        for (int i = text.Length - 1; i >= text.Length - 6; i--){
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

        text = Regex.Replace(text, @"(?<![a-zA-Z])[""‟‟""„’”“]|[""‟‟""„’”“](?![a-zA-Z])", "");
        text = Regex.Replace(text, @"(?<![a-zA-Z]),(?![a-zA-Z])", ", ");
        text = Regex.Replace(text, @"[""‟‟""„’”“]", "'");
        text = Regex.Replace(text, @"\b\w*[^\x00-\x7F]\w*\b", "");
        text = text.Replace("—", "");
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Substring(1);
        text = StringFixer.FixConcatenatedWords(text).Trim();
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
        "or", "so", "to", "up", "us", "we", "the"
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


