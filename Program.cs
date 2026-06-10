using UglyToad.PdfPig;
using System.Text.RegularExpressions;
using System.Text;






internal class Program{
    

    private static readonly string[] Books = Directory.GetFiles("Books");
    private static Random rnd = new();
    private static async Task Main(string[] args){
        Console.Clear();
        List<string> list =getBookText();
        List<int> selectedLines = new();
        for (int i = 0; i < 5; i++){
            int index = rnd.Next(0, list.Count);
            if (selectedLines.Contains(index)){i--; continue;}
            Console.Write($"{list[index]}");
            selectedLines.Add(index);
        }

        Console.WriteLine("");

    }







    private static string getBook(int index){
        return Path.GetFileName(Books[index]);

    }

    private static int getStartPage(string bookName){
        if (int.TryParse(Regex.Match(bookName, @"\{(\d+)\}").Groups[1].Value, out int number)){
            return number - 1;
        }
        return 99999;
    }

    private static List<string> getBookText(){
        string bookName = getBook(rnd.Next(0, Books.Length));
        
        using var document = PdfDocument.Open($"Books/{bookName}");
        List<string> pages = document.GetPages().Skip(getStartPage(bookName))
                        .Select(x => x.Text)
                        .Where(x => x.Length >= 50)
                        .ToList();

        pages = pages.Select(x => cleanText(x)).ToList();
        return buildString(pages).Split('.').Select(x => x + ".").ToList();
    }

    private static string buildString(List<string> list){
        return string.Concat(list);
    }

    private static string cleanText(string text){

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

        text = text.Replace("’", "");
        text = Regex.Replace(text, @"\s+", " ").Trim();


        return text;
    }

}



