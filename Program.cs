using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

public class MainForm : Form
{
    FlowLayoutPanel boostModePanel;
    Label selectLabel;
    Label feedbackLabel;
    string currentBoostMode;

    public MainForm()
    {
        // Initialize Boost Mode Panel and Label
        boostModePanel = new FlowLayoutPanel
        {
            Location = new Point(20, 50),
            Size = new Size(390, 190),
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
        CreateBoostModeButton("Unlock Processor Performance Boost Mode");

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

        Opacity = 0.6;

        // Adjust font sizes based on DPI scaling
        //float scaleFactor = CreateGraphics().DpiX / 96f;
        //float fontSize = 9f * scaleFactor;
        //Font = new Font(Font.FontFamily, fontSize);
        //selectLabel.Font = new Font(Font.FontFamily, fontSize - 4);
        //boostModePanel.Font = new Font(Font.FontFamily, fontSize - 5);
        //feedbackLabel.Font = new Font(Font.FontFamily, fontSize - 4);

        // Get the current boost mode
        currentBoostMode = GetCurrentBoostMode();
        UpdateButtonColors();
    }

    void CreateBoostModeButton(string buttonText)
    {
        Button boostModeButton = new Button
        {
            Text = buttonText,
            Size = new Size(180, 40)
        };

        boostModeButton.Click += (sender, e) => ApplyBoostMode(buttonText);

        boostModePanel.Controls.Add(boostModeButton);
    }

    void ApplyBoostMode(string boostMode)
    {
        string selectedOption = boostMode;
        string setting = null;
        if (selectedOption == "Disabled")
        {
            setting = "0";
        }
        else if (selectedOption == "Enabled")
        {
            setting = "1";
        }
        else if (selectedOption == "Aggressive (Default)")
        {
            setting = "2";
        }
        else if (selectedOption == "Efficient Enabled")
        {
            setting = "3";
        }
        else if (selectedOption == "Efficient Aggressive")
        {
            setting = "4";
        }
        else if (selectedOption == "Aggressive At Guaranteed")
        {
            setting = "5";
        }
        else if (selectedOption == "Efficient Aggressive At Guaranteed")
        {
            setting = "6";
        }
        else if (selectedOption == "Show Processor Performance Boost Mode")
        {
            RunCommand("-attributes sub_processor perfboostmode -attrib_hide");
            return;
        }

        if (setting == null)
        {
            feedbackLabel.Text = "Please select a mode.";
            return;
        }

        RunCommand("/setacvalueindex scheme_current sub_processor perfboostmode " + setting);
        RunCommand("/setactive scheme_current");

        currentBoostMode = GetCurrentBoostMode();
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
