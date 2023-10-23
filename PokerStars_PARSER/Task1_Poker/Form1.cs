using System;
using System.IO;
using System.Windows.Forms;
using System.IO.Compression;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace Task1_Poker
{
    public partial class Form1 : Form
    {
        #region settings load
        public Form1()
        {
            InitializeComponent();
            this.KeyDown += Form1_KeyDown;
            this.KeyPreview = true;
        }
       
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.W)
            {
                this.Close();
            }
        }
        #endregion

        #region unzip archive (EDIT PATH!!!)
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "ZIP Files|*.zip";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string zipFilePath = openFileDialog.FileName;
                string extractionPath = Path.Combine(Path.GetTempPath(), "PokerStarsHands");

                try
                {
                    ZipFile.ExtractToDirectory(zipFilePath, extractionPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошла ошибка при извлечении архива: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion

        #region JSON-Parser Data
        private void button2_Click(object sender, EventArgs e)
        {
            string extractionPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PokerStarsHands");

            try
            {
                string[] handHistoryFiles = Directory.GetFiles(extractionPath);
                Dictionary<string, double> playerTotalWinnings = new Dictionary<string, double>();
                int totalHands = 0;

                foreach (string filePath in handHistoryFiles)
                {
                    ProcessHandHistoryFile(filePath, playerTotalWinnings, ref totalHands);
                }

                foreach (var playerName in playerTotalWinnings.Keys.ToList())
                {
                    playerTotalWinnings[playerName] = Math.Round(playerTotalWinnings[playerName], 2);
                }

                var result = new
                {
                    TotalWinnings = Math.Round(playerTotalWinnings.Values.Sum(), 2),
                    TotalHands = totalHands,
                    PlayerWinnings = playerTotalWinnings
                };

                string jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);

                File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "summary_results.json"), jsonResult);

                MessageBox.Show("Данные успешно записаны в файл summary_results.json", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcessHandHistoryFile(string filePath, Dictionary<string, double> playerTotalWinnings, ref int totalHands)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                int currentBigBlindIndex = -1;
                double currentBigBlindWinnings = 0;
                int currentButtonIndex = -1;
                double currentButtonWinnings = 0;
                double totalPot = 0;

                foreach (string line in lines)
                {
                    if (line.Contains("Seat "))
                    {
                        totalHands++;
                    }

                    if (line.Contains("collected ($"))
                    {
                        var playerName = ExtractPlayerNameWithoutPosition(line);
                        var winnings = ExtractWinnings(line);
                        if (playerName != null)
                        {
                            if (playerTotalWinnings.ContainsKey(playerName))
                            {
                                playerTotalWinnings[playerName] += winnings;
                            }
                            else
                            {
                                playerTotalWinnings[playerName] = winnings;
                            }

                            if (currentBigBlindIndex != -1 && currentButtonIndex != -1)
                            {
                                playerTotalWinnings[playerName] += currentBigBlindWinnings;
                                playerTotalWinnings[playerName] += currentButtonWinnings;
                            }
                        }
                    }
                    else if (line.Contains("Total pot $"))
                    {
                        totalPot = ExtractTotalPot(line);
                    }
                    else if (line.Contains("big blind"))
                    {
                        currentBigBlindIndex = ExtractSeatIndex(line);
                        currentBigBlindWinnings = totalPot;
                    }
                    else if (line.Contains("button"))
                    {
                        currentButtonIndex = ExtractSeatIndex(line);
                        currentButtonWinnings = totalPot;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при обработке файла: " + filePath);
                Console.WriteLine(ex.Message);
            }
        }

        private string ExtractPlayerNameWithoutPosition(string line)
        {
            int startIndex = line.IndexOf(": ") + 2;
            int endIndex = line.IndexOf(" collected ($", startIndex);
            if (endIndex < 0)
            {
                endIndex = line.IndexOf("collected $", startIndex);
            }

            if (startIndex >= 0 && endIndex >= 0)
            {
                string fullPlayerName = line.Substring(startIndex, endIndex - startIndex);
                int indexOfSpace = fullPlayerName.LastIndexOf('(');
                if (indexOfSpace > 0)
                {
                    return fullPlayerName.Substring(0, indexOfSpace - 1);
                }
                return fullPlayerName;
            }

            return null;
        }

        private double ExtractWinnings(string line)
        {
            int startIndex = line.IndexOf("($") + 2;
            int endIndex = line.IndexOf(")", startIndex);

            if (startIndex >= 0 && endIndex >= 0)
            {
                string winningsStr = line.Substring(startIndex, endIndex - startIndex);
                Match match = Regex.Match(winningsStr, @"([0-9.]+)");
                if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double winnings))
                {
                    return winnings;
                }
            }

            return 0;
        }

        private int ExtractSeatIndex(string line)
        {
            Match match = Regex.Match(line, @"Seat (\d+):");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int index))
            {
                return index;
            }

            return -1;
        }

        private double ExtractTotalPot(string line)
        {
            Match match = Regex.Match(line, @"Total pot \$([0-9.]+)");
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double pot))
            {
                return pot;
            }

            return 0;
        }
        #endregion
    }
}
