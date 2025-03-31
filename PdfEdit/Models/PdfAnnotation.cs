namespace PdfEdit.Models
{
    public class PdfAnnotation
    {
        public int PageNumber { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string Text { get; set; }
    }
}
