using System.ComponentModel.DataAnnotations;

namespace Launcher.Core.Assets.Validators;

public static class SkinValidator
{
    public static ValidationResult? ValidateSkinFile(string? filePath, ValidationContext context)
    {
        if (string.IsNullOrEmpty(filePath))
            return new ValidationResult("Errors.SkinValidation.NoFileSelected");

        if (!File.Exists(filePath))
            return new ValidationResult("Errors.SkinValidation.FileNotFound");

        if (Path.GetExtension(filePath).ToLower() != ".png")
            return new ValidationResult("Errors.SkinValidation.NotPng");

        try
        {
            var dimensions = GetPngDimensions(filePath);
            
            bool isClassic = dimensions == (64, 32);
            bool isModern = dimensions == (64, 64);

            if (!isClassic && !isModern)
            {
                return new ValidationResult($"Errors.SkinValidation.WrongSize");
            }

            return ValidationResult.Success;
        }
        catch
        {
            return new ValidationResult("Errors.SkinValidation.InvalidPng");
        }
    }
    
    private static (int width, int height) GetPngDimensions(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);
        
        fs.Position = 16;
        
        var widthBytes = br.ReadBytes(4);
        Array.Reverse(widthBytes);
        int width = BitConverter.ToInt32(widthBytes, 0);

        var heightBytes = br.ReadBytes(4);
        Array.Reverse(heightBytes);
        int height = BitConverter.ToInt32(heightBytes, 0);

        return (width, height);
    }
}