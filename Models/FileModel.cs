namespace MACSAPI.Models
{
    public class FileModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; }  
        public string FullName { get; set; }  
        public string FileName { get; set; }
        public string FileSizeInKB { get; set; } 
        public int CountFile { get; set; }  
        public DateTime Date { get; set; }  
        public List<string> SavedFileList { get; set; }  

        public FileModel()
        {
            SavedFileList = new List<string>();
        }
    }

}
