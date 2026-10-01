using Microsoft.Win32;
using System.Text;
using System.Text.RegularExpressions;
using PM5Control.Core.Bwm;
using PM5Control.Core.Connections;
using PM5Control.Core.Discovery;
using PM5Control.Core.Protocols.Pm3;

namespace PM5Control.Desktop;

internal sealed class MainForm2 : Form
{
    private const string ClientVersion = "0.6.1";
    private const string BuildCommit = "pending CI";
    private static readonly Color Bg = Color.FromArgb(18,20,24);
    private static readonly Color LogBg = Color.FromArgb(10,12,15);
    private static readonly Color TextColor = Color.FromArgb(226,230,236);
    private static readonly Color Muted = Color.FromArgb(145,153,164);
    private static readonly Color Orange = Color.FromArgb(255,122,24);
    private static readonly Color Green = Color.FromArgb(94,214,130);
    private readonly Label _transport=ValueLabel(),_usb=ValueLabel(),_ble=ValueLabel(),_wifi=ValueLabel(),_device=ValueLabel(),_arm=ValueLabel(),_fpga=ValueLabel(),_bwm=ValueLabel(),_build=ValueLabel();
    private readonly TextBox _consoleLog=new(),_diagLog=new(),_cmdLine=new();
    private readonly ComboBox _command=new();
    private readonly Button _execute=new(),_cancel=new(),_analyze=new();
    private readonly ComboBox _wirelessTransport=new(), _bleDevice=new();
    private readonly TextBox _wifiHost=new(), _wifiPort=new();
    private readonly Label _wirelessStatus=ValueLabel();
    private readonly Button _wirelessConnect=new(), _wirelessDiag=new(), _otaButton=new(), _wifiDiscover=new();
    private readonly ComboBox _wifiDiscovered=new();
    private IPm3CommandTransport? _wirelessCommandTransport;
    private IAsyncDisposable? _wirelessDisposable;
    private readonly CheckBox _developer=new(),_raw=new(),_timestamps=new();
    private CancellationTokenSource? _cts;
    private string? _port; private bool _busy;

    public MainForm2(){ Text="Proxmark5 Control Center"; StartPosition=FormStartPosition.CenterScreen; MinimumSize=new Size(1000,700); Size=new Size(1180,780); BackColor=Bg; ForeColor=TextColor; Font=new Font("Segoe UI",10F); BuildUi(); RefreshPorts(); Log(_diagLog,$"PM5 Control Center v{ClientVersion} started. Read-only mode: write/reset/flash unavailable."); }

