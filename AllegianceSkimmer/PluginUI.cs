using System;
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
            hud.OnRender += Hud_OnRender;
        }

        private void Hud_OnRender(object sender, EventArgs e)
        {
            try
            {
                if (ImGui.BeginTabBar("tabs"))
                {
                    if (ImGui.BeginTabItem("Scan"))
                    {
                        if (ImGui.Button("Start Scan", new Vector2(-1, 64)))
                        {
                            OnStartScanButtonPress();
                        }

                        if (ImGui.Button("Stop Scan", new Vector2(-1, 32)))
                        {
                            OnStopScanButtonPress();
                        }

                        var progressBarSizeVec = new Vector2(-1, (int)ImGui.GetFontSize() * 2);
                        if (PluginCore.currentScan != null)
                        {
                            uint n = (uint)PluginCore.currentScan.characters.FindAll(c => c.Resolved).Count;
                            uint N = PluginCore.currentScan.ExpectedSize;
                            float progress = (float)n / (float)N;
                            string text = $"{n}/{N} characters processed.";

                            ImGui.ProgressBar(progress, progressBarSizeVec);
                            ImGui.Text(text);
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Result"))
                    {
                        if (ImGui.BeginTable("table1", 2, ImGuiTableFlags.ScrollY))
                        {
                            ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
                            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None);
                            ImGui.TableSetupColumn("Scanned", ImGuiTableColumnFlags.None);
                            ImGui.TableHeadersRow();

                            if (PluginCore.currentScan != null)
                            {
                                for (int i = 0; i < PluginCore.currentScan.characters.Count; i++)
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableSetColumnIndex(0);
                                    ImGui.Text(PluginCore.currentScan.characters[i].Name);
                                    ImGui.TableSetColumnIndex(1);
                                    if (PluginCore.currentScan.characters[i].Resolved)
                                    {
                                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.Vec4ToCol(new Vector4(0, (float)0.8, 0, (float)0.5)));
                                        ImGui.Text("Scanned");

                                    }
                                    else
                                    {
                                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.Vec4ToCol(new Vector4((float)0.8, (float)0.8, 0, (float)0.5)));
                                        ImGui.Text("Queued");
                                    }
                                }

                            }
                            ImGui.EndTable();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Tree"))
                    {
                        if (ImGui.TreeNode("Tree"))
                        {
                            ImGui.Text("Treeeee");
                            ImGui.TreePush("Treeeee");
                            ImGui.TreePush("Treeeeeeeee");
                            ImGui.TreePop();
                            ImGui.TreePop();
                            ImGui.TreePop();
                        }

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Export"))
                    {
                        ImGui.InputText("Path", ref ExportPath, 64);

                        if (ImGui.Button("Save"))
                        {
                            OnExportSaveButtonClicked();
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