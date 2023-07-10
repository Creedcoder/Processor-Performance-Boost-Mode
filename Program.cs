using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

public class MainForm : Form
{
    FlowLayoutPanel boostModePanel;
    Label selectLabel;
    Label feedbackLabel;
    string currentBoostMode;
    string oldBoostMode;

    // Add a field to store the configuration
    List<(string appName, string boostMode)> config = new List<(string appName, string boostMode)>();

    // Add a field to keep track of whether the configuration file should be used
    bool useConfigFile = false;

    public MainForm()
    {
        // Initialize Boost Mode Panel and Label
        boostModePanel = new FlowLayoutPanel
        {
            Location = new Point(20, 50),
            Size = new Size(570, 190),
            FlowDirection = FlowDirection.TopDown
        };

        selectLabel = new Label { Text = "Select Processor Performance Boost Mode", Location = new Point(20, 20), AutoSize = true};
        feedbackLabel = new Label { Text = "made by Creedcoder", Location = new Point(20, 240), Width = 190};

        // Create boost mode buttons
        CreateBoostModeButton("Disabled");
        CreateBoostModeButton("Enabled");
        CreateBoostModeButton("Aggressive (Default)");
        CreateBoostModeButton("Efficient Enabled");
        CreateBoostModeButton("Efficient Aggressive");
        CreateBoostModeButton("Aggressive At Guaranteed");
        CreateBoostModeButton("Efficient Aggressive At Guaranteed");

        // Add a button to Unlock Processor Performance Boost Mode
        Button unlockBoostModeButton = new Button
        {
            Text = "Unlock Processor Performance Boost Mode",
            Size = new Size(180, 40)
        };

        unlockBoostModeButton.Click += (sender, e) =>
        {
            RunCommand("-attributes sub_processor perfboostmode -attrib_hide");
        };

        boostModePanel.Controls.Add(unlockBoostModeButton);

        // Add a button to toggle the use of the configuration file
        Button toggleConfigButton = new Button
        {
            Text = "Auto Config File",
            Size = new Size(180, 40),
            BackColor = SystemColors.Control
    };

        toggleConfigButton.Click += (sender, e) =>
        {
            if (!useConfigFile)
            {
                // Load the configuration file and enable the use of the configuration file
                LoadConfigFile();
                useConfigFile = true;
                toggleConfigButton.BackColor = Color.LightCyan;
                oldBoostMode = currentBoostMode;
            } else
            {
                // Disable the use of the configuration file
                useConfigFile = false;
                toggleConfigButton.BackColor = SystemColors.Control;
            }
        };

        boostModePanel.Controls.Add(toggleConfigButton);

        // Add controls to the form
        Controls.Add(selectLabel);
        Controls.Add(boostModePanel);
        Controls.Add(feedbackLabel);

        // Set form properties
        Text = "Processor Performance Boost Mode";
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        StartPosition = FormStartPosition.CenterScreen;
        ControlBox = true; // Enable the control box (close button)
        MinimizeBox = false; // Disable the minimize button
        MaximizeBox = false; // Disable the maximize button

        //Opacity = 0.6;

        // Get the current boost mode
        currentBoostMode = GetCurrentBoostMode();
        UpdateButtonColors();

        // Create a timer
        Timer timer = new Timer();
        timer.Interval = 5000;  // Set the interval to 5 seconds (adjust as needed)
        timer.Tick += (sender, e) => CheckApplicationAndSetBoostMode();
        timer.Start();
    }

    // Method to create a boost mode button with the specified text
    void CreateBoostModeButton(string buttonText)
    {
        // Create a button for each boost mode
        Button boostModeButton = new Button
        {
            Text = buttonText,
            Size = new Size(180, 40)
        };

        boostModeButton.Click += (sender, e) => ApplyBoostMode(buttonText);

        boostModePanel.Controls.Add(boostModeButton);
    }