    private void BuildUi(){
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,BackColor=Bg}; root.RowStyles.Add(new RowStyle(SizeType.Absolute,76)); root.RowStyles.Add(new RowStyle(SizeType.Percent,100)); Controls.Add(root);
        var header=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,BackColor=Bg,Padding=new Padding(18,10,18,8)}; header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var title=new Label{Text="PROXMARK5  CONTROL CENTER",AutoSize=true,Font=new Font("Segoe UI Black",18F),ForeColor=Orange}; var sub=new Label{Text=$"Read-only diagnostic client · v{ClientVersion} · {BuildCommit}",AutoSize=true,ForeColor=Muted,Font=new Font("Consolas",9F),Location=new Point(0,38)}; var left=new Panel{Dock=DockStyle.Fill}; left.Controls.Add(title);left.Controls.Add(sub); var ro=new Label{Text="READ-ONLY",AutoSize=true,BackColor=Green,ForeColor=Color.Black,Padding=new Padding(10,5,10,5)}; header.Controls.Add(left);header.Controls.Add(ro);root.Controls.Add(header);
        var tabs=new TabControl{Dock=DockStyle.Fill,BackColor=Bg,ForeColor=TextColor,Padding=new Point(14,6)}; tabs.TabPages.Add(BuildDeviceTab());tabs.TabPages.Add(BuildConsoleTab());tabs.TabPages.Add(BuildWirelessTab());tabs.TabPages.Add(BuildDiagnosticsTab());tabs.TabPages.Add(BuildSettingsTab());root.Controls.Add(tabs);
    }
    private TabPage BuildDeviceTab(){ var p=Page("DEVICE"); var g=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=9,Padding=new Padding(20),BackColor=Bg};g.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,230));g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); AddRow(g,0,"Transport",_transport);AddRow(g,1,"USB / Serial",_usb);AddRow(g,2,"Bluetooth / BLE",_ble);AddRow(g,3,"Wi-Fi",_wifi);AddRow(g,4,"Device family",_device);AddRow(g,5,"ARM firmware",_arm);AddRow(g,6,"FPGA",_fpga);AddRow(g,7,"ESP32 / BWM",_bwm);AddRow(g,8,"Client build",_build);_build.Text=$"v{ClientVersion} · {BuildCommit}";p.Controls.Add(g);return p; }
    private TabPage BuildConsoleTab(){ var p=Page("CONSOLE"); var quick=new FlowLayoutPanel{Dock=DockStyle.Top,Height=52,Padding=new Padding(18,10,18,4),BackColor=Bg}; foreach(var x in new[]{("PING","hw ping"),("VERSION","hw version"),("CAPABILITIES","hw capabilities"),("STATUS","hw status")}){var b=new Button{Text=x.Item1,AutoSize=true};b.Click+=async(_,_)=>await ExecuteCommandAsync(x.Item2);quick.Controls.Add(b);}p.Controls.Add(quick);
        var cmd=new FlowLayoutPanel{Dock=DockStyle.Top,Height=48,Padding=new Padding(18,5,18,5),BackColor=Bg};_cmdLine.Width=430;_cmdLine.PlaceholderText="PM5> whitelisted command";_execute.Text="Execute";_cancel.Text="Cancel";_cancel.Enabled=false;_analyze.Text="Connect / analyze";_execute.Click+=async(_,_)=>await ExecuteCommandAsync(_cmdLine.Text.Trim());_cancel.Click+=(_,_)=>CancelCurrent();_analyze.Click+=async(_,_)=>await AnalyzeAsync();cmd.Controls.Add(_cmdLine);cmd.Controls.Add(_execute);cmd.Controls.Add(_cancel);cmd.Controls.Add(_analyze);p.Controls.Add(cmd);
        _command.DropDownStyle=ComboBoxStyle.DropDownList;_command.Width=250;_command.Items.AddRange(new object[]{"hw version","hw status","hw capabilities","hw ping"});_command.SelectedIndex=0;_command.SelectedIndexChanged+=(_,_)=>_cmdLine.Text=_command.Text;cmd.Controls.Add(_command); ConfigureLog(_consoleLog);p.Controls.Add(_consoleLog);return p; }
    private TabPage BuildWirelessTab()
    {
        var p=Page("BWM / WIRELESS");
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=8,Padding=new Padding(20),BackColor=Bg};
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,240));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        AddRow(root,0,"Transport",_wirelessStatus);
        _wirelessTransport.DropDownStyle=ComboBoxStyle.DropDownList;_wirelessTransport.Width=260;_wirelessTransport.Items.AddRange(new object[]{"Wi-Fi / TCP","Bluetooth LE"});_wirelessTransport.SelectedIndex=0;_wirelessTransport.SelectedIndexChanged+=(_,_)=>UpdateWirelessEditors();root.Controls.Add(_wirelessTransport,1,1);
        root.Controls.Add(new Label{Text="Wi-Fi host / IP",ForeColor=Muted,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,2);_wifiHost.Text="Proxmark5";_wifiHost.Width=300;root.Controls.Add(_wifiHost,1,2);
        root.Controls.Add(new Label{Text="TCP port",ForeColor=Muted,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,3);_wifiPort.Text="7777";_wifiPort.Width=100;root.Controls.Add(_wifiPort,1,3);
        root.Controls.Add(new Label{Text="BLE device",ForeColor=Muted,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,4);_bleDevice.DropDownStyle=ComboBoxStyle.DropDownList;_bleDevice.Width=420;root.Controls.Add(_bleDevice,1,4);
        root.Controls.Add(new Label{Text="Find PM5 on Wi-Fi",ForeColor=Muted,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,5);
        var discovery=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,BackColor=Bg};
        _wifiDiscover.Text="Discover (mDNS)";_wifiDiscover.AutoSize=true;_wifiDiscover.Click+=async(_,_)=>await DiscoverWifiAsync();
        _wifiDiscovered.DropDownStyle=ComboBoxStyle.DropDownList;_wifiDiscovered.Width=420;_wifiDiscovered.DisplayMember=nameof(Pm5MdnsService.InstanceName);
        _wifiDiscovered.SelectedIndexChanged+=(_,_)=>ApplyDiscoveredWifiEndpoint();
        discovery.Controls.Add(_wifiDiscover);discovery.Controls.Add(_wifiDiscovered);root.Controls.Add(discovery,1,5);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,BackColor=Bg};_wirelessConnect.Text="Connect";_wirelessConnect.AutoSize=true;_wirelessConnect.Click+=async(_,_)=>await ConnectWirelessAsync();var scan=new Button{Text="Scan BLE",AutoSize=true};scan.Click+=async(_,_)=>await ScanBleAsync();_wirelessDiag.Text="BWM version/status";_wirelessDiag.AutoSize=true;_wirelessDiag.Click+=async(_,_)=>await WirelessDiagnosticAsync();_otaButton.Text="BWM OTA…";_otaButton.AutoSize=true;_otaButton.Click+=async(_,_)=>await RunBwmOtaAsync();actions.Controls.Add(_wirelessConnect);actions.Controls.Add(scan);actions.Controls.Add(_wirelessDiag);actions.Controls.Add(_otaButton);root.Controls.Add(actions,1,6);
        var info=new Label{Dock=DockStyle.Fill,ForeColor=Muted,Text="Protocol support: PM3-NG over native BWM Wi-Fi/TCP and BWM BLE SPP. OTA accepts only ESP32-C2 images (magic 0xE9, chip 0x000C). Physical end-to-end verification is tracked separately.",Padding=new Padding(0,10,0,0)};root.Controls.Add(info,1,7);
        p.Controls.Add(root);UpdateWirelessEditors();SetWirelessButtonState(false);return p;
    }

    private void UpdateWirelessEditors(){var wifi=string.Equals(_wirelessTransport.Text,"Wi-Fi / TCP",StringComparison.Ordinal);_wifiHost.Enabled=wifi;_wifiPort.Enabled=wifi;_wifiDiscover.Enabled=wifi;_wifiDiscovered.Enabled=wifi;_bleDevice.Enabled=!wifi;_otaButton.Enabled=true;}

    private async Task DiscoverWifiAsync()
    {
        _wifiDiscover.Enabled=false;
        _wifiDiscovered.Items.Clear();
        _wirelessStatus.Text="Searching for PM5 BWM via mDNS…";
        try
        {
            var services=await Pm5MdnsDiscovery.DiscoverAsync(TimeSpan.FromSeconds(3));
            foreach(var service in services)_wifiDiscovered.Items.Add(service);
            if(services.Count>0)
            {
                _wirelessStatus.Text=$"mDNS found {services.Count} PM5 service(s)";
                _wifiDiscovered.SelectedIndex=0;
                Log(_consoleLog,$"mDNS found {services.Count} PM5 service(s). Select a result to fill host/port; connection is not automatic.");
                foreach(var service in services)
                {
                    var address=service.Addresses.FirstOrDefault()?.ToString()??service.HostName;
                    Log(_diagLog,$"mDNS: {service.InstanceName} -> {address}:{service.Port} ({service.HostName})");
                }
            }
            else
            {
                _wirelessStatus.Text="No mDNS result · manual IP/port still available";
                Log(_consoleLog,"No PM5 mDNS result. This does not prove the device is absent: mDNS may be disabled or multicast filtered. Enter the IP/hostname and TCP port manually.");
            }
        }
        catch(Exception ex)
        {
            _wirelessStatus.Text="mDNS discovery unavailable · manual IP/port still available";
            Log(_consoleLog,$"mDNS discovery failed: {ex.Message}. Manual IP/port remains available.");
        }
        finally
        {
            _wifiDiscover.Enabled=string.Equals(_wirelessTransport.Text,"Wi-Fi / TCP",StringComparison.Ordinal);
        }
    }

    private void ApplyDiscoveredWifiEndpoint()
    {
        if(_wifiDiscovered.SelectedItem is not Pm5MdnsService service)return;
        _wifiHost.Text=service.Addresses.FirstOrDefault()?.ToString()??service.HostName;
        _wifiPort.Text=service.Port.ToString();
        _wirelessStatus.Text=$"mDNS endpoint selected · {service.HostName}:{service.Port}";
        Log(_consoleLog,$"Selected mDNS endpoint {service.InstanceName}: {_wifiHost.Text}:{_wifiPort.Text}. Press Connect when ready.");
    }

    private async Task ScanBleAsync()
    {
        try
        {
            var devices=await WindowsBleProxmarkTransport.DiscoverAsync();_bleDevice.Items.Clear();foreach(var d in devices)_bleDevice.Items.Add(d);if(_bleDevice.Items.Count>0)_bleDevice.SelectedIndex=0;Log(_consoleLog,$"BLE scan: {devices.Count} PM5/BWM candidate(s) found.");foreach(var d in devices)Log(_diagLog,$"BLE candidate: {d}");
        }
        catch(Exception ex){Log(_consoleLog,$"BLE scan failed: {ex.Message}");}
    }

    private async Task ConnectWirelessAsync()
    {
        await DisconnectWirelessAsync();
        try
        {
            if(string.Equals(_wirelessTransport.Text,"Wi-Fi / TCP",StringComparison.Ordinal))
            {
                if(!int.TryParse(_wifiPort.Text,out var port))throw new InvalidOperationException("Invalid TCP port.");
                var t=new WifiTcpTransport(_wifiHost.Text.Trim(),port);await t.ConnectAsync();_wirelessCommandTransport=t;_wirelessDisposable=t;_wirelessStatus.Text=$"Wi-Fi/TCP ACTIVE · {_wifiHost.Text}:{port}";_wifiHost.BackColor=Color.Honeydew;Log(_consoleLog,$"Connected to PM5 BWM Wi-Fi/TCP at {_wifiHost.Text}:{port}.");
            }
            else
            {
                if(_bleDevice.SelectedItem is not BleDeviceCandidate d)throw new InvalidOperationException("Scan and select a PM5/BWM BLE device first.");
                var t=new WindowsBleProxmarkTransport(d.BluetoothAddress);await t.ConnectAsync();_wirelessCommandTransport=t;_wirelessDisposable=t;_wirelessStatus.Text=$"BLE ACTIVE · {d.Name}";Log(_consoleLog,$"Connected to PM5 BWM BLE SPP: {d}.");
            }
            SetWirelessButtonState(true);
        }
        catch(Exception ex){await DisconnectWirelessAsync();_wirelessStatus.Text="Wireless transport not connected";Log(_consoleLog,$"Wireless connect failed: {ex.Message}");}
    }

    private async Task WirelessDiagnosticAsync()
    {
        if(_wirelessCommandTransport is null){Log(_consoleLog,"Connect a BWM wireless transport first.");return;}
        try
        {
            var v=await _wirelessCommandTransport.SendCommandAsync(Pm3CommandCode.Pm5BwmEspOta,new[]{BwmEspFirmwareUpdater.ActionVersion});
            Log(_consoleLog,$"BWM ESP version: {(v.Response.Status==0?System.Text.Encoding.UTF8.GetString(v.Response.Payload).TrimEnd('\\0'):"ERROR status="+v.Response.Status)}");
            var status=await _wirelessCommandTransport.SendCommandAsync(Pm3CommandCode.Status);
            Log(_consoleLog,$"PM3 status: status={status.Response.Status}, reason={status.Response.Reason}, payload={status.Response.Payload.Length} bytes.");
        }
        catch(Exception ex){Log(_consoleLog,$"BWM diagnostic failed: {ex.Message}");}
    }

    private async Task RunBwmOtaAsync()
    {
        if(_wirelessCommandTransport is null){Log(_consoleLog,"Connect a BWM wireless transport first.");return;}
        using var dialog=new OpenFileDialog{Title="Select ESP32-C2 BWM firmware",Filter="ESP32 firmware (*.bin)|*.bin|All files (*.*)|*.*"};if(dialog.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            var image=await File.ReadAllBytesAsync(dialog.FileName);var info=BwmEspFirmwareUpdater.InspectImage(image);if(!info.IsValid)throw new InvalidDataException(info.Error);
            var confirm=MessageBox.Show(this,$"Flash BWM ESP32-C2 firmware?\\n\\n{Path.GetFileName(dialog.FileName)}\\n{image.Length:N0} bytes\\n\\nOnly the validated ESP32-C2 OTA protocol will be used.","BWM OTA",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning);if(confirm!=DialogResult.OK)return;
            _otaButton.Enabled=false;var updater=new BwmEspFirmwareUpdater(_wirelessCommandTransport);var progress=new Progress<BwmEspUpdateProgress>(x=>{_wirelessStatus.Text=$"BWM OTA {x.BytesSent:N0}/{x.TotalBytes:N0} ({x.Fraction:P0})";});await updater.UpdateAsync(image,progress);Log(_consoleLog,$"BWM OTA transfer completed for {Path.GetFileName(dialog.FileName)}. The BWM may briefly disconnect while rebooting.");try{var version=await updater.ReadVersionAsync();Log(_consoleLog,$"BWM post-OTA version: {version ?? "not confirmed"}");}catch(Exception verify){Log(_consoleLog,$"BWM post-OTA version not confirmed: {verify.Message}");}
        }
        catch(Exception ex){Log(_consoleLog,$"BWM OTA failed: {ex.Message}");}
        finally{_otaButton.Enabled=true;}
    }

    private async Task DisconnectWirelessAsync(){if(_wirelessDisposable is not null){try{await _wirelessDisposable.DisposeAsync();}catch{}}_wirelessDisposable=null;_wirelessCommandTransport=null;_wirelessStatus.Text="Wireless transport not connected";SetWirelessButtonState(false);}
    private void SetWirelessButtonState(bool connected){_wirelessDiag.Enabled=connected;_otaButton.Enabled=connected;}

    private TabPage BuildDiagnosticsTab(){var p=Page("DIAGNOSTICS");var info=new Label{Dock=DockStyle.Top,Height=42,Text="Developer/raw transport view. Interleaved frames, TX/RX, timeout and response matching are shown here.",Padding=new Padding(18,12,18,4),ForeColor=Muted};p.Controls.Add(info);ConfigureLog(_diagLog);p.Controls.Add(_diagLog);return p;}
    private TabPage BuildSettingsTab(){var p=Page("SETTINGS");var g=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=2,RowCount=6,Padding=new Padding(24),Height=240,BackColor=Bg};g.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,260));g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));g.Controls.Add(new Label{Text="COM port",ForeColor=Muted,Dock=DockStyle.Fill},0,0);g.Controls.Add(new Label{Text="Auto-detect (COM3 preferred)",ForeColor=TextColor,Dock=DockStyle.Fill},1,0);g.Controls.Add(new Label{Text="Baudrate",ForeColor=Muted,Dock=DockStyle.Fill},0,1);g.Controls.Add(new Label{Text="460800",ForeColor=TextColor,Dock=DockStyle.Fill},1,1);_developer.Text="Developer mode";_developer.Checked=true;_raw.Text="Show raw frames";_raw.Checked=true;_timestamps.Text="Show timestamps";_timestamps.Checked=true;g.Controls.Add(_developer,0,2);g.Controls.Add(_raw,0,3);g.Controls.Add(_timestamps,0,4);g.Controls.Add(new Label{Text="Safety: READ-ONLY · write/reset/flash disabled",ForeColor=Green,Dock=DockStyle.Fill},1,3);p.Controls.Add(g);return p;}
    private static TabPage Page(string t)=>new(t){BackColor=Bg,ForeColor=Color.White}; private static Label ValueLabel()=>new(){Dock=DockStyle.Fill,Text="UNKNOWN",TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Consolas",10F,FontStyle.Bold),ForeColor=Color.White}; private static void AddRow(TableLayoutPanel g,int r,string n,Label v){g.RowStyles.Add(new RowStyle(SizeType.Percent,100F/9F));g.Controls.Add(new Label{Text=n,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.FromArgb(145,153,164)},0,r);g.Controls.Add(v,1,r);} private static void ConfigureLog(TextBox b){b.Multiline=true;b.ReadOnly=true;b.ScrollBars=ScrollBars.Vertical;b.Dock=DockStyle.Fill;b.BackColor=LogBg;b.ForeColor=Green;b.Font=new Font("Consolas",9.5F);}
    private async Task AnalyzeAsync(){if(!TryGetPort())return;await RunAsync("Connect / analyze",async t=>{var i=await Pm3ReadOnlyInspector.InspectAsync(t);_transport.Text="USB / Serial ACTIVE";_usb.Text=$"Connected · {_port}";_device.Text=i.Identity.Hardware;_arm.Text=i.Identity.ArmFirmware;_fpga.Text=i.Identity.FpgaFirmware;_bwm.Text=$"PM3 capabilities v{i.Capabilities.SchemaVersion}; {(i.Capabilities.IsRdv4?"RDV4 flag set":"no RDV4 flag")}";_ble.Text="NOT ACTIVE · BLE telemetry not queried";_wifi.Text="NOT ACTIVE · Wi-Fi telemetry not queried";Log(_consoleLog,"Read-only handshake succeeded.");Log(_consoleLog,$"ARM: {i.Identity.ArmFirmware}");Log(_consoleLog,$"FPGA: {i.Identity.FpgaFirmware}");Log(_consoleLog,$"Hardware: {i.Identity.Hardware}");Log(_consoleLog,$"Capabilities schema v{i.Capabilities.SchemaVersion}; known={i.Capabilities.IsKnownSchema}; USB={(i.Capabilities.ViaUsb?"yes":"no")}; BigBuf={i.Capabilities.BigBufferSize} bytes; features={string.Join(", ",i.Capabilities.EnabledFeatures)}.");});}
    private async Task ExecuteCommandAsync(string command){if(string.IsNullOrWhiteSpace(command)){Log(_consoleLog,"Empty command rejected.");return;}command=command.Trim();var allowed=new[]{"hw ping","hw version","hw capabilities","hw status"};if(!allowed.Contains(command,StringComparer.OrdinalIgnoreCase)){Log(_consoleLog,$"Rejected: command is not on the read-only whitelist: {command}");return;}if(!TryGetPort())return;await RunAsync(command,async t=>{switch(command.ToLowerInvariant()){case "hw version":var id=await Pm3ReadOnlyInspector.QueryVersionAsync(t);_device.Text=id.Hardware;_arm.Text=id.ArmFirmware;_fpga.Text=id.FpgaFirmware;Log(_consoleLog,$"ARM: {id.ArmFirmware}");Log(_consoleLog,$"FPGA: {id.FpgaFirmware}");Log(_consoleLog,$"Hardware: {id.Hardware}");break;case "hw capabilities":var c=await Pm3ReadOnlyInspector.QueryCapabilitiesAsync(t);_transport.Text=$"USB / Serial ACTIVE · capabilities v{c.SchemaVersion}";_ble.Text="NOT ACTIVE · no BLE transport selected";_wifi.Text="NOT ACTIVE · no Wi-Fi transport selected";_bwm.Text=$"Capabilities v{c.SchemaVersion} · BigBuf {c.BigBufferSize} bytes";Log(_consoleLog,$"CMD_CAPABILITIES schema v{c.SchemaVersion}; known={c.IsKnownSchema}; USB={(c.ViaUsb?"yes":"no")}; FPC={(c.ViaFpc?"yes":"no")}; baud={c.BaudRate}; BigBuf={c.BigBufferSize} bytes.");Log(_consoleLog,$"Enabled: {string.Join(", ",c.EnabledFeatures)}");Log(_diagLog,$"CAPABILITIES RAW: {Convert.ToHexString(c.RawPayload)}");break;case "hw status":var s=await Pm3ReadOnlyInspector.QueryStatusAsync(t);Log(_consoleLog,$"CMD_STATUS: {(s.Success?"OK":"FAILED")} (status={s.Status}, reason={s.Reason}, payload={s.PayloadLength} bytes)");var lines=DecodeDebugText(s.DebugFrames);if(lines.Count>0){Log(_consoleLog,$"hw status text: {lines.Count} lines received");foreach(var line in lines)Log(_consoleLog,$"  {line}");}foreach(var f in s.DebugFrames)Log(_diagLog,$"Interleaved 0x{f.Command:X4}: status={f.Status}, reason={f.Reason}, payload={f.Payload.Length} bytes, raw={Convert.ToHexString(f.Payload)}");break;case "hw ping":var q=await Pm3ReadOnlyInspector.PingAsync(t);Log(_consoleLog,$"CMD_PING: {(q.Success?"OK":"FAILED")} (status={q.Status}, reason={q.Reason}, payload={q.PayloadLength} bytes)");break;}});}
    private static IReadOnlyList<string> DecodeDebugText(IEnumerable<Pm3NgResponse> frames){var result=new List<string>();foreach(var f in frames.Where(x=>x.Command==0x0100)){var data=f.Payload;var start=(data.Length>=2&&data[0]==0x01&&data[1]==0x00)?2:0;if(start>=data.Length)continue;var text=Encoding.UTF8.GetString(data,start,data.Length-start).Replace("\0",string.Empty);text=Regex.Replace(text,"\\x1B\\[[0-9;]*m",string.Empty);text=text.Trim();if(!string.IsNullOrWhiteSpace(text))result.Add(text);}return result;}
    private async Task RunAsync(string label,Func<Pm3SerialTransport,Task> action){if(_busy||string.IsNullOrWhiteSpace(_port))return;_busy=true;_cts=new CancellationTokenSource();_execute.Enabled=false;_cancel.Enabled=true;_analyze.Enabled=false;Log(_consoleLog,$"--- {label} ---");await using var t=new Pm3SerialTransport(_port);try{await t.ConnectAsync();await action(t);_transport.Text="USB/Serial transaction OK";}catch(OperationCanceledException){Log(_consoleLog,$"{label}: CANCELLED");}catch(Exception ex){_transport.Text="Transaction failed";Log(_consoleLog,$"{label} failed: {ex.Message}");}finally{_cts.Dispose();_cts=null;_busy=false;_execute.Enabled=true;_cancel.Enabled=false;_analyze.Enabled=true;}}
    private void CancelCurrent(){_cts?.Cancel();Log(_diagLog,"Cancel requested. Transport cancellation will be applied by the active operation if supported.");}
    private bool TryGetPort(){RefreshPorts();if(!string.IsNullOrWhiteSpace(_port))return true;Log(_consoleLog,"No Windows serial port detected.");return false;}
    private void RefreshPorts(){var ports=GetSerialPorts();_port=ports.FirstOrDefault(p=>p.Equals("COM3",StringComparison.OrdinalIgnoreCase))??ports.FirstOrDefault();_usb.Text=_port is null?"Not detected":$"Detected · {_port}";_transport.Text=_port is null?"No USB serial transport":"USB serial endpoint detected";_ble.Text="NOT ACTIVE · not queried";_wifi.Text="NOT ACTIVE · not queried";}
    private static IReadOnlyList<string> GetSerialPorts(){var r=new List<string>();using var k=Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM");if(k is null)return r;foreach(var n in k.GetValueNames())if(k.GetValue(n) is string p&&!string.IsNullOrWhiteSpace(p))r.Add(p);return r.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToArray();}
    private void Log(TextBox b,string m){b.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}{Environment.NewLine}");}
}
