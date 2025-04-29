namespace KLib.MSGraphHide.Data
{
    public class Error
    {
        public string code;
        public string message;
        public Error() { }
        public Error(string code, string message)
        {
            this.code = code;
            this.message = message;
        }
    }
}