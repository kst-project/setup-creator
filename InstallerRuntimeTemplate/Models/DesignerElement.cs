
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SetupStudio.Models
{
    public enum DesignerElementType
    {
        Heading,
        Text,
        Button,
        Image,
        Video,
        InstallPath,
        Divider
    }

    public class DesignerElement : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = "Element";
        private string _text = "Новый элемент";
        private DesignerElementType _type = DesignerElementType.Text;

        private double _x = 40;
        private double _y = 40;
        private double _width = 220;
        private double _height = 48;
        private double _fontSize = 16;
        private double _cornerRadius = 8;

        private string _background = "#FFFFFFFF";
        private string _foreground = "#FF111827";
        private string _borderColor = "#FFCBD5E1";
        private double _borderThickness = 0;
        private string _imagePath = "";
        private string _videoPath = "";
        private string _placeholder = "Папка установки";

        public string Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Text
        {
            get => _text;
            set => SetField(ref _text, value);
        }

        public DesignerElementType Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        public double X
        {
            get => _x;
            set => SetField(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetField(ref _y, value);
        }

        public double Width
        {
            get => _width;
            set => SetField(ref _width, Math.Max(1, value));
        }

        public double Height
        {
            get => _height;
            set => SetField(ref _height, Math.Max(1, value));
        }

        public double FontSize
        {
            get => _fontSize;
            set => SetField(ref _fontSize, Math.Max(1, value));
        }

        public double CornerRadius
        {
            get => _cornerRadius;
            set => SetField(ref _cornerRadius, Math.Max(0, value));
        }

        public string Background
        {
            get => _background;
            set => SetField(ref _background, value);
        }

        public string Foreground
        {
            get => _foreground;
            set => SetField(ref _foreground, value);
        }

        public string BorderColor
        {
            get => _borderColor;
            set => SetField(ref _borderColor, value);
        }

        public double BorderThickness
        {
            get => _borderThickness;
            set => SetField(ref _borderThickness, Math.Max(0, value));
        }

        public string ImagePath
        {
            get => _imagePath;
            set => SetField(ref _imagePath, value);
        }

        public string VideoPath
        {
            get => _videoPath;
            set => SetField(ref _videoPath, value);
        }

        public string Placeholder
        {
            get => _placeholder;
            set => SetField(ref _placeholder, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return;

            field = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
