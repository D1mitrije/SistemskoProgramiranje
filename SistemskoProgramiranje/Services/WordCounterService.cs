using System.Text.RegularExpressions;

namespace SistemskoProgramiranje
{
    public class WordCounterService
    {
        private readonly string rootFolder;

        private readonly ExpiringCache cache;

        private readonly SafeLogger logger;

        public WordCounterService(
            string rootFolder,
            ExpiringCache cache,
            SafeLogger logger)
        {
            this.rootFolder = rootFolder;
            this.cache = cache;
            this.logger = logger;
        }

        public string ProcessRequest(string rawUrl)
        {
            string fileName =
                ExtractFileName(rawUrl);

            WordCountResult result =
                cache.GetOrCreate(
                    fileName,
                    () => CountWordsInFile(fileName)
                );

            if (result.Count == 0)
            {
                return
                    $"Fajl: {result.FileName}\n" +
                    $"Nema reci sa vise suglasnika nego samoglasnika.\n" +
                    $"Rezultat iz kesa: {(result.FromCache ? "DA" : "NE")}";
            }

            return
                $"Fajl: {result.FileName}\n" +
                $"Broj reci: {result.Count}\n" +
                $"Putanja: {result.FilePath}\n" +
                $"Rezultat iz kesa: {(result.FromCache ? "DA" : "NE")}";
        }

        private string ExtractFileName(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl) ||
                rawUrl == "/")
            {
                throw new ArgumentException(
                    "Primer: http://localhost:5050/fajl.txt"
                );
            }

            string fileName =
                rawUrl.TrimStart('/');

            fileName =
                Uri.UnescapeDataString(fileName);

            if (fileName.Contains("..") ||
                fileName.Contains("/") ||
                fileName.Contains("\\"))
            {
                throw new ArgumentException(
                    "Naziv fajla nije validan."
                );
            }

            return fileName;
        }

        private WordCountResult CountWordsInFile(
            string fileName)
        {
            logger.Log($"Pretraga fajla: {fileName}");

            string? filePath =
                Directory.GetFiles(
                    rootFolder,
                    fileName,
                    SearchOption.AllDirectories
                ).FirstOrDefault();

            if (filePath == null)
            {
                throw new FileNotFoundException(
                    $"Fajl '{fileName}' nije pronadjen."
                );
            }

            string text =
                File.ReadAllText(filePath);

            MatchCollection words =
                Regex.Matches(text, @"\b[\p{L}]+\b");

            int counter = 0;

            foreach (Match match in words)
            {
                if (HasMoreConsonantsThanVowels(
                    match.Value))
                {
                    counter++;
                }
            }

            logger.Log($"Zavrseno brojanje za {fileName}");

            return new WordCountResult
            {
                FileName = fileName,
                FilePath = filePath,
                Count = counter,
                FromCache = false
            };
        }

        private bool HasMoreConsonantsThanVowels(
            string word)
        {
            int vowels = 0;
            int consonants = 0;

            foreach (char c in word.ToLower())
            {
                if (!char.IsLetter(c))
                {
                    continue;
                }

                if (IsVowel(c))
                {
                    vowels++;
                }
                else
                {
                    consonants++;
                }
            }

            return consonants > vowels;
        }

        private bool IsVowel(char c)
        {
            return c == 'a' ||
                   c == 'e' ||
                   c == 'i' ||
                   c == 'o' ||
                   c == 'u';
        }
    }
}