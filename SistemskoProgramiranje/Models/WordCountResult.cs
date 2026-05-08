namespace SistemskoProgramiranje
{
    public class WordCountResult
    {
        public string FileName { get; set; } = "";

        public string FilePath { get; set; } = "";

        public int Count { get; set; }

        public bool FromCache { get; set; }

        public WordCountResult CloneAsCached()
        {
            return new WordCountResult
            {
                FileName = this.FileName,
                FilePath = this.FilePath,
                Count = this.Count,
                FromCache = true
            };
        }
    }
}