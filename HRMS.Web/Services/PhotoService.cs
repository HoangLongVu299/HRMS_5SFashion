using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
namespace HRMS.Web.Services;
public static class PhotoService
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public static string? Mime(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) return "image/png";
        if (bytes.Length >= 3 && bytes[0]==255 && bytes[1]==216 && bytes[2]==255) return "image/jpeg";
        // Ảnh cũ trong database có thể là GIF/BMP. Chỉ phục vụ dữ liệu đã có, không nhận upload mới các loại này.
        if (bytes.Length >= 6 && (System.Text.Encoding.ASCII.GetString(bytes,0,6) is "GIF87a" or "GIF89a")) return "image/gif";
        if (bytes.Length >= 2 && bytes[0]==66 && bytes[1]==77) return "image/bmp";
        return null;
    }
    [SupportedOSPlatform("windows")]
    public static async Task<byte[]?> ReadAsync(IFormFile? file)
    {
        if (file == null) return null;
        if (file.Length == 0 || file.Length > MaxBytes) throw new BusinessException("Ảnh phải có dữ liệu và không vượt quá 2 MB.", "Photo");
        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png")) throw new BusinessException("Chỉ nhận ảnh PNG hoặc JPEG.", "Photo");
        await using var source = file.OpenReadStream();
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[8192]; int read;
        while ((read = await source.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > MaxBytes) throw new BusinessException("Ảnh vượt quá 2 MB.", "Photo");
            buffer.Write(chunk, 0, read);
        }
        var bytes = buffer.ToArray(); var mime = Mime(bytes);
        if ((ext == ".png" && mime != "image/png") || (ext != ".png" && mime != "image/jpeg"))
            throw new BusinessException("Nội dung ảnh không khớp định dạng PNG/JPEG.", "Photo");
        try
        {
            using var input = new MemoryStream(bytes);
            using var image = Image.FromStream(input, useEmbeddedColorManagement:false, validateImageData:true);
            if (image.Width > 6000 || image.Height > 6000 || (long)image.Width * image.Height > 16000000)
                throw new BusinessException("Ảnh tối đa 6000 pixel mỗi chiều và 16 triệu pixel.", "Photo");
            using var decoded = new Bitmap(image);
            using var output = new MemoryStream();
            decoded.Save(output, mime == "image/png" ? ImageFormat.Png : ImageFormat.Jpeg);
            if (output.Length > MaxBytes) throw new BusinessException("Ảnh sau xử lý vượt 2 MB; hãy chọn ảnh nhỏ hơn.", "Photo");
            return output.ToArray();
        }
        catch (BusinessException) { throw; }
        catch (Exception ex) when (ex is ArgumentException or System.Runtime.InteropServices.ExternalException or OutOfMemoryException)
        { throw new BusinessException("Ảnh hỏng hoặc không giải mã được. Vui lòng chọn ảnh khác.", "Photo"); }
    }
}
public class BusinessException(string message, string field = "") : Exception(message)
{
    public string Field { get; } = field;
}
