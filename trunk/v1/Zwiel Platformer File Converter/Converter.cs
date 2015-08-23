using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;

namespace Zwiel_Platformer_File_Converter
{
    public partial class Converter : Form
    {
        private char[] m_illegalChars = "!@#$%^&*()_+=-~`[]\\{}|;':\",./<>?".ToCharArray();

        public Converter()
        {
            InitializeComponent();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.Filter = "Text Files (*.txt)|*.txt";
            dialog.Multiselect = false;
            dialog.ShowDialog();

            if (dialog.FileName != null)
            {
                textBoxToConvertPath.Text = dialog.FileName;
            }
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {
            if (textBoxWorldName.Text.Split(m_illegalChars).Length > 1)
            {
                MessageBox.Show("World names can only contain letters, numbers, or spaces.");
                return;
            }
            if (textBoxLevelName.Text.Split(m_illegalChars).Length > 1)
            {
                MessageBox.Show("Level names can only contain letters, numbers, or spaces.");
                return;
            }
            int convert;
            if (!int.TryParse(textBoxTimeAllotted.Text, out convert))
            {
                MessageBox.Show("The amount of time allotted to complete a level can only be an integer.");
                return;
            }
            if (!Directory.Exists(textBoxWorldName.Text))
                Directory.CreateDirectory(textBoxWorldName.Text);
            if (File.Exists(textBoxWorldName.Text + "/" + textBoxLevelName.Text + ".xml"))
                File.Delete(textBoxWorldName.Text + "/" + textBoxLevelName.Text + ".xml");
            if (ConvertText(textBoxToConvertPath.Text, textBoxWorldName.Text + "/" + textBoxLevelName.Text + ".xml", convert))
                MessageBox.Show("File successfully converted to '" + textBoxWorldName.Text + "/" + textBoxLevelName.Text + ".xml'!");
        }

        private bool ConvertText(string originPath, string destPath, int time)
        {
            if (!File.Exists(originPath))
            {
                MessageBox.Show("File '" + originPath + "' doesn't exist.");
                return false;
            }
            string[] lines = File.ReadAllLines(originPath);
            if (lines.Length < 15)
            {
                MessageBox.Show("The level must be at at least 15 tiles high");
                return false;
            }
            if (lines[0].Length < 20)
            {
                MessageBox.Show("The level must be at least 20 tiles wide");
                return false;
            }

            using (XmlWriter writer = XmlWriter.Create(destPath))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("level");
                writer.WriteAttributeString("time", time.ToString());
                writer.WriteAttributeString("width", lines[0].Length.ToString());
                writer.WriteAttributeString("height", lines[0].Length.ToString());
                writer.WriteAttributeString("name", textBoxLevelName.Text);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Length != lines[0].Length)
                    {
                        MessageBox.Show("The level must be of consistent length; line " + (i + 1) + "'s length varies from line 1's length");
                        return false;
                    }
                    for (int j = 0; j < lines[i].Length; j++)
                    {
                        string identify = lines[i][j].ToString().ToUpper();
                        #region Conversion
                        switch (identify)
                        {
                            case ".":
                                break;
                            case "X":
                                writer.WriteStartElement("Exit");
                                writer.WriteAttributeString("goTo", textBoxWorldName.Text + "/" + textBoxLevelName.Text);
                                writer.WriteAttributeString("location", GetLocation(j, i));
                                writer.WriteEndElement();
                                break;
                            case "1":
                                writer.WriteStartElement("playerStart");
                                writer.WriteAttributeString("chance", "1");
                                writer.WriteAttributeString("location", GetLocation(j, i));
                                writer.WriteEndElement();
                                break;
                            case "-":
                                SaveTileData(writer, "Platform", j, i);
                                break;
                            case "~":
                                SaveTileData(writer, "MossyGreenPlatform", j, i);
                                break;
                            case ":":
                                SaveTileData(writer, "MossyGreen", j, i);
                                break;
                            case "#":
                                SaveTileData(writer, "Orange", j, i);
                                break;
                            case "G":
                                writer.WriteStartElement("Gem");
                                writer.WriteAttributeString("location", GetLocation(j, i));
                                writer.WriteEndElement();
                                break;
                            case "A":
                                SaveEnemyData(writer, "Barbarian", j, i);
                                break;
                            case "B":
                                SaveEnemyData(writer, "Pygmy", j, i);
                                break;
                            case "C":
                                SaveEnemyData(writer, "Zombie", j, i);
                                break;
                            case "D":
                                SaveEnemyData(writer, "Skeleton", j, i);
                                break;
                            case "*":
                                SaveTrapData(writer, "Static", j, i);
                                break;
                            case "^":
                                SaveTrapData(writer, "Rising", j, i);
                                break;
                            case "V":
                                SaveTrapData(writer, "Falling", j, i);
                                break;
                            case "<":
                                SaveTrapData(writer, "Shooting-Left", j, i);
                                break;
                            case ">":
                                SaveTrapData(writer, "Shooting-Right", j, i);
                                break;
                            case "T":
                                writer.WriteStartElement("TimeBonus");
                                writer.WriteAttributeString("location", GetLocation(j, i));
                                writer.WriteEndElement();
                                break;
                            case "+":
                                SaveHealthPackData(writer, null, j, i);
                                break;
                            case "@":
                                SaveHealthPackData(writer, "poison", j, i);
                                break;
                            case "(":
                                SaveHealthPackData(writer, "weak", j, i);
                                break;
                            case ")":
                                SaveHealthPackData(writer, "strong", j, i);
                                break;
                            case "=":
                                SaveHealthPackData(writer, "ultimate", j, i);
                                break;
                            default:
                                MessageBox.Show("Character '" + identify + "' is not supported.");
                                return false;
                        }
                        #endregion
                    }
                }

                writer.WriteEndElement();

                writer.WriteEndDocument();
            }
            return true;
        }
        private static string GetLocation(int x, int y)
        {
            return x.ToString() + " - " + y.ToString();
        }
        private static void SaveTileData(XmlWriter writer, string tileType, int x, int y)
        {
            writer.WriteStartElement("tile");
            writer.WriteAttributeString("type", tileType);
            writer.WriteAttributeString("location", GetLocation(x, y));
            writer.WriteEndElement();
        }
        private static void SaveEnemyData(XmlWriter writer, string name, int x, int y)
        {
            writer.WriteStartElement("enemy");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("location", GetLocation(x, y));
            writer.WriteEndElement();
        }
        private static void SaveTrapData(XmlWriter writer, string type, int x, int y)
        {
            writer.WriteStartElement("Trap");
            writer.WriteAttributeString("location", GetLocation(x, y));
            writer.WriteAttributeString("type", type);
            if (type.Contains("-"))
                writer.WriteAttributeString("facing", type.Split('-')[1]);
            writer.WriteEndElement();
        }
        private static void SaveHealthPackData(XmlWriter writer, string type, int x, int y)
        {
            writer.WriteStartElement("HealthPack");
            writer.WriteAttributeString("location", GetLocation(x, y));
            if (type != null)
                writer.WriteAttributeString("type", type);
            writer.WriteEndElement();
        }
    }
}