namespace Lesson4
{
    internal class Program
    {

        public class Download
        {
            public Download(int id,string url)
            {
                Id = id;
                Url = url;
                FileName=Path.GetFileName(url);
                TotalBytes = -1;
                Status = "Waiting";
            }

            public int Id { get; set; }
            public string? Url { get; set; }
            public string? FileName { get; set; }
            public long TotalBytes { get; set; }
            public long DownloadedBytes { get; set; }
            public string? Status { get; set; }
        }

        static List<Download> downloads = new List<Download>();

        static Semaphore semaphore = new Semaphore(3, 3); 

        static CancellationTokenSource cts = new CancellationTokenSource();

        static HttpClient client = new HttpClient();

        static string message = "";

        static void DownloadFile(object obj)
        {
            Download d=(Download)obj;
            semaphore.WaitOne();
            try
            {
                if (cts.Token.IsCancellationRequested)
                {
                    d.Status = "Cancelled";
                    return;
                }

                d.Status = "Downloading";

                var request = new HttpRequestMessage(HttpMethod.Get, d.Url);
                var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                d.TotalBytes = response.Content.Headers.ContentLength ?? -1;

                using Stream stream = response.Content.ReadAsStream();
                byte[] buffer = new byte[8192];
                int read;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        d.Status = "Cancelled";
                        return;
                    }

                    d.DownloadedBytes += read;
                }
                d.Status = "Completed";
            }

            catch (Exception)
            {
                d.Status = "Error";
            }

            finally
            {
                semaphore.Release();
            }
        }

        static void Screen()
        {
            Console.Clear();
            Console.CursorVisible = false;
            while (true)
            {
                Console.SetCursorPosition(0, 0);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔══════════════════════════════════════╗");
                Console.WriteLine("║     MULTI-THREAD VIDEO DOWNLOADER    ║");
                Console.WriteLine("║        Semaphore: MAXIMUM 3          ║");
                Console.WriteLine("╚══════════════════════════════════════╝");
                Console.ResetColor();

                Console.WriteLine("  1-3  → Downloading");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  4+   → Waiting");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ESC  → Cancel ALL");
                Console.ResetColor();
                Console.WriteLine();

                int aktiv = 0, gozleyen = 0, tamamlanan = 0, xeta = 0;

                foreach (var d in downloads)
                {
                    int percent = 0;
                    if (d.TotalBytes > 0)
                        percent = (int)(d.DownloadedBytes * 100 / d.TotalBytes);

                    int filled = percent * 30 / 100;
                    string bar = new string('█', filled) + new string('░', 30 - filled);

                    double downMB = d.DownloadedBytes / 1024.0 / 1024.0;
                    double totalMB = d.TotalBytes / 1024.0 / 1024.0;

                    if (d.Status == "Downloading") { Console.ForegroundColor = ConsoleColor.Cyan; aktiv++; }
                    else if (d.Status == "Waiting") { Console.ForegroundColor = ConsoleColor.Yellow; gozleyen++; }
                    else if (d.Status == "Completed") { Console.ForegroundColor = ConsoleColor.Green; tamamlanan++; }
                    else { Console.ForegroundColor = ConsoleColor.Red; xeta++; }   

                    Console.Write($"#{d.Id}  {d.Status,-12}");
                    Console.ResetColor();
                    Console.Write($"[{bar}] {percent,3}%");

                    if (d.TotalBytes > 0)
                        Console.WriteLine($"  {downMB:F1} MB/{totalMB:F1} MB     ");
                    else
                        Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    if (d.TotalBytes > 0)
                        Console.WriteLine($"    {d.FileName}");
                    else
                        Console.WriteLine("    Unknown");
                    Console.ResetColor();
                    Console.WriteLine();

                }

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Aktiv: {aktiv} | Gözləyən: {gozleyen} | Tamamlanan: {tamamlanan} | Xəta: {xeta}");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(message + "                                        ");
                
                Console.ResetColor();

                Thread.Sleep(300);

            }
        }


        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            downloads.Add(new Download(1, "https://proof.ovh.net/files/10Mb.dat"));
            downloads.Add(new Download(2, "https://proof.ovh.net/files/100Mb.dat"));
            downloads.Add(new Download(3, "https://proof.ovh.net/files/100Mb.dat"));
            downloads.Add(new Download(4, "https://proof.ovh.net/files/10Mb.dat"));

            for (int i = 0; i < downloads.Count; i++)
            {
                Thread t = new Thread(DownloadFile);
                t.Start(downloads[i]);
            }

            Thread screen = new Thread(Screen);
            screen.Start();

            while (true)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Escape)
                {
                    cts.Cancel();
                    message = "Bütün yükləmələr dayandırıldı!";
                }

                if (key == ConsoleKey.Enter)
                {
                    int completed = 0;
                    foreach (Download d in downloads)
                    {
                        if (d.Status == "Completed")
                            completed++;
                    }

                    if (completed == 0)
                        message = "Heç nə yüklənə bilmədi.";
                    else
                        message = $"{completed} fayl yükləndi, {downloads.Count - completed} fayl yarımçıq qaldı.";
                }
            }

        }
    }
}