    // Method to load the configuration file and store the configuration in the config list
    void LoadConfigFile()
    {
        // Check if the configuration file exists before loading
        if (!File.Exists("config.xml"))
        {
            MessageBox.Show("Configuration file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        XmlDocument doc = new XmlDocument();
        doc.Load("config.xml");

        XmlNodeList applications = doc.DocumentElement.SelectNodes("/Config/Application");

        // Clear the existing configuration
        config.Clear();

        foreach (XmlNode application in applications)
        {
            string appName = application.SelectSingleNode("AppName").InnerText;
            string boostMode = application.SelectSingleNode("BoostMode").InnerText;

            // Add the application and boost mode to the configuration
            config.Add((appName, boostMode));
        }
    }

    void CheckApplicationAndSetBoostMode()
    {
        // Check if the configuration file should be used
        if (useConfigFile)
        {
            foreach (var (appName, boostMode) in config)
            {
                Process[] processes = Process.GetProcessesByName(appName);

                // The application is running
                if (processes.Length > 0)
                {
                    // Check if the boost mode is already applied
                    if (currentBoostMode != boostMode)
                    {
                        // The application is running, set the specified boost mode
                        ApplyBoostMode(boostMode);
                    }
                    return;  // Exit the method after applying a boost mode
                }
            }
        }

        // If the configuration file should not be used or no applications from the configuration file are running,
        // revert back to the old boost mode
        if (oldBoostMode != currentBoostMode)
        {
            ApplyBoostMode(oldBoostMode);
        }
    }

    void ApplyBoostMode(string boostMode)
    {
        string setting = null;
        if (boostMode == "Disabled")
        {
            setting = "0";
        }
        else if (boostMode == "Enabled")
        {
            setting = "1";
        }
        else if (boostMode == "Aggressive (Default)")
        {
            setting = "2";
        }
        else if (boostMode == "Efficient Enabled")
        {
            setting = "3";
        }
        else if (boostMode == "Efficient Aggressive")
        {
            setting = "4";
        }
        else if (boostMode == "Aggressive At Guaranteed")
        {
            setting = "5";
        }
        else if (boostMode == "Efficient Aggressive At Guaranteed")
        {
            setting = "6";
        }

        if (setting == null)
        {
            feedbackLabel.Text = "Please select a mode.";
            return;
        }

        // Run the command to apply the boost mode
        RunCommand("/setacvalueindex scheme_current sub_processor perfboostmode " + setting);
        RunCommand("/setactive scheme_current");

        // Update the current boost mode
        currentBoostMode = GetCurrentBoostMode();
        if (!useConfigFile)
            oldBoostMode = currentBoostMode;

        // Update the button colors
        UpdateButtonColors();
        //feedbackLabel.Text = "Done.";
    }

    void RunCommand(string arguments)
    {
       var processStartInfo = new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var process = Process.Start(processStartInfo);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string errorMessage = process.StandardError.ReadToEnd();
            feedbackLabel.Text = "Error: " + errorMessage;
        }
    }

    string GetCurrentBoostMode()
    {
        // Run the command to get the current boost mode
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = "/QUERY scheme_current sub_processor perfboostmode",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var process = Process.Start(processStartInfo);
        process.WaitForExit();

        // Parse the output to get the current boost mode
        if (process.ExitCode == 0)
        {
            string output = process.StandardOutput.ReadToEnd();
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                int valueStartIndex = line.IndexOf("0x");
                if (valueStartIndex != -1)
                {
                    string value = line.Substring(valueStartIndex);
                    return GetBoostModeText(value);
                }
            }
        }
        else
        {
            string errorMessage = process.StandardError.ReadToEnd();
            feedbackLabel.Text = "Error: " + errorMessage;
        }

        return "Unknown";
    }

    string GetBoostModeText(string value)
    {
        switch (value)
        {
            case "0x00000000":
                return "Disabled";
            case "0x00000001":
                return "Enabled";
            case "0x00000002":
                return "Aggressive (Default)";
            case "0x00000003":
                return "Efficient Enabled";
            case "0x00000004":
                return "Efficient Aggressive";
            case "0x00000005":
                return "Aggressive At Guaranteed";
            case "0x00000006":
                return "Efficient Aggressive At Guaranteed";
            default:
                return "Unknown";
        }
    }

    void UpdateButtonColors()
    {
        // Update the color of each button based on the current boost mode
        foreach (Control control in boostModePanel.Controls)
        {
            if (control is Button button)
            {
                if (button.Text == currentBoostMode)
                {
                    button.BackColor = Color.LightGreen;
                }
                else
                {
                    if (button.Text != "Auto Config File")
                        button.BackColor = SystemColors.Control;
                }
            }
        }
    }

    [STAThread]
    static void Main()
    {
        if (Environment.OSVersion.Version.Major >= 6)
        {
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(true);
        }
        Application.Run(new MainForm());
    }

    // Declare the SetProcessDPIAware method
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();
}
