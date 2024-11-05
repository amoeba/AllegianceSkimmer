using System;
using System.Diagnostics;
using System.Numerics;

using ImGuiNET;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;

namespace AllegianceSkimmer
{
    internal class PluginUI : IDisposable
    {

        private readonly Hud hud;
        public string ExportPath = "scanresult.json";

        public PluginUI()
        {
            hud = UBService.Huds.CreateHud("Allegiance Skimmer");
            hud.ShowInBar = true;
            // Temporary
            hud.Visible = true;
            hud.OnPreRender += Hud_OnPreRender;
            hud.OnRender += Hud_OnRender;
        }

        private void Hud_OnPreRender(object sender, EventArgs e)
        {
            ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.Always);
        }

        private void Hud_OnRender(object sender, EventArgs e)
        {
            try
            {
                if (ImGui.BeginTabBar("tabs"))
                {
                    if (ImGui.BeginTabItem("Scan"))
                    {

                        ImGui.TextWrapped("Start a scan by clicking the Start Scan button below.");

                        if (ImGui.Button("Start Scan"))
                        {
                            OnStartScanButtonPress();
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Stop Scan"))
                        {
                            OnStopScanButtonPress();
                        }

                        if (PluginCore.currentScan != null && PluginCore.currentScan.characters != null)
                        {
                            var progressBarSizeVec = new Vector2(-1, (int)ImGui.GetFontSize() * 2);

                            uint n = (uint)PluginCore.currentScan.characters.FindAll(c => c.Resolved).Count;
                            uint N = PluginCore.currentScan.ExpectedSize;
                            float progress = (float)n / (float)N;
                            string text = $"{n}/{N} characters processed.";

                            ImGui.ProgressBar(progress, progressBarSizeVec);

                            if (ImGui.BeginTable("table1", 2, ImGuiTableFlags.ScrollY, new Vector2(-1, (int)ImGui.GetFontSize() * 10)))
                            {
                                ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
                                ImGui.TableSetupColumn("Character", ImGuiTableColumnFlags.None);
                                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.None);
                                ImGui.TableHeadersRow();

                                if (PluginCore.currentScan != null)
                                {
                                    var charsInFlight = PluginCore.currentScan.characters.FindAll(x => !x.Resolved);
                                    for (int i = 0; i < charsInFlight.Count; i++)
                                    {
                                        ImGui.TableNextRow();
                                        ImGui.TableSetColumnIndex(0);
                                        ImGui.Text(charsInFlight[i].Name);

                                        ImGui.TableSetColumnIndex(1);
                                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.Vec4ToCol(new Vector4((float)0.8, (float)0.8, 0, (float)0.5)));
                                        ImGui.Text("Queued");
                                    }
                                }

                                ImGui.EndTable();
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Export"))
                    {
                        ImGui.TextWrapped("Export the latest scan as JSON.");
                        ImGui.InputText("Path", ref ExportPath, 64);

                        if (ImGui.Button("Save"))
                        {
                            OnExportSaveButtonClicked();
                        }

                        ImGui.TextWrapped($"The file will be saved to {Globals.PluginDirectory}.");

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Help"))
                    {
                        ImGui.TextWrapped($"If you run into issues, please file an issue at {PluginCore.Website}.");

                        if (ImGui.Button("Open in Browser"))
                        {
                            ProcessStartInfo args = new ProcessStartInfo
                            {
                                FileName = PluginCore.Website,
                                UseShellExecute = true
                            };

                            try
                            {
                                Process.Start(args);
                            }
                            catch (Exception ex)
                            {
                                Utilities.Message($"Failed to open browser. Please visit {PluginCore.Website} manually.");
                            }
                        }
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
            catch (Exception ex)
            {
                Logging.Log(ex);
            }
        }

        public void Dispose()
        {
            hud.Dispose();
        }

        private void OnStartScanButtonPress()
        {
            PluginCore.StartScan();
        }

        private void OnStopScanButtonPress()
        {
            PluginCore.StopScan();
        }

        private void OnNextButtonPressed()
        {
            PluginCore.IterateScan();
        }

        private void OnExportSaveButtonClicked()
        {
            Export.DoExport(ExportPath);
        }
    }
}