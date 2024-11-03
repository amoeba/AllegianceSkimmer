using System;
using System.ComponentModel.Design;
using System.Numerics;
using Decal.Adapter;
using ImGuiNET;
using UtilityBelt.Service;
using UtilityBelt.Service.Views;

namespace AllegianceSkimmer
{
    internal class ExampleUI : IDisposable
    {

        private readonly Hud hud;
        public string ExportPath = "scanresult.json";

        public ExampleUI()
        {
            // Create a new UBService Hud
            hud = UBService.Huds.CreateHud("AllegianceSkimmer");

            // set to show our icon in the UBService HudBar
            hud.ShowInBar = true;

            // Temporary
            hud.Visible = true;

            // subscribe to the hud render event so we can draw some controls
            hud.OnRender += Hud_OnRender;

        }

        /// <summary>
        /// Called every time the ui is redrawing.
        /// </summary>
        private void Hud_OnRender(object sender, EventArgs e)
        {
            try
            {
                //ImGui.ShowDemoWindow();

                if (ImGui.BeginTabBar("tabs"))
                {
                    if (ImGui.BeginTabItem("Scan"))
                    {
                        if (ImGui.Button("Start Scan"))
                        {
                            OnStartScanButtonPress();
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Stop Scan"))
                        {
                            OnStopScanButtonPress();
                        }

                        var progressBarSizeVec = new Vector2(-1, (int)ImGui.GetFontSize() * 2);
                        if (PluginCore.currentScan != null)
                        {
                            int n = PluginCore.currentScan.characters.FindAll(c => c.Resolved).Count;
                            int N = PluginCore.currentScan.characters.Count;
                            string text = $"{n}/{N} Items Processed. Fraction is {(float)n / N}";
                            ImGui.ProgressBar((float)n / N, progressBarSizeVec, text);
                        }
                        else
                        {
                            ImGui.ProgressBar(0, progressBarSizeVec, "Scan not started.");
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
            var textToShow = $"Start Scan Pressed";

            CoreManager.Current.Actions.AddChatText(textToShow, 1);
            UBService.Huds.Toaster.Add(textToShow, ToastType.Info);

            PluginCore.StartScan();
        }

        private void OnStopScanButtonPress()
        {
            var textToShow = $"Stop Scan Pressed";

            CoreManager.Current.Actions.AddChatText(textToShow, 1);
            UBService.Huds.Toaster.Add(textToShow, ToastType.Info);

            PluginCore.StopScan();
        }

        private void OnNextButtonPressed()
        {
            var textToShow = $"Next Scan Pressed";

            CoreManager.Current.Actions.AddChatText(textToShow, 1);
            UBService.Huds.Toaster.Add(textToShow, ToastType.Info);

            PluginCore.IterateScan();
        }

        private void OnExportSaveButtonClicked()
        {
            Utilities.Message("OnExportSaveButtonClicked");
            Export.DoExport(ExportPath);
        }
    }
}