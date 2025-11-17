namespace Launcher.UI.WPF.Models;

/// <summary>
/// Модель элемента ComboBox с изображением и названием
/// </summary>
public class ComboBoxImageItem
{
    public string Name { get; set; }
    public string Image { get; set; }
    
    public int Id { get; set; }

    public ComboBoxImageItem(string name, string image)
    {
        Name = name;
        Image = image;
    }

    public override string ToString() => Name;
}

