using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Mvc;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf.Annot;
using System.Text.Json;
using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Pdf.Xobject;
using iText.Kernel.Geom;


namespace PdfEdit.Controllers
{
    public class PdfController : Controller
    {
        private readonly string _uploadPath = "wwwroot/uploads";

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                Directory.CreateDirectory(_uploadPath);
                string filePath = System.IO.Path.Combine(_uploadPath, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return RedirectToAction("Edit", new { fileName = file.FileName });
            }
            return View("Index");
        }

        public IActionResult Edit(string fileName)
        {
            ViewBag.FileName = fileName;
            return View();
        }

        [HttpPost]
        public IActionResult SavePdf([FromBody] PdfSaveRequest request)
        {
            string inputFilePath = System.IO.Path.Combine(_uploadPath, request.FileName);
            string outputFilePath = System.IO.Path.Combine(_uploadPath, "modified_" + request.FileName);

            if (!System.IO.File.Exists(inputFilePath))
            {
                return NotFound("Original PDF file not found.");
            }

            try
            {
                using (PdfReader reader = new PdfReader(inputFilePath))
                using (PdfWriter writer = new PdfWriter(outputFilePath))
                using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                {
                    foreach (var annotation in request.Annotations)
                    {
                        if (annotation.PageNumber > 0 && annotation.PageNumber <= pdfDoc.GetNumberOfPages())
                        {
                            PdfPage page = pdfDoc.GetPage(annotation.PageNumber);
                            float pageHeight = page.GetPageSize().GetHeight(); // Get PDF page height

                            PdfCanvas canvas = new PdfCanvas(page);
                            

                            
                            if (annotation.IsIcon)
                            {
                                string iconPath = System.IO.Path.Combine(_uploadPath, annotation.IconFileName);
                                if (System.IO.File.Exists(iconPath))
                                {
                                    float iconWidth = 20;
                                    float iconHeight = 20;
                                    ImageData imageData = ImageDataFactory.Create(iconPath);
                                    PdfImageXObject imageXObject = new PdfImageXObject(imageData);
                                    Rectangle iconRectangle = new Rectangle(annotation.X, annotation.Y - iconHeight, iconWidth, iconHeight);

                                    // Draw image in the defined rectangle
                                    canvas.AddImageFittedIntoRectangle(imageData, iconRectangle, false);
                                    //canvas.AddXObjectAt(imageXObject, annotation.X, annotation.Y - iconHeight, iconWidth, iconHeight);
                                }
                            }
                            else if (annotation.IsText)
                            {
                                PdfFont font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA,
                                         PdfEncodings.WINANSI,
                                         PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED); 
                                float fontSize = 16;

                                // 🔹 Fix the Y-coordinate flipping by subtracting from pageHeight
                                float adjustedY = pageHeight - annotation.Y;

                                // ✅ Make sure text aligns correctly (subtract half the font size)
                                canvas.BeginText()
                                    .SetFontAndSize(font, fontSize)
                                    .MoveText(annotation.X, annotation.Y) // **FIXED Y FLIPPING**
                                    .ShowText(annotation.Text)
                                    .EndText()
                                    .Release();
                            }
                        }
                        else
                        {
                            return BadRequest($"Invalid page number: {annotation.PageNumber}");
                        }
                    }

                    pdfDoc.Close();
                }

                if (!System.IO.File.Exists(outputFilePath) || new FileInfo(outputFilePath).Length == 0)
                {
                    return BadRequest("Failed to generate a valid PDF.");
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(outputFilePath);
                return File(fileBytes, "application/pdf", "edited_" + request.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing the PDF: " + ex.Message);
            }
        }

        public class PdfSaveRequest
        {
            public string FileName { get; set; }
            public List<AnnotationRequest> Annotations { get; set; }
        }

        public class AnnotationRequest
        {
            public int PageNumber { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public string Text { get; set; }
            public bool IsText { get; set; } = false;
            public bool IsIcon { get; set; } = false;
            public string IconFileName { get; set; } // Path to the icon
        }
    }
}
