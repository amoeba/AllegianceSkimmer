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
        /// <summary>
        /// The UBService Hud
        /// </summary>
        private readonly Hud hud;

        /// <summary>
        /// The default value for TestText.
        /// </summary>
        public const string DefaultTestText = "Some Test Text";

        /// <summary>
        /// Some test text. This value is used to the text input in our UI.
        /// </summary>
        public string TestText = DefaultTestText.ToString();

        public ExampleUI()
        {
            // Create a new UBService Hud
            hud = UBService.Huds.CreateHud("AllegianceSkimmer");

            // set to show our icon in the UBService HudBar
            hud.ShowInBar = true;

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
                ImGui.ShowDemoWindow();

                ImGui.SeparatorText("Conrols");

                if (ImGui.Button("Start Scan"))
                {
                    OnStartScanButtonPress();
                }

                ImGui.SameLine();

                if (ImGui.Button("Stop Scan"))
                {
                    OnStopScanButtonPress();
                }

                ImGui.SeparatorText("Progress");

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

                ImGui.SeparatorText("Table");

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

                            } else
                            {
                                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.Vec4ToCol(new Vector4((float)0.8, (float)0.8, 0, (float)0.5)));
                                ImGui.Text("Queued");
                            }
                        }

                    }
                    ImGui.EndTable();
                }

                ImGui.SeparatorText("Tree");
                
                if (ImGui.TreeNode("Tree"))
                {
                    ImGui.Text("Treeeee");
                    ImGui.TreePush("Treeeee");
                    ImGui.TreePush("Treeeeeeeee");
                    ImGui.TreePop();
                    ImGui.TreePop();
                    ImGui.TreePop();
                }
            }
            catch (Exception ex)
            {
                PluginCore.Log(ex);
            }
        }

        /// <summary>
        /// Called when our print test text button is pressed
        /// </summary>
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


        /// <summary>
        /// Called when our print test text button is pressed
        /// </summary>
        private void OnNextButtonPressed()
        {
            var textToShow = $"Next Scan Pressed";

            CoreManager.Current.Actions.AddChatText(textToShow, 1);
            UBService.Huds.Toaster.Add(textToShow, ToastType.Info);

            PluginCore.IterateScan();
        }

        public void Dispose()
        {
            hud.Dispose();
        }
    }
}