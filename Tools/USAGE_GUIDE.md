# Quick Usage Guide - Enhanced Network Discovery Tool

## 🚀 Getting Started (3 Simple Steps)

### Step 1: Run the Tool
Choose any of these options:
```cmd
# Option A: Double-click the batch file
run-discovery.bat

# Option B: Use PowerShell
.\run-discovery.ps1

# Option C: Manual execution
cd NetworkDiscovery
dotnet run
```

### Step 2: Wait for Scanning
The tool will automatically:
- ✅ Detect your network interfaces
- ✅ Scan for Tapo devices (3-5 seconds)
- ✅ Scan general network devices (10-30 seconds)

### Step 3: Review Results
You'll see two sections:
- **🔌 TAPO DEVICES**: Your TP-Link smart devices with detailed info
- **🌐 OTHER DEVICES**: Computers, routers, printers, etc.

## 🎯 What to Expect

### If You Have Tapo Devices:
```
🔌 TAPO DEVICES FOUND (2):
═══════════════════════════════
📍 IP Address: 192.168.1.105
   Device Type: SMART.TAPOPLUG
   Model: P110
   MAC Address: AA:BB:CC:DD:EE:FF
   Alias: Living Room Lamp

📍 IP Address: 192.168.1.108
   Device Type: SMART.IPCAMERA
   Model: C200
   Alias: Front Door Camera
```

### Always Shows Other Devices:
```
🌐 OTHER NETWORK DEVICES (3):
═══════════════════════════════════════
📍 IP Address: 192.168.1.1
   Hostname: router.home
   Device Type: Web Server
   Response Time: 2ms
```

## 🔧 Troubleshooting

### "No Tapo devices found"
1. **Check Network**: Ensure Tapo devices and computer are on same Wi-Fi
2. **Try Administrator**: Right-click → "Run as Administrator" 
3. **Check Firewall**: Temporarily disable Windows Firewall to test
4. **Power Cycle**: Restart your Tapo devices

### "Permission denied" or "Access denied"
1. **Run as Admin**: Right-click PowerShell → "Run as Administrator"
2. **Check Antivirus**: Some antivirus blocks network scanning
3. **Network Policy**: Corporate networks may block broadcast packets

### "No devices found at all"
1. **Verify Connection**: Check you're connected to Wi-Fi/Ethernet
2. **Check IP Range**: Tool scans 192.168.x.x and 10.x.x.x networks
3. **Router Settings**: Some routers have "device isolation" enabled

## 💡 Pro Tips

### For Best Results:
- **Connect to main network** (not guest network)
- **Disable VPN** while scanning
- **Close unnecessary apps** that use network
- **Wait patiently** - full scan takes 30-45 seconds

### Understanding Results:
- **Tapo Discovery Method "UDP Broadcast"** = Direct device communication
- **Discovery Method "mDNS"** = Found via network service announcement
- **MAC Address shown** = Device fully identified
- **"Unknown" fields** = Device didn't provide that information

### Network Security:
- ✅ **Safe to use** on your home network
- ⚠️ **Ask permission** before using on work/public networks
- 🔒 **Non-invasive** - only discovers, doesn't access or control

## 📋 Quick Reference

| What You Want | File to Run |
|---------------|-------------|
| **Simple scan** | `run-discovery.bat` |
| **PowerShell** | `.\run-discovery.ps1` |
| **Development** | `cd NetworkDiscovery && dotnet run` |

## 🆘 Still Having Issues?

1. **Check Requirements:**
   - Windows 10/11
   - .NET 11.0 installed
   - Network connection active

2. **Common Solutions:**
   - Restart router and devices
   - Reconnect to Wi-Fi
   - Run PowerShell as Administrator
   - Temporarily disable Windows Firewall

3. **Advanced Troubleshooting:**
   - Check Windows Event Logs
   - Test with other devices on same network
   - Verify Tapo app can see devices on phone

---

**📞 Need Help?** Check the detailed `README.md` files for comprehensive documentation and technical details.