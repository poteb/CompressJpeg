using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompressJpeg
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        private string output;
        private string folder;
        private readonly ImageCodecInfo jpgEncoder;
        private readonly EncoderParameters myEncoderParameters;
        private readonly EncoderParameter myEncoderParameter;
        private int filesProcessed;
        private int foldersProcessed;
        private long fileSizeBefore;
        private long fileSizeAfter;

        public string Folder { get { return this.folder; } set { this.folder = value; OnPropertyChanged(); } }
        public string Output { get { return this.output; } set { this.output = value; OnPropertyChanged(); } }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            jpgEncoder = GetEncoder(ImageFormat.Jpeg);

            myEncoderParameters = new EncoderParameters(1);
            myEncoderParameter = new EncoderParameter(Encoder.Quality, 70L);
            myEncoderParameters.Param[0] = myEncoderParameter;
        }

        private async void bDoIt_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            filesProcessed = 0;
            foldersProcessed = 0;
            fileSizeBefore = 0;
            fileSizeAfter = 0;
            Output = string.Empty;
            AddOutput("Starting");
            if (!Directory.Exists(folder))
            {
                AddOutput("Folder does not exist");
                return;
            }
            await ProcessFolder(folder, cbSubfolders.IsChecked == true);
            AddOutput($"All done, {filesProcessed} files in {foldersProcessed} folders, size before {fileSizeBefore} and after {fileSizeAfter}");
        }

        private async Task ProcessFolder(string folder, bool recursive)
        {
            await Task.Run(() =>
            {
                AddOutput($"Folder {folder} start");
                var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(x => x.ToLower().EndsWith(".jpg") || x.ToLower().EndsWith(".jpeg"));
                foreach (var file in files)
                {
                    try
                    {
                        Bitmap bmp;
                        using (var bmpT = new Bitmap(file))
                        {
                            bmp = new Bitmap(bmpT);
                        }
                        fileSizeBefore += new FileInfo(file).Length;
                        var tempName = $"{file}_";
                        bmp.Save(tempName, jpgEncoder, myEncoderParameters);
                        bmp.Dispose();
                        File.Delete(file);
                        File.Move(tempName, file);
                        AddOutput($"{file} done");
                        filesProcessed++;
                        fileSizeAfter += new FileInfo(file).Length;
                    }
                    catch (Exception ex)
                    {
                        AddOutput(ex.Message);
                        return;
                    }
                }
                AddOutput($"Folder {folder} done");
                foldersProcessed++;
            });
            if (recursive)
            {
                var subs = Directory.GetDirectories(folder); foreach (var sub in subs)
                    await ProcessFolder(sub, true);
            }
        }
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
                if (codec.FormatID == format.Guid)
                    return codec;
            return null;
        }

        private void AddOutput(string text)
        {
            Output = $"{text}{Environment.NewLine}{output}";
        }
        private void TbClear_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Output = string.Empty;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion INotifyPropertyChanged
    }
}