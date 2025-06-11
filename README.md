# CyberLabTrainerv1
# CyberLabTrainer

**CyberLabTrainer** is an interactive desktop application built in **C# (.NET Framework 4.8)** using **WPF**. It simulates real-world cybersecurity training scenarios, focusing on defensive techniques like log analysis, process monitoring, and PowerShell investigation. Everything runs locally using real system components like PowerShell and the Windows Event Log.

---

## Features

### PowerShell Execution
- Run actual PowerShell commands inside the app.
- Output is streamed live from the system's PowerShell engine.
- Ideal for learning PowerShell syntax and testing defensive scripts.

### Event Log Viewer
- Loads real entries from the **Security** event log.
- Used for incident response practice and log analysis.
- Includes filtering and partial message display for clarity.

### Simulated Blue Team Challenges
- Includes custom-built modules to simulate threats like:
  - Malicious processes
  - Suspicious TCP connections
- Challenges are interactive, with a "Respond" button to score user input.
- Hints and real tools are provided (PowerShell, event viewer, etc.).

### Real-Time Scoring
- Each completed challenge awards a score.
- Encourages accurate analysis and reinforces cybersecurity best practices.

### Built-In Practice Modules
- PowerShell Runner  
- Event Log Viewer  
- Malware Process Detection Simulator  
- TCP Connection Monitor with fake command-and-control traffic  
- Scoring Engine with feedback  

---

## Getting Started

### Prerequisites
- Windows 10 or newer
- Visual Studio 2019 or 2022
- .NET Framework 4.8 SDK installed

### Setup Instructions
1. Clone the repository:
```bash
git clone https://github.com/your-username/CyberLabTrainer.git
