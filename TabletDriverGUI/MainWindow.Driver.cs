﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TabletDriverGUI
{
    public partial class MainWindow : Window
    {

        //
        // Start the driver
        //
        void StartDriver()
        {

            if (running) return;

            // Try to start the driver
            try
            {
                running = true;

                // Console timer
                timerConsoleUpdate.Start();

                driver.Start(config.DriverPath, config.DriverArguments);
                if (!driver.IsRunning)
                {
                    SetStatus("Can't start the driver! Check the console!");
                    driver.ConsoleAddText("ERROR! Can't start the driver!");
                }
            }

            // Start failed
            catch (Exception e)
            {
                SetStatus("Can't start the driver! Check the console!");
                driver.ConsoleAddText("ERROR! Can't start the driver!\n  " + e.Message);
            }
        }


        //
        // Stop the driver
        //
        void StopDriver()
        {
            if (!running) return;
            running = false;
            driver.Stop();
            timerConsoleUpdate.Stop();
        }


        //
        // Send settings to the driver
        //
        private void SendSettingsToDriver()
        {
            if (!driver.IsRunning) return;

            settingCommands.Clear();

            // Desktop size
            settingCommands.Add("DesktopSize " + textDesktopWidth.Text + " " + textDesktopHeight.Text);

            // Screen area
            settingCommands.Add("ScreenArea " +
                Utils.GetNumberString(config.ScreenArea.Width) + " " + Utils.GetNumberString(config.ScreenArea.Height) + " " +
                Utils.GetNumberString(config.ScreenArea.X) + " " + Utils.GetNumberString(config.ScreenArea.Y)
            );


            //
            // Tablet area
            //
            // Inverted
            if (config.Invert)
            {
                settingCommands.Add("TabletArea " +
                    Utils.GetNumberString(config.TabletArea.Width) + " " +
                    Utils.GetNumberString(config.TabletArea.Height) + " " +
                    Utils.GetNumberString(config.TabletFullArea.Width - config.TabletArea.X) + " " +
                    Utils.GetNumberString(config.TabletFullArea.Height - config.TabletArea.Y)
                );
                settingCommands.Add("Rotate " + Utils.GetNumberString(config.TabletArea.Rotation + 180));
            }
            // Normal
            else
            {
                settingCommands.Add("TabletArea " +
                    Utils.GetNumberString(config.TabletArea.Width) + " " +
                    Utils.GetNumberString(config.TabletArea.Height) + " " +
                    Utils.GetNumberString(config.TabletArea.X) + " " +
                    Utils.GetNumberString(config.TabletArea.Y)
                );
                settingCommands.Add("Rotate " + Utils.GetNumberString(config.TabletArea.Rotation));
            }


            // Output Mode
            switch (config.OutputMode)
            {
                case Configuration.OutputModes.Absolute:
                    settingCommands.Add("OutputMode Absolute");
                    break;
                case Configuration.OutputModes.Relative:
                    settingCommands.Add("OutputMode Relative");
                    settingCommands.Add("RelativeSensitivity " +
                        Utils.GetNumberString(config.ScreenArea.Width / config.TabletArea.Width) +
                        " " +
                        Utils.GetNumberString(config.ScreenArea.Height / config.TabletArea.Height)
                    );
                    break;
                case Configuration.OutputModes.Digitizer:
                    settingCommands.Add("OutputMode Digitizer");
                    break;

                case Configuration.OutputModes.SendInput:
                    settingCommands.Add("OutputMode SendInputAbsolute");
                    break;

            }

            //
            // Pen button map
            //
            if (config.DisableButtons)
            {
                settingCommands.Add("ClearButtonMap");
            }
            else
            {
                settingCommands.Add("ClearButtonMap");
                int button = 1;
                foreach (string key in config.ButtonMap)
                {
                    settingCommands.Add("ButtonMap " + button + " \"" + key + "\"");
                    button++;
                }
            }

            //
            // Tablet button map
            //
            if (config.DisableTabletButtons)
            {
                settingCommands.Add("ClearAuxButtonMap");
            }
            else
            {
                settingCommands.Add("ClearAuxButtonMap");
                int button = 1;
                foreach (string key in config.TabletButtonMap)
                {
                    if (key != "")
                    {
                        settingCommands.Add("AuxButtonMap " + button + " \"" + key + "\"");
                    }
                    button++;
                }
            }


            //
            // Pressure
            //
            settingCommands.Add("PressureSensitivity " + Utils.GetNumberString(config.PressureSensitivity));
            settingCommands.Add("PressureDeadzone " + Utils.GetNumberString(config.PressureDeadzone));


            //
            // Scroll
            //
            settingCommands.Add("ScrollSensitivity " + Utils.GetNumberString(config.ScrollSensitivity));
            settingCommands.Add("ScrollAcceleration " + Utils.GetNumberString(config.ScrollAcceleration));
            settingCommands.Add("ScrollStopCursor " + (config.ScrollStopCursor ? "true" : "false"));


            // Smoothing filter
            if (config.SmoothingEnabled)
            {
                settingCommands.Add("FilterTimerInterval " + Utils.GetNumberString(config.SmoothingInterval));
                settingCommands.Add(
                    "Smoothing " + Utils.GetNumberString(config.SmoothingLatency) + " 90 " +
                    (config.SmoothingOnlyWhenButtons ? "true" : "false")
                );
            }
            else
            {
                settingCommands.Add("FilterTimerInterval 10");
                settingCommands.Add("Smoothing 0");
            }

            // Noise filter
            if (config.NoiseFilterEnabled)
            {
                settingCommands.Add("Noise " + Utils.GetNumberString(config.NoiseFilterBuffer) + " " + Utils.GetNumberString(config.NoiseFilterThreshold));
            }
            else
            {
                settingCommands.Add("Noise 0");
            }


            // Anti-smoothing filter
            if (config.AntiSmoothingEnabled)
            {
                settingCommands.Add("AntiSmoothing " + Utils.GetNumberString(config.AntiSmoothingShape, "0.000") + " " +
                    Utils.GetNumberString(config.AntiSmoothingCompensation) + " " +
                    (config.AntiSmoothingOnlyWhenHover ? "true" : "false"));
            }
            else
            {
                settingCommands.Add("AntiSmoothing 0");
            }


            // Debugging
            if (config.DebuggingEnabled)
            {
                settingCommands.Add("Debug true");
            }
            else
            {
                settingCommands.Add("Debug false");
            }


            // Commands after settings
            if (config.CustomCommands.Length > 0)
            {
                foreach (string command in config.CustomCommands)
                {
                    string tmp = command.Trim();
                    if (tmp.Length > 0)
                    {
                        settingCommands.Add(tmp);
                    }
                }
            }

            //
            // Send commands to the driver
            //
            foreach (string command in settingCommands)
            {
                // Skip comments
                if (command.StartsWith("#")) continue;

                driver.SendCommand(command);
            }

            //
            // Write settings to usersettings.cfg
            //
            try
            {
                File.WriteAllLines("config\\usersettings.cfg", settingCommands.ToArray());
            }
            catch (Exception)
            {
            }

        }


        //
        // Driver message received
        //
        private void OnDriverMessageReceived(object sender, TabletDriver.DriverEventArgs e)
        {
            //ConsoleAddText(e.Message);
        }


        //
        // Driver error received
        //
        private void OnDriverErrorReceived(object sender, TabletDriver.DriverEventArgs e)
        {
            SetStatusWarning(e.Message);
        }


        //
        // Driver status message received
        //
        private void OnDriverStatusReceived(object sender, TabletDriver.DriverEventArgs e)
        {
            string variableName = e.Message;
            string parameters = e.Parameters;
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProcessStatusMessage(variableName, parameters);
            });
        }
        // Process driver status message
        private void ProcessStatusMessage(string variableName, string parameters)
        {

            //
            // Tablet Name
            //
            if (variableName == "tablet")
            {
                string tabletName = parameters;
                string title = "TabletDriverGUI - " + tabletName;
                Title = title;

                // Limit notify icon text length
                if (title.Length > 63)
                {
                    notifyIcon.Text = tabletName.Substring(0, 63);
                }
                else
                {
                    notifyIcon.Text = tabletName;
                }
                SetStatus("Connected to " + tabletName);
            }

            //
            // Tablet width
            //
            if (variableName == "width")
            {
                if (Utils.ParseNumber(parameters, out double val))
                {
                    config.TabletFullArea.Width = val;
                    config.TabletFullArea.X = val / 2.0;
                    LoadSettingsFromConfiguration();
                    UpdateSettingsToConfiguration();
                    if (isFirstStart)
                        SendSettingsToDriver();
                }
            }

            //
            // Tablet height
            //
            if (variableName == "height")
            {
                if (Utils.ParseNumber(parameters, out double val))
                {
                    config.TabletFullArea.Height = val;
                    config.TabletFullArea.Y = val / 2.0;
                    LoadSettingsFromConfiguration();
                    UpdateSettingsToConfiguration();
                    if (isFirstStart)
                        SendSettingsToDriver();

                }
            }


            //
            // Tablet measurement to tablet area
            //
            if (variableName == "measurement" && isEnabledMeasurementToArea)
            {
                string[] stringValues = parameters.Split(' ');
                int valueCount = stringValues.Count();
                if (valueCount >= 4)
                {
                    double minimumX = 10000;
                    double minimumY = 10000;
                    double maximumX = -10000;
                    double maximumY = -10000;
                    for (int i = 0; i < valueCount; i += 2)
                    {
                        if (
                            Utils.ParseNumber(stringValues[i], out double x)
                            &&
                            Utils.ParseNumber(stringValues[i + 1], out double y)
                        )
                        {
                            // Find limits
                            if (x > maximumX) maximumX = x;
                            if (x < minimumX) minimumX = x;
                            if (y > maximumY) maximumY = y;
                            if (y < minimumY) minimumY = y;
                        }
                    }

                    double areaWidth = maximumX - minimumX;
                    double areaHeight = maximumY - minimumY;
                    double centerX = minimumX + areaWidth / 2.0;
                    double centerY = minimumY + areaHeight / 2.0;

                    config.TabletArea.Width = areaWidth;
                    config.TabletArea.Height = areaHeight;
                    config.TabletArea.X = centerX;
                    config.TabletArea.Y = centerY;
                    LoadSettingsFromConfiguration();
                    UpdateSettingsToConfiguration();


                }
                isEnabledMeasurementToArea = false;
                buttonDrawArea.IsEnabled = true;
                SetStatus("");
            }


            if (variableName == "aux_buttons")
            {
                if (Utils.ParseNumber(parameters, out double test))
                {
                    tabletButtonCount = (int)test;
                    if (tabletButtonCount > 0)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            GroupBox box = (GroupBox)wrapPanelTabletButtons.Children[i];
                            if (i >= tabletButtonCount)
                            {
                                box.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                box.Visibility = Visibility.Visible;
                            }
                        }
                        groupBoxTabletButtons.Visibility = Visibility.Visible;

                    }
                    if (isFirstStart)
                        SendSettingsToDriver();
                }

            }
        }


        //
        // Driver Started
        //
        private void OnDriverStarted(object sender, EventArgs e)
        {
            // Debugging commands
            if (config.DebuggingEnabled)
            {
                driver.SendCommand("HIDList");
            }

            driver.SendCommand("GetCommands");
            driver.SendCommand("Echo");
            driver.SendCommand("Echo   Driver version: " + Version);
            try { driver.SendCommand("echo   Windows version: " + Environment.OSVersion.VersionString); } catch (Exception) { }
            try
            {
                driver.SendCommand("Echo   Windows product: " +
                    Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "").ToString());
                driver.SendCommand("Echo   Windows release: " +
                    Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString());
            }
            catch (Exception)
            {
            }
            driver.SendCommand("Echo");
            driver.SendCommand("CheckTablet");
            SendSettingsToDriver();
            driver.SendCommand("Info");
            driver.SendCommand("Start");
            driver.SendCommand("Log Off");
            driver.SendCommand("LogDirect False");
            driver.SendCommand("Echo");
            driver.SendCommand("Echo Driver started!");
            driver.SendCommand("Echo");
        }


        //
        // Driver Stopped
        //
        private void OnDriverStopped(object sender, EventArgs e)
        {
            if (running)
            {

                // Automatic restart?
                if (config.AutomaticRestart)
                {
                    SetStatus("Driver stopped. Restarting! Check console !!!");
                    driver.ConsoleAddText("Driver stopped. Restarting!");

                    // Run in the main application thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        driver.Stop();
                        timerRestart.Start();
                    });

                }
                else
                {
                    SetStatus("Driver stopped!");
                    driver.ConsoleAddText("Driver stopped!");
                }

                // Run in the main application thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Title = "TabletDriverGUI";
                    notifyIcon.Text = "No tablet found";
                    groupBoxTabletButtons.Visibility = Visibility.Collapsed;
                });

            }
        }


        //
        // Driver restart timer tick
        //
        private void TimerRestart_Tick(object sender, EventArgs e)
        {
            if (running)
            {
                driver.Start(config.DriverPath, config.DriverArguments);
            }
            timerRestart.Stop();
        }


        //
        // Restart Driver button click
        //
        private void RestartDriverClick(object sender, RoutedEventArgs e)
        {
            if (running)
            {
                StopDriver();
            }
            StartDriver();
        }


    }
}
